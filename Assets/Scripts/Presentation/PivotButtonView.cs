using Pathweaver.Core.State;
using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Spends a Pivot Token to take a conduit off the board.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A button of its own, under the pips that count the tokens. Arming used to be a tap on the
    /// pips themselves, which meant the only way to discover the mechanic was to try tapping a
    /// counter — so tokens were earned and displayed and never used.
    /// </para>
    /// <para>
    /// Three states, because the control answers two questions at once: whether a token is available
    /// at all, and whether one is armed and waiting for a conduit to be chosen. It dims rather than
    /// disappearing when there is nothing to spend, like the skip button beside it.
    /// </para>
    /// </remarks>
    internal sealed class PivotButtonView : MonoBehaviour
    {
        /// <summary>
        /// Below the pip column, so the count and the control that spends it read as one group.
        /// </summary>
        private static readonly Vector2 ViewportPosition = new Vector2(0.12f, 0.10f);

        private const float ButtonRadius = 0.34f;
        private const float GlyphWidth = 0.22f;
        private const float GlyphThickness = 0.075f;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private GameSession _session;

        private MeshRenderer _backgroundRenderer;

        private Camera ResolvedCamera => _camera != null ? _camera : Camera.main;

        internal float TouchRadiusPixels => Mathf.Min(Screen.width, Screen.height) * 0.11f;

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

        internal Vector2 ScreenPosition => ResolvedCamera.WorldToScreenPoint(WorldPosition);

        internal bool IsPressed(Vector2 screenPosition)
            => Vector2.Distance(screenPosition, ScreenPosition) <= TouchRadiusPixels;

        private void OnEnable()
        {
            if (_session != null)
            {
                _session.StateChanged += OnStateChanged;
                _session.PivotArmedChanged += OnPivotArmedChanged;
            }

            if (_session?.State != null)
            {
                OnStateChanged(_session.State);
            }
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.StateChanged -= OnStateChanged;
                _session.PivotArmedChanged -= OnPivotArmedChanged;
            }
        }

        private void Start()
        {
            Build();

            if (_session?.State != null)
            {
                OnStateChanged(_session.State);
            }
        }

        private void Update()
        {
            transform.position = WorldPosition;

            // Kept the same size on screen whatever the camera shows, exactly as HexButton is. These
            // three predate that class and were left world-sized, so they shrank on a board zoomed out to
            // fit a large level — and the pip column above this one would have drifted away from it.
            transform.localScale =
                Vector3.one * Menus.HexButton.ScaleFor(ResolvedCamera.orthographicSize);
        }

        private void OnPivotArmedChanged(bool armed)
        {
            if (_session?.State != null)
            {
                OnStateChanged(_session.State);
            }
        }

        private void OnStateChanged(GameState state)
        {
            if (_backgroundRenderer == null)
            {
                return;
            }

            var canSpend = state != null && state.PivotTokens.CanSpend && state.Board.OccupiedCount > 0;
            var armed = _session != null && _session.IsPivotArmed;

            _backgroundRenderer.material.color = armed
                ? BoardPalette.TokenArmed
                : canSpend ? BoardPalette.PivotReady : BoardPalette.PivotSpent;
        }

        private void Build()
        {
            if (_boardView == null)
            {
                return;
            }

            _backgroundRenderer = AddPart(
                "Background", HexMeshFactory.CreateHexagon(ButtonRadius), BoardPalette.PivotSpent, 0f);

            // A hexagon with a bar struck through it: this cell, taken off the board. A plain minus
            // would say "less" rather than "remove that tile".
            AddPart(
                "Cell", HexMeshFactory.CreateHexagon(0.15f), BoardPalette.PivotGlyphCell, -0.02f);
            AddPart(
                "Bar", HexMeshFactory.CreateRectangle(GlyphWidth, GlyphThickness),
                BoardPalette.RestartArrow, -0.04f);
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
            renderer.sharedMaterial = _boardView.TileMaterial;
            renderer.material.color = colour;

            return renderer;
        }
    }
}
