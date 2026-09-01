using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// How a first-run hint appears, waits, and leaves.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pure function of a phase, like <c>PayoutFloat</c> and <c>EndpointPulse</c>, so it can be frozen
    /// for a capture and checked without a device.
    /// </para>
    /// <para>
    /// It has a hold in the middle, which the payout float does not: a payout is a reward and a hint is a
    /// sentence, and a sentence needs time to be read by someone who is not expecting it.
    /// </para>
    /// </remarks>
    internal static class CoachMarkFade
    {
        /// <summary>How long a hint stays on screen if the player does nothing, in seconds.</summary>
        /// <remarks>
        /// Long enough to read twice, short enough that a hint never becomes furniture. A hint is also
        /// dismissed the moment the player does the thing it asked for, which is the usual way it ends.
        /// </remarks>
        internal const float DurationSeconds = 5.5f;

        /// <summary>What share of the time is spent appearing, and the same again leaving.</summary>
        private const float EdgeFraction = 0.12f;

        /// <summary>How opaque the hint is at the given point in its life.</summary>
        /// <param name="phase">Zero when it appears, one when it is gone. Clamped.</param>
        internal static float AlphaAt(float phase)
        {
            var clamped = Mathf.Clamp01(phase);

            if (clamped < EdgeFraction)
            {
                return clamped / EdgeFraction;
            }

            if (clamped > 1f - EdgeFraction)
            {
                return (1f - clamped) / EdgeFraction;
            }

            return 1f;
        }

        /// <summary>
        /// The same, for a player who has asked for reduced motion.
        /// </summary>
        /// <remarks>
        /// A fade is not motion in the sense the setting is about — nothing travels — so it is kept. What
        /// would be wrong here is removing the hint, which is information.
        /// </remarks>
        internal static float AlphaAtStill(float phase) => AlphaAt(phase);
    }
}
