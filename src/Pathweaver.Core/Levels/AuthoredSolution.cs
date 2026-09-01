using System;
using System.Collections.Generic;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Rules;
using Pathweaver.Core.State;

namespace Pathweaver.Core.Levels
{
    /// <summary>
    /// Replays a solution the author of a level wrote down.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The solvability gate proves a level clearable by searching for a way through it. That works up
    /// to about the size of the boards biome one uses and then stops: a twenty-eight cell board with
    /// room to pan exhausted six hundred thousand states in fourteen seconds without a verdict, and the
    /// space grows with the freedom the player has at each step — which is exactly what makes a large
    /// board worth having.
    /// </para>
    /// <para>
    /// So a level may instead carry its own solution, and be certified by replaying it. That is a
    /// weaker guarantee, and worth being honest about: a searched level is proven clearable by
    /// something that did not know the answer, while a replayed one is proven clearable by its author.
    /// It still says the thing that matters to a player — this board can be finished — and the
    /// alternative for a large board is not certifying it at all.
    /// </para>
    /// <para>
    /// The replay is in the simulation rather than in the tests because it is the same machinery a hint
    /// would need, and because <c>GameEngine</c> refusing a move is what makes the certification real.
    /// </para>
    /// </remarks>
    public static class AuthoredSolution
    {
        /// <summary>
        /// Plays the level's authored solution and returns the state it ends in.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// A move was rejected, or a placement had no legal rotation. The message names the step, since
        /// a solution that has drifted out of step with its level is otherwise very hard to read.
        /// </exception>
        public static GameState Replay(LevelDefinition level, ulong? seed = null)
        {
            if (level is null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (level.Solution.Count == 0)
            {
                throw new InvalidOperationException($"{level.Id} carries no authored solution.");
            }

            var state = seed.HasValue ? level.CreateGame(seed.Value) : level.CreateGame();

            for (var step = 0; step < level.Solution.Count; step++)
            {
                state = ApplyStep(level, state, level.Solution[step], step);
            }

            return state;
        }

        private static GameState ApplyStep(
            LevelDefinition level, GameState state, AuthoredMove move, int step)
        {
            if (move.IsSkip)
            {
                return Attempt(level, state, new SkipTile(), move, step);
            }

            if (move.Rotation.HasValue)
            {
                return Attempt(level, state, new PlaceTile(move.At, move.Rotation.Value), move, step);
            }

            // No rotation given, so find the one that fits. On a board with one route through a cell
            // there is exactly one, which is why writing them all down by hand would be transcription
            // rather than information.
            for (var rotation = 0; rotation < HexCoord.Directions.Count; rotation++)
            {
                var turned = state.HeldTile.RotateClockwise(rotation);

                if (PlacementRules.IsLegal(state.Board, state.Endpoints, move.At, turned))
                {
                    return GameEngine.Apply(state, new PlaceTile(move.At, rotation));
                }
            }

            throw new InvalidOperationException(
                Describe(level, move, step)
                + $": no rotation of {state.HeldTile} can be placed there.");
        }

        private static GameState Attempt(
            LevelDefinition level, GameState state, GameCommand command, AuthoredMove move, int step)
        {
            try
            {
                return GameEngine.Apply(state, command);
            }
            catch (InvalidOperationException error)
            {
                throw new InvalidOperationException(
                    $"{Describe(level, move, step)}: {error.Message}", error);
            }
        }

        private static string Describe(LevelDefinition level, AuthoredMove move, int step)
            => $"{level.Id} step {step + 1} ({move})";

        /// <summary>
        /// Every cell the solution places a tile on, in order.
        /// </summary>
        /// <remarks>
        /// Exposed for the tests that check a solution is not accidentally the same cell twice, which
        /// would be an authoring slip that the replay would report far less clearly.
        /// </remarks>
        public static IEnumerable<AuthoredMove> Placements(LevelDefinition level)
        {
            foreach (var move in level.Solution)
            {
                if (!move.IsSkip)
                {
                    yield return move;
                }
            }
        }
    }
}
