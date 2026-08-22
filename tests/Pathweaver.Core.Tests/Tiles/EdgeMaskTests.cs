using Pathweaver.Core.Hex;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Tests.Tiles;

public class EdgeMaskTests
{
    [Fact]
    public void An_empty_mask_has_no_open_edges()
    {
        // Act
        var mask = EdgeMask.None;

        // Assert
        Assert.Equal(0, mask.OpenEdgeCount);
        for (var direction = 0; direction < 6; direction++)
        {
            Assert.False(mask.HasEdge(direction));
        }
    }

    [Fact]
    public void A_mask_opens_exactly_the_given_directions()
    {
        // Arrange / Act
        var mask = EdgeMask.FromDirections(0, 3);

        // Assert
        Assert.True(mask.HasEdge(0));
        Assert.True(mask.HasEdge(3));
        Assert.False(mask.HasEdge(1));
        Assert.Equal(2, mask.OpenEdgeCount);
    }

    [Fact]
    public void Direction_indices_wrap()
    {
        // Arrange
        var mask = EdgeMask.FromDirections(0);

        // Act / Assert
        Assert.True(mask.HasEdge(6));
        Assert.True(mask.HasEdge(-6));
        Assert.True(mask.HasEdge(12));
    }

    [Fact]
    public void Repeating_a_direction_is_rejected()
    {
        // A duplicate means the tile definition is wrong, not that the edge is
        // doubly open.
        Assert.Throws<ArgumentException>(() => EdgeMask.FromDirections(2, 2));
    }

    [Fact]
    public void An_out_of_range_direction_is_rejected()
    {
        // FromDirections takes authored data, so unlike HasEdge it does not wrap
        // silently — an index of 7 in level JSON is a mistake worth surfacing.
        Assert.Throws<ArgumentOutOfRangeException>(() => EdgeMask.FromDirections(7));
        Assert.Throws<ArgumentOutOfRangeException>(() => EdgeMask.FromDirections(-1));
    }

    [Fact]
    public void Open_directions_enumerate_in_ascending_order()
    {
        // Arrange
        var mask = EdgeMask.FromDirections(4, 1, 5);

        // Act / Assert — stable order keeps flow tracing deterministic
        Assert.Equal(new[] { 1, 4, 5 }, mask.OpenDirections);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 4)]
    [InlineData(2, 5)]
    [InlineData(3, 0)]
    [InlineData(4, 1)]
    [InlineData(5, 2)]
    public void The_opposite_of_a_direction_faces_back(int direction, int expected)
    {
        // Act / Assert
        Assert.Equal(expected, EdgeMask.Opposite(direction));
    }

    [Fact]
    public void The_opposite_direction_is_the_reverse_step_on_the_grid()
    {
        // Ties the mask convention to the grid: stepping out of a cell and back
        // in through the opposite edge must return to where you started.
        for (var direction = 0; direction < 6; direction++)
        {
            var start = new HexCoord(2, -1);
            var neighbour = start.Neighbour(direction);

            Assert.Equal(start, neighbour.Neighbour(EdgeMask.Opposite(direction)));
        }
    }

    [Fact]
    public void Rotating_clockwise_moves_every_edge_one_direction_along()
    {
        // Arrange
        var mask = EdgeMask.FromDirections(0, 2);

        // Act
        var rotated = mask.RotateClockwise();

        // Assert
        Assert.Equal(EdgeMask.FromDirections(1, 3), rotated);
    }

    [Fact]
    public void Rotating_wraps_past_the_last_direction()
    {
        // Arrange
        var mask = EdgeMask.FromDirections(5);

        // Act / Assert
        Assert.Equal(EdgeMask.FromDirections(0), mask.RotateClockwise());
    }

    [Fact]
    public void Six_clockwise_rotations_return_the_original_mask()
    {
        // Arrange
        var mask = EdgeMask.FromDirections(0, 1, 4);

        // Act
        var rotated = mask;
        for (var i = 0; i < 6; i++)
        {
            rotated = rotated.RotateClockwise();
        }

        // Assert
        Assert.Equal(mask, rotated);
    }

    [Fact]
    public void Rotating_by_several_steps_matches_repeated_single_steps()
    {
        // Arrange
        var mask = EdgeMask.FromDirections(1, 2, 5);

        // Act
        var bulk = mask.RotateClockwise(4);
        var repeated = mask.RotateClockwise().RotateClockwise().RotateClockwise().RotateClockwise();

        // Assert
        Assert.Equal(repeated, bulk);
    }

    [Fact]
    public void Rotation_step_counts_wrap_and_accept_negatives()
    {
        // Arrange
        var mask = EdgeMask.FromDirections(0, 3);

        // Act / Assert
        Assert.Equal(mask, mask.RotateClockwise(6));
        Assert.Equal(mask.RotateClockwise(5), mask.RotateClockwise(-1));
    }

    [Fact]
    public void Rotation_preserves_the_open_edge_count()
    {
        // Arrange
        var mask = EdgeMask.FromDirections(0, 2, 3, 5);

        // Act / Assert
        Assert.Equal(4, mask.RotateClockwise(3).OpenEdgeCount);
    }

    [Fact]
    public void A_rotated_edge_lands_where_the_rotated_coordinate_points()
    {
        // The whole point of aligning the mask with HexCoord.Directions: a tile
        // rotation and a coordinate rotation must agree, or a rotated tile would
        // visually point somewhere it does not connect.
        for (var direction = 0; direction < 6; direction++)
        {
            var mask = EdgeMask.FromDirections(direction);
            var rotatedMask = mask.RotateClockwise();
            var rotatedCoordinate = HexCoord.Directions[direction].RotateClockwise();

            var expectedDirection = HexCoord.Directions.ToList().IndexOf(rotatedCoordinate);
            Assert.True(rotatedMask.HasEdge(expectedDirection));
            Assert.Equal(1, rotatedMask.OpenEdgeCount);
        }
    }

    [Fact]
    public void Masks_with_the_same_edges_are_equal()
    {
        // Arrange
        var first = EdgeMask.FromDirections(1, 3);
        var second = EdgeMask.FromDirections(3, 1);

        // Act / Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first == second);
        Assert.False(first != second);
    }
}
