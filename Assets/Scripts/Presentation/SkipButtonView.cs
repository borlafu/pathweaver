using Pathweaver.Core.State;
using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Discards the tile in hand for the next one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A single forced draw leaves a player with only one real decision when the tile is
    /// awkward: place it somewhere wasteful. A skip turns that into a choice with a cost,
    /// which is a decision rather than a chore.
    /// </para>
    /// <para>
    /// It dims when none remain rather than disappearing. A control that vanishes teaches
    /// nothing about why it is gone, and the pips beside it say how many are left.
    /// </para>
    /// </remarks>
    internal sealed class SkipButtonView : MonoBehaviour
    {
        private static readonly Vector2 ViewportPosition = new Vector2(0.86f, 0.10f);

        private const float ButtonRadius = BoardGlyphs.ButtonRadius;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private GameSession _session;

        private MeshRenderer _backgroundRenderer;
        private bool _isAvailable;

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

            if (_session?.State != null)
            {
                OnStateChanged(_session.State);
            }
        }

        private void Update()
        {
            transform.position = WorldPosition;

        }

        private void OnStateChanged(GameState state)
        {
            _isAvailable = state != null && state.SkipTokens.CanSpend;

            if (_backgroundRenderer != null)
            {
                _backgroundRenderer.material.color =
                    _isAvailable ? BoardPalette.SkipReady : BoardPalette.SkipSpent;
            }
        }

        private void Build()
        {
            if (_boardView == null)
            {
                return;
            }

            _backgroundRenderer = AddPart(
                "Background", HexMeshFactory.CreateHexagon(ButtonRadius), BoardPalette.SkipSpent, 0f);

            // Two chevrons pointing right: the tile after this one, and the one after that. A single
            // arrow would read as "go" rather than "next".
            //
            // One mitred mesh each, rather than the two loose rectangles per chevron this used to be —
            // those crossed at the apex and left a notch on the outside of the joint.
            //
            // The shapes come from BoardGlyphs so the help screen can draw this same mark, rather than a
            // second drawing of it that would drift.
            foreach (var part in BoardGlyphs.Skip())
            {
                AddPart(part.Name, part.Mesh, part.Colour, part.Depth, part.Offset);
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
            renderer.sharedMaterial = _boardView.TileMaterial;
            renderer.material.color = colour;

            return renderer;
        }
    }
}
