using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Tiles;
using Pathweaver.Core.Levels;
using Pathweaver.Core.Rules;
using Pathweaver.Core.State;

namespace Pathweaver.Core.Tests.Solving;

/// <summary>
/// The outcome of a solve attempt.
/// </summary>
internal sealed class SolveResult
{
    internal SolveResult(
        bool solved,
        IReadOnlyList<GameCommand> commands,
        int nodesExplored,
        bool budgetExhausted,
        bool exhaustive)
    {
        Solved = solved;
        Commands = commands;
        NodesExplored = nodesExplored;
        BudgetExhausted = budgetExhausted;
        Exhaustive = exhaustive;
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

    /// <summary>
    /// Whether every legal move was considered.
    /// </summary>
    /// <remarks>
    /// False once the search narrowed its options to keep a large board tractable. An unsolved
    /// result is then only "not found", never "does not exist" — a distinction worth carrying in
    /// the type rather than in a comment, because the two demand different responses from whoever
    /// reads the failure.
    /// </remarks>
    internal bool Exhaustive { get; }
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
    internal const int DefaultNodeBudget = 600_000;

    /// <summary>
    /// How many placements to try at each step.
    /// </summary>
    /// <remarks>
    /// An open thirty-cell board offers well over a hundred legal placements, and depth-first search
    /// through all of them spends its budget rearranging conduits far from any hub. Trying only the
    /// most promising few finds routes quickly, at the price of completeness — which is why an
    /// unsolved result reports whether it looked everywhere.
    /// </remarks>
    internal const int BranchWidth = 8;

    internal static SolveResult Solve(LevelDefinition level, ulong seed, int nodeBudget = DefaultNodeBudget)
        => Solve(level.CreateGame(seed), level.TargetScore, nodeBudget);

    internal static SolveResult Solve(GameState start, long targetScore, int nodeBudget = DefaultNodeBudget)
    {
        var explored = 0;
        var narrowed = false;
        var moves = new List<GameCommand>();
        var seen = new HashSet<long>();

        var solved = Search(start, targetScore, moves, seen, ref explored, ref narrowed, nodeBudget);

        return new SolveResult(
            solved,
            solved ? moves.ToArray() : Array.Empty<GameCommand>(),
            explored,
            budgetExhausted: !solved && explored >= nodeBudget,
            exhaustive: !narrowed);
    }

    private static bool Search(
        GameState state,
        long targetScore,
        List<GameCommand> moves,
        HashSet<long> seen,
        ref int explored,
        ref bool narrowed,
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

        // A branch where no unpaid pair can still be joined cannot reach the target, however many
        // conduits are left to place. Without this the search happily fills a board that has already
        // walled off every remaining hub, which is what exhausted the budget on a 35-cell level.
        if (!AnyPairStillReachable(state))
        {
            return false;
        }

        var candidates = Ordered(state).ToList();
        if (candidates.Count > BranchWidth)
        {
            narrowed = true;
            candidates = candidates.Take(BranchWidth).ToList();
        }

        foreach (var placement in candidates)
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
            if (Search(next, targetScore, moves, seen, ref explored, ref narrowed, nodeBudget))
            {
                return true;
            }

            moves.RemoveAt(moves.Count - 1);
        }

        // Skipping is a move the player has, so the search has to have it too. Without this the
        // solver treated any tile that fits nowhere as a dead end and declared boards impossible
        // that a player clears easily — which is exactly what happened on a board of zigzag
        // corridors, where a straight conduit can never be placed and the bag deals them anyway.
        // Always considered, not only when stuck. Restricting it to deadlocks looked like a cheap
        // saving and was wrong: on a board of zigzag corridors a straight conduit is placeable in
        // several spots and useful in none, so the skip a player would obviously take was the one
        // branch the search refused. Skips are bounded by the tokens held, so this adds a handful of
        // decisions per line of play rather than a factor at every node.
        if (state.SkipTokens.CanSpend)
        {
            explored++;

            var skip = new SkipTile();
            var afterSkip = GameEngine.Apply(state, skip);

            if (seen.Add(Signature(afterSkip)))
            {
                moves.Add(skip);
                if (Search(afterSkip, targetScore, moves, seen, ref explored, ref narrowed, nodeBudget))
                {
                    return true;
                }

                moves.RemoveAt(moves.Count - 1);
            }
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
    /// Whether any spring and hub pair that has not paid out could still be joined.
    /// </summary>
    /// <remarks>
    /// Asks only whether a path exists through cells that are empty or already carry a conduit of
    /// the right kind — not whether the tiles to build it will arrive. That makes it a cheap
    /// over-estimate, which is what a pruning test has to be: it may keep a branch that turns out
    /// to be hopeless, but it never discards one that could still succeed.
    /// </remarks>
    private static bool AnyPairStillReachable(GameState state)
    {
        var paid = new HashSet<(HexCoord, HexCoord)>();
        foreach (var route in state.CompletedRoutes)
        {
            paid.Add((route.Spring, route.Hub));
        }

        foreach (var spring in state.Endpoints)
        {
            if (spring.Role != EndpointRole.Spring)
            {
                continue;
            }

            foreach (var hub in state.Endpoints)
            {
                if (hub.Role != EndpointRole.Hub || hub.Kind != spring.Kind)
                {
                    continue;
                }

                if (paid.Contains((spring.Coordinate, hub.Coordinate)))
                {
                    continue;
                }

                if (PathCouldExist(state, spring, hub))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool PathCouldExist(GameState state, FlowEndpoint spring, FlowEndpoint hub)
    {
        var board = state.Board;
        var blocked = new HashSet<HexCoord>();

        foreach (var endpoint in state.Endpoints)
        {
            blocked.Add(endpoint.Coordinate);
        }

        var visited = new HashSet<HexCoord>();
        var frontier = new Queue<HexCoord>();

        foreach (var start in board.NeighboursOf(spring.Coordinate))
        {
            if (IsUsable(board, blocked, start, spring.Kind) && visited.Add(start))
            {
                frontier.Enqueue(start);
            }
        }

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();

            foreach (var neighbour in board.NeighboursOf(current))
            {
                if (neighbour.Equals(hub.Coordinate))
                {
                    return true;
                }

                if (IsUsable(board, blocked, neighbour, spring.Kind) && visited.Add(neighbour))
                {
                    frontier.Enqueue(neighbour);
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Whether a cell could carry part of a route of the given kind: empty, or already carrying a
    /// conduit of that kind.
    /// </summary>
    private static bool IsUsable(
        HexGrid<ConduitTile> board, HashSet<HexCoord> endpointCells, HexCoord cell, ResourceKind kind)
    {
        if (endpointCells.Contains(cell))
        {
            return false;
        }

        return !board.TryGet(cell, out var tile) || tile.Kind == kind;
    }

    /// <summary>
    /// A stable identity for a board and its score, for pruning repeats.
    /// </summary>
    /// <remarks>
    /// A 64-bit hash rather than a string. The search visits hundreds of thousands of states and the
    /// string version spent much of its time building and comparing text. A hash can collide in
    /// principle, which would drop a branch; at this width and this many states that is far less
    /// likely than the search timing out, which is the failure it prevents.
    /// </remarks>
    private static long Signature(GameState state)
    {
        unchecked
        {
            var hash = 1469598103934665603L;
            hash = (hash ^ state.Score) * 1099511628211L;

            // The held tile and the skips left are part of the position. A skip changes neither the
            // board nor the score, so without these two a skip would look like a state already
            // visited and the search would refuse to take it.
            hash = (hash ^ (int)state.HeldTile.Kind) * 1099511628211L;
            hash = (hash ^ state.HeldTile.Edges.Bits) * 1099511628211L;
            hash = (hash ^ state.SkipTokens.Count) * 1099511628211L;

            foreach (var (coordinate, tile) in state.Board.OccupiedCells)
            {
                hash = (hash ^ coordinate.Q) * 1099511628211L;
                hash = (hash ^ coordinate.R) * 1099511628211L;
                hash = (hash ^ (int)tile.Kind) * 1099511628211L;
                hash = (hash ^ tile.Edges.Bits) * 1099511628211L;
            }

            return hash;
        }
    }
}
