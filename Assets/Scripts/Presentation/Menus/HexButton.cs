using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathweaver.Game.Presentation.Menus
{
    /// <summary>
    /// A tappable hexagon, positioned in viewport space.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every control in the game is a hexagon because every control is generated geometry, and a
    /// hexagon is the shape the board already speaks in. Sharing one component keeps their touch
    /// areas and layout consistent — the restart and skip buttons predate this and each grew their
    /// own copy of the same maths, which is worth folding in later.
    /// </para>
    /// <para>
    /// Positioned in viewport fractions rather than world units so a control sits in the same
    /// place on any screen shape without anyone recomputing it.
    /// </para>
    /// </remarks>
    internal sealed class HexButton : MonoBehaviour
    {
        /// <summary>
        /// How far in front of the board a button's face sits, in world units.
        /// </summary>
        /// <remarks>
        /// Named because other things have to be placed relative to it. The camera looks along +Z from
        /// negative Z, so a smaller number is nearer the viewer.
        /// </remarks>
        internal const float FaceDepth = -1.5f;

        /// <summary>
        /// Where a text label sitting on or beside a button belongs.
        /// </summary>
        /// <remarks>
        /// In front of the face and in front of the glyphs stacked on it. The help button's question
        /// mark and every settings label were drawn at <c>TextLabel</c>'s default depth of -0.6, which is
        /// *behind* a face at -1.5, so they were invisible on the device while rendering perfectly well
        /// in isolation. Nothing in the label's own code was wrong, which is why this constant lives
        /// here, next to the number it has to beat.
        /// </remarks>
        internal const float LabelDepth = FaceDepth - 0.1f;
        private readonly List<GameObject> _glyphs = new List<GameObject>();

        private Camera _camera;
        private Material _material;
        private MeshRenderer _face;
        private Vector2 _viewportPosition;
        private float _radius;
        private float _touchRadiusFraction;

        /// <summary>Whatever the button is for, so a screen can identify it.</summary>
        internal string Id { get; private set; }

        internal bool IsEnabled { get; private set; } = true;

        internal Vector3 WorldPosition
        {
            get
            {
                var world = _camera.ViewportToWorldPoint(
                    new Vector3(_viewportPosition.x, _viewportPosition.y, 0f));
                world.z = FaceDepth;
                return world;
            }
        }

        internal float TouchRadiusPixels
            => Mathf.Min(Screen.width, Screen.height) * _touchRadiusFraction;

        /// <summary>
        /// How much to scale a button drawn for the menu camera, to keep its size on screen.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Every radius passed to <see cref="Create"/> is in world units and was chosen against
        /// <c>MenuCamera.OrthographicSize</c>. That is fine on a menu, where the camera is always at that
        /// size — and wrong everywhere else, because a world size occupies less of the screen the more
        /// world the camera shows. The pause screen keeps the board's framing on purpose, so over a large
        /// board its controls were drawn a third of their intended size and lost among the cells.
        /// </para>
        /// <para>
        /// The touch radius is already a fraction of the screen, so it never had this problem — which
        /// means the visual and the tappable area disagreed, and the visual was the wrong one.
        /// </para>
        /// </remarks>
        internal static float ScaleFor(float orthographicSize)
            => orthographicSize / MenuCamera.OrthographicSize;

        internal static HexButton Create(
            Transform parent,
            string id,
            Camera camera,
            Material material,
            Vector2 viewportPosition,
            float radius,
            Color colour,
            float touchRadiusFraction = 0.12f)
        {
            var button = new GameObject($"Button {id}").AddComponent<HexButton>();
            button.transform.SetParent(parent, worldPositionStays: false);

            button.Id = id;
            button._camera = camera;
            button._material = material;
            button._viewportPosition = viewportPosition;
            button._radius = radius;
            button._touchRadiusFraction = touchRadiusFraction;

            button._face = button.AddPart(
                "Face", HexMeshFactory.CreateHexagon(radius), colour, depth: 0f);

            // Placed and scaled here as well as in Update. Update is what keeps both right when the
            // camera changes, but it does not run in an Editor scene assembled for a preview capture —
            // which left every button of a screen stacked at the origin in the first help-screen capture.
            button.transform.position = button.WorldPosition;
            button.transform.localScale = Vector3.one * ScaleFor(camera.orthographicSize);

            return button;
        }

        /// <summary>Whether a screen position falls on this button.</summary>
        internal bool IsPressed(Vector2 screenPosition)
        {
            if (!IsEnabled)
            {
                return false;
            }

            var buttonScreen = _camera.WorldToScreenPoint(WorldPosition);
            return Vector2.Distance(screenPosition, buttonScreen) <= TouchRadiusPixels;
        }

        internal void SetColour(Color colour)
        {
            if (_face != null)
            {
                _face.material.color = colour;
            }
        }

        /// <summary>
        /// Dims a button and stops it responding, rather than hiding it.
        /// </summary>
        /// <remarks>
        /// A locked level that vanished would leave a player wondering how many there are; one
        /// that is visibly present and unavailable answers that on its own.
        /// </remarks>
        internal void SetEnabled(bool enabled, Color colour)
        {
            IsEnabled = enabled;
            SetColour(colour);
        }

        /// <summary>
        /// Adds a shape to the button's face.
        /// </summary>
        /// <remarks>
        /// Each glyph sits slightly in front of the one before it. They used to share a depth, which
        /// left two coplanar meshes fighting for the same pixels: the settings gear is a disc with a
        /// darker disc punched through it, and it rendered as a plain white disc because the hole was
        /// level with the ring rather than in front of it.
        /// </remarks>
        internal GameObject AddGlyph(Mesh mesh, Color colour, Vector3 offset = default, float rotation = 0f)
        {
            var depth = -0.02f - (_glyphs.Count * 0.005f);
            var glyph = AddPart("Glyph", mesh, colour, depth, offset, rotation).gameObject;
            _glyphs.Add(glyph);
            return glyph;
        }

        internal void ClearGlyphs()
        {
            foreach (var glyph in _glyphs)
            {
                if (glyph != null)
                {
                    Destroy(glyph);
                }
            }

            _glyphs.Clear();
        }

        internal float Radius => _radius;

        private void Update()
        {
            transform.position = WorldPosition;

            if (_camera != null)
            {
                transform.localScale = Vector3.one * ScaleFor(_camera.orthographicSize);
            }
        }

        private MeshRenderer AddPart(
            string childName, Mesh mesh, Color colour, float depth, Vector3 offset = default, float rotation = 0f)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, worldPositionStays: false);
            child.transform.localPosition = new Vector3(offset.x, offset.y, depth);
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);

            child.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.material.color = colour;

            return renderer;
        }
    }
}
