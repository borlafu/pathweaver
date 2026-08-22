using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Tests.Tiles;

public class ConduitTileTests
{
    [Fact]
    public void A_tile_carries_its_resource_kind_and_edges()
    {
        // Arrange / Act
        var tile = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(0, 3));

        // Assert
        Assert.Equal(ResourceKind.Water, tile.Kind);
        Assert.True(tile.HasEdge(0));
        Assert.True(tile.HasEdge(3));
        Assert.False(tile.HasEdge(1));
    }

    [Fact]
    public void A_tile_with_fewer_than_two_edges_is_rejected()
    {
        // A conduit has to carry flow through itself. One opening is a dead end
        // and zero is a blank, neither of which is a drawable tile.
        Assert.Throws<ArgumentException>(
            () => new ConduitTile(ResourceKind.Water, EdgeMask.None));
        Assert.Throws<ArgumentException>(
            () => new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(2)));
    }

    [Fact]
    public void An_undefined_resource_kind_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ConduitTile((ResourceKind)99, EdgeMask.FromDirections(0, 3)));
    }

    [Fact]
    public void Rotating_returns_a_new_tile_and_leaves_the_original_alone()
    {
        // Arrange
        var original = new ConduitTile(ResourceKind.Crystal, EdgeMask.FromDirections(0, 2));

        // Act
        var rotated = original.RotateClockwise();

        // Assert
        Assert.True(original.HasEdge(0));
        Assert.False(original.HasEdge(1));
        Assert.True(rotated.HasEdge(1));
        Assert.True(rotated.HasEdge(3));
    }

    [Fact]
    public void Rotation_preserves_the_resource_kind()
    {
        // Arrange
        var tile = new ConduitTile(ResourceKind.Trade, EdgeMask.FromDirections(1, 4));

        // Act / Assert
        Assert.Equal(ResourceKind.Trade, tile.RotateClockwise(3).Kind);
    }

    [Fact]
    public void Six_rotations_return_the_original_tile()
    {
        // Arrange
        var tile = new ConduitTile(ResourceKind.Wind, EdgeMask.FromDirections(0, 1, 3));

        // Act / Assert
        Assert.Equal(tile, tile.RotateClockwise(6));
    }

    [Fact]
    public void Two_tiles_of_the_same_kind_connect_through_facing_edges()
    {
        // Arrange — a tile open east, and its eastern neighbour open west
        var here = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(0, 2));
        var neighbour = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(3, 5));

        // Act / Assert
        Assert.True(here.ConnectsTo(neighbour, 0));
    }

    [Fact]
    public void Tiles_do_not_connect_when_the_neighbour_edge_is_closed()
    {
        // Arrange
        var here = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(0, 2));
        var neighbour = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(1, 5));

        // Act / Assert
        Assert.False(here.ConnectsTo(neighbour, 0));
    }

    [Fact]
    public void Tiles_do_not_connect_when_this_edge_is_closed()
    {
        // Arrange
        var here = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(1, 2));
        var neighbour = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(3, 5));

        // Act / Assert
        Assert.False(here.ConnectsTo(neighbour, 0));
    }

    [Fact]
    public void Different_resource_kinds_never_connect()
    {
        // PRD section 3.1: resources route from their own springs to their own
        // hubs. Water must not flow down a crystal conduit even when the edges
        // line up.
        // Arrange
        var water = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(0, 2));
        var crystal = new ConduitTile(ResourceKind.Crystal, EdgeMask.FromDirections(3, 5));

        // Act / Assert
        Assert.False(water.ConnectsTo(crystal, 0));
    }

    [Fact]
    public void Connection_is_symmetric()
    {
        // Arrange
        var here = new ConduitTile(ResourceKind.Wind, EdgeMask.FromDirections(0, 2));
        var neighbour = new ConduitTile(ResourceKind.Wind, EdgeMask.FromDirections(3, 5));

        // Act / Assert
        Assert.True(here.ConnectsTo(neighbour, 0));
        Assert.True(neighbour.ConnectsTo(here, EdgeMask.Opposite(0)));
    }

    [Fact]
    public void Tiles_with_the_same_kind_and_edges_are_equal()
    {
        // Arrange
        var first = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(0, 3));
        var second = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(3, 0));

        // Act / Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Tiles_differing_only_by_kind_are_not_equal()
    {
        // Arrange
        var water = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(0, 3));
        var wind = new ConduitTile(ResourceKind.Wind, EdgeMask.FromDirections(0, 3));

        // Act / Assert
        Assert.NotEqual(water, wind);
    }

    [Fact]
    public void All_four_resource_kinds_from_the_PRD_exist()
    {
        // Arrange / Act
        var kinds = Enum.GetValues<ResourceKind>();

        // Assert
        Assert.Contains(ResourceKind.Water, kinds);
        Assert.Contains(ResourceKind.Wind, kinds);
        Assert.Contains(ResourceKind.Crystal, kinds);
        Assert.Contains(ResourceKind.Trade, kinds);
    }
}
