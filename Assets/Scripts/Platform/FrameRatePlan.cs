using UnityEngine;

namespace Pathweaver.Game.Platform
{
    /// <summary>
    /// Chooses refresh rates, separated from the component that applies them so the
    /// decision can be tested without a device.
    /// </summary>
    /// <remarks>
    /// PRD section 5.2 asks for 120 or 90 Hz while a tile is moving and 30 Hz while the
    /// player is thinking. Battery life is a stated value for the commuter persona, and
    /// a puzzle board that is not animating has nothing to redraw.
    /// </remarks>
    internal static class FrameRatePlan
    {
        /// <summary>The fastest rate worth asking for, however capable the screen.</summary>
        internal const int MaximumActiveHz = 120;

        /// <summary>What to run at while nothing is moving.</summary>
        internal const int IdleHz = 30;

        /// <summary>
        /// How long after the last interaction to drop to the idle rate.
        /// </summary>
        /// <remarks>
        /// Long enough that a pause between two placements does not visibly stutter,
        /// short enough that a player reading the board is not paying for frames.
        /// </remarks>
        internal const float IdleAfterSeconds = 1.5f;

        /// <summary>
        /// The active rate for a screen of the given refresh rate.
        /// </summary>
        /// <remarks>
        /// Never asks for more than the screen can deliver, and never drops below the
        /// idle rate: a device reporting something implausible should still get a
        /// playable frame rate rather than whatever the number says.
        /// </remarks>
        internal static int ActiveRateFor(float screenRefreshHz)
        {
            if (screenRefreshHz <= 0f || float.IsNaN(screenRefreshHz))
            {
                // An unknown screen gets a rate every Android device can manage.
                return 60;
            }

            var rounded = Mathf.RoundToInt(screenRefreshHz);
            return Mathf.Clamp(rounded, IdleHz, MaximumActiveHz);
        }
    }
}
