using System;
using System.Collections.Generic;
using System.Linq;
using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Rules;
using Pathweaver.Core.Scoring;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.State
{
    /// <summary>
    /// Applies a command to a game, producing the next state.
    /// </summary>
    /// <remarks>
    /// The engine is the only thing that advances a game, and it never modifies
    /// what it is given. An illegal command throws rather than returning the state
    /// unchanged: a silent refusal would let an interface believe a move happened
    /// and leave the two out of step.
    /// </remarks>
    public static class GameEngine
    {
        public static GameState Apply(GameState state, GameCommand command)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            switch (command)
            {
                case PlaceTile place:
                    return ApplyPlace(state, place);
                case PivotRetrieve retrieve:
                    return ApplyRetrieve(state, retrieve);
                case SkipTile _:
                    return ApplySkip(state);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(command), command, "Unknown command.");
            }
        }

        private static GameState ApplyPlace(GameState state, PlaceTile command)
        {
            var tile = state.HeldTile.RotateClockwise(command.Rotation);

            // IsLegal throws for a cell off the board, which is a caller mistake
            // rather than a refused move.
            if (!PlacementRules.IsLegal(state.Board, state.Endpoints, command.At, tile))
            {
                throw new InvalidOperationException(
                    $"{tile} cannot be placed at {command.At}: it must join a matching conduit or endpoint on an empty cell.");
            }

            var board = state.Board.Place(command.At, tile);
            var (bag, nextTile) = state.Bag.Draw();

            return Harvest(
                state, board, bag: bag, heldTile: nextTile,
                pivotTokens: state.PivotTokens, skipTokens: state.SkipTokens);
        }

        private static GameState ApplySkip(GameState state)
        {
            if (!state.SkipTokens.CanSpend)
            {
                throw new InvalidOperationException("A skip is required to discard the held tile.");
            }

            var (bag, nextTile) = state.Bag.Draw();

            // No harvest pass: discarding a tile cannot complete a route, so running one
            // would only re-examine a board that has not changed.
            return state.With(
                bag: bag,
                heldTile: nextTile,
                skipTokens: state.SkipTokens.Spend());
        }

        private static GameState ApplyRetrieve(GameState state, PivotRetrieve command)
        {
            RequireCellOnBoard(state, command.At);

            if (state.Board.IsEmpty(command.At))
            {
                throw new InvalidOperationException($"No conduit at {command.At} to retrieve.");
            }

            if (!state.PivotTokens.CanSpend)
            {
                throw new InvalidOperationException("A Pivot Token is required to retrieve a placed conduit.");
            }

            // The conduit is discarded: the token buys back the space, not the tile,
            // so the held tile is untouched.
            var board = state.Board.Remove(command.At);

            return Harvest(
                state, board, bag: state.Bag, heldTile: state.HeldTile,
                pivotTokens: state.PivotTokens.Spend(), skipTokens: state.SkipTokens);
        }

        /// <summary>
        /// Pays out any route completed for the first time, and grants the Pivot
        /// Tokens those routes earn.
        /// </summary>
        /// <remarks>
        /// <see cref="FlowResolver"/> reports every route currently complete, so the
        /// state's record of paid-out pairs is what makes a payout happen once. A
        /// player who retrieves a conduit and puts it back is not paid twice: the
        /// resources already flowed, and the harvest is spent.
        /// </remarks>
        private static GameState Harvest(
            GameState state,
            HexGrid<ConduitTile> board,
            TileBag bag,
            ConduitTile heldTile,
            TokenPool pivotTokens,
            TokenPool skipTokens)
        {
            var routes = FlowResolver.FindCompletedRoutes(board, state.Endpoints);

            var paidOut = new HashSet<CompletedRoute>(state.PaidOutRoutes());
            var score = state.Score;
            var pivots = pivotTokens;
            var skips = skipTokens;

            foreach (var route in routes)
            {
                var pair = new CompletedRoute(route.Spring.Coordinate, route.Hub.Coordinate);
                if (!paidOut.Add(pair))
                {
                    continue;
                }

                score += ScoreTable.ScoreFor(state.BaseRouteScore, route.Length);

                // Every completed route pays out in one currency or the other, so none
                // feels wasted: length buys power, closing early buys flexibility.
                pivots = pivots.Earn(TokenRules.PivotTokensFor(route.Length));
                skips = skips.Earn(TokenRules.SkipTokensFor(route.Length));
            }

            return state.With(
                board: board,
                bag: bag,
                heldTile: heldTile,
                pivotTokens: pivots,
                skipTokens: skips,
                score: score,
                completedRoutes: paidOut);
        }

        private static void RequireCellOnBoard(GameState state, HexCoord at)
        {
            if (!state.Board.Contains(at))
            {
                throw new ArgumentOutOfRangeException(nameof(at), at, "Cell lies outside the board.");
            }
        }
    }
}
