using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Marks each screen edge the board continues past.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A board larger than the screen said nothing about being larger than the screen. The opening flight
    /// shows the whole valley once and is then gone; after that a player has only the board's cut-off
    /// edges to go on, and a cut-off edge looks exactly like a board that ends there.
    /// </para>
    /// <para>
    /// Chevrons rather than a glow, because the material the board is drawn with is opaque and a gradient
    /// needs alpha — and because a chevron says which way, which is the whole content of the thing. The
    /// skip control already uses one, so the shape is not new vocabulary.
    /// </para>
    /// <para>
    /// They dim to nothing as the clamp is reached, so a player pushing against the edge of the board sees
    /// the offer withdrawn rather than being told there is more when there is not.
    /// </para>
    /// </remarks>
    internal sealed class PanHintView : MonoBehaviour
    {
        /// <summary>How far in from each edge a mark sits, as a viewport fraction.</summary>
        internal const float EdgeInset = 0.035f;

        /// <summary>
        /// Where the top and bottom marks sit vertically, as viewport fractions.
        /// </summary>
        /// <remarks>
        /// Inside the board's own band rather than at the screen edge, because the top and bottom of the
        /// screen belong to the reporting strip and the drawer — a mark drawn there would sit on the
        /// progress bar or under the tray.
        /// </remarks>
        internal static float TopRow => 1f - BoardFraming.TopStripFraction - EdgeInset;

        internal static float BottomRow => BoardFraming.TrayHeightFraction + EdgeInset;

        private const float ChevronLength = 0.11f;
        private const float ChevronHeight = 0.1f;
        private const float ChevronThickness = 0.035f;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private BoardCameraFitter _cameraFitter;

        [SerializeField]
        private Camera _camera;

        private Transform _left;
        private Transform _right;
        private Transform _up;
        private Transform _down;
        private MeshRenderer _leftMark;
        private MeshRenderer _rightMark;
        private MeshRenderer _upMark;
        private MeshRenderer _downMark;

        private Camera ResolvedCamera => _camera != null ? _camera : Camera.main;

        private void Start()
        {
            if (_boardView == null)
            {
                return;
            }

            // A chevron built pointing right, then turned. One mesh for all four, because they are the
            // same mark four times and a player should read them as such.
            var mesh = GlyphMeshFactory.CreateChevron(ChevronLength, ChevronHeight, ChevronThickness);

            (_right, _rightMark) = AddMark(mesh, "Right", 0f);
            (_left, _leftMark) = AddMark(mesh, "Left", 180f);
            (_up, _upMark) = AddMark(mesh, "Up", 90f);
            (_down, _downMark) = AddMark(mesh, "Down", -90f);
        }

        private void Update()
        {
            var camera = ResolvedCamera;
            if (camera == null || _cameraFitter == null || _right == null)
            {
                return;
            }

            var room = _cameraFitter.Room;

            // Nothing at all on a board that fits, rather than four marks dimmed to invisible: the
            // cheapest way to be certain a small level looks exactly as it did.
            var active = room.IsAnywhere;

            _right.gameObject.SetActive(active);
            _left.gameObject.SetActive(active);
            _up.gameObject.SetActive(active);
            _down.gameObject.SetActive(active);

            if (!active)
            {
                return;
            }

            var middle = (TopRow + BottomRow) * 0.5f;

            Place(_right, _rightMark, camera, new Vector2(1f - EdgeInset, middle), room.Right);
            Place(_left, _leftMark, camera, new Vector2(EdgeInset, middle), room.Left);
            Place(_up, _upMark, camera, new Vector2(0.5f, TopRow), room.Up);
            Place(_down, _downMark, camera, new Vector2(0.5f, BottomRow), room.Down);
        }

        private static void Place(
            Transform mark, MeshRenderer renderer, Camera camera, Vector2 viewport, float room)
        {
            var world = camera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, 0f));

            // In front of the board and of the backdrop bands, and behind everything the player touches.
            mark.position = new Vector3(world.x, world.y, -0.15f);

            // Sized against the camera, like the menu controls, so a mark at the edge of the screen is the
            // same size whatever zoom the board is at.
            mark.localScale = Vector3.one * Menus.HexButton.ScaleFor(camera.orthographicSize);

            renderer.material.color = PanHint.ColourFor(room);
        }

        private (Transform, MeshRenderer) AddMark(Mesh mesh, string childName, float rotationDegrees)
        {
            var child = new GameObject($"PanHint {childName}");
            child.transform.SetParent(transform, worldPositionStays: false);
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);

            child.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _boardView.TileMaterial;
            renderer.material.color = BoardPalette.Background;

            return (child.transform, renderer);
        }
    }
}
