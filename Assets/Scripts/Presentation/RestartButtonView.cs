using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// A restart button, and the only way out of a board with no moves left.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Added after a real session ended with no legal placement and no way to do anything
    /// about it. A puzzle that can reach a dead end without offering an exit is not
    /// difficult, it is broken, and PRD section 3.2B treats exactly that frustration as a
    /// design failure rather than a challenge.
    /// </para>
    /// <para>
    /// It sits in the bottom corner, within thumb reach but away from the tray, so it cannot
    /// be hit while placing. When no move remains it turns urgent and pulses, because at that
    /// point it is not an option among several — it is the only thing left to press.
    /// </para>
    /// </remarks>
    internal sealed class RestartButtonView : MonoBehaviour
    {
        /// <summary>Where the button sits, in viewport coordinates.</summary>
        private static readonly Vector2 ViewportPosition = new Vector2(0.12f, 0.10f);

        private const float ButtonRadius = 0.34f;
        private const float ArrowRadius = 0.21f;
        private const float ArrowThickness = 0.07f;
        private const float ArrowSweepDegrees = 265f;
        private const float PulsesPerSecond = 1.3f;
        private const float PulseDepth = 0.12f;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private GameSession _session;

        private Transform _body;
        private MeshRenderer _backgroundRenderer;
        private bool _isUrgent;

        private Camera ResolvedCamera => _camera != null ? _camera : Camera.main;

        /// <summary>
        /// How large an area counts as pressing the button.
        /// </summary>
        internal float TouchRadiusPixels => Mathf.Min(Screen.width, Screen.height) * 0.11f;

        internal Vector2 ScreenPosition
            => ResolvedCamera.WorldToScreenPoint(WorldPosition);

        internal Vector3 WorldPosition
        {
            get
            {
                var world = ResolvedCamera.ViewportToWorldPoint(
                    new Vector3(ViewportPosition.x, ViewportPosition.y, 0f));
                world.z = -0.3f;
                return world;
            }
        }

        /// <summary>Whether a screen position counts as pressing the button.</summary>
        internal bool IsPressed(Vector2 screenPosition)
            => Vector2.Distance(screenPosition, ScreenPosition) <= TouchRadiusPixels;

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
            transform.position = WorldPosition;

            if (_body == null)
            {
                return;
            }

            // Only the stuck state moves. A button that pulses all the time stops meaning
            // anything.
            var scale = _isUrgent
                ? 1f + (Mathf.Sin(Time.unscaledTime * PulsesPerSecond * Mathf.PI * 2f) * PulseDepth)
                : 1f;

            _body.localScale = Vector3.one * scale;
        }

        private void OnStateChanged(Pathweaver.Core.State.GameState state)
        {
            _isUrgent = state != null && state.IsDeadlocked;

            if (_backgroundRenderer != null)
            {
                _backgroundRenderer.material.color =
                    _isUrgent ? BoardPalette.RestartUrgent : BoardPalette.RestartIdle;
            }
        }

        private void Build()
        {
            if (_boardView == null)
            {
                return;
            }

            _body = new GameObject("Body").transform;
            _body.SetParent(transform, worldPositionStays: false);

            _backgroundRenderer = AddPart(
                _body, "Background", HexMeshFactory.CreateHexagon(ButtonRadius),
                BoardPalette.RestartIdle, 0f);

            AddPart(
                _body,
                "Arrow",
                HexMeshFactory.CreateCircularArrow(ArrowRadius, ArrowThickness, ArrowSweepDegrees),
                BoardPalette.RestartArrow,
                -0.01f);
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
