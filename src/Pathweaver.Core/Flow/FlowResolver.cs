using System;
using System.Collections.Generic;
using System.Linq;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Flow
{
    /// <summary>
    /// Finds the routes a board currently completes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the heart of the game: PRD section 3.1 step 3 pays out when an
    /// active path connects a source spring to a destination hub. Scoring, quotas,
    /// and objective progress all read from what this returns.
    /// </para>
    /// <para>
    /// It is a pure function over a board and its endpoints, holding no state, so
    /// it can run after every placement with no bookkeeping to invalidate.
    /// </para>
    /// </remarks>
    public static class FlowResolver
    {
        /// <summary>
        /// Every completed route on the board, at most one per spring and hub pair.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A route runs from a conduit adjacent to a spring to a conduit adjacent
        /// to a hub, and covers conduits only — endpoints occupy their own cells
        /// and are never tiles. Two touching endpoints therefore complete nothing:
        /// a route is built from tiles, so a spring pressed against a hub is not a
        /// free harvest.
        /// </para>
        /// <para>
        /// Where a network offers several paths between the same pair, the shortest
        /// is reported. That is the conservative reading of the PRD's length
        /// multiplier — a player who builds a loop cannot claim the longer way
        /// round — and it keeps the result single-valued, which scoring depends on.
        /// </para>
        /// <para>
        /// Results are ordered by kind, then spring, then hub, so the order does
        /// not depend on how endpoints were supplied.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when an endpoint sits outside the board.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when two endpoints share a cell, or when a conduit occupies an
        /// endpoint cell. Both mean the board is invalid rather than merely
        /// incomplete, and reporting no routes would hide the mistake.
        /// </exception>
        public static IReadOnlyList<Route> FindCompletedRoutes(
            HexGrid<ConduitTile> board, IEnumerable<FlowEndpoint> endpoints)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (endpoints is null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            var all = endpoints.ToArray();
            ValidateEndpoints(board, all);

            var springs = Ordered(all.Where(endpoint => endpoint.Role == EndpointRole.Spring));
            var hubs = Ordered(all.Where(endpoint => endpoint.Role == EndpointRole.Hub));

            var routes = new List<Route>();
            foreach (var spring in springs)
            {
                var matchingHubs = hubs.Where(hub => hub.Kind == spring.Kind).ToList();
                if (matchingHubs.Count == 0)
                {
                    continue;
                }

                routes.AddRange(RoutesFrom(board, spring, matchingHubs));
            }

            return routes;
        }

        private static void ValidateEndpoints(HexGrid<ConduitTile> board, FlowEndpoint[] endpoints)
        {
            var seen = new HashSet<HexCoord>();

            foreach (var endpoint in endpoints)
            {
                if (!board.Contains(endpoint.Coordinate))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(endpoints), endpoint.Coordinate, $"Endpoint {endpoint} lies outside the board.");
                }

                if (!seen.Add(endpoint.Coordinate))
                {
                    throw new ArgumentException(
                        $"Cell {endpoint.Coordinate} carries more than one endpoint.", nameof(endpoints));
                }

                if (!board.IsEmpty(endpoint.Coordinate))
                {
                    throw new ArgumentException(
                        $"Cell {endpoint.Coordinate} holds a conduit, but {endpoint} occupies it.",
                        nameof(endpoints));
                }
            }
        }

        private static IEnumerable<Route> RoutesFrom(
            HexGrid<ConduitTile> board, FlowEndpoint spring, IReadOnlyList<FlowEndpoint> hubs)
        {
            var reached = Traverse(board, spring);
            if (reached.Count == 0)
            {
                return Array.Empty<Route>();
            }

            var routes = new List<Route>();
            foreach (var hub in hubs)
            {
                var lastConduit = ClosestConduitFeeding(board, reached, hub);
                if (lastConduit is null)
                {
                    continue;
                }

                var path = PathTo(reached, lastConduit.Value);
                routes.Add(new Route(spring.Kind, spring, hub, path));
            }

            return routes;
        }

        /// <summary>
        /// Walks the conduits reachable from a spring, recording how each was
        /// first reached and how far along the path it lies.
        /// </summary>
        /// <remarks>
        /// Breadth-first from every conduit that feeds the spring at once, so the
        /// recorded parent chain is a shortest path. Seeds are taken in ascending
        /// direction order and frontier cells expand in ascending open-edge order,
        /// which keeps the traversal — and so the reported path when several tie in
        /// length — identical on every device.
        /// </remarks>
        private static Dictionary<HexCoord, (HexCoord Previous, int Distance)> Traverse(
            HexGrid<ConduitTile> board, FlowEndpoint spring)
        {
            var reached = new Dictionary<HexCoord, (HexCoord Previous, int Distance)>();
            var frontier = new Queue<HexCoord>();

            for (var direction = 0; direction < HexCoord.Directions.Count; direction++)
            {
                var candidate = spring.Coordinate.Neighbour(direction);
                if (!board.Contains(candidate) || !board.TryGet(candidate, out var tile))
                {
                    continue;
                }

                // The conduit has to face the spring, and carry its resource.
                if (tile.Kind != spring.Kind || !tile.HasEdge(EdgeMask.Opposite(direction)))
                {
                    continue;
                }

                if (reached.ContainsKey(candidate))
                {
                    continue;
                }

                reached[candidate] = (candidate, 0);
                frontier.Enqueue(candidate);
            }

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (!board.TryGet(current, out var tile))
                {
                    continue;
                }

                var distance = reached[current].Distance;

                foreach (var direction in tile.Edges.OpenDirections)
                {
                    var neighbour = current.Neighbour(direction);
                    if (reached.ContainsKey(neighbour) || !board.Contains(neighbour))
                    {
                        continue;
                    }

                    if (!board.TryGet(neighbour, out var neighbourTile)
                        || !tile.ConnectsTo(neighbourTile, direction))
                    {
                        continue;
                    }

                    reached[neighbour] = (current, distance + 1);
                    frontier.Enqueue(neighbour);
                }
            }

            return reached;
        }

        /// <summary>
        /// The reached conduit adjacent to the hub and open towards it that lies
        /// closest to the spring.
        /// </summary>
        /// <remarks>
        /// Ties break on ascending direction, so the choice is stable.
        /// </remarks>
        private static HexCoord? ClosestConduitFeeding(
            HexGrid<ConduitTile> board,
            Dictionary<HexCoord, (HexCoord Previous, int Distance)> reached,
            FlowEndpoint hub)
        {
            HexCoord? best = null;
            var bestDistance = int.MaxValue;

            for (var direction = 0; direction < HexCoord.Directions.Count; direction++)
            {
                var candidate = hub.Coordinate.Neighbour(direction);
                if (!reached.TryGetValue(candidate, out var entry))
                {
                    continue;
                }

                if (!board.TryGet(candidate, out var tile) || !tile.HasEdge(EdgeMask.Opposite(direction)))
                {
                    continue;
                }

                if (entry.Distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = entry.Distance;
                }
            }

            return best;
        }

        private static IReadOnlyList<HexCoord> PathTo(
            Dictionary<HexCoord, (HexCoord Previous, int Distance)> reached, HexCoord to)
        {
            var reversed = new List<HexCoord>();
            var step = to;

            while (true)
            {
                reversed.Add(step);

                var previous = reached[step].Previous;
                if (previous.Equals(step))
                {
                    break;
                }

                step = previous;
            }

            reversed.Reverse();
            return reversed;
        }

        private static List<FlowEndpoint> Ordered(IEnumerable<FlowEndpoint> endpoints)
            => endpoints
                .OrderBy(endpoint => (int)endpoint.Kind)
                .ThenBy(endpoint => endpoint.Coordinate.Q)
                .ThenBy(endpoint => endpoint.Coordinate.R)
                .ToList();
    }
}
