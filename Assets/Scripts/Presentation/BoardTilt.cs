using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// How far the board leans, how tall its cells stand, and where it sits in depth.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The lean is applied to the board's own root transform, not to the camera. Under an orthographic
    /// projection those produce the same image, and rotating the board leaves the camera axis-aligned —
    /// which every HUD view depends on, since each anchors itself by calling
    /// <c>ViewportToWorldPoint</c> and then overwriting z. Tilting the camera would have skewed the
    /// tray, the pip columns, and every button along with the board.
    /// </para>
    /// <para>
    /// Kept apart from <see cref="BoardView"/> and free of any component, so the arithmetic that
    /// decides whether the board still fits on a phone can be checked without one — the same reason
    /// <c>EndpointPulse</c> is separate from the thing that applies it.
    /// </para>
    /// </remarks>
    internal static class BoardTilt
    {
        /// <summary>
        /// How far the board leans away from the viewer, in degrees.
        /// </summary>
        /// <remarks>
        /// Enough to read as depth and not enough to foreshorten a hexagon into an ambiguous shape.
        /// Every vertical distance on the board is multiplied by its cosine, so a steeper lean costs
        /// board area on a screen that has none to spare.
        /// </remarks>
        internal const float Degrees = 15f;

        /// <summary>
        /// How tall a cell stands, in world units.
        /// </summary>
        /// <remarks>
        /// Half an edge. A regular hexagon's edge equals its circumradius, and
        /// <see cref="HexMetrics.Size"/> is that circumradius, so this is half of it.
        /// </remarks>
        internal const float BlockHeight = HexMetrics.Size * 0.5f;

        /// <summary>
        /// The least far back the board ever sits, in world units.
        /// </summary>
        /// <remarks>
        /// A floor rather than the answer. <see cref="DepthOffsetFor"/> is what a board actually uses.
        /// </remarks>
        internal const float MinimumDepthOffset = 1.5f;

        /// <summary>
        /// How much clearance the board keeps in front of itself, in world units.
        /// </summary>
        internal const float DepthClearance = 0.3f;

        /// <summary>
        /// How far back a board of the given half height has to sit, in world units.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The lean swings the near edge of the board toward the viewer by the board's own half height
        /// times the sine of the angle. This has to be a function of the board rather than a constant,
        /// which a fixed 1.5 was: it was chosen against a half height of 2.5, and the first valley large
        /// enough to need panning has a half height of 6.5. Its southern rim reached z = -0.18 — in front
        /// of the backdrop that keeps the board out from behind the interface, and almost in front of the
        /// tray.
        /// </para>
        /// <para>
        /// The half height is the board's own, before the lean foreshortens it, because the swing is
        /// driven by the untilted distance from the middle.
        /// </para>
        /// </remarks>
        internal static float DepthOffsetFor(float boardHalfHeight)
            => Mathf.Max(
                MinimumDepthOffset,
                (boardHalfHeight * Mathf.Sin(Degrees * Mathf.Deg2Rad)) + DepthClearance);

        /// <summary>The board root's rotation.</summary>
        /// <remarks>
        /// Positive about X, so the far rows lean away and the near rows lean toward the viewer. The
        /// other sign would tip the board over backwards and show it from underneath.
        /// </remarks>
        internal static Quaternion Rotation => Quaternion.Euler(Degrees, 0f, 0f);

        /// <summary>Where the board root sits, for a board of the given half height.</summary>
        internal static Vector3 PositionFor(float boardHalfHeight)
            => new Vector3(0f, 0f, DepthOffsetFor(boardHalfHeight));

        /// <summary>
        /// How much a vertical distance on the board shrinks on screen.
        /// </summary>
        /// <remarks>
        /// The cosine of the lean. Around 0.966 at 15 degrees, which is why the lean is affordable.
        /// </remarks>
        internal static float VerticalForeshortening => Mathf.Cos(Degrees * Mathf.Deg2Rad);

        /// <summary>
        /// How far below its own top face a cell's near rim reaches on screen, in world units.
        /// </summary>
        /// <remarks>
        /// A leaning block hangs below the plane its top face lies in, so the bottom row of a board
        /// needs this much more room than its centres suggest. Forgetting it clips the near rim, which
        /// looks like a rendering fault rather than a framing one.
        /// </remarks>
        internal static float ScreenOverhang
            => BlockHeight * Mathf.Sin(Degrees * Mathf.Deg2Rad);
    }
}
