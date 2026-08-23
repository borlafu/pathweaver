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

        private readonly Dictionary<HexCoord, CellView> _cells = new Dictionary<HexCoord, CellView>();

        private Mesh _hexMesh;
        private Mesh _spokeMesh;
        private Material _material;

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
        /// Creates a cell view per cell in the state's board.
        /// </summary>
        internal void Build(GameState state)
        {
            Clear();
            EnsureResources();

            foreach (var coordinate in state.Board.Coordinates)
            {
                var cell = new GameObject($"Cell {coordinate}").AddComponent<CellView>();
                cell.transform.SetParent(transform, worldPositionStays: false);
                cell.Initialise(coordinate, _hexMesh, _spokeMesh, _material);

                _cells.Add(coordinate, cell);
            }

            Refresh(state);
        }

        /// <summary>
        /// Updates every cell to match the state, optionally marking where the held
        /// tile could go.
        /// </summary>
        internal void Refresh(GameState state, ISet<HexCoord> availableCells = null)
        {
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
                    cell.ShowConduit(tile);
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

        private void EnsureResources()
        {
            _hexMesh ??= HexMeshFactory.CreateHexagon(HexMetrics.Size * 0.92f);
            _spokeMesh ??= HexMeshFactory.CreateSpoke(TileVisual.SpokeLength, TileVisual.SpokeThickness);

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
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
