using Pathweaver.Core.Flow;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// The shape of the breath that tells a spring from a hub.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A spring's ring grows from the middle of the cell out to its rim; a hub's collapses from the rim
    /// into the middle. That is the second channel the art guide asks for in section 9 — "a radiating
    /// versus converging silhouette" — and it matters beyond decoration: until now the only thing
    /// separating a source from a destination was a background colour, which a colour-blind player does
    /// not have.
    /// </para>
    /// <para>
    /// Pure maths, separate from the component that applies it, so the motion can be checked without a
    /// device and so a still frame can be posed at any phase for review.
    /// </para>
    /// </remarks>
    internal static class EndpointPulse
    {
        /// <summary>
        /// How long one breath takes, in seconds.
        /// </summary>
        /// <remarks>
        /// Slow on purpose. The frame governor drops to 30 Hz once the board is idle, which is where a
        /// board being thought about spends its life, and this gives a cycle 54 frames to travel in. A
        /// faster pulse looks stepped at 30 Hz — and the fix for that must never be to raise the idle
        /// rate, which is a hard budget in PRD section 5.2.
        /// </remarks>
        internal const float PeriodSeconds = 1.8f;

        /// <summary>
        /// The largest the ring gets, as a share of the cell.
        /// </summary>
        /// <remarks>
        /// Below one, because the ring is built at the drawn hexagon's own radius: at a scale above one
        /// it would spill into the neighbouring cell and read as a rendering fault rather than a pulse.
        /// </remarks>
        internal const float MaximumScale = 0.92f;

        /// <summary>The scale a ring rests at when motion is switched off.</summary>
        internal const float RestingScale = 0f;

        /// <summary>How small the ring is at the tight end of its travel.</summary>
        private const float MinimumScale = 0.18f;

        /// <summary>
        /// How large the ring is at a point in the cycle, as a transform scale.
        /// </summary>
        /// <remarks>
        /// Eased so the ring lingers where it is most legible — near the rim for a spring, near the
        /// centre for a hub — and moves quickly through the other end.
        /// </remarks>
        internal static float ScaleAt(float elapsedSeconds, EndpointRole role)
        {
            var travel = Eased(PhaseOf(elapsedSeconds));

            // A hub is the mirror of a spring, so the two are guaranteed to differ rather than being
            // two copies of the same easing with a sign somebody might get wrong.
            if (role == EndpointRole.Hub)
            {
                travel = 1f - travel;
            }

            return Mathf.Lerp(MinimumScale, MaximumScale, travel);
        }

        /// <summary>
        /// How far the ring has faded into the cell behind it: 0 fully lit, 1 invisible.
        /// </summary>
        /// <remarks>
        /// The material is opaque — there is no alpha to fade — so a ring disappears by being lerped to
        /// the colour of the cell it sits on. It has to reach exactly 1 at the end of the cycle, or the
        /// jump back to the start of the next one is visible as a flicker.
        /// </remarks>
        internal static float FadeAt(float elapsedSeconds)
        {
            var phase = PhaseOf(elapsedSeconds);

            // Lit for the first part of the travel, dissolving over the rest. Fading from the very
            // first frame would leave the ring dim for its whole life.
            const float HeldLit = 0.45f;

            if (phase <= HeldLit)
            {
                return 0f;
            }

            return (phase - HeldLit) / (1f - HeldLit);
        }

        /// <summary>
        /// A per-endpoint offset, so several endpoints on one board do not beat in unison.
        /// </summary>
        /// <remarks>
        /// Derived from the cell's own coordinates rather than from a counter or the time it was created,
        /// because the board is rebuilt on every state change: anything else would make every endpoint
        /// on the board jump each time the player rotated the tile in hand.
        /// </remarks>
        internal static float PhaseOffsetFor(int q, int r)
        {
            // The golden ratio's fractional part spreads any number of offsets evenly across the cycle.
            const float GoldenFraction = 0.618034f;

            var index = (q * 31) + (r * 17);
            return Mathf.Repeat(index * GoldenFraction, 1f) * PeriodSeconds;
        }

        private static float PhaseOf(float elapsedSeconds)
            => Mathf.Repeat(elapsedSeconds, PeriodSeconds) / PeriodSeconds;

        /// <summary>Smoothstep: slow at both ends, quick through the middle.</summary>
        private static float Eased(float phase) => phase * phase * (3f - (2f * phase));
    }
}
