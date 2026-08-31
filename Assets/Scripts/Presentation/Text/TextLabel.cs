using TMPro;
using UnityEngine;

namespace Pathweaver.Game.Presentation.Text
{
    /// <summary>
    /// A line of text anchored in viewport space.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A world-space text mesh rather than a Canvas. Every control in the game is a mesh positioned
    /// by viewport fraction — see <c>HexButton</c> and <c>TokenPipsView</c> — and introducing
    /// a Canvas would mean two ways of placing things on screen, two hit-testing paths, and two
    /// answers to what happens when the camera moves.
    /// </para>
    /// <para>
    /// Size comes from <see cref="LabelMetrics"/> and is recomputed each frame, because the board
    /// camera's orthographic size changes with the board it is framing. A fixed font size would leave
    /// the same words twice as large on a small level as on a large one.
    /// </para>
    /// </remarks>
    internal sealed class TextLabel : MonoBehaviour
    {
        /// <summary>
        /// How far in front of the board text sits, in world units.
        /// </summary>
        /// <remarks>
        /// In front of the tray, which sits at -0.2, and of the pip column at -0.4, because a label
        /// that is occluded by the thing it describes is worse than no label.
        /// </remarks>
        private const float DefaultDepth = -0.6f;

        private TextMeshPro _text;
        private Camera _camera;
        private Vector2 _viewportPosition;
        private float _heightFraction;
        private float _depth;

        /// <summary>What the label currently reads.</summary>
        internal string Text => _text != null ? _text.text : string.Empty;

        /// <summary>Where the label is anchored, as a viewport fraction.</summary>
        internal Vector2 ViewportPosition => _viewportPosition;

        /// <summary>The label's line height, as a fraction of screen height.</summary>
        internal float HeightFraction => _heightFraction;

        /// <summary>
        /// Creates a label and places it.
        /// </summary>
        /// <param name="heightFraction">
        /// One of <see cref="LabelMetrics"/>'s named sizes. Smaller than
        /// <see cref="LabelMetrics.MinimumHeightFraction"/> is clamped up and complained about,
        /// because unreadable text is a worse outcome than a layout that overflows.
        /// </param>
        internal static TextLabel Create(
            Transform parent,
            Camera camera,
            string name,
            Vector2 viewportPosition,
            float heightFraction,
            Color colour,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            float depth = DefaultDepth)
        {
            var label = new GameObject($"Label {name}").AddComponent<TextLabel>();
            label.transform.SetParent(parent, worldPositionStays: false);

            if (heightFraction < LabelMetrics.MinimumHeightFraction)
            {
                Debug.LogWarning(
                    $"[text] label '{name}' asked for {heightFraction:0.###} of screen height, "
                    + $"below the legible minimum of {LabelMetrics.MinimumHeightFraction:0.###}. "
                    + "Clamped. Use fewer words instead.");

                heightFraction = LabelMetrics.MinimumHeightFraction;
            }

            label._camera = camera;
            label._viewportPosition = viewportPosition;
            label._heightFraction = heightFraction;
            label._depth = depth;

            label._text = label.gameObject.AddComponent<TextMeshPro>();
            label._text.alignment = alignment;
            label._text.color = colour;
            label._text.textWrappingMode = TextWrappingModes.NoWrap;

            // Auto-sizing is off deliberately. It would let TextMesh Pro shrink a long string below
            // the legible minimum this class exists to defend.
            label._text.enableAutoSizing = false;

            label.Place();

            return label;
        }

        internal void SetText(string value)
        {
            if (_text != null)
            {
                _text.text = value ?? string.Empty;
            }
        }

        internal void SetColour(Color colour)
        {
            if (_text != null)
            {
                _text.color = colour;
            }
        }

        /// <summary>Moves the label without rebuilding it.</summary>
        internal void SetViewportPosition(Vector2 viewportPosition)
        {
            _viewportPosition = viewportPosition;
            Place();
        }

        /// <summary>
        /// Allows word wrapping within the given width, in viewport fractions.
        /// </summary>
        /// <remarks>
        /// Off by default because a label that wraps unexpectedly overlaps whatever is beneath it. The
        /// help screen wants it; a number beside a bar does not.
        /// </remarks>
        internal void SetWrapWidth(float viewportWidthFraction)
        {
            if (_text == null || _camera == null)
            {
                return;
            }

            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.rectTransform.sizeDelta = new Vector2(
                _camera.orthographicSize * 2f * _camera.aspect * viewportWidthFraction,
                _text.rectTransform.sizeDelta.y);
        }

        private void Update()
        {
            Place();
        }

        private void Place()
        {
            var camera = _camera != null ? _camera : Camera.main;
            if (camera == null || _text == null)
            {
                return;
            }

            var world = camera.ViewportToWorldPoint(
                new Vector3(_viewportPosition.x, _viewportPosition.y, 0f));

            transform.position = new Vector3(world.x, world.y, _depth);
            _text.fontSize = LabelMetrics.FontSize(camera.orthographicSize, _heightFraction);
        }
    }
}
