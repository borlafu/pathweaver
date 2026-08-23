using System;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Flow
{
    /// <summary>
    /// Whether an endpoint produces or consumes a resource.
    /// </summary>
    /// <remarks>
    /// Values appear in level JSON. Never renumber an existing member.
    /// </remarks>
    public enum EndpointRole
    {
        /// <summary>A source spring, where flow begins.</summary>
        Spring = 0,

        /// <summary>A destination hub, where flow is harvested.</summary>
        Hub = 1,
    }

    /// <summary>
    /// A source spring or destination hub occupying one cell of the board.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An endpoint takes up its own cell, so the player cannot place a conduit
    /// there. Flow enters the network through any neighbouring conduit that is
    /// open towards the endpoint and carries the matching resource, which means
    /// geometry alone decides where a route can start — no separate facing needs
    /// authoring, and a spring works the same whether it sits on the rim or deep
    /// inside the board.
    /// </para>
    /// <para>
    /// Because endpoints occupy cells, <c>Route.Length</c> counts only
    /// player-placed conduits, matching the PRD's description of L as tile
    /// length.
    /// </para>
    /// </remarks>
    public readonly struct FlowEndpoint : IEquatable<FlowEndpoint>
    {
        private FlowEndpoint(HexCoord coordinate, ResourceKind kind, EndpointRole role)
        {
            Coordinate = coordinate;
            Kind = kind;
            Role = role;
        }

        /// <summary>The cell this endpoint occupies.</summary>
        public HexCoord Coordinate { get; }

        public ResourceKind Kind { get; }

        public EndpointRole Role { get; }

        public static bool operator ==(FlowEndpoint left, FlowEndpoint right) => left.Equals(right);

        public static bool operator !=(FlowEndpoint left, FlowEndpoint right) => !left.Equals(right);

        public static FlowEndpoint Spring(HexCoord coordinate, ResourceKind kind)
            => Create(coordinate, kind, EndpointRole.Spring);

        public static FlowEndpoint Hub(HexCoord coordinate, ResourceKind kind)
            => Create(coordinate, kind, EndpointRole.Hub);

        public bool Equals(FlowEndpoint other)
            => Coordinate.Equals(other.Coordinate) && Kind == other.Kind && Role == other.Role;

        public override bool Equals(object? obj) => obj is FlowEndpoint other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Coordinate, (int)Kind, (int)Role);

        public override string ToString() => $"{Kind} {Role} at {Coordinate}";

        private static FlowEndpoint Create(HexCoord coordinate, ResourceKind kind, EndpointRole role)
        {
            if (!Enum.IsDefined(typeof(ResourceKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown resource kind.");
            }

            return new FlowEndpoint(coordinate, kind, role);
        }
    }
}
