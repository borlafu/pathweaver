using System;
using System.Collections.Generic;
using Pathweaver.Core.Flow;

namespace Pathweaver.Core.Rules
{
    /// <summary>
    /// What earns a Pivot Token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PRD section 3.2B says tokens come from "high-efficiency plays" without
    /// fixing a number. The chosen rule: a completed route of
    /// <see cref="MinimumRouteLength"/> conduits or more earns one token.
    /// </para>
    /// <para>
    /// That threshold rewards exactly the extended routing the
    /// <c>1.35^(L-1)</c> curve already pushes players toward, so taking the
    /// congestion risk also earns the means to escape its consequences. Risk and
    /// rescue reinforce each other instead of pulling apart, and the rule is short
    /// enough for the interface to teach in one line.
    /// </para>
    /// </remarks>
    public static class PivotTokenRule
    {
        /// <summary>
        /// The shortest route that earns a token. Public because the player has to
        /// be able to learn the rule.
        /// </summary>
        public const int MinimumRouteLength = 4;

        /// <summary>
        /// Tokens earned by a single route of the given length.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown for a length below one, which is not a completed route.
        /// </exception>
        public static int TokensFor(int routeLength)
        {
            if (routeLength < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(routeLength), routeLength, "A completed route covers at least one conduit.");
            }

            return routeLength >= MinimumRouteLength ? 1 : 0;
        }

        /// <summary>
        /// Tokens earned by a set of newly completed routes.
        /// </summary>
        public static int TokensEarned(IEnumerable<Route> routes)
        {
            if (routes is null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            var tokens = 0;
            foreach (var route in routes)
            {
                tokens += TokensFor(route.Length);
            }

            return tokens;
        }
    }
}
