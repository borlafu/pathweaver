using System.Globalization;
using Pathweaver.Core.State;
using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Shows how close the run is to clearing the level's quota.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bar answers the question a player actually has, which is "am I nearly done". The number
    /// beneath it answers the one they ask next, which is "by how much" — and that one cannot be
    /// answered by a shape. It was a bar alone until there was a font.
    /// </para>
    /// <para>
    /// It exists because a completed route produced no visible change whatsoever on the first
    /// device build. The simulation was scoring correctly and the game looked broken.
    /// </para>
    /// </remarks>
    internal sealed class ProgressBarView : MonoBehaviour
    {
        internal const float ViewportY = 0.94f;

        /// <summary>
        /// Where the score sits, just below the bar.
        /// </summary>
        /// <remarks>
        /// Below rather than inside: the bar is 0.12 world units tall and body text at the same
        /// camera is around 0.18, so a number placed inside it would overflow its own track.
        /// </remarks>
        internal const float ScoreViewportY = 0.905f;

        /// <summary>
        /// How much of the screen width the bar spans.
        /// </summary>
        /// <remarks>
        /// Narrowed from 0.8 when restart moved to the top left and pause to the top right. The bar
        /// now runs between them rather than under them.
        /// </remarks>
        private const float WidthFraction = 0.52f;
        private const float Height = 0.12f;
        private const float FillEase = 6f;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private GameSession _session;

        private Transform _bar;
        private Transform _fill;
        private MeshRenderer _fillRenderer;
        private Text.TextLabel _score;
        private float _shownFraction;
        private float _targetFraction;

        private Camera ResolvedCamera => _camera != null ? _camera : Camera.main;

        private void OnEnable()
        {
            if (_session != null)
            {
                _session.StateChanged += OnStateChanged;
            }

            CatchUp();
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.StateChanged -= OnStateChanged;
            }
        }

        private void Start()
        {
            Build();
            CatchUp();
        }

        /// <summary>
        /// Reads the current state directly, for the case where this view was disabled when the
        /// state last changed.
        /// </summary>
        private void CatchUp()
        {
            if (_session?.State != null)
            {
                OnStateChanged(_session.State);
                _shownFraction = _targetFraction;
            }
        }

        private void Update()
        {
            if (_fill == null || _bar == null)
            {
                return;
            }

            Reposition();

            // Width is measured every frame rather than baked at Start. BoardCameraFitter
            // resizes the camera during the session's own Start, and Unity does not define
            // which runs first — so a width captured once could be computed against the
            // camera's pre-fit size and leave the bar the wrong length.
            var width = WorldWidth();
            _bar.localScale = new Vector3(width, 1f, 1f);

            // Eased rather than snapped: the movement is what catches the eye, and a bar that
            // jumps has already finished moving by the time a player looks at it.
            _shownFraction = Mathf.Lerp(_shownFraction, _targetFraction, Time.deltaTime * FillEase);

            _fill.localScale = new Vector3(Mathf.Max(_shownFraction, 0.0001f), 1f, 1f);
            _fill.localPosition = new Vector3(-0.5f + (_shownFraction * 0.5f), 0f, -0.01f);
        }

        private void OnStateChanged(GameState state)
        {
            if (state == null || _session.TargetScore <= 0)
            {
                _targetFraction = 0f;

                // Cleared rather than left alone, or the previous level's score stays on screen over
                // the next one's empty bar.
                _score?.SetText(string.Empty);
                return;
            }

            _targetFraction = Mathf.Clamp01((float)state.Score / _session.TargetScore);

            if (_fillRenderer != null)
            {
                _fillRenderer.material.color = _targetFraction >= 1f
                    ? BoardPalette.ProgressComplete
                    : BoardPalette.ProgressFill;
            }

            ShowScore(state.Score, _session.TargetScore);
        }

        /// <summary>
        /// Writes the score against the target.
        /// </summary>
        /// <remarks>
        /// Grouped with separators, because a five-figure endless score read as one run of digits is
        /// slower to compare against a target than the bar it sits under. Invariant culture, so the
        /// separator does not change with the device's locale while the rest of the interface has no
        /// language at all.
        /// </remarks>
        private void ShowScore(long score, long target)
        {
            if (_score == null)
            {
                return;
            }

            _score.SetText(
                $"{score.ToString("N0", CultureInfo.InvariantCulture)} / "
                + target.ToString("N0", CultureInfo.InvariantCulture));

            // Matches the fill, so the moment the quota is met is said twice — once by the bar
            // turning and once by the number — and neither says it by colour alone, because the
            // number itself has passed the target.
            _score.SetColour(
                score >= target ? BoardPalette.ProgressComplete : BoardPalette.TextPrimary);
        }

        private void Build()
        {
            if (_boardView == null)
            {
                return;
            }

            // Built one world unit wide and scaled to fit, so the meshes never need rebuilding
            // when the camera changes.
            _bar = new GameObject("Bar").transform;
            _bar.SetParent(transform, worldPositionStays: false);

            AddPart(_bar, "Track", HexMeshFactory.CreateRectangle(1f, Height), BoardPalette.ProgressTrack, 0f);

            var fill = AddPart(_bar, "Fill", HexMeshFactory.CreateRectangle(1f, Height * 0.72f),
                BoardPalette.ProgressFill, -0.01f);

            _fill = fill.transform;
            _fillRenderer = fill;

            // Parented here so it is hidden and shown with the bar. TextLabel places itself in world
            // space, so being a child does not move it.
            _score = Text.TextLabel.Create(
                transform,
                ResolvedCamera,
                "score",
                new Vector2(0.5f, ScoreViewportY),
                Text.LabelMetrics.BodyHeightFraction,
                BoardPalette.TextPrimary);
        }

        private float WorldWidth()
        {
            var camera = ResolvedCamera;
            var left = camera.ViewportToWorldPoint(new Vector3(0.5f - (WidthFraction * 0.5f), 0f, 0f));
            var right = camera.ViewportToWorldPoint(new Vector3(0.5f + (WidthFraction * 0.5f), 0f, 0f));

            return Mathf.Abs(right.x - left.x);
        }

        private void Reposition()
        {
            var world = ResolvedCamera.ViewportToWorldPoint(new Vector3(0.5f, ViewportY, 0f));
            transform.position = new Vector3(world.x, world.y, -0.5f);
        }

        private MeshRenderer AddPart(Transform parent, string childName, Mesh mesh, Color colour, float depth)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(parent, worldPositionStays: false);
            child.transform.localPosition = new Vector3(0f, 0f, depth);

            child.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _boardView.TileMaterial;
            renderer.material.color = colour;

            return renderer;
        }
    }
}
