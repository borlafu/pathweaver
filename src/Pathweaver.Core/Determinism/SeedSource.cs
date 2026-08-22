using System;

namespace Pathweaver.Core.Determinism
{
    /// <summary>
    /// Turns a calendar date into a seed, and a seed into per-subsystem
    /// generators.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Daily Expedition must present the same puzzle to every player who
    /// opens the game on a given date, computed on the device with no network
    /// call. That makes the date the only input, and makes this class the single
    /// place a date becomes randomness.
    /// </para>
    /// <para>
    /// Callers pass an explicit year, month, and day rather than a
    /// <see cref="DateTime"/>. That keeps the decision about which clock to read
    /// — device local date, as the product requires — at the presentation layer,
    /// and keeps this deterministic and trivially testable.
    /// </para>
    /// </remarks>
    public static class SeedSource
    {
        private const int MinimumYear = 1;
        private const int MaximumYear = 9999;

        /// <summary>
        /// Derives the seed for a calendar date.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the date does not exist, including 29 February outside a
        /// leap year.
        /// </exception>
        public static ulong ForDate(int year, int month, int day)
        {
            if (year < MinimumYear || year > MaximumYear)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year), year, $"Year must be between {MinimumYear} and {MaximumYear}.");
            }

            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(month), month, "Month must be between 1 and 12.");
            }

            var daysInMonth = DateTime.DaysInMonth(year, month);
            if (day < 1 || day > daysInMonth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(day), day, $"Day must be between 1 and {daysInMonth} for {year}-{month:00}.");
            }

            // Multiplying each component by a distinct odd constant keeps
            // transposed dates apart, which a plain yyyymmdd concatenation does
            // not guarantee once it reaches the mixer.
            var packed = unchecked(
                ((ulong)year * 100_000UL) +
                ((ulong)month * 1_000UL) +
                (ulong)day);

            return Mix(packed);
        }

        /// <summary>
        /// Builds the generator a given subsystem draws from.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the stream is not a defined <see cref="PathweaverStream"/>.
        /// </exception>
        public static Pcg32 Stream(ulong seed, PathweaverStream stream)
        {
            if (!Enum.IsDefined(typeof(PathweaverStream), stream))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stream), stream, "Unknown stream.");
            }

            return Pcg32.Seed(seed, (ulong)stream);
        }

        /// <summary>
        /// The SplitMix64 finalizer. Scatters a counter-like input so that
        /// neighbouring values produce unrelated outputs.
        /// </summary>
        private static ulong Mix(ulong value)
        {
            unchecked
            {
                var z = value + 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}
