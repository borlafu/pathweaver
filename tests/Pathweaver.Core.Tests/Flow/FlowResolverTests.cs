using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Tests.Flow;

public class FlowResolverTests
{
    private const int East = 0;
    private const int West = 3;

    private static readonly EdgeMask EastWest = EdgeMask.FromDirections(East, West);

    private static HexGrid<ConduitTile> EmptyBoard() => HexGrid<ConduitTile>.Hexagon(2);

    private static ConduitTile Straight(ResourceKind kind = ResourceKind.Water)
        => new ConduitTile(kind, EastWest);

    /// <summary>
    /// A spring feeding the west edge of (-2,0) and a hub drinking from the east
    /// edge of (2,0), so a full row along the q axis completes a route.
    /// </summary>
    private static FlowEndpoint Spring(ResourceKind kind = ResourceKind.Water)
        => FlowEndpoint.Spring(new HexCoord(-2, 0), West, kind);

    private static FlowEndpoint Hub(ResourceKind kind = ResourceKind.Water)
        => FlowEndpoint.Hub(new HexCoord(2, 0), East, kind);

    private static HexGrid<ConduitTile> WithRow(
        HexGrid<ConduitTile> board, ResourceKind kind = ResourceKind.Water, int fromQ = -2, int toQ = 2)
    {
        for (var q = fromQ; q <= toQ; q++)
        {
            board = board.Place(new HexCoord(q, 0), Straight(kind));
        }

        return board;
    }

    [Fact]
    public void An_empty_board_completes_no_routes()
    {
        // Act
        var routes = FlowResolver.FindCompletedRoutes(
            EmptyBoard(), new[] { Spring(), Hub() });

        // Assert
        Assert.Empty(routes);
    }

    [Fact]
    public void A_connected_row_completes_one_route()
    {
        // Arrange
        var board = WithRow(EmptyBoard());

        // Act
        var routes = FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() });

        // Assert
        var route = Assert.Single(routes);
        Assert.Equal(ResourceKind.Water, route.Kind);
        Assert.Equal(5, route.Length);
    }

    [Fact]
    public void A_completed_route_lists_its_tiles_from_spring_to_hub()
    {
        // Arrange
        var board = WithRow(EmptyBoard());

        // Act
        var route = Assert.Single(FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() }));

        // Assert
        Assert.Equal(
            new[]
            {
                new HexCoord(-2, 0),
                new HexCoord(-1, 0),
                new HexCoord(0, 0),
                new HexCoord(1, 0),
                new HexCoord(2, 0),
            },
            route.Tiles);
    }

    [Fact]
    public void A_gap_in_the_row_completes_nothing()
    {
        // Arrange — every cell but the middle
        var board = EmptyBoard()
            .Place(new HexCoord(-2, 0), Straight())
            .Place(new HexCoord(-1, 0), Straight())
            .Place(new HexCoord(1, 0), Straight())
            .Place(new HexCoord(2, 0), Straight());

        // Act / Assert
        Assert.Empty(FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() }));
    }

    [Fact]
    public void A_misaligned_tile_breaks_the_route()
    {
        // Arrange — the middle tile opens north-east and south-west instead
        var board = WithRow(EmptyBoard())
            .Remove(HexCoord.Zero)
            .Place(HexCoord.Zero, new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(1, 4)));

        // Act / Assert
        Assert.Empty(FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() }));
    }

    [Fact]
    public void A_route_of_the_wrong_resource_kind_completes_nothing()
    {
        // PRD section 3.1: each resource flows from its own springs to its own
        // hubs, so a crystal conduit cannot serve a water spring.
        // Arrange
        var board = WithRow(EmptyBoard(), ResourceKind.Crystal);

        // Act / Assert
        Assert.Empty(FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() }));
    }

    [Fact]
    public void A_spring_needs_the_first_tile_open_towards_it()
    {
        // Arrange — the end tile faces north-east and east, not west
        var board = WithRow(EmptyBoard())
            .Remove(new HexCoord(-2, 0))
            .Place(new HexCoord(-2, 0), new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(East, 1)));

        // Act / Assert
        Assert.Empty(FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() }));
    }

    [Fact]
    public void A_hub_needs_the_last_tile_open_towards_it()
    {
        // Arrange
        var board = WithRow(EmptyBoard())
            .Remove(new HexCoord(2, 0))
            .Place(new HexCoord(2, 0), new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(West, 4)));

        // Act / Assert
        Assert.Empty(FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() }));
    }

    [Fact]
    public void A_spring_serving_two_hubs_completes_two_routes()
    {
        // Arrange — a row plus a branch upward from the origin
        var board = WithRow(EmptyBoard(), ResourceKind.Water, -2, 1)
            .Remove(HexCoord.Zero)
            .Place(HexCoord.Zero, new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(East, West, 4)))
            .Place(new HexCoord(0, -1), new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(1, 4)));

        var secondHub = FlowEndpoint.Hub(new HexCoord(0, -1), 1, ResourceKind.Water);
        var firstHub = FlowEndpoint.Hub(new HexCoord(1, 0), East, ResourceKind.Water);

        // Act
        var routes = FlowResolver.FindCompletedRoutes(board, new[] { Spring(), firstHub, secondHub });

        // Assert
        Assert.Equal(2, routes.Count);
        Assert.All(routes, route => Assert.Equal(ResourceKind.Water, route.Kind));
    }

    [Fact]
    public void Each_spring_and_hub_pair_completes_at_most_one_route()
    {
        // Arrange
        var board = WithRow(EmptyBoard());

        // Act
        var routes = FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() });

        // Assert — a network with several paths still scores once per pair
        Assert.Single(routes);
    }

    [Fact]
    public void A_dead_end_branch_does_not_lengthen_the_route()
    {
        // Arrange — the row, plus a spur hanging off the origin that leads nowhere
        var board = WithRow(EmptyBoard())
            .Remove(HexCoord.Zero)
            .Place(HexCoord.Zero, new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(East, West, 4)))
            .Place(new HexCoord(0, -1), new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(1, 4)));

        // Act
        var route = Assert.Single(FlowResolver.FindCompletedRoutes(board, new[] { Spring(), Hub() }));

        // Assert — the spur is connected, but it is not on the path
        Assert.Equal(5, route.Length);
        Assert.DoesNotContain(new HexCoord(0, -1), route.Tiles);
    }

    [Fact]
    public void Only_endpoints_of_matching_kind_pair_up()
    {
        // Arrange
        var board = WithRow(EmptyBoard());

        // Act — a crystal hub cannot receive from a water spring
        var routes = FlowResolver.FindCompletedRoutes(
            board, new[] { Spring(), Hub(ResourceKind.Crystal) });

        // Assert
        Assert.Empty(routes);
    }

    [Fact]
    public void Two_springs_and_two_hubs_of_different_kinds_stay_separate()
    {
        // Arrange — a water row along q, and a crystal pair below it
        var board = WithRow(EmptyBoard())
            .Place(new HexCoord(-1, 1), new ConduitTile(ResourceKind.Crystal, EastWest))
            .Place(new HexCoord(0, 1), new ConduitTile(ResourceKind.Crystal, EastWest));

        var crystalSpring = FlowEndpoint.Spring(new HexCoord(-1, 1), West, ResourceKind.Crystal);
        var crystalHub = FlowEndpoint.Hub(new HexCoord(0, 1), East, ResourceKind.Crystal);

        // Act
        var routes = FlowResolver.FindCompletedRoutes(
            board, new[] { Spring(), Hub(), crystalSpring, crystalHub });

        // Assert
        Assert.Equal(2, routes.Count);
        Assert.Contains(routes, route => route.Kind == ResourceKind.Water && route.Length == 5);
        Assert.Contains(routes, route => route.Kind == ResourceKind.Crystal && route.Length == 2);
    }

    [Fact]
    public void A_spring_and_hub_on_the_same_cell_completes_a_single_tile_route()
    {
        // Arrange — the shortest legal route: one tile serving both endpoints
        var board = EmptyBoard().Place(HexCoord.Zero, Straight());
        var spring = FlowEndpoint.Spring(HexCoord.Zero, West, ResourceKind.Water);
        var hub = FlowEndpoint.Hub(HexCoord.Zero, East, ResourceKind.Water);

        // Act
        var route = Assert.Single(FlowResolver.FindCompletedRoutes(board, new[] { spring, hub }));

        // Assert
        Assert.Equal(1, route.Length);
    }

    [Fact]
    public void Results_are_ordered_deterministically()
    {
        // Generation and scoring both walk this list, so its order must not
        // depend on how the endpoints were supplied.
        // Arrange
        var board = WithRow(EmptyBoard())
            .Place(new HexCoord(-1, 1), new ConduitTile(ResourceKind.Crystal, EastWest))
            .Place(new HexCoord(0, 1), new ConduitTile(ResourceKind.Crystal, EastWest));

        var crystalSpring = FlowEndpoint.Spring(new HexCoord(-1, 1), West, ResourceKind.Crystal);
        var crystalHub = FlowEndpoint.Hub(new HexCoord(0, 1), East, ResourceKind.Crystal);

        // Act
        var oneOrder = FlowResolver.FindCompletedRoutes(
            board, new[] { Spring(), Hub(), crystalSpring, crystalHub });
        var otherOrder = FlowResolver.FindCompletedRoutes(
            board, new[] { crystalHub, Hub(), crystalSpring, Spring() });

        // Assert
        Assert.Equal(
            oneOrder.Select(route => (route.Kind, route.Length)),
            otherOrder.Select(route => (route.Kind, route.Length)));
    }

    [Fact]
    public void An_endpoint_outside_the_grid_is_rejected()
    {
        // Arrange
        var board = EmptyBoard();
        var stray = FlowEndpoint.Spring(new HexCoord(9, 9), West, ResourceKind.Water);

        // Act / Assert — bad level data, not a board with no routes
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FlowResolver.FindCompletedRoutes(board, new[] { stray, Hub() }));
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
