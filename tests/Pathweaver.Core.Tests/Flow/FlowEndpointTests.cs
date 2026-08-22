using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Tests.Flow;

public class FlowEndpointTests
{
    [Fact]
    public void A_spring_records_its_cell_edge_and_kind()
    {
        // Act
        var spring = FlowEndpoint.Spring(new HexCoord(1, -1), 2, ResourceKind.Wind);

        // Assert
        Assert.Equal(new HexCoord(1, -1), spring.Coordinate);
        Assert.Equal(2, spring.Direction);
        Assert.Equal(ResourceKind.Wind, spring.Kind);
        Assert.Equal(EndpointRole.Spring, spring.Role);
    }

    [Fact]
    public void A_hub_records_the_receiving_role()
    {
        // Act
        var hub = FlowEndpoint.Hub(HexCoord.Zero, 5, ResourceKind.Trade);

        // Assert
        Assert.Equal(EndpointRole.Hub, hub.Role);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(7)]
    public void An_out_of_range_direction_is_rejected(int direction)
    {
        // Endpoints come from authored level data, where an index outside 0 to 5
        // is a mistake rather than shorthand.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FlowEndpoint.Spring(HexCoord.Zero, direction, ResourceKind.Water));
    }

    [Fact]
    public void An_undefined_resource_kind_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FlowEndpoint.Hub(HexCoord.Zero, 0, (ResourceKind)42));
    }

    [Fact]
    public void Endpoints_with_the_same_parts_are_equal()
    {
        // Arrange
        var first = FlowEndpoint.Spring(new HexCoord(2, 0), 3, ResourceKind.Water);
        var second = FlowEndpoint.Spring(new HexCoord(2, 0), 3, ResourceKind.Water);

        // Act / Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_spring_and_a_hub_at_the_same_place_are_not_equal()
    {
        // Arrange
        var spring = FlowEndpoint.Spring(new HexCoord(2, 0), 3, ResourceKind.Water);
        var hub = FlowEndpoint.Hub(new HexCoord(2, 0), 3, ResourceKind.Water);

        // Act / Assert
        Assert.NotEqual(spring, hub);
    }
}
