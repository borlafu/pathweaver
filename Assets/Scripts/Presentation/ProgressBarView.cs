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
    /// A bar rather than a number, because the project has no font: digits would mean building
    /// a glyph renderer before a player could see that anything had happened at all. A bar also
    /// answers the question a player actually has, which is "am I nearly done", not "what is my
    /// score to the point".
    /// </para>
    /// <para>
    /// It exists because a completed route produced no visible change whatsoever on the first
    /// device build. The simulation was scoring correctly and the game looked broken.
    /// </para>
    /// </remarks>
    internal sealed class ProgressBarView : MonoBehaviour
    {
        private const float ViewportY = 0.94f;
        private const float WidthFraction = 0.8f;
        private const float Height = 0.12f;
        private const float FillEase = 6f;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private GameSession _session;

        private Transform _fill;
        private MeshRenderer _fillRenderer;
        private float _width;
        private float _shownFraction;
        private float _targetFraction;

        private Camera ResolvedCamera => _camera != null ? _camera : Camera.main;

        private void OnEnable()
        {
            if (_session != null)
            {
                _session.StateChanged += OnStateChanged;
            }
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
        }

        private void Update()
        {
            if (_fill == null)
            {
                return;
            }

            Reposition();

            // Eased rather than snapped: the movement is what catches the eye, and a bar that
            // jumps has already finished moving by the time a player looks at it.
            _shownFraction = Mathf.Lerp(_shownFraction, _targetFraction, Time.deltaTime * FillEase);

            _fill.localScale = new Vector3(Mathf.Max(_shownFraction, 0.0001f), 1f, 1f);
            _fill.localPosition = new Vector3(
                -(_width * 0.5f) + (_width * _shownFraction * 0.5f), 0f, -0.01f);
        }

        private void OnStateChanged(GameState state)
        {
            if (state == null || _session.TargetScore <= 0)
            {
                _targetFraction = 0f;
                return;
            }

            _targetFraction = Mathf.Clamp01((float)state.Score / _session.TargetScore);

            if (_fillRenderer != null)
            {
                _fillRenderer.material.color = _targetFraction >= 1f
                    ? BoardPalette.ProgressComplete
                    : BoardPalette.ProgressFill;
            }
        }

        private void Build()
        {
            if (_boardView == null)
            {
                return;
            }

            _width = WorldWidth();

            AddPart("Track", HexMeshFactory.CreateRectangle(_width, Height), BoardPalette.ProgressTrack, 0f);

            // A unit-wide rectangle scaled on X, so the fill grows from the left edge rather
            // than from its centre.
            var fill = AddPart("Fill", HexMeshFactory.CreateRectangle(_width, Height * 0.72f),
                BoardPalette.ProgressFill, -0.01f);

            _fill = fill.transform;
            _fillRenderer = fill;
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

        private MeshRenderer AddPart(string childName, Mesh mesh, Color colour, float depth)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, worldPositionStays: false);
            child.transform.localPosition = new Vector3(0f, 0f, depth);

            child.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _boardView.TileMaterial;
            renderer.material.color = colour;

            return renderer;
        }
    }
}
