using Pathweaver.Core.Determinism;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Tests.Tiles;

public class TileBagTests
{
    private static readonly ConduitTile Straight =
        new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(0, 3));

    private static readonly ConduitTile Bend =
        new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(0, 2));

    private static readonly ConduitTile Tee =
        new ConduitTile(ResourceKind.Wind, EdgeMask.FromDirections(0, 2, 4));

    private static TileBag NewBag(params ConduitTile[] tiles)
        => TileBag.Create(tiles, SeedSource.Stream(1234UL, PathweaverStream.TileBag));

    [Fact]
    public void A_bag_needs_at_least_one_tile()
    {
        // Arrange
        var generator = SeedSource.Stream(1UL, PathweaverStream.TileBag);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => TileBag.Create(Array.Empty<ConduitTile>(), generator));
    }

    [Fact]
    public void A_new_bag_holds_one_of_each_tile()
    {
        // Arrange / Act
        var bag = NewBag(Straight, Bend, Tee);

        // Assert
        Assert.Equal(3, bag.Remaining);
    }

    [Fact]
    public void Drawing_returns_a_new_bag_and_leaves_the_original_untouched()
    {
        // Arrange
        var original = NewBag(Straight, Bend, Tee);

        // Act
        var (drawn, _) = original.Draw();

        // Assert
        Assert.Equal(3, original.Remaining);
        Assert.Equal(2, drawn.Remaining);
    }

    [Fact]
    public void Drawing_twice_from_the_same_bag_yields_the_same_tile()
    {
        // The bag is a value: holding one and drawing from it repeatedly must not
        // advance anything, which is what makes undo and replay safe.
        // Arrange
        var bag = NewBag(Straight, Bend, Tee);

        // Act
        var (_, first) = bag.Draw();
        var (_, second) = bag.Draw();

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void A_cycle_deals_every_tile_once_before_repeating_any()
    {
        // PRD section 3.2B treats deadlock frustration as a design failure. A
        // bag that could withhold a needed tile for an arbitrary run of draws
        // would manufacture exactly that, so each cycle is a permutation rather
        // than independent picks.
        // Arrange
        var bag = NewBag(Straight, Bend, Tee);
        var drawn = new List<ConduitTile>();

        // Act
        for (var i = 0; i < 3; i++)
        {
            ConduitTile tile;
            (bag, tile) = bag.Draw();
            drawn.Add(tile);
        }

        // Assert
        Assert.Equal(3, drawn.Distinct().Count());
        Assert.Contains(Straight, drawn);
        Assert.Contains(Bend, drawn);
        Assert.Contains(Tee, drawn);
    }

    [Fact]
    public void The_bag_refills_once_empty()
    {
        // Arrange
        var bag = NewBag(Straight, Bend);

        // Act — draw a full cycle, then one more
        (bag, _) = bag.Draw();
        (bag, _) = bag.Draw();
        Assert.Equal(0, bag.Remaining);

        var (afterRefill, _) = bag.Draw();

        // Assert — the supply is endless, so a drained bag reshuffles
        Assert.Equal(1, afterRefill.Remaining);
    }

    [Fact]
    public void Two_hundred_draws_stay_within_the_defined_tiles()
    {
        // Arrange
        var bag = NewBag(Straight, Bend, Tee);
        var allowed = new HashSet<ConduitTile> { Straight, Bend, Tee };

        // Act / Assert
        for (var i = 0; i < 200; i++)
        {
            ConduitTile tile;
            (bag, tile) = bag.Draw();
            Assert.Contains(tile, allowed);
        }
    }

    [Fact]
    public void Every_tile_appears_with_equal_frequency_across_whole_cycles()
    {
        // Arrange
        var bag = NewBag(Straight, Bend, Tee);
        var counts = new Dictionary<ConduitTile, int>();

        // Act — 30 draws is exactly ten cycles of three
        for (var i = 0; i < 30; i++)
        {
            ConduitTile tile;
            (bag, tile) = bag.Draw();
            counts[tile] = counts.TryGetValue(tile, out var count) ? count + 1 : 1;
        }

        // Assert
        Assert.Equal(3, counts.Count);
        Assert.All(counts.Values, count => Assert.Equal(10, count));
    }

    [Fact]
    public void The_same_seed_produces_the_same_draw_sequence()
    {
        // Arrange
        var seed = SeedSource.ForDate(2026, 8, 23);
        var first = TileBag.Create(
            new[] { Straight, Bend, Tee }, SeedSource.Stream(seed, PathweaverStream.TileBag));
        var second = TileBag.Create(
            new[] { Straight, Bend, Tee }, SeedSource.Stream(seed, PathweaverStream.TileBag));

        // Act / Assert — the Daily Expedition depends on this
        for (var i = 0; i < 50; i++)
        {
            ConduitTile a, b;
            (first, a) = first.Draw();
            (second, b) = second.Draw();
            Assert.Equal(a, b);
        }
    }

    [Fact]
    public void Different_seeds_produce_different_draw_sequences()
    {
        // Arrange
        var monday = TileBag.Create(
            new[] { Straight, Bend, Tee },
            SeedSource.Stream(SeedSource.ForDate(2026, 8, 24), PathweaverStream.TileBag));
        var tuesday = TileBag.Create(
            new[] { Straight, Bend, Tee },
            SeedSource.Stream(SeedSource.ForDate(2026, 8, 25), PathweaverStream.TileBag));

        // Act
        var mondayDraws = new List<ConduitTile>();
        var tuesdayDraws = new List<ConduitTile>();
        for (var i = 0; i < 20; i++)
        {
            ConduitTile a, b;
            (monday, a) = monday.Draw();
            (tuesday, b) = tuesday.Draw();
            mondayDraws.Add(a);
            tuesdayDraws.Add(b);
        }

        // Assert
        Assert.NotEqual(mondayDraws, tuesdayDraws);
    }

    [Fact]
    public void A_single_tile_bag_always_deals_that_tile()
    {
        // Arrange
        var bag = NewBag(Straight);

        // Act / Assert
        for (var i = 0; i < 10; i++)
        {
            ConduitTile tile;
            (bag, tile) = bag.Draw();
            Assert.Equal(Straight, tile);
        }
    }

    [Fact]
    public void Duplicated_tiles_weight_the_bag()
    {
        // Repeating a tile in the definition is how a level makes it common,
        // so duplicates are meaningful rather than an error.
        // Arrange
        var bag = NewBag(Straight, Straight, Straight, Bend);
        var straightCount = 0;

        // Act — one full cycle of four
        for (var i = 0; i < 4; i++)
        {
            ConduitTile tile;
            (bag, tile) = bag.Draw();
            if (tile == Straight)
            {
                straightCount++;
            }
        }

        // Assert
        Assert.Equal(3, straightCount);
    }
}
