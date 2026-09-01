using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// The move from a whole-board view down to the zoom a board is played at.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A board too large to fit on a phone has to be navigated, and a player cannot navigate somewhere
    /// they have never seen. So a board opens by showing all of itself and then settling near a spring:
    /// the pan that follows is navigation rather than groping.
    /// </para>
    /// <para>
    /// A pure function of a phase, exactly as <c>EndpointPulse</c> and <c>FlowPulse</c> are. That is what
    /// lets <c>CaptureBoardPreview</c> freeze it anywhere in its run and judge the motion from four
    /// stills without a device, and what makes it testable at all.
    /// </para>
    /// <para>
    /// Zoom is interpolated in the logarithm of the orthographic size rather than linearly. A linear
    /// ramp between 9 and 3 spends most of its time in the wide half and then rushes the last part,
    /// because what the eye reads as speed is the proportional change; the logarithm makes it even.
    /// </para>
    /// </remarks>
    internal static class BoardIntroFlight
    {
        /// <summary>
        /// How long the flight runs, in seconds.
        /// </summary>
        /// <remarks>
        /// Long enough to read the board's shape, short enough not to be in the way of a player who
        /// already knows it. The board is not interactive during it, so this is time taken from someone
        /// who wants to play.
        /// </remarks>
        internal const float DurationSeconds = 1.1f;

        /// <summary>
        /// Where the camera looks and how much it shows, at the given point in the flight.
        /// </summary>
        /// <param name="phase">
        /// Zero at the birds-eye view, one at the playing zoom. Values outside are clamped, so a phase
        /// past the end rests exactly on the destination rather than overshooting it.
        /// </param>
        internal static (Vector2 LookAt, float OrthographicSize) Evaluate(
            float phase,
            Vector2 birdsEyeLookAt,
            float birdsEyeSize,
            Vector2 playingLookAt,
            float playingSize)
        {
            var eased = Ease(Mathf.Clamp01(phase));

            return (
                Vector2.Lerp(birdsEyeLookAt, playingLookAt, eased),
                Mathf.Exp(Mathf.Lerp(Mathf.Log(birdsEyeSize), Mathf.Log(playingSize), eased)));
        }

        /// <summary>
        /// Smooth at both ends.
        /// </summary>
        /// <remarks>
        /// A camera that starts and stops abruptly reads as a cut rather than a move, and a cut does not
        /// tell the player that where they end up is part of where they began — which is the entire
        /// point of showing the board first.
        /// </remarks>
        internal static float Ease(float phase)
        {
            var clamped = Mathf.Clamp01(phase);

            return clamped * clamped * (3f - (2f * clamped));
        }
    }
}
