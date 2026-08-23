using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Tests.Flow;

public class FlowEndpointTests
{
    [Fact]
    public void A_spring_occupies_a_cell_and_carries_a_kind()
    {
        // Act
        var spring = FlowEndpoint.Spring(new HexCoord(1, -1), ResourceKind.Wind);

        // Assert
        Assert.Equal(new HexCoord(1, -1), spring.Coordinate);
        Assert.Equal(ResourceKind.Wind, spring.Kind);
        Assert.Equal(EndpointRole.Spring, spring.Role);
    }

    [Fact]
    public void A_hub_records_the_receiving_role()
    {
        // Act
        var hub = FlowEndpoint.Hub(HexCoord.Zero, ResourceKind.Trade);

        // Assert
        Assert.Equal(EndpointRole.Hub, hub.Role);
    }

    [Fact]
    public void An_undefined_resource_kind_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FlowEndpoint.Hub(HexCoord.Zero, (ResourceKind)42));
    }

    [Fact]
    public void Endpoints_with_the_same_parts_are_equal()
    {
        // Arrange
        var first = FlowEndpoint.Spring(new HexCoord(2, 0), ResourceKind.Water);
        var second = FlowEndpoint.Spring(new HexCoord(2, 0), ResourceKind.Water);

        // Act / Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first == second);
        Assert.False(first != second);
    }

    [Fact]
    public void A_spring_and_a_hub_on_the_same_cell_are_not_equal()
    {
        // Arrange
        var spring = FlowEndpoint.Spring(new HexCoord(2, 0), ResourceKind.Water);
        var hub = FlowEndpoint.Hub(new HexCoord(2, 0), ResourceKind.Water);

        // Act / Assert
        Assert.NotEqual(spring, hub);
    }

    [Fact]
    public void Endpoints_of_different_kinds_are_not_equal()
    {
        // Arrange
        var water = FlowEndpoint.Spring(HexCoord.Zero, ResourceKind.Water);
        var crystal = FlowEndpoint.Spring(HexCoord.Zero, ResourceKind.Crystal);

        // Act / Assert
        Assert.NotEqual(water, crystal);
    }
}
