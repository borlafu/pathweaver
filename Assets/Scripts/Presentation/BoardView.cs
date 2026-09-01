using System.Collections.Generic;
using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.State;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Draws a whole board, and keeps it in step with the game state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Built once from the level's shape, then refreshed from a state. Refreshing
    /// rather than rebuilding matters because the state is immutable: every move
    /// produces a new one, and tearing down thirty-odd cell views per placement would
    /// churn allocations on a device that is meant to sip battery.
    /// </para>
    /// <para>
    /// The view reads the state and never writes to it.
    /// </para>
    /// </remarks>
    internal sealed class BoardView : MonoBehaviour
    {
        /// <summary>
        /// The material cells are drawn with.
        /// </summary>
        /// <remarks>
        /// An asset reference, not <c>Shader.Find</c>. A shader no material asset points at
        /// is stripped from a player build, so <c>Shader.Find</c> returns null on device
        /// while working perfectly in the Editor — which is how the first build on hardware
        /// came up blank with "Value cannot be null. Parameter name: shader".
        /// </remarks>
        [SerializeField]
        private Material _tileMaterial;

        /// <summary>
        /// Artwork for the board. Optional: without one, everything is generated geometry.
        /// </summary>
        [SerializeField]
        private BoardTheme _theme;

        private readonly Dictionary<HexCoord, CellView> _cells = new Dictionary<HexCoord, CellView>();
        private readonly List<CellView> _pulsingCells = new List<CellView>();

        /// <summary>How long a harvested route stays lit, in seconds.</summary>
        private const float FlashSeconds = 0.9f;

        private Mesh _hexMesh;
        private Mesh _spokeMesh;
        private Mesh _ringMesh;
        private Material _material;
        private GameState _lastState;
        private ISet<HexCoord> _lastAvailable;
        private readonly HashSet<HexCoord> _flashing = new HashSet<HexCoord>();
        private float _flashEndsAt = -1f;

        /// <summary>The meshes and material cells are drawn with, for other views.</summary>
        internal Mesh HexMesh
        {
            get
            {
                EnsureResources();
                return _hexMesh;
            }
        }

        internal Mesh SpokeMesh
        {
            get
            {
                EnsureResources();
                return _spokeMesh;
            }
        }

        /// <summary>The board's artwork, or null while it is still placeholder geometry.</summary>
        internal BoardTheme Theme => _theme;

        internal Material TileMaterial
        {
            get
            {
                EnsureResources();
                return _material;
            }
        }

        /// <summary>How many cells are currently drawn.</summary>
        internal int CellCount => _cells.Count;

        /// <summary>
        /// The cells carrying an endpoint's breathing ring, in board order.
        /// </summary>
        /// <remarks>
        /// Collected once per <see cref="Build"/> and handed out as the cached list, so the animator that
        /// reads it every frame allocates nothing. Endpoints are authored into the level and never move,
        /// which is what makes collecting them once correct.
        /// </remarks>
        internal IReadOnlyList<CellView> PulsingCells => _pulsingCells;

        /// <summary>Whether a harvested route is currently lit.</summary>
        /// <remarks>
        /// Read by the flow animation, which stands aside during the flash: the flash is the payout, and
        /// a travelling dot would be lost inside a route lit near-white anyway.
        /// </remarks>
        internal bool IsFlashing => _flashing.Count > 0;

        /// <summary>
        /// Creates a cell view per cell in the state's board.
        /// </summary>
        internal void Build(GameState state)
        {
            Clear();
            EnsureResources();

            // The lean lives on this transform, not on the camera. Under an orthographic projection the
            // two produce the same image, and this way the camera stays axis-aligned — which every HUD
            // view depends on, because each anchors itself through the camera and then overwrites z.
            //
            // How far back it sits depends on how tall it is, because the lean swings the near rim toward
            // the viewer in proportion to that. Measured here from the board's own coordinates rather
            // than through this transform, which is the thing being set.
            transform.SetPositionAndRotation(
                BoardTilt.PositionFor(LocalHalfHeight(state)), BoardTilt.Rotation);

            foreach (var coordinate in state.Board.Coordinates)
            {
                CreateCell(coordinate);
            }

            // Endpoints get their ring here rather than in Refresh, because Refresh runs on every state
            // change and the ring must survive all of them untouched.
            foreach (var endpoint in state.Endpoints)
            {
                if (_cells.TryGetValue(endpoint.Coordinate, out var cell))
                {
                    cell.AttachPulse(endpoint, _ringMesh, _material);
                    _pulsingCells.Add(cell);
                }
            }

            Refresh(state);
        }

        /// <summary>
        /// Creates one cell view, replacing any cell already at that coordinate.
        /// </summary>
        /// <remarks>
        /// Separated from <see cref="Build"/> so a caller with no game state can draw cells — the
        /// store art renders arrangements that are tidy rather than reachable, and inventing a legal
        /// position to promote the game would be the wrong way round.
        /// </remarks>
        internal CellView CreateCell(HexCoord coordinate)
        {
            EnsureResources();

            if (_cells.TryGetValue(coordinate, out var existing))
            {
                return existing;
            }

            var cell = new GameObject($"Cell {coordinate}").AddComponent<CellView>();
            cell.transform.SetParent(transform, worldPositionStays: false);
            cell.Initialise(coordinate, _hexMesh, _spokeMesh, _material, _theme);

            _cells.Add(coordinate, cell);
            return cell;
        }

        /// <summary>
        /// Updates every cell to match the state, optionally marking where the held
        /// tile could go.
        /// </summary>
        /// <summary>
        /// Lights the given conduits, then returns them to normal.
        /// </summary>
        internal void FlashHarvested(IEnumerable<HexCoord> tiles)
        {
            _flashing.Clear();

            foreach (var tile in tiles)
            {
                _flashing.Add(tile);
            }

            if (_flashing.Count == 0)
            {
                return;
            }

            _flashEndsAt = Time.unscaledTime + FlashSeconds;
            Refresh(_lastState, _lastAvailable);
        }

        private void Update()
        {
            if (_flashEndsAt < 0f || Time.unscaledTime < _flashEndsAt)
            {
                return;
            }

            _flashEndsAt = -1f;
            _flashing.Clear();
            Refresh(_lastState, _lastAvailable);
        }

        /// <summary>
        /// Updates every cell to match the state.
        /// </summary>
        /// <param name="state">The state to draw.</param>
        /// <param name="markedCells">
        /// Cells to mark: where the held tile could go, or — while a Pivot Token is armed — the
        /// conduits the token could act on.
        /// </param>
        /// <param name="pivotArmed">
        /// Whether the marks are pivot targets. Occupied cells are drawn as conduits, so a marked
        /// conduit needs a different treatment from a marked empty cell, which is simply lit.
        /// </param>
        internal void Refresh(
            GameState state, ISet<HexCoord> markedCells = null, bool pivotArmed = false)
        {
            if (state == null)
            {
                return;
            }

            var availableCells = markedCells;

            // Remembered so a flash starting or ending can redraw without the caller having to
            // hand the state over again.
            _lastState = state;
            _lastAvailable = availableCells;

            var endpoints = new Dictionary<HexCoord, FlowEndpoint>();
            foreach (var endpoint in state.Endpoints)
            {
                endpoints[endpoint.Coordinate] = endpoint;
            }

            foreach (var pair in _cells)
            {
                var coordinate = pair.Key;
                var cell = pair.Value;

                if (endpoints.TryGetValue(coordinate, out var endpoint))
                {
                    cell.ShowEndpoint(endpoint);
                    continue;
                }

                if (state.Board.TryGet(coordinate, out var tile))
                {
                    if (_flashing.Contains(coordinate))
                    {
                        cell.ShowHarvestedConduit(tile);
                    }
                    else if (pivotArmed && availableCells != null && availableCells.Contains(coordinate))
                    {
                        cell.ShowPivotable(tile);
                    }
                    else
                    {
                        cell.ShowConduit(tile);
                    }

                    continue;
                }

                if (availableCells != null && availableCells.Contains(coordinate))
                {
                    cell.ShowAvailable();
                    continue;
                }

                cell.ShowEmpty();
            }
        }

        /// <summary>
        /// The world position of a cell, for anything that needs to point at one.
        /// </summary>
        internal Vector3 WorldPositionOf(HexCoord coordinate)
            => transform.TransformPoint(HexMetrics.ToWorld(coordinate));

        /// <summary>
        /// Half the board's height in its own coordinates, before the lean foreshortens it.
        /// </summary>
        private static float LocalHalfHeight(GameState state)
        {
            var lowest = float.MaxValue;
            var highest = float.MinValue;

            foreach (var coordinate in state.Board.Coordinates)
            {
                var y = HexMetrics.ToWorld(coordinate).y;
                lowest = Mathf.Min(lowest, y);
                highest = Mathf.Max(highest, y);
            }

            return state.Board.Coordinates.Count == 0 ? 0f : (highest - lowest) * 0.5f;
        }

        private void EnsureResources()
        {
            _hexMesh ??= HexMeshFactory.CreateHexagon(HexMetrics.Size * 0.92f);
            _spokeMesh ??= HexMeshFactory.CreateSpoke(TileVisual.SpokeLength, TileVisual.SpokeThickness);

            // Built at the drawn hexagon's own radius, so a pulse scaled to one exactly fills its cell
            // and never spills into the neighbour.
            _ringMesh ??= GlyphMeshFactory.CreateRing(HexMetrics.Size * 0.92f, HexMetrics.Size * 0.2f);

            if (_material == null)
            {
                if (_tileMaterial == null)
                {
                    Debug.LogError(
                        "[BoardView] No tile material assigned. Run ProjectBootstrap.CreateGameScene.");
                    return;
                }

                // Instanced from the asset so per-cell colour changes do not write back to
                // the shared material.
                _material = new Material(_tileMaterial);
            }
        }

        private void Clear()
        {
            foreach (var cell in _cells.Values)
            {
                if (cell != null)
                {
                    Destroy(cell.gameObject);
                }
            }

            _cells.Clear();

            // The rings were children of those cells, so they are gone with them.
            _pulsingCells.Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
