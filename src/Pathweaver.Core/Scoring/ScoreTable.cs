using System;
using System.Numerics;

namespace Pathweaver.Core.Scoring
{
    /// <summary>
    /// The route length payoff curve from PRD section 3.2A,
    /// <c>S = S_base * 1.35^(L-1)</c>, in integer arithmetic.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Math.Pow</c> is deliberately absent. IEEE 754 permits differing
    /// last-bit results for transcendental functions across platforms and
    /// runtimes, so two devices could disagree about a score — unacceptable when
    /// the Daily Expedition presents the same puzzle worldwide and players compare
    /// results.
    /// </para>
    /// <para>
    /// Instead, multipliers are precomputed once as scaled integers using exact
    /// rational arithmetic: <c>round(135^(L-1) * Scale / 100^(L-1))</c>, evaluated
    /// with <see cref="BigInteger"/> so nothing overflows or rounds early. The
    /// obvious alternative — repeatedly multiplying by 135/100 — compounds its
    /// rounding error and lands about 5.5 million scaled units below the true
    /// value by <see cref="MaxRouteLength"/>.
    /// </para>
    /// </remarks>
    public static class ScoreTable
    {
        /// <summary>
        /// The fixed-point denominator multipliers are expressed in. A multiplier
        /// of <see cref="Scale"/> means exactly one.
        /// </summary>
        public const long Scale = 1_000_000L;

        /// <summary>
        /// The longest route the table covers.
        /// </summary>
        /// <remarks>
        /// A route cannot exceed the cell count of its level, and MVP levels are
        /// far smaller than this. The limit exists so the table is finite and so
        /// <see cref="MaxBaseScore"/> can be chosen to rule out overflow.
        /// </remarks>
        public const int MaxRouteLength = 64;

        /// <summary>
        /// The largest base score accepted.
        /// </summary>
        /// <remarks>
        /// Chosen so that <see cref="MaxBaseScore"/> multiplied by the largest
        /// multiplier stays inside a <see cref="long"/>. PRD base values are two
        /// digits, so this leaves ample room.
        /// </remarks>
        public const long MaxBaseScore = 10_000L;

        private const int NumeratorPerStep = 135;
        private const int DenominatorPerStep = 100;

        private static readonly long[] Multipliers = BuildMultipliers();

        /// <summary>
        /// The scaled multiplier for a route of the given tile length.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown for lengths below 1 or above <see cref="MaxRouteLength"/>.
        /// </exception>
        public static long MultiplierFor(int length)
        {
            RequireValidLength(length);
            return Multipliers[length];
        }

        /// <summary>
        /// The score a route earns, rounded to the nearest whole point with halves
        /// going up.
        /// </summary>
        /// <remarks>
        /// Rounding is defined rather than incidental because players compare
        /// routes, and an unexplained one-point difference reads as a bug.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown for a negative base score, a base score above
        /// <see cref="MaxBaseScore"/>, or an unsupported length. Rejecting a large
        /// base beats silently wrapping into a negative score.
        /// </exception>
        public static long ScoreFor(long baseScore, int length)
        {
            if (baseScore < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseScore), baseScore, "A base score cannot be negative.");
            }

            if (baseScore > MaxBaseScore)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseScore), baseScore, $"A base score cannot exceed {MaxBaseScore}.");
            }

            RequireValidLength(length);

            var scaled = baseScore * Multipliers[length];
            return (scaled + (Scale / 2)) / Scale;
        }

        /// <summary>
        /// Precomputes every multiplier exactly, from length 1 upward.
        /// </summary>
        /// <remarks>
        /// Index 0 is unused so that a length indexes itself. Numerator and
        /// denominator are kept as separate big integers and divided only once per
        /// entry, so no intermediate rounding occurs.
        /// </remarks>
        private static long[] BuildMultipliers()
        {
            var multipliers = new long[MaxRouteLength + 1];
            var scale = new BigInteger(Scale);

            for (var length = 1; length <= MaxRouteLength; length++)
            {
                var exponent = length - 1;
                var numerator = BigInteger.Pow(NumeratorPerStep, exponent) * scale;
                var denominator = BigInteger.Pow(DenominatorPerStep, exponent);

                // Round half up: add half the denominator before dividing.
                var rounded = ((numerator * 2) + denominator) / (denominator * 2);
                multipliers[length] = (long)rounded;
            }

            return multipliers;
        }

        private static void RequireValidLength(int length)
        {
            if (length < 1 || length > MaxRouteLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length), length, $"Route length must be between 1 and {MaxRouteLength}.");
            }
        }
    }
}
