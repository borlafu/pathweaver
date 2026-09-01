using Pathweaver.Core.State;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Frames the whole board on screen, leaving the bottom clear for the tile tray.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A fixed orthographic size cannot work. That value is a half-height, so on a tall
    /// phone the visible width is the height multiplied by an aspect ratio near 0.45 —
    /// a board six world units across shows about three. The first build on hardware
    /// filled the screen with four cells and cut the endpoints off both edges, while the
    /// square preview looked correct.
    /// </para>
    /// <para>
    /// So the fit is computed from the board's own extents, against the width as well as
    /// the height, and the camera is offset upward so the tray does not cover the board.
    /// </para>
    /// </remarks>
    internal sealed class BoardCameraFitter : MonoBehaviour
    {
        /// <summary>
        /// The share of screen height reserved for the tray, matching where
        /// <see cref="HeldTileView"/> puts it.
        /// </summary>
        private const float TrayHeightFraction = 0.24f;

        /// <summary>Breathing room around the board, in world units.</summary>
        private const float MarginWorldUnits = 0.25f;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private BoardView _boardView;

        /// <summary>
        /// Sizes and positions the camera for the given board.
        /// </summary>
        internal void Fit(GameState state)
        {
            var camera = _camera != null ? _camera : Camera.main;
            if (camera == null || state == null)
            {
                return;
            }

            var minimum = new Vector2(float.MaxValue, float.MaxValue);
            var maximum = new Vector2(float.MinValue, float.MinValue);

            var board = _boardView != null ? _boardView.transform : null;

            foreach (var coordinate in state.Board.Coordinates)
            {
                // Measured where the cell actually is rather than where the hex maths puts it. The board
                // leans, so its own coordinates are no longer its screen extents — and going through the
                // transform means any future change to the lean needs no change here.
                var local = HexMetrics.ToWorld(coordinate);
                var centre = board != null ? board.TransformPoint(local) : local;

                minimum = Vector2.Min(minimum, new Vector2(centre.x, centre.y));
                maximum = Vector2.Max(maximum, new Vector2(centre.x, centre.y));
            }

            // A cell reaches HexMetrics.Size beyond its centre in every direction, and a leaning block
            // also hangs below the plane of its own top face. Without the overhang the near rim of the
            // bottom row is clipped, which reads as a rendering fault rather than a framing one.
            var halfWidth = ((maximum.x - minimum.x) * 0.5f) + HexMetrics.Size + MarginWorldUnits;
            var halfHeight = ((maximum.y - minimum.y) * 0.5f)
                             + (HexMetrics.Size * BoardTilt.VerticalForeshortening)
                             + BoardTilt.ScreenOverhang
                             + MarginWorldUnits;

            var aspect = camera.aspect > 0f ? camera.aspect : 1f;

            // Whichever axis runs out of room first decides the zoom.
            var sizeForWidth = halfWidth / aspect;
            var sizeForHeight = halfHeight / (1f - TrayHeightFraction);
            var orthographicSize = Mathf.Max(sizeForWidth, sizeForHeight);

            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;

            // The board area is the screen above the tray, so its centre sits above the
            // screen centre by the tray's share of the half-height.
            var boardCentre = (minimum + maximum) * 0.5f;
            var position = camera.transform.position;
            camera.transform.position = new Vector3(
                boardCentre.x,
                boardCentre.y - (orthographicSize * TrayHeightFraction),
                position.z);
        }
    }
}
