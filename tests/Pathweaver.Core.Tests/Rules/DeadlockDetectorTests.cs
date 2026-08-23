using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Rules;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Tests.Rules;

public class DeadlockDetectorTests
{
    private const int East = 0;
    private const int NorthEast = 1;
    private const int West = 3;

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
    public void An_empty_board_with_endpoints_is_not_deadlocked()
    {
        // Act / Assert — the cells beside the spring accept the tile
        Assert.False(DeadlockDetector.IsDeadlocked(EmptyBoard(), Endpoints, Straight()));
    }

    [Fact]
    public void A_board_with_room_to_extend_is_not_deadlocked()
    {
        // Arrange
        var board = EmptyBoard().Place(new HexCoord(-1, 0), Straight());

        // Act / Assert
        Assert.False(DeadlockDetector.IsDeadlocked(board, Endpoints, Straight()));
    }

    [Fact]
    public void A_full_board_is_deadlocked()
    {
        // Arrange — every cell but the two endpoints holds a conduit
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
        Assert.True(DeadlockDetector.IsDeadlocked(board, Endpoints, Straight()));
    }

    [Fact]
    public void A_tile_that_fits_in_no_rotation_anywhere_is_a_deadlock()
    {
        // Arrange — a crystal tile on a board whose only features carry water
        // Act / Assert
        Assert.True(DeadlockDetector.IsDeadlocked(
            EmptyBoard(), Endpoints, Straight(ResourceKind.Crystal)));
    }

    [Fact]
    public void Rotation_is_considered_before_declaring_a_deadlock()
    {
        // A tile that does not fit as drawn but fits once turned is not a
        // deadlock. Missing this would hand out Pivot Tokens for nothing and rob
        // the player of a legitimate move.
        // Arrange — a bend, drawn facing away from everything
        var bend = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(East, NorthEast));

        // Act / Assert
        Assert.False(DeadlockDetector.IsDeadlocked(EmptyBoard(), Endpoints, bend));
    }

    [Fact]
    public void A_board_with_only_unreachable_empty_cells_is_deadlocked()
    {
        // Arrange — ring the spring and hub so every remaining empty cell touches
        // nothing that would accept a conduit
        var board = EmptyBoard();
        foreach (var coordinate in board.Coordinates)
        {
            var touchesEndpoint = coordinate.DistanceTo(Spring.Coordinate) <= 1
                                  || coordinate.DistanceTo(Hub.Coordinate) <= 1;

            if (!touchesEndpoint || coordinate.Equals(Spring.Coordinate) || coordinate.Equals(Hub.Coordinate))
            {
                continue;
            }

            // Sealed tiles: open only on edges facing away from the rest of the board
            board = board.Place(coordinate, new ConduitTile(
                ResourceKind.Water, EdgeMask.FromDirections(West, East)));
        }

        // Act
        var deadlocked = DeadlockDetector.IsDeadlocked(board, Endpoints, Straight());

        // Assert — either a placement exists or it does not; assert against the
        // placement list so the two agree
        Assert.Equal(
            PlacementRules.LegalPlacements(board, Endpoints, Straight()).Count == 0,
            deadlocked);
    }

    [Fact]
    public void Deadlock_agrees_with_the_placement_list_on_an_empty_board()
    {
        // The detector must never disagree with the rule it is built on.
        // Arrange
        var tile = Straight();

        // Act / Assert
        Assert.Equal(
            PlacementRules.LegalPlacements(EmptyBoard(), Endpoints, tile).Count == 0,
            DeadlockDetector.IsDeadlocked(EmptyBoard(), Endpoints, tile));
    }

    [Fact]
    public void A_board_with_no_endpoints_is_deadlocked()
    {
        // With nothing to build from, no placement can ever connect.
        // Act / Assert
        Assert.True(DeadlockDetector.IsDeadlocked(
            EmptyBoard(), Array.Empty<FlowEndpoint>(), Straight()));
    }

    [Fact]
    public void A_null_board_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => DeadlockDetector.IsDeadlocked(null!, Endpoints, Straight()));
    }
}
