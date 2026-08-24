using Pathweaver.Core.Atlas;

namespace Pathweaver.Core.Tests.Atlas;

public class AtlasProgressTests
{
    [Fact]
    public void A_new_player_has_no_essence_and_no_nodes()
    {
        var progress = AtlasProgress.Empty;

        Assert.Equal(0, progress.Essence);
        Assert.Empty(progress.UnlockedNodes);
    }

    [Fact]
    public void Harvested_essence_adds_up()
    {
        // Act
        var progress = AtlasProgress.Empty.WithEssence(3).WithEssence(4);

        // Assert
        Assert.Equal(7, progress.Essence);
    }

    [Fact]
    public void Unlocking_a_node_spends_its_cost()
    {
        // Arrange
        var progress = AtlasProgress.Empty.WithEssence(5);

        // Act
        var after = progress.WithUnlocked("spring-well", cost: 2);

        // Assert
        Assert.Equal(3, after.Essence);
        Assert.Contains("spring-well", after.UnlockedNodes);

        // The value it came from is untouched, as everything in the simulation is.
        Assert.Equal(5, progress.Essence);
        Assert.Empty(progress.UnlockedNodes);
    }

    [Fact]
    public void Unlocking_the_same_node_twice_charges_once()
    {
        // Arrange
        var progress = AtlasProgress.Empty.WithEssence(9).WithUnlocked("spring-well", cost: 2);

        // Act
        var again = progress.WithUnlocked("spring-well", cost: 2);

        // Assert
        Assert.Equal(progress.Essence, again.Essence);
        Assert.Single(again.UnlockedNodes);
    }

    [Fact]
    public void Essence_never_goes_negative()
    {
        // Only a damaged file or a caller ignoring affordability can ask for this, and neither is
        // worth losing an atlas over.
        Assert.Equal(0, AtlasProgress.Empty.WithUnlocked("expensive", cost: 40).Essence);
    }

    [Fact]
    public void Progress_survives_a_round_trip()
    {
        // Arrange
        var progress = AtlasProgress.Empty
            .WithEssence(11)
            .WithUnlocked("spring-well", cost: 2)
            .WithUnlocked("deep-channel", cost: 3);

        // Act
        var reloaded = AtlasProgressFormat.Read(AtlasProgressFormat.Write(progress));

        // Assert
        Assert.Equal(progress.Essence, reloaded.Essence);
        Assert.Equal(progress.UnlockedNodes, reloaded.UnlockedNodes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not an atlas")]
    [InlineData("pathweaver-atlas 99\nessence 4")]
    public void Unreadable_text_yields_an_empty_atlas_rather_than_an_error(string text)
    {
        Assert.Equal(0, AtlasProgressFormat.Read(text).Essence);
    }

    [Fact]
    public void An_unrecognised_node_identifier_is_kept()
    {
        // A node renamed or removed between builds must not silently refund itself, and must not
        // cost the player the essence they spent on it either.
        var progress = AtlasProgressFormat.Read("pathweaver-atlas 1\nessence 2\nsome-old-node\n");

        Assert.Contains("some-old-node", progress.UnlockedNodes);
        Assert.Equal(2, progress.Essence);
    }
}

public class AtlasMapTests
{
    private const string Pack = """
        pack: biome1
        node: spring-well cost 2 at 0,0 gives skip 1
        node: deep-channel cost 3 at 1,0 gives token 1 needs spring-well
        node: ley-line cost 4 at 0,1 gives essence 1 needs spring-well
        node: wayfarer-mark cost 6 at 1,1 gives token 1 needs deep-channel,ley-line
        """;

    [Fact]
    public void A_pack_loads_its_nodes()
    {
        // Act
        var map = AtlasLoader.Parse(Pack);

        // Assert
        Assert.Equal(4, map.Nodes.Count);

        var well = map.Node("spring-well");
        Assert.Equal(2, well.Cost);
        Assert.Equal(AtlasEffectKind.Skip, well.Effect.Kind);
        Assert.Equal(1, well.Effect.Amount);
        Assert.Empty(well.Requires);
    }

    [Fact]
    public void A_node_records_what_it_needs()
    {
        var map = AtlasLoader.Parse(Pack);

        Assert.Equal(new[] { "deep-channel", "ley-line" }, map.Node("wayfarer-mark").Requires);
    }

    [Fact]
    public void Packs_combine_without_touching_each_other()
    {
        // The requirement that matters for the future: a biome pack docks on by adding a file. It
        // declares what it attaches to, because otherwise a prerequisite in another file and a typo
        // look identical and one of the two has to be rejected.
        // Arrange
        var second = """
            pack: biome2
            docks: deep-channel
            node: frost-vein cost 5 at 2,0 gives skip 1 needs deep-channel
            """;

        // Act
        var map = AtlasMap.Combine(AtlasLoader.Parse(Pack), AtlasLoader.Parse(second));

        // Assert
        Assert.Equal(5, map.Nodes.Count);
        Assert.Equal("biome2", map.Node("frost-vein").Pack);
        Assert.Equal("biome1", map.Node("spring-well").Pack);
    }

    [Fact]
    public void A_node_that_needs_something_missing_is_rejected()
    {
        // A pack referring to a node nobody ships would present an unreachable node, which looks
        // like a bug in the atlas rather than a mistake in the file.
        var text = """
            pack: broken
            node: orphan cost 2 at 0,0 gives skip 1 needs nothing-here
            """;

        Assert.Throws<AtlasFormatException>(() => AtlasLoader.Parse(text));
    }

    [Fact]
    public void A_duplicate_node_identifier_is_rejected()
    {
        var text = """
            pack: broken
            node: twin cost 2 at 0,0 gives skip 1
            node: twin cost 3 at 1,0 gives token 1
            """;

        Assert.Throws<AtlasFormatException>(() => AtlasLoader.Parse(text));
    }

    [Fact]
    public void A_cycle_is_rejected()
    {
        // Two nodes each needing the other can never be unlocked, which is worth catching in CI
        // rather than in a player's atlas.
        var text = """
            pack: broken
            node: a cost 2 at 0,0 gives skip 1 needs b
            node: b cost 2 at 1,0 gives skip 1 needs a
            """;

        Assert.Throws<AtlasFormatException>(() => AtlasLoader.Parse(text));
    }

    [Theory]
    [InlineData("node: bad cost 0 at 0,0 gives skip 1")]
    [InlineData("node: bad cost 2 at 0,0 gives skip 0")]
    [InlineData("node: bad cost 2 at 0,0 gives wobble 1")]
    [InlineData("node: bad cost two at 0,0 gives skip 1")]
    [InlineData("node: bad at 0,0 gives skip 1")]
    [InlineData("wobble: yes")]
    public void A_malformed_line_is_rejected_with_its_line_number(string line)
    {
        var error = Assert.Throws<AtlasFormatException>(() => AtlasLoader.Parse($"pack: broken\n{line}"));

        Assert.Equal(2, error.Line);
    }

    [Fact]
    public void A_root_node_is_available_from_the_start()
    {
        // Arrange
        var map = AtlasLoader.Parse(Pack);
        var progress = AtlasProgress.Empty.WithEssence(2);

        // Assert
        Assert.True(map.CanUnlock("spring-well", progress));
        Assert.False(map.CanUnlock("deep-channel", progress));
    }

    [Fact]
    public void A_node_needs_its_prerequisites_before_essence_matters()
    {
        // Arrange — plenty of essence, nothing unlocked
        var map = AtlasLoader.Parse(Pack);
        var rich = AtlasProgress.Empty.WithEssence(100);

        // Assert
        Assert.False(map.CanUnlock("wayfarer-mark", rich));
    }

    [Fact]
    public void A_node_already_unlocked_cannot_be_unlocked_again()
    {
        var map = AtlasLoader.Parse(Pack);
        var progress = AtlasProgress.Empty.WithEssence(5).WithUnlocked("spring-well", cost: 2);

        Assert.False(map.CanUnlock("spring-well", progress));
    }

    [Fact]
    public void Unlocked_nodes_add_up_into_bonuses()
    {
        // Arrange
        var map = AtlasLoader.Parse(Pack);
        var progress = AtlasProgress.Empty
            .WithEssence(20)
            .WithUnlocked("spring-well", cost: 2)
            .WithUnlocked("deep-channel", cost: 3)
            .WithUnlocked("ley-line", cost: 4);

        // Act
        var bonuses = map.BonusesFor(progress);

        // Assert — relics are additive, because a permanent upgrade that replaced a level's own
        // allowance would make a generous level worse than a mean one
        Assert.Equal(1, bonuses.Skips);
        Assert.Equal(1, bonuses.Tokens);
        Assert.Equal(1, bonuses.EssencePerClear);
    }

    [Fact]
    public void A_node_from_a_pack_that_is_not_installed_is_ignored_by_bonuses()
    {
        // A player who unlocked a node in a pack that is later absent keeps the record — see the
        // progress format — but must not keep the bonus, or the atlas would pay for something the
        // build no longer contains.
        var map = AtlasLoader.Parse(Pack);
        var progress = AtlasProgress.Empty.WithEssence(9).WithUnlocked("frost-vein", cost: 5);

        Assert.Equal(0, map.BonusesFor(progress).Skips);
    }
}

public class AtlasEssenceTests
{
    [Theory]
    [InlineData(246, 100, 2)]
    [InlineData(448, 100, 4)]
    [InlineData(99, 100, 0)]
    [InlineData(1000, 100, 10)]
    public void Essence_is_one_per_base_score_harvested(long score, long baseRouteScore, int expected)
    {
        // A rule a player can work out from the numbers they can already see, and integer division
        // rather than rounding so no device can disagree about the reward.
        Assert.Equal(expected, AtlasEssence.ForClear(score, baseRouteScore));
    }

    [Fact]
    public void A_bonus_is_added_after_the_harvest_is_counted()
    {
        // The essence relic is worth a flat extra per clear rather than a share of the score, so it
        // helps a struggling player rather than compounding for a strong one.
        Assert.Equal(4, AtlasEssence.ForClear(246, 100, essenceBonus: 2));
    }

    [Fact]
    public void A_base_score_of_zero_is_refused_rather_than_dividing()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AtlasEssence.ForClear(100, 0));
    }
}
