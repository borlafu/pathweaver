using System;

namespace Pathweaver.Core.Determinism
{
    /// <summary>
    /// The PCG32 permuted congruential generator: 64 bits of state, 32 bits of
    /// output, selectable stream.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exists because <c>System.Random</c> cannot be used. Its algorithm is
    /// implementation-defined and has changed between .NET versions, so it gives
    /// no promise that two devices agree. The Daily Expedition requires the
    /// opposite: a date-derived seed that produces an identical grid everywhere,
    /// with no server involved.
    /// </para>
    /// <para>
    /// The generator is a readonly struct. Drawing a number returns a new
    /// generator rather than advancing this one, which keeps the whole
    /// simulation free of hidden state changes and makes a run trivially
    /// replayable from a snapshot.
    /// </para>
    /// </remarks>
    public readonly struct Pcg32 : IEquatable<Pcg32>
    {
        private const ulong Multiplier = 6364136223846793005UL;

        private readonly ulong _state;
        private readonly ulong _increment;

        private Pcg32(ulong state, ulong increment)
        {
            _state = state;
            _increment = increment;
        }

        /// <summary>
        /// Creates a generator from a starting state and a stream selector.
        /// </summary>
        /// <param name="state">The seed value.</param>
        /// <param name="sequence">
        /// Selects one of 2^63 distinct streams. Two generators sharing a state
        /// but differing here produce unrelated sequences, which is how
        /// subsystems draw independently without one shifting another.
        /// </param>
        public static Pcg32 Seed(ulong state, ulong sequence)
        {
            // The reference implementation's seeding routine: set the increment
            // from the stream, step once, add the seed, step again.
            var increment = (sequence << 1) | 1UL;
            var generator = new Pcg32(0UL, increment);

            generator = generator.Advance();
            generator = new Pcg32(generator._state + state, increment);
            return generator.Advance();
        }

        /// <summary>
        /// Draws the next 32-bit value, returning it alongside the advanced
        /// generator.
        /// </summary>
        public (Pcg32 Generator, uint Value) NextUInt32()
        {
            var previousState = _state;
            var advanced = Advance();

            // XSH-RR output permutation: xorshift the high bits down, then
            // rotate by an amount taken from the top five bits of the state.
            var xorshifted = (uint)(((previousState >> 18) ^ previousState) >> 27);
            var rotation = (int)(previousState >> 59);
            var value = (xorshifted >> rotation) | (xorshifted << ((-rotation) & 31));

            return (advanced, value);
        }

        /// <summary>
        /// Draws the next value in <c>[0, exclusiveBound)</c> with no modulo
        /// bias, returning it alongside the advanced generator.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="exclusiveBound"/> is zero, because no
        /// value can satisfy an empty range.
        /// </exception>
        public (Pcg32 Generator, uint Value) NextUInt32(uint exclusiveBound)
        {
            if (exclusiveBound == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveBound),
                    "An exclusive bound of zero admits no values.");
            }

            // Discard the values that would make the range wrap unevenly.
            // Rejection keeps every outcome equally likely, which plain modulo
            // does not, and it stays deterministic because the rejected draws
            // are part of the sequence.
            var threshold = unchecked((uint)-(int)exclusiveBound) % exclusiveBound;

            var generator = this;
            while (true)
            {
                uint candidate;
                (generator, candidate) = generator.NextUInt32();

                if (candidate >= threshold)
                {
                    return (generator, candidate % exclusiveBound);
                }
            }
        }

        public bool Equals(Pcg32 other) => _state == other._state && _increment == other._increment;

        public override bool Equals(object? obj) => obj is Pcg32 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(_state, _increment);

        private Pcg32 Advance()
            => new Pcg32(unchecked((_state * Multiplier) + _increment), _increment);
    }
}
