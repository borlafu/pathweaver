using System;

namespace Pathweaver.Core.Endless
{
    /// <summary>
    /// What a given round of Endless Wayfare asks for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Separated from the generator because escalation is the whole progression in Endless — there
    /// is nothing to unlock — so it is the part most likely to be retuned, and it is worth being
    /// able to read the curve without reading the board builder.
    /// </para>
    /// <para>
    /// Every value is a function of the round alone, never of the seed. Two players on round nine
    /// face boards of the same size and the same demands, and only the layout differs.
    /// </para>
    /// </remarks>
    internal readonly struct EndlessDifficulty
    {
        private EndlessDifficulty(int pairs, int routeLength, int radius, int startingSkips, int startingTokens)
        {
            Pairs = pairs;
            RouteLength = routeLength;
            Radius = radius;
            StartingSkips = startingSkips;
            StartingTokens = startingTokens;
        }

        /// <summary>How many spring and hub pairs the board carries.</summary>
        internal int Pairs { get; }

        /// <summary>How many conduits each planned route needs.</summary>
        internal int RouteLength { get; }

        internal int Radius { get; }

        internal int StartingSkips { get; }

        internal int StartingTokens { get; }

        /// <summary>
        /// The share of the plan's own score a player must reach, as a fraction: four fifths.
        /// </summary>
        /// <remarks>
        /// Below one on purpose. At exactly the plan's score a single wasted tile in a planned cell
        /// would make the round unwinnable, and a player with no skips left can be forced to waste
        /// one. A fifth of slack is roughly one route falling a conduit or two short.
        /// <para>
        /// A fraction rather than 0.8, because no part of the simulation may use floating point:
        /// two devices are allowed to disagree about the last bit of a double, and they are not
        /// allowed to disagree about whether a round was cleared.
        /// </para>
        /// </remarks>
        internal const int TargetNumerator = 4;

        /// <summary>Denominator of <see cref="TargetNumerator"/>.</summary>
        internal const int TargetDenominator = 5;

        /// <summary>Resource kinds available, in the order rounds introduce them.</summary>
        internal const int MaximumPairs = 3;

        internal static EndlessDifficulty ForRound(int round)
        {
            if (round < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(round), round, "Rounds are counted from one.");
            }

            // A second network at round 5, a third at round 9: enough rounds of one network to
            // learn the length curve before the board asks for attention in two places.
            var pairs = Math.Min(MaximumPairs, 1 + ((round - 1) / 4));

            // Routes lengthen every third round and stop at eight. Beyond that the length
            // multiplier is worth more than a whole extra route, which turns every board into the
            // same board: find the longest snake.
            var routeLength = Math.Min(8, 3 + ((round - 1) / 3));

            // The board has to hold every planned route with room to spare, or the walks have
            // nowhere to go and the generator spends its attempts backing out of dead ends. Twice
            // the planned cells is what that spare room costs.
            var demand = pairs * (routeLength + 2) * 2;
            var radius = SmallestRadiusHolding(demand);

            // Skips carry the round when the bag deals for the other network. One Pivot Token from
            // round ten, where three networks and eight-conduit routes leave no room for a
            // misplacement.
            var startingSkips = 3;
            var startingTokens = round >= 10 ? 1 : 0;

            return new EndlessDifficulty(pairs, routeLength, radius, startingSkips, startingTokens);
        }

        /// <summary>Cells in a hexagon of the given radius: 3r² + 3r + 1.</summary>
        internal static int CellsInHexagon(int radius) => (3 * radius * radius) + (3 * radius) + 1;

        private static int SmallestRadiusHolding(int cells)
        {
            for (var radius = 2; radius < 6; radius++)
            {
                if (CellsInHexagon(radius) >= cells)
                {
                    return radius;
                }
            }

            return 6;
        }
    }
}
