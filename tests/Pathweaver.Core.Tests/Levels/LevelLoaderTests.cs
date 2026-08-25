using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Levels;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Tests.Levels;

public class LevelLoaderTests
{
    private const string ValidLevel = """
        # The first level of biome one.
        id: biome1-01
        name: First Waters
        base-score: 100
        target-score: 246
        tokens: 0
        shape: hexagon 3

        spring: -3,0 water
        hub: 2,0 water

        tile: 0,3 water x4
        tile: 0,2 water
        """;

    [Fact]
    public void A_valid_level_loads_its_identity()
    {
        // Act
        var level = LevelLoader.Parse(ValidLevel);

        // Assert
        Assert.Equal("biome1-01", level.Id);
        Assert.Equal("First Waters", level.Name);
    }

    [Fact]
    public void A_valid_level_loads_its_scoring()
    {
        // Act
        var level = LevelLoader.Parse(ValidLevel);

        // Assert
        Assert.Equal(100, level.BaseRouteScore);
        Assert.Equal(246, level.TargetScore);
        Assert.Equal(0, level.StartingTokens);
    }

    [Fact]
    public void A_hexagon_shape_expands_to_its_cells()
    {
        // Act
        var level = LevelLoader.Parse(ValidLevel);

        // Assert — 3r^2 + 3r + 1 for radius 3
        Assert.Equal(37, level.Shape.Count);
        Assert.Contains(HexCoord.Zero, level.Shape);
    }

    [Fact]
    public void Endpoints_load_with_their_kind_and_role()
    {
        // Act
        var level = LevelLoader.Parse(ValidLevel);

        // Assert
        Assert.Equal(2, level.Endpoints.Count);
        Assert.Contains(FlowEndpoint.Spring(new HexCoord(-3, 0), ResourceKind.Water), level.Endpoints);
        Assert.Contains(FlowEndpoint.Hub(new HexCoord(2, 0), ResourceKind.Water), level.Endpoints);
    }

    [Fact]
    public void A_tile_count_repeats_the_tile_in_the_bag()
    {
        // Repetition is how a level weights its supply, so the count has to
        // survive loading rather than collapsing to one entry.
        // Act
        var level = LevelLoader.Parse(ValidLevel);

        // Assert
        Assert.Equal(5, level.BagTiles.Count);
        Assert.Equal(4, level.BagTiles.Count(tile => tile.Edges == EdgeMask.FromDirections(0, 3)));
        Assert.Equal(1, level.BagTiles.Count(tile => tile.Edges == EdgeMask.FromDirections(0, 2)));
    }

    [Fact]
    public void Comments_and_blank_lines_are_ignored()
    {
        // Act / Assert — the valid level above contains both
        Assert.Equal("biome1-01", LevelLoader.Parse(ValidLevel).Id);
    }

    [Fact]
    public void An_explicit_cell_list_defines_an_irregular_shape()
    {
        // Handcrafted levels are not all hexagons.
        // Arrange
        var text = """
            id: odd-shape
            base-score: 100
            target-score: 100
            cell: -1,0
            cell: 0,0
            cell: 1,0
            spring: -1,0 water
            hub: 1,0 water
            tile: 0,3 water
            """;

        // Act
        var level = LevelLoader.Parse(text);

        // Assert
        Assert.Equal(3, level.Shape.Count);
    }

    [Fact]
    public void A_level_becomes_a_playable_game()
    {
        // Act
        var state = LevelLoader.Parse(ValidLevel).CreateGame(seed: 42UL);

        // Assert
        Assert.Equal(37, state.Board.Coordinates.Count);
        Assert.Equal(2, state.Endpoints.Count);
        Assert.Equal(100, state.BaseRouteScore);
        Assert.False(state.IsDeadlocked);
    }

    [Fact]
    public void The_same_level_and_seed_produce_the_same_game()
    {
        // Arrange
        var level = LevelLoader.Parse(ValidLevel);

        // Act
        var first = level.CreateGame(seed: 9UL);
        var second = level.CreateGame(seed: 9UL);

        // Assert
        Assert.Equal(first.HeldTile, second.HeldTile);
    }

    [Theory]
    [InlineData(245, false)]
    [InlineData(246, true)]
    [InlineData(1000, true)]
    public void A_level_knows_what_score_clears_it(long score, bool expected)
    {
        // The win condition is a rule, so it lives in the simulation where CI can check it.
        // It began in the Unity layer, which meant the one thing a level is judged by was the
        // one thing no test could reach.
        var level = LevelLoader.Parse(ValidLevel);

        Assert.Equal(expected, level.IsClearedBy(score));
    }

    [Fact]
    public void An_unknown_key_is_rejected_with_its_line_number()
    {
        // Level files are hand-authored, so an error has to point at the line.
        // Arrange
        var text = """
            id: broken
            base-score: 100
            target-score: 100
            wobble: yes
            """;

        // Act
        var error = Assert.Throws<LevelFormatException>(() => LevelLoader.Parse(text));

        // Assert
        Assert.Equal(4, error.Line);
        Assert.Contains("wobble", error.Message);
    }

    [Fact]
    public void A_line_without_a_key_is_rejected()
    {
        // Arrange
        var text = """
            id: broken
            this line has no colon
            """;

        // Act
        var error = Assert.Throws<LevelFormatException>(() => LevelLoader.Parse(text));

        // Assert
        Assert.Equal(2, error.Line);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("base-score")]
    [InlineData("target-score")]
    public void A_missing_required_key_is_rejected(string missing)
    {
        // Arrange
        var lines = new List<string>
        {
            "id: x",
            "base-score: 100",
            "target-score: 100",
            "cell: 0,0",
            "cell: 1,0",
            "spring: 0,0 water",
            "hub: 1,0 water",
            "tile: 0,3 water",
        };
        lines.RemoveAll(line => line.StartsWith($"{missing}:", StringComparison.Ordinal));

        // Act
        var error = Assert.Throws<LevelFormatException>(
            () => LevelLoader.Parse(string.Join("\n", lines)));

        // Assert
        Assert.Contains(missing, error.Message);
    }

    [Fact]
    public void A_level_with_no_shape_is_rejected()
    {
        // Arrange
        var text = """
            id: shapeless
            base-score: 100
            target-score: 100
            spring: 0,0 water
            hub: 1,0 water
            tile: 0,3 water
            """;

        // Act / Assert
        Assert.Throws<LevelFormatException>(() => LevelLoader.Parse(text));
    }

    [Fact]
    public void A_malformed_coordinate_is_rejected_with_its_line()
    {
        // Arrange
        var text = """
            id: bad-coord
            base-score: 100
            target-score: 100
            cell: nonsense
            """;

        // Act
        var error = Assert.Throws<LevelFormatException>(() => LevelLoader.Parse(text));

        // Assert
        Assert.Equal(4, error.Line);
    }

    [Fact]
    public void An_unknown_resource_kind_is_rejected()
    {
        // Arrange
        var text = """
            id: bad-kind
            base-score: 100
            target-score: 100
            cell: 0,0
            cell: 1,0
            spring: 0,0 lava
            hub: 1,0 water
            tile: 0,3 water
            """;

        // Act
        var error = Assert.Throws<LevelFormatException>(() => LevelLoader.Parse(text));

        // Assert
        Assert.Contains("lava", error.Message);
    }

    [Fact]
    public void A_tile_with_one_open_edge_is_rejected()
    {
        // Arrange
        var text = """
            id: dead-end-tile
            base-score: 100
            target-score: 100
            cell: 0,0
            cell: 1,0
            spring: 0,0 water
            hub: 1,0 water
            tile: 0 water
            """;

        // Act / Assert
        Assert.Throws<LevelFormatException>(() => LevelLoader.Parse(text));
    }

    [Fact]
    public void An_endpoint_outside_the_shape_is_rejected()
    {
        // Arrange
        var text = """
            id: stray-endpoint
            base-score: 100
            target-score: 100
            cell: 0,0
            cell: 1,0
            spring: 9,9 water
            hub: 1,0 water
            tile: 0,3 water
            """;

        // Act
        var error = Assert.Throws<LevelFormatException>(() => LevelLoader.Parse(text));

        // Assert
        Assert.Contains("9, 9", error.Message);
    }

    [Fact]
    public void A_level_with_no_spring_is_rejected()
    {
        // A level with nothing to route from cannot be completed, so it should
        // never reach the solver, let alone a player.
        // Arrange
        var text = """
            id: no-spring
            base-score: 100
            target-score: 100
            cell: 0,0
            cell: 1,0
            hub: 1,0 water
            tile: 0,3 water
            """;

        // Act / Assert
        Assert.Throws<LevelFormatException>(() => LevelLoader.Parse(text));
    }

    [Fact]
    public void A_level_with_no_hub_is_rejected()
    {
        // Arrange
        var text = """
            id: no-hub
            base-score: 100
            target-score: 100
            cell: 0,0
            cell: 1,0
            spring: 0,0 water
            tile: 0,3 water
            """;

        // Act / Assert
        Assert.Throws<LevelFormatException>(() => LevelLoader.Parse(text));
    }

    [Fact]
    public void A_level_with_an_empty_bag_is_rejected()
    {
        // Arrange
        var text = """
            id: no-tiles
            base-score: 100
            target-score: 100
            cell: 0,0
            cell: 1,0
            spring: 0,0 water
            hub: 1,0 water
            """;

        // Act / Assert
        Assert.Throws<LevelFormatException>(() => LevelLoader.Parse(text));
    }

    [Theory]
    [InlineData("target-score: 0")]
    [InlineData("target-score: -5")]
    [InlineData("base-score: 0")]
    [InlineData("tokens: -1")]
    // More than the base ceiling holds. Ceilings above it are earned through the World Atlas, so a
    // level file cannot assume one: the surplus would be invisible on a board played without the
    // relics, since the pip column shows a ceiling rather than a hoard.
    [InlineData("tokens: 4")]
    [InlineData("skips: 6")]
    [InlineData("shape: hexagon -1")]
    [InlineData("tile: 0,3 water x0")]
    public void Implausible_numbers_are_rejected(string line)
    {
        // Arrange — a level that is otherwise valid, with one field spoiled
        var lines = new List<string>
        {
            "id: numbers",
            "base-score: 100",
            "target-score: 100",
            "tokens: 0",
            "shape: hexagon 2",
            "spring: -2,0 water",
            "hub: 2,0 water",
            "tile: 0,3 water",
        };

        var key = line.Split(':')[0];
        var index = lines.FindIndex(existing => existing.StartsWith($"{key}:", StringComparison.Ordinal));
        if (index >= 0)
        {
            lines[index] = line;
        }
        else
        {
            lines.Add(line);
        }

        // Act / Assert
        Assert.Throws<LevelFormatException>(() => LevelLoader.Parse(string.Join("\n", lines)));
    }

    [Fact]
    public void Two_endpoints_on_one_cell_are_rejected()
    {
        // Arrange
        var text = """
            id: stacked
            base-score: 100
            target-score: 100
            cell: 0,0
            cell: 1,0
            spring: 0,0 water
            hub: 0,0 water
            tile: 0,3 water
            """;

        // Act / Assert
        Assert.Throws<LevelFormatException>(() => LevelLoader.Parse(text));
    }

    [Fact]
    public void Empty_text_is_rejected()
    {
        Assert.Throws<LevelFormatException>(() => LevelLoader.Parse("   "));
    }

    [Fact]
    public void Null_text_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => LevelLoader.Parse(null!));
    }
}
