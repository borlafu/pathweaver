using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Tests.Flow;

/// <summary>
/// Geometry used throughout: a radius-2 hexagon. A spring occupies (-2,0) and a
/// hub occupies (2,0), so the three cells between them — (-1,0), (0,0), (1,0) —
/// are the conduits the player places. East is direction 0, west is direction 3.
/// </summary>
public class FlowResolverTests
{
    // Clockwise on screen from due east, which is what HexMetrics guarantees by
    // negating the vertical axis. These were originally named for the textbook
    // counter-clockwise mapping, which made every geometry comment here misleading.
    private const int East = 0;
    private const int SouthEast = 1;
    private const int West = 3;
    private const int NorthWest = 4;

    private static readonly EdgeMask EastWest = EdgeMask.FromDirections(East, West);

    private static readonly HexCoord SpringCell = new HexCoord(-2, 0);
    private static readonly HexCoord HubCell = new HexCoord(2, 0);

    private static HexGrid<ConduitTile> EmptyBoard() => HexGrid<ConduitTile>.Hexagon(2);

    private static ConduitTile Straight(ResourceKind kind = ResourceKind.Water)
        => new ConduitTile(kind, EastWest);

    private static FlowEndpoint Spring(ResourceKind kind = ResourceKind.Water)
        => FlowEndpoint.Spring(SpringCell, kind);

    private static FlowEndpoint Hub(ResourceKind kind = ResourceKind.Water)
        => FlowEndpoint.Hub(HubCell, kind);

    /// <summary>Fills the three conduit cells between the spring and the hub.</summary>
    private static HexGrid<ConduitTile> WithRow(
        HexGrid<ConduitTile> board, ResourceKind kind = ResourceKind.Water)
    {
        for (var q = -1; q <= 1; q++)
        {
            board = board.Place(new HexCoord(q, 0), Straight(kind));
        }

        return board;
    }

    [Fact]
    public void An_empty_board_completes_no_routes()
    {
        // Act / Assert
        Assert.Empty(FlowResolver.FindCompletedRoutes(EmptyBoard(), new[] { Spring(), Hub() }));
    }

    [Fact]
    public void A_connected_row_completes_one_route()
    {
        // Arrange
        var board = WithRow(EmptyBoard());

        // Act
        var route = Assert.Single(FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() }));

        // Assert — three conduits, so L is 3; the spring and hub cells are not tiles
        Assert.Equal(ResourceKind.Water, route.Kind);
        Assert.Equal(3, route.Length);
    }

    [Fact]
    public void A_route_lists_only_the_conduits_ordered_from_the_spring()
    {
        // Arrange
        var board = WithRow(EmptyBoard());

        // Act
        var route = Assert.Single(FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() }));

        // Assert
        Assert.Equal(
            new[] { new HexCoord(-1, 0), new HexCoord(0, 0), new HexCoord(1, 0) },
            route.Tiles);
        Assert.DoesNotContain(SpringCell, route.Tiles);
        Assert.DoesNotContain(HubCell, route.Tiles);
    }

    [Fact]
    public void A_gap_completes_nothing()
    {
        // Arrange — the middle conduit is missing
        var board = EmptyBoard()
            .Place(new HexCoord(-1, 0), Straight())
            .Place(new HexCoord(1, 0), Straight());

        // Act / Assert
        Assert.Empty(FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() }));
    }

    [Fact]
    public void A_misaligned_conduit_breaks_the_route()
    {
        // Arrange — the middle conduit runs north-east to south-west instead
        var board = EmptyBoard()
            .Place(new HexCoord(-1, 0), Straight())
            .Place(HexCoord.Zero, new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(SouthEast, NorthWest)))
            .Place(new HexCoord(1, 0), Straight());

        // Act / Assert
        Assert.Empty(FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() }));
    }

    [Fact]
    public void Conduits_of_the_wrong_kind_complete_nothing()
    {
        // PRD section 3.1: each resource flows from its own springs to its own hubs.
        // Arrange
        var board = WithRow(EmptyBoard(), ResourceKind.Crystal);

        // Act / Assert
        Assert.Empty(FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() }));
    }

    [Fact]
    public void The_first_conduit_must_be_open_towards_the_spring()
    {
        // Arrange — the conduit beside the spring faces east and north-east, not west
        var board = WithRow(EmptyBoard())
            .Remove(new HexCoord(-1, 0))
            .Place(new HexCoord(-1, 0), new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(East, SouthEast)));

        // Act / Assert
        Assert.Empty(FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() }));
    }

    [Fact]
    public void The_last_conduit_must_be_open_towards_the_hub()
    {
        // Arrange
        var board = WithRow(EmptyBoard())
            .Remove(new HexCoord(1, 0))
            .Place(new HexCoord(1, 0), new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(West, NorthWest)));

        // Act / Assert
        Assert.Empty(FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() }));
    }

    [Fact]
    public void A_single_conduit_between_adjacent_endpoints_is_a_route_of_one()
    {
        // Arrange — spring at (-1,0), hub at (1,0), one conduit between them
        var board = EmptyBoard().Place(HexCoord.Zero, Straight());
        var spring = FlowEndpoint.Spring(new HexCoord(-1, 0), ResourceKind.Water);
        var hub = FlowEndpoint.Hub(new HexCoord(1, 0), ResourceKind.Water);

        // Act
        var route = Assert.Single(FlowResolver.FindCompletedRoutes(board, new[] { spring, hub }));

        // Assert — the shortest legal route, earning the unmultiplied base score
        Assert.Equal(1, route.Length);
    }

    [Fact]
    public void Touching_endpoints_with_no_conduit_complete_nothing()
    {
        // A route is built from tiles, so a spring pressed against a hub is not
        // a free harvest.
        // Arrange
        var spring = FlowEndpoint.Spring(HexCoord.Zero, ResourceKind.Water);
        var hub = FlowEndpoint.Hub(new HexCoord(1, 0), ResourceKind.Water);

        // Act / Assert
        Assert.Empty(FlowResolver.FindCompletedRoutes(EmptyBoard(), new[] { spring, hub }));
    }

    [Fact]
    public void An_interior_spring_feeds_any_adjacent_conduit_open_towards_it()
    {
        // The case the previous edge-attached model could not express: a spring
        // surrounded by placeable cells rather than sitting on the rim.
        // Arrange — spring at the origin, hub east at (2,0), conduit at (1,0)
        var board = EmptyBoard().Place(new HexCoord(1, 0), Straight());
        var spring = FlowEndpoint.Spring(HexCoord.Zero, ResourceKind.Water);

        // Act
        var route = Assert.Single(FlowResolver.FindCompletedRoutes(board, new[] { spring, Hub() }));

        // Assert
        Assert.Equal(1, route.Length);
    }

    [Fact]
    public void A_spring_serving_two_hubs_completes_two_routes()
    {
        // Arrange — the row, plus a branch north from the origin to a second hub
        var board = WithRow(EmptyBoard())
            .Remove(HexCoord.Zero)
            .Place(HexCoord.Zero, new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(East, West, NorthWest)));

        var northHub = FlowEndpoint.Hub(new HexCoord(0, -1), ResourceKind.Water);

        // Act
        var routes = FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub(), northHub });

        // Assert
        Assert.Equal(2, routes.Count);
        Assert.All(routes, route => Assert.Equal(ResourceKind.Water, route.Kind));
        Assert.Contains(routes, route => route.Length == 3);
        Assert.Contains(routes, route => route.Length == 2);
    }

    [Fact]
    public void Each_spring_and_hub_pair_completes_at_most_one_route()
    {
        // Arrange
        var board = WithRow(EmptyBoard());

        // Act / Assert
        Assert.Single(FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() }));
    }

    [Fact]
    public void A_dead_end_spur_does_not_lengthen_the_route()
    {
        // Arrange — the row, plus a spur hanging north off the origin leading nowhere
        var board = WithRow(EmptyBoard())
            .Remove(HexCoord.Zero)
            .Place(HexCoord.Zero, new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(East, West, NorthWest)))
            .Place(new HexCoord(0, -1), new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(SouthEast, NorthWest)));

        // Act
        var route = Assert.Single(FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() }));

        // Assert — connected, but not on the path
        Assert.Equal(3, route.Length);
        Assert.DoesNotContain(new HexCoord(0, -1), route.Tiles);
    }

    [Fact]
    public void Only_endpoints_of_matching_kind_pair_up()
    {
        // Arrange
        var board = WithRow(EmptyBoard());

        // Act / Assert
        Assert.Empty(FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub(ResourceKind.Crystal) }));
    }

    [Fact]
    public void Two_resource_networks_on_one_board_stay_separate()
    {
        // Arrange — water along r=0, crystal along r=1
        var board = WithRow(EmptyBoard())
            .Place(new HexCoord(0, 1), new ConduitTile(ResourceKind.Crystal, EastWest));

        var crystalSpring = FlowEndpoint.Spring(new HexCoord(-1, 1), ResourceKind.Crystal);
        var crystalHub = FlowEndpoint.Hub(new HexCoord(1, 1), ResourceKind.Crystal);

        // Act
        var routes = FlowResolver.FindCompletedRoutes(
            board, new[] { Spring(), Hub(), crystalSpring, crystalHub });

        // Assert
        Assert.Equal(2, routes.Count);
        Assert.Contains(routes, route => route.Kind == ResourceKind.Water && route.Length == 3);
        Assert.Contains(routes, route => route.Kind == ResourceKind.Crystal && route.Length == 1);
    }

    [Fact]
    public void Results_are_ordered_deterministically()
    {
        // Scoring walks this list, so its order must not depend on how the
        // endpoints were supplied.
        // Arrange
        var board = WithRow(EmptyBoard())
            .Place(new HexCoord(0, 1), new ConduitTile(ResourceKind.Crystal, EastWest));

        var crystalSpring = FlowEndpoint.Spring(new HexCoord(-1, 1), ResourceKind.Crystal);
        var crystalHub = FlowEndpoint.Hub(new HexCoord(1, 1), ResourceKind.Crystal);

        // Act
        var oneOrder = FlowResolver.FindCompletedRoutes(
            board, new[] { Spring(), Hub(), crystalSpring, crystalHub });
        var otherOrder = FlowResolver.FindCompletedRoutes(
            board, new[] { crystalHub, Hub(), crystalSpring, Spring() });

        // Assert
        Assert.Equal(
            oneOrder.Select(route => (route.Kind, route.Length, route.Tiles.First())),
            otherOrder.Select(route => (route.Kind, route.Length, route.Tiles.First())));
    }

    [Fact]
    public void An_endpoint_outside_the_grid_is_rejected()
    {
        // Arrange
        var stray = FlowEndpoint.Spring(new HexCoord(9, 9), ResourceKind.Water);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FlowResolver.FindCompletedRoutes(EmptyBoard(), new[] { stray, Hub() }));
    }

    [Fact]
    public void Two_endpoints_on_one_cell_are_rejected()
    {
        // A cell holds one feature. Two would mean the level data is wrong.
        // Arrange
        var spring = FlowEndpoint.Spring(HexCoord.Zero, ResourceKind.Water);
        var hub = FlowEndpoint.Hub(HexCoord.Zero, ResourceKind.Water);

        // Act / Assert
        Assert.Throws<ArgumentException>(
            () => FlowResolver.FindCompletedRoutes(EmptyBoard(), new[] { spring, hub }));
    }

    [Fact]
    public void A_conduit_placed_on_an_endpoint_cell_is_rejected()
    {
        // Endpoint cells are occupied by the feature, so placement must exclude
        // them. Surfacing it here catches a placement bug at the boundary rather
        // than letting the board score as though the feature were a conduit.
        // Arrange
        var board = EmptyBoard().Place(SpringCell, Straight());

        // Act / Assert
        Assert.Throws<ArgumentException>(
            () => FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() }));
    }

    [Fact]
    public void A_null_endpoint_collection_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => FlowResolver.FindCompletedRoutes(EmptyBoard(), null!));
    }

    [Fact]
    public void A_null_board_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => FlowResolver.FindCompletedRoutes(null!, new[] { Spring(), Hub() }));
    }
}
