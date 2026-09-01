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
        /// How far back the whole board sits, in world units.
        /// </summary>
        /// <remarks>
        /// The lean swings the near edge of the board toward the viewer by up to the board's own half
        /// height times the sine of the angle — around 0.65 on a large level. Without this offset that
        /// edge would cross in front of the tray at -0.2 and the pip columns at -0.4, and board cells
        /// would draw over the HUD. One offset does what a second camera and a layer mask were going to.
        /// </remarks>
        internal const float DepthOffset = 1.5f;

        /// <summary>The board root's rotation.</summary>
        /// <remarks>
        /// Positive about X, so the far rows lean away and the near rows lean toward the viewer. The
        /// other sign would tip the board over backwards and show it from underneath.
        /// </remarks>
        internal static Quaternion Rotation => Quaternion.Euler(Degrees, 0f, 0f);

        /// <summary>The board root's position.</summary>
        internal static Vector3 Position => new Vector3(0f, 0f, DepthOffset);

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
