using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Two opaque bands that keep the board out from behind the HUD.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The board is framed to sit between the tray at the bottom and the reporting strip at the top, and
    /// for every board that fits on screen that is enough — there is simply no board in those bands. A
    /// board taller than the screen has board everywhere, so the first large level put cells behind the
    /// progress bar and left the score unreadable over them.
    /// </para>
    /// <para>
    /// Opaque rather than a fade, and in the background colour, so the board visibly runs *under* the
    /// interface rather than competing with it. A translucent scrim would need alpha, which the unlit
    /// material the whole board is drawn with does not have — and a half-hidden cell is worse to read
    /// than a hidden one.
    /// </para>
    /// <para>
    /// Sized from the camera every frame, like <see cref="ProgressBarView"/>, because the camera's
    /// orthographic size changes with the board it is framing and again while the opening flight runs.
    /// </para>
    /// </remarks>
    internal sealed class HudBackdrop : MonoBehaviour
    {
        /// <summary>
        /// How far in front of the board the bands sit, in world units.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the boundary between the board and the interface, not just a number: everything that
        /// belongs to the board is behind it and everything that belongs to the HUD is in front. The pip
        /// columns at -0.4, the labels at -0.6 and the buttons at -1.5 were already in front; the held
        /// tile rested at zero, so the first band drawn here made the tile in the tray vanish and come
        /// back the instant it was dragged. <c>HudLayoutTests</c> now holds that ordering.
        /// </para>
        /// <para>
        /// The board's leaning near edge is kept behind by <c>BoardTilt.DepthOffsetFor</c>, which is why
        /// that had to become a function of the board's height rather than a constant.
        /// </para>
        /// </remarks>
        internal const float Depth = -0.1f;

        /// <summary>Overshoot beyond the screen edges, as a fraction of the visible size.</summary>
        /// <remarks>
        /// A band exactly the width of the screen shows a seam at the edge on some aspect ratios, and it
        /// costs nothing to draw it wider than the screen.
        /// </remarks>
        private const float Overshoot = 1.2f;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private Camera _camera;

        private Transform _top;
        private Transform _bottom;

        private Camera ResolvedCamera => _camera != null ? _camera : Camera.main;

        private void Start()
        {
            if (_boardView == null)
            {
                return;
            }

            // Built one world unit square and scaled to fit, so the meshes never need rebuilding when
            // the camera changes.
            _top = AddBand("Top");
            _bottom = AddBand("Bottom");
        }

        private void Update()
        {
            var camera = ResolvedCamera;
            if (camera == null || _top == null || _bottom == null)
            {
                return;
            }

            var halfHeight = camera.orthographicSize;
            var halfWidth = halfHeight * (camera.aspect > 0f ? camera.aspect : 1f);
            var centre = camera.transform.position;

            Place(_top, centre, halfWidth, halfHeight, BoardFraming.TopStripFraction, atTop: true);
            Place(_bottom, centre, halfWidth, halfHeight, BoardFraming.TrayHeightFraction, atTop: false);
        }

        /// <summary>
        /// Puts a band along one edge of the screen, however large the screen currently is.
        /// </summary>
        private static void Place(
            Transform band, Vector3 cameraCentre, float halfWidth, float halfHeight,
            float heightFraction, bool atTop)
        {
            // The fraction is of the full screen height, and halfHeight is half of it.
            var bandHeight = halfHeight * 2f * heightFraction;
            var edge = atTop ? cameraCentre.y + halfHeight : cameraCentre.y - halfHeight;
            var inward = atTop ? -bandHeight * 0.5f : bandHeight * 0.5f;

            band.localScale = new Vector3(halfWidth * 2f * Overshoot, bandHeight, 1f);
            band.position = new Vector3(cameraCentre.x, edge + inward, Depth);
        }

        private Transform AddBand(string childName)
        {
            var child = new GameObject($"Backdrop {childName}");
            child.transform.SetParent(transform, worldPositionStays: false);

            child.AddComponent<MeshFilter>().sharedMesh = HexMeshFactory.CreateRectangle(1f, 1f);

            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _boardView.TileMaterial;
            renderer.material.color = BoardPalette.Background;

            return child.transform;
        }
    }
}
