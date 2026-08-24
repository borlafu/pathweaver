using System.Collections.Generic;
using Pathweaver.Core.Flow;
using Pathweaver.Core.State;
using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Runs a light along every completed route, spring to hub.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One dot per route, pooled: the objects are made once and reused, and the route list is rebuilt only
    /// when the board changes. Nothing here resolves flow or allocates per frame — the per-frame work is a
    /// modulo, a lerp and a transform write per route.
    /// </para>
    /// <para>
    /// The dots stand aside while a harvested route is lit. The flash is the payout; a travelling dot
    /// would be invisible inside a route drawn near-white, and the sequencing reads better as
    /// "paid, then flowing".
    /// </para>
    /// <para>
    /// Like the endpoint pulse, this never notifies the frame governor. Motion at the 30 Hz idle rate is
    /// the point; pinning the active rate to animate would spend battery on a board nobody is touching.
    /// </para>
    /// </remarks>
    internal sealed class FlowPulseAnimator : MonoBehaviour
    {
        /// <summary>How big the travelling light is, against a conduit spoke 0.14 wide.</summary>
        private const float DotRadius = 0.1f;

        /// <summary>In front of the spokes and the resource mark, so the light passes over them.</summary>
        private const float Depth = -0.03f;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private GameSession _session;

        private readonly List<Transform> _dots = new List<Transform>();
        private readonly List<Material> _materials = new List<Material>();
        private readonly List<List<Vector3>> _paths = new List<List<Vector3>>();
        private readonly List<float> _phases = new List<float>();

        private Mesh _dotMesh;
        private int _active;
        private bool _isResting;

        private void OnEnable()
        {
            if (_session != null)
            {
                _session.StateChanged += OnStateChanged;
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
            }

            HideAll();
        }

        /// <summary>
        /// Rebuilds the paths the dots follow.
        /// </summary>
        /// <remarks>
        /// A route reshaped by a pivot keeps its phase, because that comes from its endpoints — so the dot
        /// carries on from the same fraction of the way along the new path rather than snapping back to the
        /// spring. A route that breaks loses its dot here, and only here: without a player input there is
        /// no state change, so a pulse never blinks out on its own.
        /// </remarks>
        private void OnStateChanged(GameState state)
        {
            if (_boardView == null || _session == null)
            {
                return;
            }

            var routes = _session.ActiveRoutes;
            _active = routes.Count;

            for (var index = 0; index < _active; index++)
            {
                var route = routes[index];

                EnsureSlot(index);

                var path = _paths[index];
                path.Clear();
                path.Add(_boardView.WorldPositionOf(route.Spring.Coordinate));

                for (var tile = 0; tile < route.Tiles.Count; tile++)
                {
                    path.Add(_boardView.WorldPositionOf(route.Tiles[tile]));
                }

                path.Add(_boardView.WorldPositionOf(route.Hub.Coordinate));

                _phases[index] = FlowPulse.PhaseFor(route.Spring.Coordinate, route.Hub.Coordinate);
                _materials[index].color = Color.Lerp(
                    BoardPalette.ForKind(route.Kind), BoardPalette.HarvestFlash, 0.45f);
            }

            for (var index = _active; index < _dots.Count; index++)
            {
                _dots[index].gameObject.SetActive(false);
            }

            _isResting = false;
        }

        private void Update()
        {
            if (_boardView == null || _active == 0)
            {
                return;
            }

            // Reduced motion silences this outright, and the flash owns the screen while it lasts.
            if (GameSettings.ReduceMotion || _boardView.IsFlashing)
            {
                if (!_isResting)
                {
                    HideAll();
                    _isResting = true;
                }

                return;
            }

            var now = Time.unscaledTime;

            for (var index = 0; index < _active; index++)
            {
                var dot = _dots[index];

                if (!dot.gameObject.activeSelf)
                {
                    dot.gameObject.SetActive(true);
                }

                var position = FlowPulse.PositionAt(_paths[index], now, _phases[index]);
                dot.position = new Vector3(position.x, position.y, Depth);
            }

            _isResting = false;
        }

        private void EnsureSlot(int index)
        {
            while (_dots.Count <= index)
            {
                _dotMesh ??= GlyphMeshFactory.CreateDisc(DotRadius);

                var dot = new GameObject($"Flow{_dots.Count}");
                dot.transform.SetParent(transform, worldPositionStays: false);

                dot.AddComponent<MeshFilter>().sharedMesh = _dotMesh;

                var renderer = dot.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _boardView.TileMaterial;
                renderer.material.color = BoardPalette.HarvestFlash;

                _dots.Add(dot.transform);
                _materials.Add(renderer.material);
                _paths.Add(new List<Vector3>());
                _phases.Add(0f);
            }

            _dots[index].gameObject.SetActive(true);
        }

        private void HideAll()
        {
            for (var index = 0; index < _dots.Count; index++)
            {
                if (_dots[index] != null)
                {
                    _dots[index].gameObject.SetActive(false);
                }
            }
        }
    }
}
