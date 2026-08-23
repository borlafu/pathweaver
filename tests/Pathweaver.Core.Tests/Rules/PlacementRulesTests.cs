using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Rules;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Tests.Rules;

/// <summary>
/// A radius-2 hexagon with a water spring at (-2,0) and a water hub at (2,0).
/// </summary>
public class PlacementRulesTests
{
    // Clockwise on screen from due east, which is what HexMetrics guarantees by
    // negating the vertical axis. These were originally named for the textbook
    // counter-clockwise mapping, which made every geometry comment here misleading.
    private const int East = 0;
    private const int SouthEast = 1;
    private const int West = 3;
    private const int NorthWest = 4;

    private static readonly EdgeMask EastWest = EdgeMask.FromDirections(East, West);

    private static readonly FlowEndpoint Spring =
        FlowEndpoint.Spring(new HexCoord(-2, 0), ResourceKind.Water);

    private static readonly FlowEndpoint Hub =
        FlowEndpoint.Hub(new HexCoord(2, 0), ResourceKind.Water);

    private static FlowEndpoint[] Endpoints => new[] { Spring, Hub };

    private static HexGrid<ConduitTile> EmptyBoard() => HexGrid<ConduitTile>.Hexagon(2);

    private static ConduitTile Straight(ResourceKind kind = ResourceKind.Water)
        => new ConduitTile(kind, EastWest);

    [Fact]
    public void A_tile_beside_a_spring_and_open_towards_it_may_be_placed()
    {
        // Act / Assert
        Assert.True(PlacementRules.IsLegal(
            EmptyBoard(), Endpoints, new HexCoord(-1, 0), Straight()));
    }

    [Fact]
    public void A_tile_beside_a_spring_but_closed_towards_it_may_not()
    {
        // Arrange — open north-east and south-west, so nothing faces the spring
        var tile = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(SouthEast, NorthWest));

        // Act / Assert
        Assert.False(PlacementRules.IsLegal(EmptyBoard(), Endpoints, new HexCoord(-1, 0), tile));
    }

    [Fact]
    public void A_tile_touching_nothing_may_not_be_placed()
    {
        // The rule that gives deadlock meaning: a player cannot start a
        // disconnected island somewhere convenient and join it up later.
        // Act / Assert
        Assert.False(PlacementRules.IsLegal(
            EmptyBoard(), Endpoints, new HexCoord(0, 1), Straight()));
    }

    [Fact]
    public void A_tile_of_another_kind_may_not_join_a_spring()
    {
        // Act / Assert
        Assert.False(PlacementRules.IsLegal(
            EmptyBoard(), Endpoints, new HexCoord(-1, 0), Straight(ResourceKind.Crystal)));
    }

    [Fact]
    public void A_tile_extending_an_existing_conduit_may_be_placed()
    {
        // Arrange
        var board = EmptyBoard().Place(new HexCoord(-1, 0), Straight());

        // Act / Assert
        Assert.True(PlacementRules.IsLegal(board, Endpoints, HexCoord.Zero, Straight()));
    }

    [Fact]
    public void A_tile_beside_a_conduit_with_no_facing_edge_may_not_be_placed()
    {
        // Arrange — the neighbour runs east-west, the candidate runs north-east
        // to south-west, so their edges never meet
        var board = EmptyBoard().Place(new HexCoord(-1, 0), Straight());
        var tile = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(SouthEast, NorthWest));

        // Act / Assert
        Assert.False(PlacementRules.IsLegal(board, Endpoints, HexCoord.Zero, tile));
    }

    [Fact]
    public void An_occupied_cell_may_not_be_placed_on()
    {
        // Arrange
        var board = EmptyBoard().Place(new HexCoord(-1, 0), Straight());

        // Act / Assert
        Assert.False(PlacementRules.IsLegal(board, Endpoints, new HexCoord(-1, 0), Straight()));
    }

    [Fact]
    public void An_endpoint_cell_may_not_be_placed_on()
    {
        // Springs and hubs occupy their cells, so a conduit cannot share one.
        // Act / Assert
        Assert.False(PlacementRules.IsLegal(EmptyBoard(), Endpoints, Spring.Coordinate, Straight()));
        Assert.False(PlacementRules.IsLegal(EmptyBoard(), Endpoints, Hub.Coordinate, Straight()));
    }

    [Fact]
    public void A_cell_outside_the_board_is_rejected()
    {
        // Asking about a cell that does not exist is a caller bug, not a
        // placement that happens to be illegal.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PlacementRules.IsLegal(EmptyBoard(), Endpoints, new HexCoord(9, 9), Straight()));
    }

    [Fact]
    public void Legal_placements_on_an_empty_board_all_touch_an_endpoint()
    {
        // Act
        var placements = PlacementRules.LegalPlacements(EmptyBoard(), Endpoints, Straight());

        // Assert — only the cells adjacent to the spring or the hub qualify
        Assert.NotEmpty(placements);
        Assert.All(placements, placement =>
            Assert.True(
                placement.Coordinate.DistanceTo(Spring.Coordinate) == 1
                || placement.Coordinate.DistanceTo(Hub.Coordinate) == 1,
                $"{placement.Coordinate} touches neither endpoint."));
    }

    [Fact]
    public void Legal_placements_include_the_rotations_that_fit()
    {
        // Arrange — a bend, which only connects to the spring in some rotations
        var bend = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(East, SouthEast));

        // Act
        var placements = PlacementRules.LegalPlacements(EmptyBoard(), Endpoints, bend);

        // Assert
        Assert.All(placements, placement => Assert.InRange(placement.Rotation, 0, 5));
        Assert.Contains(placements, placement => placement.Rotation != 0);
    }

    [Fact]
    public void A_returned_placement_carries_the_rotated_tile()
    {
        // Arrange
        var bend = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(East, SouthEast));

        // Act
        var placements = PlacementRules.LegalPlacements(EmptyBoard(), Endpoints, bend);

        // Assert — the caller can place exactly what it was handed
        Assert.All(placements, placement =>
        {
            Assert.Equal(bend.RotateClockwise(placement.Rotation), placement.Tile);
            Assert.True(PlacementRules.IsLegal(
                EmptyBoard(), Endpoints, placement.Coordinate, placement.Tile));
        });
    }

    [Fact]
    public void Legal_placements_are_ordered_deterministically()
    {
        // Generation and the solver both walk this list.
        // Arrange
        var board = EmptyBoard().Place(new HexCoord(-1, 0), Straight());

        // Act
        var first = PlacementRules.LegalPlacements(board, Endpoints, Straight());
        var second = PlacementRules.LegalPlacements(board, Endpoints, Straight());

        // Assert
        Assert.Equal(
            first.Select(placement => (placement.Coordinate, placement.Rotation)),
            second.Select(placement => (placement.Coordinate, placement.Rotation)));
    }

    [Fact]
    public void A_full_board_offers_no_placements()
    {
        // Arrange — fill everything except the endpoint cells
        var board = EmptyBoard();
        foreach (var coordinate in board.Coordinates)
        {
            if (coordinate.Equals(Spring.Coordinate) || coordinate.Equals(Hub.Coordinate))
            {
                continue;
            }

            board = board.Place(coordinate, Straight());
        }

        // Act / Assert
        Assert.Empty(PlacementRules.LegalPlacements(board, Endpoints, Straight()));
    }

    [Fact]
    public void A_tile_with_no_matching_network_offers_no_placements()
    {
        // Arrange — a crystal tile on a board holding only water features
        // Act
        var placements = PlacementRules.LegalPlacements(
            EmptyBoard(), Endpoints, Straight(ResourceKind.Crystal));

        // Assert
        Assert.Empty(placements);
    }

    [Fact]
    public void A_null_board_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => PlacementRules.LegalPlacements(null!, Endpoints, Straight()));
    }

    [Fact]
    public void A_null_endpoint_collection_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => PlacementRules.LegalPlacements(EmptyBoard(), null!, Straight()));
    }
}
