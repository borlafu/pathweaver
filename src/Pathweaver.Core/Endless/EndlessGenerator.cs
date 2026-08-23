using System;
using System.Collections.Generic;
using System.Linq;
using Pathweaver.Core.Determinism;
using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Levels;
using Pathweaver.Core.Rules;
using Pathweaver.Core.Scoring;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Endless
{
    /// <summary>
    /// Builds the boards of Endless Wayfare.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Boards are generated backwards: the routes are walked first and the board is derived from
    /// them. That is what makes a generated round solvable by construction. The alternative —
    /// scattering springs and hubs and then searching for a solution — has to run a solver on the
    /// device, and has to decide what to do when a board turns out to be impossible. Neither is
    /// needed here, because a board is never built that has no answer.
    /// </para>
    /// <para>
    /// The generated tiles carry only the two edges their own cell needs, so a planned conduit can
    /// never accidentally connect to a neighbouring route: its open edges point along its own path
    /// and nowhere else. Each network also gets a distinct resource kind, so two routes cannot
    /// short-circuit through each other even where their cells touch.
    /// </para>
    /// <para>
    /// Everything here draws from <see cref="PathweaverStream.GridLayout"/> and nothing reads a
    /// clock, so a round is reproducible from its run seed and round number alone.
    /// </para>
    /// </remarks>
    public static class EndlessGenerator
    {
        /// <summary>Points a single-conduit route is worth. The authored levels use the same value.</summary>
        private const long BaseRouteScore = 100L;

        /// <summary>
        /// How many times a route is re-planned from a different spring before giving up on the
        /// length it was asked for.
        /// </summary>
        /// <remarks>
        /// A walk can paint itself into a corner — it is a random walk that may not revisit a cell —
        /// and starting again elsewhere is cheaper than backtracking. The board is sized to leave
        /// twice the room the routes need, so a failure is rare rather than routine.
        /// </remarks>
        private const int PlanAttempts = 32;

        /// <summary>
        /// The kinds rounds hand out, one per network.
        /// </summary>
        /// <remarks>
        /// Distinct per network on purpose. Two networks of one kind could join through a shared
        /// cell and pay for a route nobody planned, and the flow rules would be right to allow it —
        /// so the board simply never asks the question.
        /// </remarks>
        private static readonly ResourceKind[] Kinds =
        {
            ResourceKind.Water,
            ResourceKind.Wind,
            ResourceKind.Crystal,
        };

        /// <summary>
        /// Generates one round of an endless run.
        /// </summary>
        /// <param name="round">The round number, counted from one.</param>
        /// <param name="seed">The seed for the whole run.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown for a round below one.</exception>
        public static EndlessRound Generate(int round, ulong seed)
        {
            var difficulty = EndlessDifficulty.ForRound(round);
            var roundSeed = SeedSource.ForRound(seed, round);
            var random = SeedSource.Stream(roundSeed, PathweaverStream.GridLayout);

            var shape = HexagonCells(difficulty.Radius);
            var shapeSet = new HashSet<HexCoord>(shape);
            var taken = new HashSet<HexCoord>();

            var plans = new List<RoutePlan>();

            for (var index = 0; index < difficulty.Pairs; index++)
            {
                var plan = PlanRoute(
                    Kinds[index % Kinds.Length], shape, shapeSet, taken, difficulty.RouteLength, ref random);

                if (plan is null)
                {
                    // Every route already placed still makes a playable board, so a network that
                    // cannot be fitted is dropped rather than allowed to fail the whole round.
                    continue;
                }

                plans.Add(plan);

                taken.Add(plan.Spring);
                taken.Add(plan.Hub);
                foreach (var cell in plan.Path)
                {
                    taken.Add(cell);
                }
            }

            if (plans.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No route could be planned for round {round} on a radius {difficulty.Radius} board.");
            }

            var endpoints = plans
                .SelectMany(plan => new[]
                {
                    FlowEndpoint.Spring(plan.Spring, plan.Kind),
                    FlowEndpoint.Hub(plan.Hub, plan.Kind),
                })
                .ToArray();

            var solution = plans.SelectMany(Conduits).ToArray();
            var bag = BuildBag(solution);

            var planScore = plans.Sum(plan => ScoreTable.ScoreFor(BaseRouteScore, plan.Path.Count));
            var target = planScore * EndlessDifficulty.TargetNumerator / EndlessDifficulty.TargetDenominator;

            var level = new LevelDefinition(
                id: $"endless-{round}",
                name: $"Wayfare {round}",
                shape: shape,
                endpoints: endpoints,
                bagTiles: bag,
                baseRouteScore: BaseRouteScore,
                targetScore: target,
                startingTokens: difficulty.StartingTokens,
                startingSkips: difficulty.StartingSkips,
                seed: roundSeed);

            return new EndlessRound(level, solution);
        }

        /// <summary>
        /// Walks a route of the requested length, or returns null if it could not be fitted.
        /// </summary>
        private static RoutePlan? PlanRoute(
            ResourceKind kind,
            HexCoord[] shape,
            HashSet<HexCoord> shapeSet,
            HashSet<HexCoord> taken,
            int routeLength,
            ref Pcg32 random)
        {
            // Shorter is better than nothing: a round with a three-conduit route where a six was
            // asked for is still a round, and the target follows the plan rather than the request.
            for (var length = routeLength; length >= 2; length--)
            {
                for (var attempt = 0; attempt < PlanAttempts; attempt++)
                {
                    var plan = TryWalk(kind, shape, shapeSet, taken, length, ref random);
                    if (plan is not null)
                    {
                        return plan;
                    }
                }
            }

            return null;
        }

        private static RoutePlan? TryWalk(
            ResourceKind kind,
            HexCoord[] shape,
            HashSet<HexCoord> shapeSet,
            HashSet<HexCoord> taken,
            int length,
            ref Pcg32 random)
        {
            var spring = shape[NextBelow(ref random, (uint)shape.Length)];
            if (taken.Contains(spring))
            {
                return null;
            }

            var used = new HashSet<HexCoord>(taken) { spring };
            var path = new List<HexCoord>(length);
            var current = spring;

            for (var step = 0; step < length; step++)
            {
                if (!TryStep(shapeSet, used, current, ref random, out var next))
                {
                    return null;
                }

                path.Add(next);
                used.Add(next);
                current = next;
            }

            if (!TryStep(shapeSet, used, current, ref random, out var hub))
            {
                return null;
            }

            return new RoutePlan(kind, spring, hub, path);
        }

        /// <summary>Picks a free neighbour at random.</summary>
        private static bool TryStep(
            HashSet<HexCoord> shapeSet,
            HashSet<HexCoord> used,
            HexCoord from,
            ref Pcg32 random,
            out HexCoord next)
        {
            var candidates = new List<HexCoord>(6);

            // Enumerated in HexCoord.Directions order, which is fixed, so the same seed picks the
            // same neighbour on every device.
            foreach (var direction in HexCoord.Directions)
            {
                var candidate = from + direction;
                if (shapeSet.Contains(candidate) && !used.Contains(candidate))
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates.Count == 0)
            {
                next = default;
                return false;
            }

            next = candidates[(int)NextBelow(ref random, (uint)candidates.Count)];
            return true;
        }

        /// <summary>
        /// Turns a planned walk into the conduits that carry it.
        /// </summary>
        /// <remarks>
        /// Each conduit opens exactly two edges: towards the cell before it and towards the cell
        /// after it. The cell before the first conduit is the spring and the cell after the last is
        /// the hub, which is why a route is complete the moment its conduits are all present.
        /// </remarks>
        private static IEnumerable<TilePlacement> Conduits(RoutePlan plan)
        {
            for (var index = 0; index < plan.Path.Count; index++)
            {
                var cell = plan.Path[index];
                var previous = index == 0 ? plan.Spring : plan.Path[index - 1];
                var next = index == plan.Path.Count - 1 ? plan.Hub : plan.Path[index + 1];

                var edges = EdgeMask.FromDirections(
                    DirectionTo(cell, previous), DirectionTo(cell, next));

                yield return new TilePlacement(cell, rotation: 0, new ConduitTile(plan.Kind, edges));
            }
        }

        /// <summary>
        /// Builds a bag that can supply the plan.
        /// </summary>
        /// <remarks>
        /// Two of every shape the plan needs, so a single unlucky cycle of the bag cannot starve a
        /// route, plus one straight per kind as something to waste. Shapes are stored in one
        /// orientation because rotation is free in play; two conduits that differ only by turning
        /// are the same tile as far as the bag is concerned.
        /// </remarks>
        private static ConduitTile[] BuildBag(TilePlacement[] solution)
        {
            var bag = new List<ConduitTile>();
            var seen = new HashSet<ConduitTile>();

            foreach (var placement in solution)
            {
                var canonical = new ConduitTile(placement.Tile.Kind, Canonical(placement.Tile.Edges));
                if (!seen.Add(canonical))
                {
                    continue;
                }

                bag.Add(canonical);
                bag.Add(canonical);
            }

            foreach (var kind in solution.Select(placement => placement.Tile.Kind).Distinct())
            {
                bag.Add(new ConduitTile(kind, EdgeMask.FromDirections(0, 3)));
            }

            return bag.ToArray();
        }

        /// <summary>
        /// The rotation of a shape with the smallest bit pattern.
        /// </summary>
        /// <remarks>
        /// Gives one name to each shape however it was found, so a bag holds one entry for a bend
        /// rather than six.
        /// </remarks>
        private static EdgeMask Canonical(EdgeMask edges)
        {
            var best = edges;

            for (var steps = 1; steps < 6; steps++)
            {
                var rotated = edges.RotateClockwise(steps);
                if (rotated.Bits < best.Bits)
                {
                    best = rotated;
                }
            }

            return best;
        }

        private static int DirectionTo(HexCoord from, HexCoord neighbour)
        {
            var offset = neighbour - from;

            for (var direction = 0; direction < HexCoord.Directions.Count; direction++)
            {
                if (HexCoord.Directions[direction].Equals(offset))
                {
                    return direction;
                }
            }

            throw new ArgumentException($"{neighbour} does not touch {from}.", nameof(neighbour));
        }

        private static HexCoord[] HexagonCells(int radius)
        {
            var cells = new List<HexCoord>(EndlessDifficulty.CellsInHexagon(radius));

            for (var q = -radius; q <= radius; q++)
            {
                var lowest = Math.Max(-radius, -q - radius);
                var highest = Math.Min(radius, -q + radius);

                for (var r = lowest; r <= highest; r++)
                {
                    cells.Add(new HexCoord(q, r));
                }
            }

            return cells.ToArray();
        }

        private static uint NextBelow(ref Pcg32 random, uint bound)
        {
            var (advanced, value) = random.NextUInt32(bound);
            random = advanced;
            return value;
        }

        /// <summary>One planned route, before it becomes tiles.</summary>
        private sealed class RoutePlan
        {
            internal RoutePlan(ResourceKind kind, HexCoord spring, HexCoord hub, List<HexCoord> path)
            {
                Kind = kind;
                Spring = spring;
                Hub = hub;
                Path = path;
            }

            internal ResourceKind Kind { get; }

            internal HexCoord Spring { get; }

            internal HexCoord Hub { get; }

            internal List<HexCoord> Path { get; }
        }
    }
}
