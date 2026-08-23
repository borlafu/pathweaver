using System.Text;
using Pathweaver.Core.Flow;
using Pathweaver.Core.Levels;
using Pathweaver.Core.Rules;
using Pathweaver.Core.State;

namespace Pathweaver.Core.Tests.Solving;

/// <summary>
/// The outcome of a solve attempt.
/// </summary>
internal sealed class SolveResult
{
    internal SolveResult(bool solved, IReadOnlyList<GameCommand> commands, int nodesExplored, bool budgetExhausted)
    {
        Solved = solved;
        Commands = commands;
        NodesExplored = nodesExplored;
        BudgetExhausted = budgetExhausted;
    }

    internal bool Solved { get; }

    /// <summary>The moves that reach the target, when one was found.</summary>
    internal IReadOnlyList<GameCommand> Commands { get; }

    internal int NodesExplored { get; }

    /// <summary>
    /// True when the search ran out of budget. A false result with this set means
    /// "not proven solvable", which is not the same as "proven unsolvable".
    /// </summary>
    internal bool BudgetExhausted { get; }
}

/// <summary>
/// Searches for a way to clear a level's target score.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately lives in the test project. Its job today is to fail the build when
/// a level cannot be completed, so an unsolvable level never reaches a player. It
/// carries no weight in the shipped game.
/// </para>
/// <para>
/// It becomes production code when the rewarded hint feature of PRD section 6.2
/// arrives, since a hint is one move from a solution. Moving it then is a smaller
/// price than shipping an unused search now.
/// </para>
/// <para>
/// The search is possible only because the simulation is deterministic and
/// immutable: the tile order is fixed by the seed, so the tree branches on
/// placement alone, and an abandoned branch needs no undo — the earlier state is
/// simply still there.
/// </para>
/// </remarks>
internal static class LevelSolver
{
    /// <summary>
    /// A cap on explored states, so a pathological level cannot hang CI.
    /// </summary>
    internal const int DefaultNodeBudget = 200_000;

    internal static SolveResult Solve(LevelDefinition level, ulong seed, int nodeBudget = DefaultNodeBudget)
        => Solve(level.CreateGame(seed), level.TargetScore, nodeBudget);

    internal static SolveResult Solve(GameState start, long targetScore, int nodeBudget = DefaultNodeBudget)
    {
        var explored = 0;
        var moves = new List<GameCommand>();
        var seen = new HashSet<string>();

        var solved = Search(start, targetScore, moves, seen, ref explored, nodeBudget);

        return new SolveResult(
            solved,
            solved ? moves.ToArray() : Array.Empty<GameCommand>(),
            explored,
            budgetExhausted: !solved && explored >= nodeBudget);
    }

    private static bool Search(
        GameState state,
        long targetScore,
        List<GameCommand> moves,
        HashSet<string> seen,
        ref int explored,
        int nodeBudget)
    {
        if (state.Score >= targetScore)
        {
            return true;
        }

        if (explored >= nodeBudget)
        {
            return false;
        }

        foreach (var placement in Ordered(state))
        {
            explored++;
            if (explored >= nodeBudget)
            {
                return false;
            }

            var command = new PlaceTile(placement.Coordinate, placement.Rotation);
            var next = GameEngine.Apply(state, command);

            // Different orders of the same placements reach the same board, so
            // without this the search re-explores permutations rather than
            // positions.
            if (!seen.Add(Signature(next)))
            {
                continue;
            }

            moves.Add(command);
            if (Search(next, targetScore, moves, seen, ref explored, nodeBudget))
            {
                return true;
            }

            moves.RemoveAt(moves.Count - 1);
        }

        return false;
    }

    /// <summary>
    /// Legal placements, nearest to a matching hub first.
    /// </summary>
    /// <remarks>
    /// Depth-first search with no ordering wanders away from the hubs and fills the
    /// board, which is why an unguided version could not prove a 37-cell level
    /// solvable inside its budget. Aiming at the hubs finds a route in a fraction of
    /// the states. Ties fall back to the board's own stable ordering, so the search
    /// stays deterministic.
    /// </remarks>
    private static IEnumerable<TilePlacement> Ordered(GameState state)
    {
        var hubs = state.Endpoints
            .Where(endpoint => endpoint.Role == EndpointRole.Hub)
            .ToList();

        return state.LegalPlacements
            .Select((placement, index) => (placement, index))
            .OrderBy(entry => hubs
                .Where(hub => hub.Kind == entry.placement.Tile.Kind)
                .Select(hub => entry.placement.Coordinate.DistanceTo(hub.Coordinate))
                .DefaultIfEmpty(int.MaxValue)
                .Min())
            .ThenBy(entry => entry.index)
            .Select(entry => entry.placement);
    }

    /// <summary>
    /// A stable description of a board and its score, for pruning repeats.
    /// </summary>
    private static string Signature(GameState state)
    {
        var builder = new StringBuilder();
        builder.Append(state.Score).Append('|');

        foreach (var (coordinate, tile) in state.Board.OccupiedCells)
        {
            builder
                .Append(coordinate.Q).Append(',')
                .Append(coordinate.R).Append(':')
                .Append((int)tile.Kind).Append(':')
                .Append(tile.Edges.Bits).Append(';');
        }

        return builder.ToString();
    }
}
