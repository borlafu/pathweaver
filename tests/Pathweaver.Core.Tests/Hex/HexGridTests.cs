using Pathweaver.Core.Hex;

namespace Pathweaver.Core.Tests.Hex;

public class HexGridTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 7)]
    [InlineData(2, 19)]
    [InlineData(3, 37)]
    public void A_hexagonal_grid_of_radius_r_holds_the_expected_cell_count(int radius, int expected)
    {
        // Act
        var grid = HexGrid<string>.Hexagon(radius);

        // Assert — the centred hex number, 3r^2 + 3r + 1
        Assert.Equal(expected, grid.Coordinates.Count);
    }

    [Fact]
    public void A_negative_radius_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HexGrid<string>.Hexagon(-1));
    }

    [Fact]
    public void An_irregular_shape_keeps_exactly_the_given_cells()
    {
        // Arrange — levels are handcrafted, so shapes are not always hexagonal
        var shape = new[]
        {
            new HexCoord(0, 0),
            new HexCoord(1, 0),
            new HexCoord(2, -1),
        };

        // Act
        var grid = HexGrid<string>.FromShape(shape);

        // Assert
        Assert.Equal(3, grid.Coordinates.Count);
        Assert.True(grid.Contains(new HexCoord(2, -1)));
        Assert.False(grid.Contains(new HexCoord(0, 1)));
    }

    [Fact]
    public void A_shape_with_no_cells_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => HexGrid<string>.FromShape(Array.Empty<HexCoord>()));
    }

    [Fact]
    public void A_shape_with_duplicate_cells_is_rejected()
    {
        // Arrange
        var shape = new[] { new HexCoord(0, 0), new HexCoord(0, 0) };

        // Act / Assert — a duplicate means the level data is wrong, so say so
        Assert.Throws<ArgumentException>(() => HexGrid<string>.FromShape(shape));
    }

    [Fact]
    public void A_new_grid_is_entirely_empty()
    {
        // Arrange
        var grid = HexGrid<string>.Hexagon(1);

        // Act / Assert
        Assert.Equal(0, grid.OccupiedCount);
        Assert.False(grid.IsFull);
        foreach (var coordinate in grid.Coordinates)
        {
            Assert.True(grid.IsEmpty(coordinate));
        }
    }

    [Fact]
    public void Placing_returns_a_new_grid_and_leaves_the_original_empty()
    {
        // Arrange
        var original = HexGrid<string>.Hexagon(1);

        // Act
        var placed = original.Place(HexCoord.Zero, "water");

        // Assert
        Assert.Equal(0, original.OccupiedCount);
        Assert.True(original.IsEmpty(HexCoord.Zero));
        Assert.Equal(1, placed.OccupiedCount);
        Assert.False(placed.IsEmpty(HexCoord.Zero));
    }

    [Fact]
    public void A_placed_value_can_be_read_back()
    {
        // Arrange
        var grid = HexGrid<string>.Hexagon(1).Place(new HexCoord(1, 0), "crystal");

        // Act
        var found = grid.TryGet(new HexCoord(1, 0), out var value);

        // Assert
        Assert.True(found);
        Assert.Equal("crystal", value);
    }

    [Fact]
    public void Reading_an_empty_cell_reports_nothing()
    {
        // Arrange
        var grid = HexGrid<string>.Hexagon(1);

        // Act / Assert
        Assert.False(grid.TryGet(HexCoord.Zero, out _));
    }

    [Fact]
    public void Placing_outside_the_grid_is_rejected()
    {
        // Arrange
        var grid = HexGrid<string>.Hexagon(1);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.Place(new HexCoord(9, 9), "wind"));
    }

    [Fact]
    public void Placing_onto_an_occupied_cell_is_rejected()
    {
        // Arrange — silently overwriting would lose a player's tile
        var grid = HexGrid<string>.Hexagon(1).Place(HexCoord.Zero, "water");

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => grid.Place(HexCoord.Zero, "wind"));
    }

    [Fact]
    public void Removing_returns_a_new_grid_and_leaves_the_original_occupied()
    {
        // Arrange
        var occupied = HexGrid<string>.Hexagon(1).Place(HexCoord.Zero, "water");

        // Act
        var cleared = occupied.Remove(HexCoord.Zero);

        // Assert
        Assert.Equal(1, occupied.OccupiedCount);
        Assert.Equal(0, cleared.OccupiedCount);
    }

    [Fact]
    public void Removing_from_an_empty_cell_is_rejected()
    {
        // Arrange
        var grid = HexGrid<string>.Hexagon(1);

        // Act / Assert — Pivot Token retrieval must not silently no-op
        Assert.Throws<InvalidOperationException>(() => grid.Remove(HexCoord.Zero));
    }

    [Fact]
    public void Removing_outside_the_grid_is_rejected()
    {
        // Arrange
        var grid = HexGrid<string>.Hexagon(1);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.Remove(new HexCoord(9, 9)));
    }

    [Fact]
    public void A_grid_is_full_once_every_cell_is_occupied()
    {
        // Arrange
        var grid = HexGrid<string>.Hexagon(1);

        // Act
        foreach (var coordinate in grid.Coordinates)
        {
            grid = grid.Place(coordinate, "trade");
        }

        // Assert — full grids are what the deadlock detector watches for
        Assert.True(grid.IsFull);
        Assert.Equal(7, grid.OccupiedCount);
    }

    [Fact]
    public void Neighbours_are_limited_to_cells_inside_the_grid()
    {
        // Arrange
        var grid = HexGrid<string>.Hexagon(1);

        // Act
        var centreNeighbours = grid.NeighboursOf(HexCoord.Zero).ToList();
        var edgeNeighbours = grid.NeighboursOf(new HexCoord(1, 0)).ToList();

        // Assert
        Assert.Equal(6, centreNeighbours.Count);
        Assert.Equal(3, edgeNeighbours.Count);
        Assert.All(edgeNeighbours, coordinate => Assert.True(grid.Contains(coordinate)));
    }

    [Fact]
    public void Neighbours_of_a_cell_outside_the_grid_are_rejected()
    {
        // Arrange
        var grid = HexGrid<string>.Hexagon(1);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.NeighboursOf(new HexCoord(9, 9)).ToList());
    }

    [Fact]
    public void Coordinate_enumeration_order_is_stable_across_instances()
    {
        // Determinism depends on this: generation walks the grid, so two runs
        // must visit cells in the same order.
        // Arrange
        var first = HexGrid<string>.Hexagon(2);
        var second = HexGrid<string>.Hexagon(2);

        // Act / Assert
        Assert.Equal(first.Coordinates, second.Coordinates);
    }

    [Fact]
    public void Coordinate_enumeration_order_survives_placement()
    {
        // Arrange
        var empty = HexGrid<string>.Hexagon(2);

        // Act
        var populated = empty.Place(new HexCoord(-2, 1), "water").Place(HexCoord.Zero, "wind");

        // Assert
        Assert.Equal(empty.Coordinates, populated.Coordinates);
    }

    [Fact]
    public void Shape_order_does_not_affect_enumeration_order()
    {
        // Arrange
        var ascending = new[] { new HexCoord(0, 0), new HexCoord(1, 0), new HexCoord(2, 0) };
        var shuffled = new[] { new HexCoord(2, 0), new HexCoord(0, 0), new HexCoord(1, 0) };

        // Act
        var fromAscending = HexGrid<string>.FromShape(ascending);
        var fromShuffled = HexGrid<string>.FromShape(shuffled);

        // Assert — level JSON authored in any order yields identical generation
        Assert.Equal(fromAscending.Coordinates, fromShuffled.Coordinates);
    }

    [Fact]
    public void Occupied_cells_enumerate_in_the_same_stable_order()
    {
        // Arrange
        var grid = HexGrid<string>.Hexagon(2)
            .Place(new HexCoord(2, -1), "trade")
            .Place(new HexCoord(-1, 0), "water");

        // Act
        var occupied = grid.OccupiedCells.ToList();

        // Assert
        Assert.Equal(2, occupied.Count);
        Assert.Equal(new HexCoord(-1, 0), occupied[0].Coordinate);
        Assert.Equal("water", occupied[0].Value);
        Assert.Equal(new HexCoord(2, -1), occupied[1].Coordinate);
    }
}
