using System;
using System.Collections.Generic;

namespace Pathweaver.Core.Hex
{
    /// <summary>
    /// An axial hex coordinate. Q runs east, R runs south-east.
    /// </summary>
    /// <remarks>
    /// Axial coordinates keep two integers per cell instead of the three a cube
    /// layout needs, and every operation here stays in integer arithmetic. That
    /// matters: the simulation must produce identical results on every device,
    /// and floating point cannot promise that.
    /// </remarks>
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        private static readonly HexCoord[] DirectionOffsets =
        {
            new HexCoord(1, 0),
            new HexCoord(0, 1),
            new HexCoord(-1, 1),
            new HexCoord(-1, 0),
            new HexCoord(0, -1),
            new HexCoord(1, -1),
        };

        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        public static HexCoord Zero => new HexCoord(0, 0);

        /// <summary>
        /// The six neighbour offsets, ordered clockwise starting due east.
        /// </summary>
        /// <remarks>
        /// The ordering is load-bearing. Conduit tiles store their open edges as
        /// a six-bit mask indexed the same way, so rotating a tile is a bit
        /// rotation and rotating a coordinate steps one index along this list.
        /// Reordering these offsets silently breaks tile rotation.
        /// </remarks>
        public static IReadOnlyList<HexCoord> Directions => DirectionOffsets;

        public int Q { get; }

        public int R { get; }

        public static HexCoord operator +(HexCoord left, HexCoord right)
            => new HexCoord(left.Q + right.Q, left.R + right.R);

        public static HexCoord operator -(HexCoord left, HexCoord right)
            => new HexCoord(left.Q - right.Q, left.R - right.R);

        public static bool operator ==(HexCoord left, HexCoord right) => left.Equals(right);

        public static bool operator !=(HexCoord left, HexCoord right) => !left.Equals(right);

        /// <summary>
        /// The adjacent coordinate in the given direction. The index wraps, so
        /// callers can add or subtract turns without normalising first.
        /// </summary>
        public HexCoord Neighbour(int directionIndex)
        {
            var wrapped = ((directionIndex % 6) + 6) % 6;
            return this + DirectionOffsets[wrapped];
        }

        /// <summary>
        /// The number of steps along the shortest path between two cells.
        /// </summary>
        public int DistanceTo(HexCoord other)
        {
            var deltaQ = Q - other.Q;
            var deltaR = R - other.R;

            // In cube space this is the largest absolute axis difference. Summing
            // all three and halving reaches the same answer without converting.
            return (Math.Abs(deltaQ) + Math.Abs(deltaR) + Math.Abs(deltaQ + deltaR)) / 2;
        }

        /// <summary>
        /// Rotates 60 degrees clockwise about the origin.
        /// </summary>
        public HexCoord RotateClockwise() => new HexCoord(-R, Q + R);

        /// <summary>
        /// Rotates 60 degrees counter-clockwise about the origin.
        /// </summary>
        public HexCoord RotateCounterClockwise() => new HexCoord(Q + R, -Q);

        public bool Equals(HexCoord other) => Q == other.Q && R == other.R;

        public override bool Equals(object? obj) => obj is HexCoord other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Q, R);

        public override string ToString() => $"({Q}, {R})";
    }
}
