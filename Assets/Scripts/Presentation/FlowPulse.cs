using System.Collections.Generic;
using Pathweaver.Core.Hex;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Where the light has got to along a completed route.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only completed routes flow, so movement along a conduit means "this pays" rather than "this is
    /// connected to something". The art guide already reserves the middle of a conduit for exactly this:
    /// "the conduit's interior needs a clear, unobstructed channel down its centre for something to
    /// visibly move along."
    /// </para>
    /// <para>
    /// Every step of a route is one cell to a neighbouring cell, so every segment of the path is the same
    /// length. That is what keeps this O(1): no table of cumulative distances, no search — a divide finds
    /// the segment and the remainder finds the position along it.
    /// </para>
    /// </remarks>
    internal static class FlowPulse
    {
        /// <summary>
        /// How fast the light travels, in world units per second.
        /// </summary>
        /// <remarks>
        /// Speed rather than a time per route: a route of nine conduits should take longer to cross than
        /// one of two, or a short route looks frantic beside a long one.
        /// </remarks>
        internal const float SpeedPerSecond = 1.9f;

        /// <summary>
        /// Where the pulse is at a moment in time, given the points of a route.
        /// </summary>
        /// <remarks>
        /// The points run spring, conduits in order, hub — which is the order <c>Route.Tiles</c> already
        /// reports, so nothing here has to work out the path.
        /// </remarks>
        internal static Vector3 PositionAt(IReadOnlyList<Vector3> points, float elapsedSeconds, float phase)
        {
            if (points == null || points.Count == 0)
            {
                return Vector3.zero;
            }

            if (points.Count == 1)
            {
                return points[0];
            }

            var segments = points.Count - 1;
            var travelled = DistanceAlong(segments, elapsedSeconds, phase);

            var segment = Mathf.Clamp((int)travelled, 0, segments - 1);
            var withinSegment = travelled - segment;

            return Vector3.Lerp(points[segment], points[segment + 1], withinSegment);
        }

        /// <summary>
        /// How far along the route the pulse is, measured in segments.
        /// </summary>
        /// <remarks>
        /// Driven from absolute time rather than from a start time recorded when the route appeared. The
        /// route list is rebuilt on every state change — including a rotation of the tile in hand, which
        /// changes no route at all — so anything remembered per route would restart every pulse on the
        /// board on an unrelated move.
        /// </remarks>
        internal static float DistanceAlong(int segments, float elapsedSeconds, float phase)
        {
            if (segments <= 0)
            {
                return 0f;
            }

            var perSegment = HexMetrics.CellSpacing / SpeedPerSecond;
            var cycle = segments * perSegment;

            var offset = Mathf.Repeat(phase, 1f) * cycle;

            return Mathf.Repeat(elapsedSeconds + offset, cycle) / perSegment;
        }

        /// <summary>
        /// A route's place in the cycle, derived from its endpoints.
        /// </summary>
        /// <remarks>
        /// Two routes starting together would beat in unison; a phase from a counter or a creation time
        /// would jump whenever the route list was rebuilt. Endpoints never move, so this is stable for as
        /// long as the route exists — including across a pivot that reshapes the path between them.
        /// </remarks>
        internal static float PhaseFor(HexCoord spring, HexCoord hub)
        {
            const float GoldenFraction = 0.618034f;

            var index = (spring.Q * 73) + (spring.R * 31) + (hub.Q * 13) + (hub.R * 7);

            return Mathf.Repeat(index * GoldenFraction, 1f);
        }
    }
}
