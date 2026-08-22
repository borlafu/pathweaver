using System;
using System.Collections.Generic;

namespace Pathweaver.Core.Tiles
{
    /// <summary>
    /// Which of a hex tile's six edges are open, as a six-bit set.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bit <c>i</c> corresponds to <c>HexCoord.Directions[i]</c>, clockwise from
    /// due east. Because the two share an indexing convention, rotating a tile is
    /// a six-bit rotation and rotating a coordinate is one step along the
    /// direction list. A test pins that agreement — without it, a rotated tile
    /// would appear to point somewhere it does not connect.
    /// </para>
    /// <para>
    /// Six bits fit in a byte with room to spare, so masks are cheap to store in
    /// a save file and cheap to compare.
    /// </para>
    /// </remarks>
    public readonly struct EdgeMask : IEquatable<EdgeMask>
    {
        private const int EdgeCount = 6;
        private const byte AllEdges = 0b111111;

        private readonly byte _bits;

        private EdgeMask(byte bits)
        {
            _bits = bits;
        }

        /// <summary>A mask with every edge closed.</summary>
        public static EdgeMask None => new EdgeMask(0);

        /// <summary>How many edges are open.</summary>
        public int OpenEdgeCount
        {
            get
            {
                var count = 0;
                for (var direction = 0; direction < EdgeCount; direction++)
                {
                    if ((_bits & (1 << direction)) != 0)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// The open directions in ascending order. Flow tracing walks this, so a
        /// stable order keeps route discovery deterministic.
        /// </summary>
        public IEnumerable<int> OpenDirections
        {
            get
            {
                for (var direction = 0; direction < EdgeCount; direction++)
                {
                    if ((_bits & (1 << direction)) != 0)
                    {
                        yield return direction;
                    }
                }
            }
        }

        /// <summary>The raw bits, for serialisation.</summary>
        public byte Bits => _bits;

        public static bool operator ==(EdgeMask left, EdgeMask right) => left.Equals(right);

        public static bool operator !=(EdgeMask left, EdgeMask right) => !left.Equals(right);

        /// <summary>
        /// Builds a mask from explicit direction indices.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="HasEdge"/>, indices are not wrapped here. This reads
        /// authored level and tile data, where an index of 7 is a mistake rather
        /// than shorthand for 1.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a direction lies outside 0 to 5.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when a direction repeats, which means the definition is wrong.
        /// </exception>
        public static EdgeMask FromDirections(params int[] directions)
        {
            if (directions is null)
            {
                throw new ArgumentNullException(nameof(directions));
            }

            byte bits = 0;
            foreach (var direction in directions)
            {
                if (direction < 0 || direction >= EdgeCount)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(directions), direction, $"Direction must be between 0 and {EdgeCount - 1}.");
                }

                var bit = (byte)(1 << direction);
                if ((bits & bit) != 0)
                {
                    throw new ArgumentException(
                        $"Direction {direction} appears more than once.", nameof(directions));
                }

                bits |= bit;
            }

            return new EdgeMask(bits);
        }

        /// <summary>
        /// Rebuilds a mask from raw bits, for deserialisation.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when bits are set above the sixth, which means the data is not
        /// a valid mask.
        /// </exception>
        public static EdgeMask FromBits(byte bits)
        {
            if ((bits & ~AllEdges) != 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bits), bits, "Only the low six bits may be set.");
            }

            return new EdgeMask(bits);
        }

        /// <summary>
        /// The direction facing back from a neighbour reached by
        /// <paramref name="direction"/>.
        /// </summary>
        public static int Opposite(int direction) => Wrap(direction + 3);

        /// <summary>
        /// Whether the given edge is open. The index wraps, so callers can add
        /// or subtract turns without normalising first.
        /// </summary>
        public bool HasEdge(int direction) => (_bits & (1 << Wrap(direction))) != 0;

        /// <summary>
        /// Rotates the mask clockwise by whole edges. Steps wrap, and negative
        /// steps rotate counter-clockwise.
        /// </summary>
        public EdgeMask RotateClockwise(int steps = 1)
        {
            var shift = Wrap(steps);
            if (shift == 0)
            {
                return this;
            }

            // A six-bit rotate: what falls off the top re-enters at the bottom.
            var rotated = (byte)(((_bits << shift) | (_bits >> (EdgeCount - shift))) & AllEdges);
            return new EdgeMask(rotated);
        }

        public bool Equals(EdgeMask other) => _bits == other._bits;

        public override bool Equals(object? obj) => obj is EdgeMask other && Equals(other);

        public override int GetHashCode() => _bits.GetHashCode();

        public override string ToString()
            => _bits == 0 ? "EdgeMask(none)" : $"EdgeMask({string.Join(",", OpenDirections)})";

        private static int Wrap(int value) => ((value % EdgeCount) + EdgeCount) % EdgeCount;
    }
}
