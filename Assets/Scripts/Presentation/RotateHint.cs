using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// The shape of the shake that tells a player the tile in hand can be turned.
    /// </summary>
    /// <remarks>
    /// Pure maths, separate from the component that applies it, so the motion can be
    /// checked without a device or a running frame loop.
    /// </remarks>
    internal static class RotateHint
    {
        /// <summary>How far the tile twists at the strongest point, in degrees.</summary>
        internal const float AmplitudeDegrees = 14f;

        /// <summary>How long one shake lasts, in seconds.</summary>
        internal const float DurationSeconds = 0.55f;

        /// <summary>The wait between shakes, in seconds.</summary>
        /// <remarks>
        /// Long enough not to nag, short enough that a player who missed one sees the next
        /// while still looking at the same board.
        /// </remarks>
        internal const float IntervalSeconds = 2.5f;

        /// <summary>Back-and-forth twists within one shake.</summary>
        private const float Oscillations = 1.5f;

        /// <summary>
        /// The twist at a point in the shake, in degrees.
        /// </summary>
        /// <remarks>
        /// A sine wave with amplitude falling to zero, so the tile ends exactly where it
        /// started. Anything that does not settle at zero would leave the tile visibly
        /// crooked, which reads as a rendering fault rather than a hint.
        /// </remarks>
        internal static float AngleAt(float elapsedSeconds)
        {
            if (elapsedSeconds <= 0f || elapsedSeconds >= DurationSeconds)
            {
                return 0f;
            }

            var progress = elapsedSeconds / DurationSeconds;
            var decay = 1f - progress;

            return Mathf.Sin(progress * Oscillations * 2f * Mathf.PI) * AmplitudeDegrees * decay;
        }
    }
}
