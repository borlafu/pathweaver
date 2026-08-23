using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Rules;
using Pathweaver.Core.State;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Tests.State;

public class GameStateTests
{
    [Fact]
    public void A_new_game_starts_with_a_tile_in_hand()
    {
        // The PRD loop begins at "draw and inspect", so a game is never waiting
        // for the player to ask for a tile.
        // Act
        var state = GameFixture.NewGame();

        // Assert
        Assert.Equal(GameFixture.Straight(), state.HeldTile);
    }

    [Fact]
    public void A_new_game_starts_empty_and_unscored()
    {
        // Act
        var state = GameFixture.NewGame();

        // Assert
        Assert.Equal(0, state.Board.OccupiedCount);
        Assert.Equal(0, state.Score);
        Assert.Equal(0, state.PivotTokens.Count);
        Assert.Empty(state.CompletedRoutes);
    }

    [Fact]
    public void A_new_game_can_start_with_tokens_granted()
    {
        // Levels may hand out a token up front as a tutorial affordance.
        Assert.Equal(2, GameFixture.NewGame(startingTokens: 2).PivotTokens.Count);
    }

    [Fact]
    public void The_held_tile_has_somewhere_to_go_at_the_start()
    {
        // Act
        var state = GameFixture.NewGame();

        // Assert
        Assert.NotEmpty(state.LegalPlacements);
        Assert.False(state.IsDeadlocked);
    }

    [Fact]
    public void Legal_placements_only_touch_the_endpoints_at_the_start()
    {
        // Act
        var placements = GameFixture.NewGame().LegalPlacements;

        // Assert
        Assert.All(placements, placement =>
            Assert.True(
                placement.Coordinate.DistanceTo(GameFixture.SpringCell) == 1
                || placement.Coordinate.DistanceTo(GameFixture.HubCell) == 1,
                $"{placement.Coordinate} touches neither endpoint."));
    }

    [Fact]
    public void An_endpoint_outside_the_board_is_rejected()
    {
        // Arrange
        var endpoints = new[]
        {
            FlowEndpoint.Spring(new HexCoord(99, 0), ResourceKind.Water),
            FlowEndpoint.Hub(GameFixture.HubCell, ResourceKind.Water),
        };

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => GameState.Create(
            HexGrid<ConduitTile>.Hexagon(3),
            endpoints,
            GameFixture.StraightBag(),
            GameFixture.BaseRouteScore,
            TokenPool.Empty,
            TokenPool.Empty));
    }

    [Fact]
    public void Two_endpoints_on_one_cell_are_rejected()
    {
        // Arrange
        var endpoints = new[]
        {
            FlowEndpoint.Spring(GameFixture.SpringCell, ResourceKind.Water),
            FlowEndpoint.Hub(GameFixture.SpringCell, ResourceKind.Water),
        };

        // Act / Assert
        Assert.Throws<ArgumentException>(() => GameState.Create(
            HexGrid<ConduitTile>.Hexagon(3),
            endpoints,
            GameFixture.StraightBag(),
            GameFixture.BaseRouteScore,
            TokenPool.Empty,
            TokenPool.Empty));
    }

    [Fact]
    public void A_game_with_no_endpoints_is_rejected()
    {
        // A board with nothing to connect is not a level.
        Assert.Throws<ArgumentException>(() => GameState.Create(
            HexGrid<ConduitTile>.Hexagon(3),
            Array.Empty<FlowEndpoint>(),
            GameFixture.StraightBag(),
            GameFixture.BaseRouteScore,
            TokenPool.Empty,
            TokenPool.Empty));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_base_route_score_below_one_is_rejected(long baseScore)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GameState.Create(
            HexGrid<ConduitTile>.Hexagon(3),
            GameFixture.Endpoints,
            GameFixture.StraightBag(),
            baseScore,
            TokenPool.Empty,
            TokenPool.Empty));
    }

    [Fact]
    public void A_null_bag_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => GameState.Create(
            HexGrid<ConduitTile>.Hexagon(3),
            GameFixture.Endpoints,
            null!,
            GameFixture.BaseRouteScore,
            TokenPool.Empty,
            TokenPool.Empty));
    }

    [Fact]
    public void A_null_board_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => GameState.Create(
            null!,
            GameFixture.Endpoints,
            GameFixture.StraightBag(),
            GameFixture.BaseRouteScore,
            TokenPool.Empty,
            TokenPool.Empty));
    }
}
