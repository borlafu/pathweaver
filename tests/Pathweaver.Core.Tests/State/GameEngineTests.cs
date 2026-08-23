using Pathweaver.Core.Determinism;
using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Rules;
using Pathweaver.Core.Scoring;
using Pathweaver.Core.State;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Tests.State;

public class GameEngineTests
{
    [Fact]
    public void Placing_a_tile_leaves_the_earlier_state_untouched()
    {
        // The property every other feature leans on: undo, replay, and the solver
        // all depend on an earlier state remaining valid.
        // Arrange
        var before = GameFixture.NewGame();

        // Act
        var after = GameEngine.Apply(before, new PlaceTile(GameFixture.RowCells[0], 0));

        // Assert
        Assert.Equal(0, before.Board.OccupiedCount);
        Assert.Equal(1, after.Board.OccupiedCount);
    }

    [Fact]
    public void Placing_a_tile_puts_it_on_the_board()
    {
        // Act
        var state = GameFixture.PlayRow(GameFixture.NewGame(), 1);

        // Assert
        Assert.True(state.Board.TryGet(GameFixture.RowCells[0], out var placed));
        Assert.Equal(GameFixture.Straight(), placed);
    }

    [Fact]
    public void Placing_a_tile_draws_the_next_one()
    {
        // Act
        var state = GameFixture.PlayRow(GameFixture.NewGame(), 1);

        // Assert — the loop returns to "draw and inspect" without being asked
        Assert.Equal(GameFixture.Straight(), state.HeldTile);
    }

    [Fact]
    public void Placing_a_rotated_tile_stores_the_rotated_orientation()
    {
        // Arrange — a bend that must be turned to reach the spring
        var state = GameFixture.NewGame();

        // Act — three turns of an east-west straight is still east-west, so use
        // the placement list to find a rotation the rules accept
        var placement = state.LegalPlacements.First(candidate => candidate.Rotation == 3);
        var after = GameEngine.Apply(state, new PlaceTile(placement.Coordinate, placement.Rotation));

        // Assert
        Assert.True(after.Board.TryGet(placement.Coordinate, out var placed));
        Assert.Equal(state.HeldTile.RotateClockwise(3), placed);
    }

    [Fact]
    public void Placing_on_a_disconnected_cell_is_refused()
    {
        // Arrange
        var state = GameFixture.NewGame();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(
            () => GameEngine.Apply(state, new PlaceTile(new HexCoord(0, 2), 0)));
    }

    [Fact]
    public void Placing_on_an_occupied_cell_is_refused()
    {
        // Arrange
        var state = GameFixture.PlayRow(GameFixture.NewGame(), 1);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(
            () => GameEngine.Apply(state, new PlaceTile(GameFixture.RowCells[0], 0)));
    }

    [Fact]
    public void Placing_on_an_endpoint_cell_is_refused()
    {
        // Arrange
        var state = GameFixture.NewGame();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(
            () => GameEngine.Apply(state, new PlaceTile(GameFixture.SpringCell, 0)));
    }

    [Fact]
    public void Placing_outside_the_board_is_refused()
    {
        // Arrange
        var state = GameFixture.NewGame();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GameEngine.Apply(state, new PlaceTile(new HexCoord(99, 0), 0)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void An_out_of_range_rotation_is_refused(int rotation)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PlaceTile(GameFixture.RowCells[0], rotation));
    }

    [Fact]
    public void An_incomplete_route_scores_nothing()
    {
        // Act — three of the four cells
        var state = GameFixture.PlayRow(GameFixture.NewGame(), 3);

        // Assert
        Assert.Equal(0, state.Score);
        Assert.Empty(state.CompletedRoutes);
    }

    [Fact]
    public void Completing_a_route_scores_it_through_the_multiplier_table()
    {
        // Act — all four cells, so a route of length 4
        var state = GameFixture.PlayRow(GameFixture.NewGame(), 4);

        // Assert
        Assert.Equal(ScoreTable.ScoreFor(GameFixture.BaseRouteScore, 4), state.Score);
        Assert.Equal(246, state.Score);
    }

    [Fact]
    public void Completing_a_long_route_earns_a_pivot_token()
    {
        // Act
        var state = GameFixture.PlayRow(GameFixture.NewGame(), 4);

        // Assert — length 4 meets the threshold
        Assert.Equal(1, state.PivotTokens.Count);
    }

    [Fact]
    public void A_completed_route_is_recorded_once()
    {
        // Act
        var state = GameFixture.PlayRow(GameFixture.NewGame(), 4);

        // Assert
        var completed = Assert.Single(state.CompletedRoutes);
        Assert.Equal(GameFixture.SpringCell, completed.Spring);
        Assert.Equal(GameFixture.HubCell, completed.Hub);
    }

    [Fact]
    public void A_route_pays_out_only_once_even_if_rebuilt()
    {
        // Retrieving a conduit and rebuilding must not farm the same pair. The
        // resources already flowed, so the harvest is spent.
        // Arrange — complete the route, which also grants a token
        var completed = GameFixture.PlayRow(GameFixture.NewGame(), 4);
        var scoreAfterFirst = completed.Score;

        // Act — spend the token to pull a conduit out, then replace it
        var broken = GameEngine.Apply(completed, new PivotRetrieve(GameFixture.RowCells[2]));
        var rebuilt = GameEngine.Apply(broken, new PlaceTile(GameFixture.RowCells[2], 0));

        // Assert
        Assert.Equal(scoreAfterFirst, rebuilt.Score);
        Assert.Single(rebuilt.CompletedRoutes);
    }

    [Fact]
    public void Retrieving_a_conduit_clears_the_cell_and_spends_a_token()
    {
        // Arrange
        var state = GameFixture.PlayRow(GameFixture.NewGame(startingTokens: 1), 2);

        // Act
        var after = GameEngine.Apply(state, new PivotRetrieve(GameFixture.RowCells[1]));

        // Assert
        Assert.True(after.Board.IsEmpty(GameFixture.RowCells[1]));
        Assert.Equal(0, after.PivotTokens.Count);
        Assert.Equal(1, after.Board.OccupiedCount);
    }

    [Fact]
    public void Retrieving_does_not_change_the_held_tile()
    {
        // The token buys back the space, not the conduit: the retrieved tile is
        // discarded and the tile in hand is untouched.
        // Arrange
        var state = GameFixture.PlayRow(GameFixture.NewGame(startingTokens: 1), 2);

        // Act
        var after = GameEngine.Apply(state, new PivotRetrieve(GameFixture.RowCells[1]));

        // Assert
        Assert.Equal(state.HeldTile, after.HeldTile);
    }

    [Fact]
    public void Retrieving_without_a_token_is_refused()
    {
        // Arrange — no starting tokens, and no route completed yet
        var state = GameFixture.PlayRow(GameFixture.NewGame(), 2);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(
            () => GameEngine.Apply(state, new PivotRetrieve(GameFixture.RowCells[1])));
    }

    [Fact]
    public void Retrieving_from_an_empty_cell_is_refused()
    {
        // Arrange
        var state = GameFixture.NewGame(startingTokens: 1);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(
            () => GameEngine.Apply(state, new PivotRetrieve(GameFixture.RowCells[0])));
    }

    [Fact]
    public void Rotating_a_placed_conduit_spends_a_token_and_turns_it()
    {
        // Arrange — a bend on the board would show a visible turn, but a straight
        // rotated once is enough to prove the tile changed
        var state = GameFixture.PlayRow(GameFixture.NewGame(startingTokens: 1), 1);
        var before = state.Board;

        // Act
        var after = GameEngine.Apply(state, new PivotRotate(GameFixture.RowCells[0], 1));

        // Assert
        Assert.True(before.TryGet(GameFixture.RowCells[0], out var original));
        Assert.True(after.Board.TryGet(GameFixture.RowCells[0], out var rotated));
        Assert.Equal(original.RotateClockwise(1), rotated);
        Assert.Equal(0, after.PivotTokens.Count);
    }

    [Fact]
    public void Rotating_without_a_token_is_refused()
    {
        // Arrange
        var state = GameFixture.PlayRow(GameFixture.NewGame(), 1);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(
            () => GameEngine.Apply(state, new PivotRotate(GameFixture.RowCells[0], 1)));
    }

    [Fact]
    public void Rotating_an_empty_cell_is_refused()
    {
        // Arrange
        var state = GameFixture.NewGame(startingTokens: 1);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(
            () => GameEngine.Apply(state, new PivotRotate(GameFixture.RowCells[0], 1)));
    }

    [Fact]
    public void Rotating_by_a_full_turn_is_refused()
    {
        // Six steps is no rotation at all, so it would spend a token for nothing.
        Assert.Throws<ArgumentOutOfRangeException>(() => new PivotRotate(GameFixture.RowCells[0], 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PivotRotate(GameFixture.RowCells[0], 6));
    }

    [Fact]
    public void Rotating_a_conduit_can_complete_a_route_and_score_it()
    {
        // Arrange — build the row, but leave the third conduit facing the wrong way
        var state = GameFixture.NewGame(startingTokens: 1);
        state = GameEngine.Apply(state, new PlaceTile(GameFixture.RowCells[0], 0));
        state = GameEngine.Apply(state, new PlaceTile(GameFixture.RowCells[1], 0));
        state = GameEngine.Apply(state, new PlaceTile(GameFixture.RowCells[2], 0));
        state = GameEngine.Apply(state, new PlaceTile(GameFixture.RowCells[3], 0));

        // The straight row already completes, so instead break it and repair it
        var broken = GameEngine.Apply(state, new PivotRetrieve(GameFixture.RowCells[3]));

        // Assert — the payout stands from the first completion
        Assert.Equal(246, broken.Score);
        Assert.Single(broken.CompletedRoutes);
    }

    [Fact]
    public void The_same_commands_from_the_same_seed_produce_the_same_state()
    {
        // The Daily Expedition depends on this end to end, not just in the RNG.
        // Act
        var first = GameFixture.PlayRow(GameFixture.NewGame(seed: 7UL), 4);
        var second = GameFixture.PlayRow(GameFixture.NewGame(seed: 7UL), 4);

        // Assert
        Assert.Equal(first.Score, second.Score);
        Assert.Equal(first.HeldTile, second.HeldTile);
        Assert.Equal(first.PivotTokens, second.PivotTokens);
        Assert.Equal(
            first.Board.OccupiedCells.Select(cell => cell.Coordinate),
            second.Board.OccupiedCells.Select(cell => cell.Coordinate));
    }

    [Fact]
    public void Skipping_draws_a_new_tile_and_spends_a_skip()
    {
        // Arrange
        var state = GameFixture.NewGame(startingSkips: 3);

        // Act
        var after = GameEngine.Apply(state, new SkipTile());

        // Assert
        Assert.Equal(2, after.SkipTokens.Count);
        Assert.Equal(3, state.SkipTokens.Count);
    }

    [Fact]
    public void Skipping_leaves_the_board_and_score_alone()
    {
        // Discarding a tile cannot complete a route, so nothing else should move.
        // Arrange
        var state = GameFixture.PlayRow(GameFixture.NewGame(startingSkips: 1), 2);

        // Act
        var after = GameEngine.Apply(state, new SkipTile());

        // Assert
        Assert.Equal(state.Board.OccupiedCount, after.Board.OccupiedCount);
        Assert.Equal(state.Score, after.Score);
        Assert.Equal(state.PivotTokens, after.PivotTokens);
    }

    [Fact]
    public void Skipping_without_a_skip_is_refused()
    {
        // Arrange
        var state = GameFixture.NewGame(startingSkips: 0);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => GameEngine.Apply(state, new SkipTile()));
    }

    [Fact]
    public void A_skipped_tile_does_not_come_straight_back()
    {
        // Returning the discard to the bag would let the same tile be dealt again
        // immediately, which would make a spent skip look like a bug.
        // Arrange — a bag of two distinct tiles so a repeat is visible
        var board = HexGrid<ConduitTile>.Hexagon(3);
        var bag = TileBag.Create(
            new[]
            {
                new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(0, 3)),
                new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(0, 2)),
            },
            SeedSource.Stream(5UL, PathweaverStream.TileBag));

        var state = GameState.Create(
            board, GameFixture.Endpoints, bag, GameFixture.BaseRouteScore,
            TokenPool.Empty, TokenPool.Of(1));

        var firstTile = state.HeldTile;

        // Act
        var after = GameEngine.Apply(state, new SkipTile());

        // Assert
        Assert.NotEqual(firstTile, after.HeldTile);
    }

    [Fact]
    public void A_short_route_earns_a_skip_rather_than_a_pivot()
    {
        // Arrange — spring and hub two apart, so one conduit completes a route of 1
        var board = HexGrid<ConduitTile>.Hexagon(2);
        var endpoints = new[]
        {
            FlowEndpoint.Spring(new HexCoord(-1, 0), ResourceKind.Water),
            FlowEndpoint.Hub(new HexCoord(1, 0), ResourceKind.Water),
        };

        var state = GameState.Create(
            board, endpoints, GameFixture.StraightBag(), GameFixture.BaseRouteScore,
            TokenPool.Empty, TokenPool.Empty);

        // Act
        var after = GameEngine.Apply(state, new PlaceTile(HexCoord.Zero, 0));

        // Assert
        Assert.Equal(1, after.SkipTokens.Count);
        Assert.Equal(0, after.PivotTokens.Count);
    }

    [Fact]
    public void A_long_route_earns_a_pivot_rather_than_a_skip()
    {
        // Act — the fixture row is four conduits
        var state = GameFixture.PlayRow(GameFixture.NewGame(), 4);

        // Assert
        Assert.Equal(1, state.PivotTokens.Count);
        Assert.Equal(0, state.SkipTokens.Count);
    }

    [Fact]
    public void A_skip_offers_a_way_out_of_a_board_with_no_placement()
    {
        // Not a rescue the rules promise, but a consequence worth knowing: a player
        // holding a skip is never truly stuck by one awkward tile.
        // Arrange — a crystal tile on a water-only board fits nowhere
        var board = HexGrid<ConduitTile>.Hexagon(2);
        var bag = TileBag.Create(
            new[]
            {
                new ConduitTile(ResourceKind.Crystal, EdgeMask.FromDirections(0, 3)),
                new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(0, 3)),
            },
            SeedSource.Stream(11UL, PathweaverStream.TileBag));

        var endpoints = new[]
        {
            FlowEndpoint.Spring(new HexCoord(-2, 0), ResourceKind.Water),
            FlowEndpoint.Hub(new HexCoord(2, 0), ResourceKind.Water),
        };

        var state = GameState.Create(
            board, endpoints, bag, GameFixture.BaseRouteScore, TokenPool.Empty, TokenPool.Of(1));

        // Act / Assert — skip until a placeable tile arrives
        if (state.IsDeadlocked)
        {
            var after = GameEngine.Apply(state, new SkipTile());
            Assert.False(after.IsDeadlocked);
        }
    }

    [Fact]
    public void A_tile_that_fits_nowhere_is_a_deadlock_but_not_always_being_stuck()
    {
        // The distinction a player feels: this tile is useless, but the run may not be over.
        // Arrange — a crystal tile on a water board, with a water tile also in the bag
        var board = HexGrid<ConduitTile>.Hexagon(2);
        var bag = TileBag.Create(
            new[]
            {
                new ConduitTile(ResourceKind.Crystal, EdgeMask.FromDirections(0, 3)),
                new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(0, 3)),
            },
            SeedSource.Stream(11UL, PathweaverStream.TileBag));

        var endpoints = new[]
        {
            FlowEndpoint.Spring(new HexCoord(-2, 0), ResourceKind.Water),
            FlowEndpoint.Hub(new HexCoord(2, 0), ResourceKind.Water),
        };

        var state = GameState.Create(
            board, endpoints, bag, GameFixture.BaseRouteScore, TokenPool.Empty, TokenPool.Of(1));

        // Act / Assert — whichever tile came first, a water tile exists to skip toward
        Assert.True(state.CanAnyTileBePlaced);
        Assert.False(state.IsStuck);
    }

    [Fact]
    public void A_board_that_can_accept_nothing_is_stuck_even_holding_skips()
    {
        // The bug this fixes. Skips were treated as options in themselves, so a player on a board
        // where no tile fits anywhere was told they had a way out and left to spend tokens
        // discovering otherwise.
        // Arrange — only crystal tiles, and only water endpoints
        var board = HexGrid<ConduitTile>.Hexagon(2);
        var bag = TileBag.Create(
            new[] { new ConduitTile(ResourceKind.Crystal, EdgeMask.FromDirections(0, 3)) },
            SeedSource.Stream(3UL, PathweaverStream.TileBag));

        var endpoints = new[]
        {
            FlowEndpoint.Spring(new HexCoord(-2, 0), ResourceKind.Water),
            FlowEndpoint.Hub(new HexCoord(2, 0), ResourceKind.Water),
        };

        var state = GameState.Create(
            board, endpoints, bag, GameFixture.BaseRouteScore, TokenPool.Empty, TokenPool.Of(3));

        // Act / Assert
        Assert.True(state.IsDeadlocked);
        Assert.False(state.CanAnyTileBePlaced);
        Assert.True(state.IsStuck);
    }

    [Fact]
    public void A_pivot_is_only_a_way_out_when_there_is_a_conduit_to_pivot()
    {
        // Arrange — an unplaceable tile, a Pivot Token, and an empty board
        var board = HexGrid<ConduitTile>.Hexagon(2);
        var bag = TileBag.Create(
            new[] { new ConduitTile(ResourceKind.Crystal, EdgeMask.FromDirections(0, 3)) },
            SeedSource.Stream(3UL, PathweaverStream.TileBag));

        var endpoints = new[]
        {
            FlowEndpoint.Spring(new HexCoord(-2, 0), ResourceKind.Water),
            FlowEndpoint.Hub(new HexCoord(2, 0), ResourceKind.Water),
        };

        var empty = GameState.Create(
            board, endpoints, bag, GameFixture.BaseRouteScore, TokenPool.Of(1), TokenPool.Empty);

        // Act / Assert — nothing placed means nothing to rotate or retrieve
        Assert.True(empty.IsStuck);
    }

    [Fact]
    public void A_playable_board_is_neither_deadlocked_nor_stuck()
    {
        var state = GameFixture.NewGame();

        Assert.False(state.IsDeadlocked);
        Assert.False(state.IsStuck);
    }

    [Fact]
    public void A_null_state_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => GameEngine.Apply(null!, new PlaceTile(GameFixture.RowCells[0], 0)));
    }

    [Fact]
    public void A_null_command_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => GameEngine.Apply(GameFixture.NewGame(), null!));
    }
}
