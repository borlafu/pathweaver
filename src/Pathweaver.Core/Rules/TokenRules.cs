using System;
using System.Collections.Generic;
using Pathweaver.Core.Flow;

namespace Pathweaver.Core.Rules
{
    /// <summary>
    /// What completing a route earns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PRD section 3.2B asks for tokens from "high-efficiency plays" without fixing a
    /// number, and section 3.2A frames the game's central choice as a short certain
    /// route against a long risky one. These rules put a reward on each side of that
    /// choice rather than on only one.
    /// </para>
    /// <para>
    /// A route of <see cref="PivotThreshold"/> conduits or more earns a Pivot Token,
    /// so taking the congestion risk also earns the means to escape its consequences.
    /// A shorter route earns a skip instead, so playing safe buys flexibility rather
    /// than nothing.
    /// </para>
    /// <para>
    /// Neither strategy dominates: power comes from length, room to manoeuvre comes
    /// from closing early, and a player who only ever does one runs short of the other.
    /// </para>
    /// </remarks>
    public static class TokenRules
    {
        /// <summary>
        /// The shortest route that earns a Pivot Token. Public because the player has
        /// to be able to learn the rule.
        /// </summary>
        public const int PivotThreshold = 4;

        /// <summary>
        /// How many of either token a player may hold before earning stops paying.
        /// </summary>
        /// <remarks>
        /// A ceiling exists so that holding tokens is a decision rather than a savings account: a
        /// full pool means the next completed route pays nothing in that currency, which is the
        /// pressure to spend. Three because that is what the interface has always shown — a pip
        /// column of three — and a counter that disagreed with the count it displayed is the defect
        /// this constant answers.
        /// </remarks>
        public const int BaseCapacity = 3;

        /// <summary>
        /// The largest ceiling any progression may reach.
        /// </summary>
        /// <remarks>
        /// Relics raise the ceiling as well as the opening hand, or an upgrade that dealt a fourth
        /// token would hand the player something they could not hold. Five is the end of that road:
        /// beyond it the anti-deadlock tokens of PRD section 3.2B stop being scarce, and a board that
        /// cannot deadlock is a board without a decision in it.
        /// </remarks>
        public const int MaximumCapacity = 5;

        /// <summary>
        /// The ceiling a player reaches with a given number of relics of one kind.
        /// </summary>
        /// <remarks>
        /// The whole progression arithmetic, in one place: the base ceiling plus what has been
        /// unlocked, never above <see cref="MaximumCapacity"/>. A pack that ships more relics than
        /// the band has room for therefore costs nothing in ceiling rather than breaking the band.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown for a negative count.</exception>
        public static int CapacityWith(int relics)
        {
            if (relics < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(relics), relics, "A ceiling cannot be lowered by a relic.");
            }

            return Math.Min(BaseCapacity + relics, MaximumCapacity);
        }

        /// <summary>
        /// Pivot Tokens earned by a single route.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown for a length below one, which is not a completed route.
        /// </exception>
        public static int PivotTokensFor(int routeLength)
            => RequireLength(routeLength) >= PivotThreshold ? 1 : 0;

        /// <summary>
        /// Skips earned by a single route.
        /// </summary>
        /// <remarks>
        /// Exactly the routes that earn no Pivot Token, so every completed route pays
        /// out in one currency or the other and none feels wasted.
        /// </remarks>
        public static int SkipTokensFor(int routeLength)
            => RequireLength(routeLength) < PivotThreshold ? 1 : 0;

        /// <summary>
        /// Pivot Tokens earned across a set of newly completed routes.
        /// </summary>
        public static int PivotTokensEarned(IEnumerable<Route> routes)
            => Sum(routes, PivotTokensFor);

        /// <summary>
        /// Skips earned across a set of newly completed routes.
        /// </summary>
        public static int SkipTokensEarned(IEnumerable<Route> routes)
            => Sum(routes, SkipTokensFor);

        private static int Sum(IEnumerable<Route> routes, Func<int, int> perRoute)
        {
            if (routes is null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            var total = 0;
            foreach (var route in routes)
            {
                total += perRoute(route.Length);
            }

            return total;
        }

        private static int RequireLength(int routeLength)
        {
            if (routeLength < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(routeLength), routeLength, "A completed route covers at least one conduit.");
            }

            return routeLength;
        }
    }
}
