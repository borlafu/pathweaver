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
    /// active path connects a source spring to a destination hub. Everything about
    /// scoring, quotas, and objective progress reads from what this returns.
    /// </para>
    /// <para>
    /// It is a pure function over a board and its endpoints, holding no state, so
    /// it can be called after every placement without bookkeeping and is trivial
    /// to test.
    /// </para>
    /// </remarks>
    public static class FlowResolver
    {
        /// <summary>
        /// Every completed route on the board, at most one per spring and hub pair.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Where a conduit network offers several paths between the same pair, the
        /// shortest is reported. That is the conservative reading of the PRD's
        /// length multiplier: a player who builds a loop cannot claim the longer
        /// way round as their route length. It also keeps the result single-valued,
        /// which scoring depends on.
        /// </para>
        /// <para>
        /// Results are ordered by kind, then spring, then hub, so the order does
        /// not depend on how endpoints were supplied.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when an endpoint sits outside the board, which means the level
        /// data is wrong rather than the board merely being incomplete.
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
            foreach (var endpoint in all)
            {
                if (!board.Contains(endpoint.Coordinate))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(endpoints),
                        endpoint.Coordinate,
                        $"Endpoint {endpoint} lies outside the board.");
                }
            }

            var springs = Ordered(all.Where(endpoint => endpoint.Role == EndpointRole.Spring));
            var hubs = Ordered(all.Where(endpoint => endpoint.Role == EndpointRole.Hub));

            var routes = new List<Route>();
            foreach (var spring in springs)
            {
                var reachableHubs = hubs.Where(hub => hub.Kind == spring.Kind).ToList();
                if (reachableHubs.Count == 0)
                {
                    continue;
                }

                routes.AddRange(RoutesFrom(board, spring, reachableHubs));
            }

            return routes;
        }

        private static IEnumerable<Route> RoutesFrom(
            HexGrid<ConduitTile> board, FlowEndpoint spring, IReadOnlyList<FlowEndpoint> hubs)
        {
            // A spring only flows if the tile it feeds exists and faces it.
            if (!board.TryGet(spring.Coordinate, out var firstTile)
                || firstTile.Kind != spring.Kind
                || !firstTile.HasEdge(spring.Direction))
            {
                return Array.Empty<Route>();
            }

            var previous = BreadthFirstSearch(board, spring);

            var routes = new List<Route>();
            foreach (var hub in hubs)
            {
                if (!previous.ContainsKey(hub.Coordinate))
                {
                    continue;
                }

                // The hub must also be faced by the tile that reaches it.
                if (!board.TryGet(hub.Coordinate, out var lastTile) || !lastTile.HasEdge(hub.Direction))
                {
                    continue;
                }

                routes.Add(new Route(spring.Kind, spring, hub, PathTo(previous, spring.Coordinate, hub.Coordinate)));
            }

            return routes;
        }

        /// <summary>
        /// Walks the connected conduits of one kind outward from a spring,
        /// recording how each cell was first reached.
        /// </summary>
        /// <remarks>
        /// Breadth-first, so the recorded parent chain is a shortest path. Frontier
        /// cells expand in ascending open-edge order, which keeps the traversal —
        /// and therefore the reported path when several are equally short —
        /// identical on every device.
        /// </remarks>
        private static Dictionary<HexCoord, HexCoord> BreadthFirstSearch(
            HexGrid<ConduitTile> board, FlowEndpoint spring)
        {
            var previous = new Dictionary<HexCoord, HexCoord> { [spring.Coordinate] = spring.Coordinate };
            var frontier = new Queue<HexCoord>();
            frontier.Enqueue(spring.Coordinate);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (!board.TryGet(current, out var tile))
                {
                    continue;
                }

                foreach (var direction in tile.Edges.OpenDirections)
                {
                    var neighbourCoordinate = current.Neighbour(direction);
                    if (previous.ContainsKey(neighbourCoordinate) || !board.Contains(neighbourCoordinate))
                    {
                        continue;
                    }

                    if (!board.TryGet(neighbourCoordinate, out var neighbourTile)
                        || !tile.ConnectsTo(neighbourTile, direction))
                    {
                        continue;
                    }

                    previous[neighbourCoordinate] = current;
                    frontier.Enqueue(neighbourCoordinate);
                }
            }

            return previous;
        }

        private static IReadOnlyList<HexCoord> PathTo(
            Dictionary<HexCoord, HexCoord> previous, HexCoord from, HexCoord to)
        {
            var reversed = new List<HexCoord>();
            var step = to;

            while (true)
            {
                reversed.Add(step);
                if (step.Equals(from))
                {
                    break;
                }

                step = previous[step];
            }

            reversed.Reverse();
            return reversed;
        }

        private static List<FlowEndpoint> Ordered(IEnumerable<FlowEndpoint> endpoints)
            => endpoints
                .OrderBy(endpoint => (int)endpoint.Kind)
                .ThenBy(endpoint => endpoint.Coordinate.Q)
                .ThenBy(endpoint => endpoint.Coordinate.R)
                .ThenBy(endpoint => endpoint.Direction)
                .ToList();
    }
}
