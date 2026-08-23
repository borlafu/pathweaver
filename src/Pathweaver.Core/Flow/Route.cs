using System;
using System.Collections.Generic;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Flow
{
    /// <summary>
    /// A completed path of conduits carrying one resource from a spring to a hub.
    /// </summary>
    /// <remarks>
    /// <see cref="Length"/> is the L in the PRD's payoff curve
    /// <c>S = S_base * 1.35^(L-1)</c>. It counts player-placed conduits only: the
    /// spring and hub occupy cells of their own and are not tiles, so a single
    /// conduit bridging two adjacent endpoints is length 1 and earns the
    /// unmultiplied base score.
    /// </remarks>
    public sealed class Route
    {
        internal Route(ResourceKind kind, FlowEndpoint spring, FlowEndpoint hub, IReadOnlyList<HexCoord> tiles)
        {
            if (tiles is null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            if (tiles.Count == 0)
            {
                throw new ArgumentException("A completed route covers at least one tile.", nameof(tiles));
            }

            Kind = kind;
            Spring = spring;
            Hub = hub;
            Tiles = tiles;
        }

        public ResourceKind Kind { get; }

        public FlowEndpoint Spring { get; }

        public FlowEndpoint Hub { get; }

        /// <summary>
        /// The tiles the flow passes through, ordered from the spring end to the
        /// hub end.
        /// </summary>
        public IReadOnlyList<HexCoord> Tiles { get; }

        /// <summary>How many tiles the route covers.</summary>
        public int Length => Tiles.Count;

        public override string ToString() => $"{Kind} route of {Length} from {Spring.Coordinate} to {Hub.Coordinate}";
    }
}
