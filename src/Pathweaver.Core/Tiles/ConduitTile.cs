using System;

namespace Pathweaver.Core.Tiles
{
    /// <summary>
    /// A hexagonal conduit tile: one resource kind, and the edges it is open on.
    /// </summary>
    /// <remarks>
    /// Tiles are values, not entities. Rotating one produces a new tile, so a
    /// board state can be held, compared, and replayed without any tile being
    /// mutated behind the game's back.
    /// </remarks>
    public readonly struct ConduitTile : IEquatable<ConduitTile>
    {
        private const int MinimumOpenEdges = 2;

        /// <summary>
        /// Creates a tile.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the kind is not a defined <see cref="ResourceKind"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when fewer than two edges are open. A conduit must carry flow
        /// through itself: one opening is a dead end, none is a blank, and
        /// neither is a tile a player could be dealt.
        /// </exception>
        public ConduitTile(ResourceKind kind, EdgeMask edges)
        {
            if (!Enum.IsDefined(typeof(ResourceKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown resource kind.");
            }

            if (edges.OpenEdgeCount < MinimumOpenEdges)
            {
                throw new ArgumentException(
                    $"A conduit needs at least {MinimumOpenEdges} open edges, found {edges.OpenEdgeCount}.",
                    nameof(edges));
            }

            Kind = kind;
            Edges = edges;
        }

        public ResourceKind Kind { get; }

        public EdgeMask Edges { get; }

        public static bool operator ==(ConduitTile left, ConduitTile right) => left.Equals(right);

        public static bool operator !=(ConduitTile left, ConduitTile right) => !left.Equals(right);

        public bool HasEdge(int direction) => Edges.HasEdge(direction);

        /// <summary>
        /// Rotates the tile clockwise by whole edges, returning a new tile.
        /// </summary>
        public ConduitTile RotateClockwise(int steps = 1)
            => new ConduitTile(Kind, Edges.RotateClockwise(steps));

        /// <summary>
        /// Whether flow can pass from this tile into a neighbour lying in
        /// <paramref name="direction"/>.
        /// </summary>
        /// <remarks>
        /// Three conditions, all required: this tile is open on that edge, the
        /// neighbour is open on the facing edge, and both carry the same resource.
        /// The last is a product rule from PRD section 3.1 — water must not flow
        /// down a crystal conduit even when the edges line up.
        /// </remarks>
        public bool ConnectsTo(ConduitTile neighbour, int direction)
        {
            if (Kind != neighbour.Kind)
            {
                return false;
            }

            return HasEdge(direction) && neighbour.HasEdge(EdgeMask.Opposite(direction));
        }

        public bool Equals(ConduitTile other) => Kind == other.Kind && Edges.Equals(other.Edges);

        public override bool Equals(object? obj) => obj is ConduitTile other && Equals(other);

        public override int GetHashCode() => HashCode.Combine((int)Kind, Edges);

        public override string ToString() => $"{Kind} {Edges}";
    }
}
