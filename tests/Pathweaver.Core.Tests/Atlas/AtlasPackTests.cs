using Pathweaver.Core.Atlas;
using Pathweaver.Core.Levels;

namespace Pathweaver.Core.Tests.Atlas;

/// <summary>
/// The gate that keeps an unreachable atlas out of the game.
/// </summary>
/// <remarks>
/// The counterpart of the level solvability gate. A level that cannot be finished and a node that
/// cannot be afforded are the same kind of fault: content a player can see and never reach, with
/// nothing on screen to say whether the fault is theirs.
/// </remarks>
public class AtlasPackTests
{
    public static TheoryData<string> PackFiles()
    {
        var data = new TheoryData<string>();
        foreach (var path in Directory.GetFiles(RepositoryPath("atlas"), "*.pwatlas"))
        {
            data.Add(path);
        }

        return data;
    }

    [Fact]
    public void At_least_one_pack_ships()
    {
        // Guards the guard: without this, every other test here would pass by having nothing to check.
        Assert.NotEmpty(Directory.GetFiles(RepositoryPath("atlas"), "*.pwatlas"));
    }

    [Theory]
    [MemberData(nameof(PackFiles))]
    public void A_shipped_pack_loads(string path)
    {
        var map = AtlasLoader.Parse(File.ReadAllText(path));

        Assert.NotEmpty(map.Nodes);
    }

    [Fact]
    public void Every_shipped_node_can_be_reached_by_unlocking_in_some_order()
    {
        // Arrange
        var map = ShippedAtlas();
        var progress = AtlasProgress.Empty.WithEssence(int.MaxValue / 2);

        // Act — unlock whatever is available, repeatedly, until nothing more can be
        var unlocked = 0;
        var stalled = false;

        while (!stalled)
        {
            stalled = true;

            foreach (var node in map.Nodes)
            {
                if (map.CanUnlock(node.Id, progress))
                {
                    progress = progress.WithUnlocked(node.Id, node.Cost);
                    unlocked++;
                    stalled = false;
                }
            }
        }

        // Assert
        Assert.Equal(map.Nodes.Count, unlocked);
    }

    [Fact]
    public void The_whole_atlas_costs_less_than_clearing_the_campaign_once_pays()
    {
        // The relationship that makes the region reachable by playing rather than by grinding. Scores
        // are taken at each level's target, which is the least a clear can pay, so the real figure is
        // higher — and Endless Wayfare pays on top of it.
        // Arrange
        var totalCost = ShippedAtlas().Nodes.Sum(node => node.Cost);

        var essenceFromCampaign = Directory
            .GetFiles(RepositoryPath("levels"), "*.pwlevel")
            .Select(path => LevelLoader.Parse(File.ReadAllText(path)))
            .Sum(level => AtlasEssence.ForClear(level.TargetScore, level.BaseRouteScore));

        // Assert
        Assert.True(
            totalCost <= essenceFromCampaign,
            $"the atlas costs {totalCost} essence and clearing every level once pays {essenceFromCampaign}.");
    }

    [Fact]
    public void The_first_node_is_affordable_before_the_second_level()
    {
        // A progression map that opens after ten levels is a progression map nobody meets. The
        // cheapest node has to be within reach of the first clear.
        // Arrange
        var map = ShippedAtlas();
        var firstLevel = LevelLoader.Parse(File.ReadAllText(Path.Combine(RepositoryPath("levels"), "biome1-01.pwlevel")));

        // Act
        var afterFirstClear = AtlasProgress.Empty.WithEssence(
            AtlasEssence.ForClear(firstLevel.TargetScore, firstLevel.BaseRouteScore));

        // Assert — two clears of the opening level, at most
        var cheapest = map.Nodes.Min(node => node.Cost);
        Assert.True(
            cheapest <= afterFirstClear.Essence * 2,
            $"the cheapest node costs {cheapest} and the first level pays {afterFirstClear.Essence}.");
    }

    [Fact]
    public void Full_unlock_stays_within_a_sane_bonus()
    {
        // A gate on the balance rather than on the format. Levels grant three skips and are proved
        // solvable on that; the atlas should ease them, not answer them.
        // Arrange
        var map = ShippedAtlas();
        var everything = AtlasProgress.Of(map.Nodes.Select(node => node.Id), essence: 0);

        // Act
        var bonuses = map.BonusesFor(everything);

        // Assert
        Assert.InRange(bonuses.Skips, 1, 3);
        Assert.InRange(bonuses.Tokens, 1, 3);
        Assert.InRange(bonuses.EssencePerClear, 1, 3);
    }

    [Fact]
    public void A_later_region_docks_onto_an_earlier_one_rather_than_replacing_it()
    {
        // PRD section 4.2's requirement, as arithmetic: nothing already shipped changes when a pack
        // arrives. A pack that renamed or repriced a first-region node would pass every other test here.
        // Arrange
        var first = AtlasLoader.Parse(
            File.ReadAllText(Path.Combine(RepositoryPath("atlas"), "biome1.pwatlas")));
        var combined = ShippedAtlas();

        // Assert
        foreach (var node in first.Nodes)
        {
            Assert.True(combined.Contains(node.Id), $"{node.Id} vanished when the packs were combined.");
            Assert.Equal(node.Cost, combined.Node(node.Id).Cost);
            Assert.Equal(node.Effect, combined.Node(node.Id).Effect);
        }
    }

    [Fact]
    public void The_atlas_costs_less_walked_than_its_prices_add_up_to()
    {
        // Discount relics are the second region's whole reason for existing, so the region has to be
        // cheaper to walk than to read. Bought cheapest-first, which is what a player who reads the
        // numbers does.
        // Arrange
        var map = ShippedAtlas();
        var faceValue = map.Nodes.Sum(node => node.Cost);

        var progress = AtlasProgress.Empty;
        var paid = 0;

        // Act — buy everything, always taking whichever affordable-by-prerequisite node is cheapest now
        while (progress.UnlockedNodes.Count < map.Nodes.Count)
        {
            var next = map.Nodes
                .Where(node => !progress.IsUnlocked(node.Id))
                .Where(node => node.Requires.All(progress.IsUnlocked))
                .OrderBy(node => map.CostOf(node.Id, progress))
                .FirstOrDefault();

            Assert.NotNull(next);

            var price = map.CostOf(next!.Id, progress);
            paid += price;
            progress = progress.WithEssence(price).WithUnlocked(next.Id, price);
        }

        // Assert
        Assert.True(
            paid < faceValue,
            $"the atlas costs {paid} walked and {faceValue} read, so its discounts save nothing.");
    }

    [Fact]
    public void A_discount_relic_is_on_the_road_rather_than_a_side_bet()
    {
        // A discount only pays back across the nodes still to be bought, so one bought late loses
        // essence. That is survivable when the node is a prerequisite — the player was going to buy it
        // anyway and its price coming down is a gift — and a trap when it is optional.
        //
        // The exception is the outer edge, whose discount is an investment in a region that has not
        // shipped. That is stated on the node in the pack file, which is the most a level file can do.
        // Arrange
        var map = ShippedAtlas();
        var required = map.Nodes.SelectMany(node => node.Requires).ToHashSet();
        var outerEdge = map.Nodes.Where(node => !required.Contains(node.Id)).Select(n => n.Id).ToHashSet();

        // Assert
        foreach (var node in map.Nodes.Where(n => n.Effect.Kind == AtlasEffectKind.Discount))
        {
            Assert.True(
                required.Contains(node.Id) || outerEdge.Contains(node.Id),
                $"{node.Id} discounts nothing that needs it and nothing depends on it.");
        }

        Assert.True(
            outerEdge.Count <= 2,
            "more than two outer nodes makes 'the edge a later region docks onto' ambiguous.");
    }

    private static AtlasMap ShippedAtlas()
        => AtlasMap.Combine(
            Directory
                .GetFiles(RepositoryPath("atlas"), "*.pwatlas")
                .Select(path => AtlasLoader.Parse(File.ReadAllText(path)))
                .ToArray());

    /// <summary>
    /// Finds a directory in the repository by locating the solution file.
    /// </summary>
    /// <remarks>
    /// Anchored on <c>Pathweaver.slnx</c> rather than by searching for a directory name. macOS is
    /// case-insensitive, and a search for "levels" once matched this test project's own folder and
    /// quietly found nothing — a failure that would not have reproduced on the Linux CI runner.
    /// </remarks>
    private static string RepositoryPath(string folder)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Pathweaver.slnx")))
            {
                return Path.Combine(directory.FullName, folder);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"No Pathweaver.slnx found above {AppContext.BaseDirectory}.");
    }
}
