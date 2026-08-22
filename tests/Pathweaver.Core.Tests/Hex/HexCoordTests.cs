using Pathweaver.Core.Hex;

namespace Pathweaver.Core.Tests.Hex;

public class HexCoordTests
{
    [Fact]
    public void Zero_is_the_origin()
    {
        // Arrange / Act
        var origin = HexCoord.Zero;

        // Assert
        Assert.Equal(0, origin.Q);
        Assert.Equal(0, origin.R);
    }

    [Fact]
    public void Directions_expose_exactly_six_neighbours()
    {
        // Act
        var directions = HexCoord.Directions;

        // Assert
        Assert.Equal(6, directions.Count);
        Assert.Equal(directions.Count, directions.Distinct().Count());
    }

    [Fact]
    public void Every_direction_is_one_step_from_the_origin()
    {
        // Act / Assert
        foreach (var direction in HexCoord.Directions)
        {
            Assert.Equal(1, HexCoord.Zero.DistanceTo(direction));
        }
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(1, 0, 1)]
    [InlineData(2, -1, 1)]
    [InlineData(3, -1, 0)]
    [InlineData(4, 0, -1)]
    [InlineData(5, 1, -1)]
    public void Direction_ordering_is_clockwise_starting_from_east(int index, int expectedQ, int expectedR)
    {
        // Act
        var direction = HexCoord.Directions[index];

        // Assert
        Assert.Equal(new HexCoord(expectedQ, expectedR), direction);
    }

    [Fact]
    public void Neighbour_offsets_the_coordinate_by_the_direction()
    {
        // Arrange
        var centre = new HexCoord(3, -2);

        // Act
        var neighbour = centre.Neighbour(0);

        // Assert
        Assert.Equal(new HexCoord(4, -2), neighbour);
    }

    [Fact]
    public void Neighbour_wraps_direction_indices_beyond_five()
    {
        // Arrange
        var centre = new HexCoord(1, 1);

        // Act / Assert
        Assert.Equal(centre.Neighbour(0), centre.Neighbour(6));
        Assert.Equal(centre.Neighbour(1), centre.Neighbour(13));
        Assert.Equal(centre.Neighbour(5), centre.Neighbour(-1));
    }

    [Fact]
    public void Distance_to_self_is_zero()
    {
        // Arrange
        var coord = new HexCoord(-4, 7);

        // Act / Assert
        Assert.Equal(0, coord.DistanceTo(coord));
    }

    [Theory]
    [InlineData(0, 0, 1, 0, 1)]
    [InlineData(0, 0, 2, -1, 2)]
    [InlineData(0, 0, -3, 3, 3)]
    [InlineData(0, 0, 3, -3, 3)]
    [InlineData(-2, 1, 2, -1, 4)]
    public void Distance_counts_the_shortest_hex_step_path(int aq, int ar, int bq, int br, int expected)
    {
        // Arrange
        var a = new HexCoord(aq, ar);
        var b = new HexCoord(bq, br);

        // Act / Assert
        Assert.Equal(expected, a.DistanceTo(b));
    }

    [Fact]
    public void Distance_is_symmetric()
    {
        // Arrange
        var a = new HexCoord(5, -3);
        var b = new HexCoord(-1, 4);

        // Act / Assert
        Assert.Equal(a.DistanceTo(b), b.DistanceTo(a));
    }

    [Fact]
    public void Addition_and_subtraction_are_inverses()
    {
        // Arrange
        var start = new HexCoord(2, 3);
        var offset = new HexCoord(-4, 1);

        // Act
        var moved = start + offset;

        // Assert
        Assert.Equal(new HexCoord(-2, 4), moved);
        Assert.Equal(start, moved - offset);
    }

    [Fact]
    public void Six_clockwise_rotations_return_to_the_starting_coordinate()
    {
        // Arrange
        var start = new HexCoord(3, -1);

        // Act
        var rotated = start;
        for (var i = 0; i < 6; i++)
        {
            rotated = rotated.RotateClockwise();
        }

        // Assert
        Assert.Equal(start, rotated);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Rotating_a_direction_clockwise_yields_the_next_direction(int index)
    {
        // Arrange
        var direction = HexCoord.Directions[index];
        var expected = HexCoord.Directions[(index + 1) % 6];

        // Act / Assert
        Assert.Equal(expected, direction.RotateClockwise());
    }

    [Fact]
    public void Counter_clockwise_rotation_undoes_clockwise_rotation()
    {
        // Arrange
        var start = new HexCoord(-2, 5);

        // Act / Assert
        Assert.Equal(start, start.RotateClockwise().RotateCounterClockwise());
    }

    [Fact]
    public void Rotation_preserves_distance_from_the_origin()
    {
        // Arrange
        var coord = new HexCoord(4, -1);

        // Act / Assert
        Assert.Equal(
            HexCoord.Zero.DistanceTo(coord),
            HexCoord.Zero.DistanceTo(coord.RotateClockwise()));
    }

    [Fact]
    public void Equal_coordinates_share_a_hash_code()
    {
        // Arrange
        var a = new HexCoord(7, -2);
        var b = new HexCoord(7, -2);

        // Act / Assert
        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Different_coordinates_are_not_equal()
    {
        // Arrange
        var a = new HexCoord(1, 2);
        var b = new HexCoord(2, 1);

        // Act / Assert
        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }
}
