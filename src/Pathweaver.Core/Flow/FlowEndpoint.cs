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
    /// A fixed spring or hub attached to one edge of one cell.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An endpoint is not a tile and does not occupy a cell of its own. It hangs
    /// off an edge: a spring at <c>(-2,0)</c> facing west feeds whatever tile sits
    /// at <c>(-2,0)</c>, provided that tile is open on its western edge. Keeping
    /// endpoints off the board means the player can place a tile there, which is
    /// how a route starts at all.
    /// </para>
    /// <para>
    /// <see cref="Direction"/> is not wrapped. Endpoints come from authored level
    /// data, where an index outside 0 to 5 means the level is wrong.
    /// </para>
    /// </remarks>
    public readonly struct FlowEndpoint : IEquatable<FlowEndpoint>
    {
        private const int EdgeCount = 6;

        private FlowEndpoint(HexCoord coordinate, int direction, ResourceKind kind, EndpointRole role)
        {
            Coordinate = coordinate;
            Direction = direction;
            Kind = kind;
            Role = role;
        }

        /// <summary>The cell this endpoint feeds or drains.</summary>
        public HexCoord Coordinate { get; }

        /// <summary>
        /// The edge of <see cref="Coordinate"/> the endpoint attaches to. The tile
        /// placed there must be open on this edge.
        /// </summary>
        public int Direction { get; }

        public ResourceKind Kind { get; }

        public EndpointRole Role { get; }

        public static bool operator ==(FlowEndpoint left, FlowEndpoint right) => left.Equals(right);

        public static bool operator !=(FlowEndpoint left, FlowEndpoint right) => !left.Equals(right);

        public static FlowEndpoint Spring(HexCoord coordinate, int direction, ResourceKind kind)
            => Create(coordinate, direction, kind, EndpointRole.Spring);

        public static FlowEndpoint Hub(HexCoord coordinate, int direction, ResourceKind kind)
            => Create(coordinate, direction, kind, EndpointRole.Hub);

        public bool Equals(FlowEndpoint other)
            => Coordinate.Equals(other.Coordinate)
               && Direction == other.Direction
               && Kind == other.Kind
               && Role == other.Role;

        public override bool Equals(object? obj) => obj is FlowEndpoint other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(Coordinate, Direction, (int)Kind, (int)Role);

        public override string ToString() => $"{Kind} {Role} at {Coordinate} edge {Direction}";

        private static FlowEndpoint Create(
            HexCoord coordinate, int direction, ResourceKind kind, EndpointRole role)
        {
            if (direction < 0 || direction >= EdgeCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(direction), direction, $"Direction must be between 0 and {EdgeCount - 1}.");
            }

            if (!Enum.IsDefined(typeof(ResourceKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown resource kind.");
            }

            return new FlowEndpoint(coordinate, direction, kind, role);
        }
    }
}
