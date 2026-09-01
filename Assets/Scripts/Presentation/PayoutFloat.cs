using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// How a payout number rises and fades, as a pure function of a phase.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same shape as <c>EndpointPulse</c> and <c>BoardIntroFlight</c>, and for the same reasons: it
    /// can be frozen anywhere for a preview capture, and it can be checked without a device.
    /// </para>
    /// <para>
    /// This exists because the score curve is the centre of the design — PRD section 3.2A — and until
    /// now nothing on screen said what a route paid. A player saw a bar move and a total change, with no
    /// way to connect either to the route they had just finished.
    /// </para>
    /// </remarks>
    internal static class PayoutFloat
    {
        /// <summary>How long a number stays on screen, in seconds.</summary>
        /// <remarks>
        /// Long enough to read four digits, short enough not to still be there when the next tile lands.
        /// </remarks>
        internal const float DurationSeconds = 1.1f;

        /// <summary>How far the number travels, as a fraction of screen height.</summary>
        /// <remarks>
        /// Small. The rise is there to separate the number from the cell it came from, not to carry it
        /// somewhere — a number that flies across the screen is a number nobody reads.
        /// </remarks>
        internal const float RiseHeightFraction = 0.05f;

        /// <summary>
        /// How far the number has risen, in viewport fractions, and how opaque it is.
        /// </summary>
        /// <param name="phase">Zero when it appears, one when it is gone. Clamped.</param>
        internal static (float Rise, float Alpha) Evaluate(float phase)
        {
            var clamped = Mathf.Clamp01(phase);

            // Rises fast and then eases, so the movement reads as the number leaving rather than
            // drifting: most of the travel is over before the fade begins.
            var rise = RiseHeightFraction * (1f - ((1f - clamped) * (1f - clamped)));

            // Fully opaque for the first half, then fades. A number that starts fading immediately is
            // one a player has to hurry to read.
            var alpha = clamped < 0.5f ? 1f : 1f - ((clamped - 0.5f) * 2f);

            return (rise, alpha);
        }

        /// <summary>
        /// The same, for a player who has asked for reduced motion.
        /// </summary>
        /// <remarks>
        /// Holds still and fades. The number is information, so it is never removed — but nothing about
        /// reading it requires it to move, which is exactly the kind of motion to drop first.
        /// </remarks>
        internal static (float Rise, float Alpha) EvaluateStill(float phase)
            => (0f, Evaluate(phase).Alpha);
    }
}
