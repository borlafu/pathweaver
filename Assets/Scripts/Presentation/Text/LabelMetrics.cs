namespace Pathweaver.Game.Presentation.Text
{
    /// <summary>
    /// How large a line of text should be, in the units TextMesh Pro wants.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Separate from <see cref="TextLabel"/> and free of <c>UnityEngine</c> types for the same reason
    /// <c>EndpointPulse</c> is separate from the component that applies it: a number that decides
    /// whether text is legible on a phone should be checkable without a phone.
    /// </para>
    /// <para>
    /// Sizes are fractions of screen height rather than world units. Every other control in the game
    /// is a fixed world size — <c>TokenPipsView.PipRadius</c> is 0.11 — which means they shrink on
    /// screen as the board camera zooms out to fit a larger board. A pip that is slightly smaller is
    /// still a pip; text that is slightly smaller is unreadable, so text is sized against the screen
    /// instead and does not care how large the board is.
    /// </para>
    /// </remarks>
    internal static class LabelMetrics
    {
        /// <summary>
        /// TextMesh Pro font-size points per world unit, for text in world space.
        /// </summary>
        /// <remarks>
        /// A world-space text mesh at font size 10 stands one world unit tall, so a size in points is
        /// a height in world units multiplied by ten. Named because it is a fact about TextMesh Pro
        /// rather than a choice, and a bare 10 in the arithmetic below would read as either.
        /// </remarks>
        internal const float PointsPerWorldUnit = 10f;

        /// <summary>A help-screen heading, or the one number a screen is about.</summary>
        internal const float HeadingHeightFraction = 0.034f;

        /// <summary>Ordinary prose and the numbers beside the progress bar.</summary>
        internal const float BodyHeightFraction = 0.022f;

        /// <summary>A level name, a unit, or anything qualifying something larger beside it.</summary>
        internal const float CaptionHeightFraction = 0.017f;

        /// <summary>
        /// The smallest text the game may draw, as a fraction of screen height.
        /// </summary>
        /// <remarks>
        /// On the 2376-pixel-tall phone the previews are rendered at, this is about 38 pixels — a
        /// little above the 14sp Android treats as the floor for body text at a typical density. It
        /// exists so a future caller cannot quietly halve a size to make something fit; the fix for
        /// text that does not fit is fewer words.
        /// </remarks>
        internal const float MinimumHeightFraction = 0.016f;

        /// <summary>
        /// The height of one line, in world units, for a camera of the given orthographic size.
        /// </summary>
        /// <remarks>
        /// An orthographic size is a half-height, so the visible world height is twice it. That
        /// doubling is the mistake <c>BoardCameraFitter</c> was written to remember.
        /// </remarks>
        internal static float WorldHeight(float orthographicSize, float heightFraction)
            => orthographicSize * 2f * heightFraction;

        /// <summary>
        /// The TextMesh Pro font size that fills the given fraction of screen height.
        /// </summary>
        internal static float FontSize(float orthographicSize, float heightFraction)
            => WorldHeight(orthographicSize, heightFraction) * PointsPerWorldUnit;
    }
}
