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
        private readonly Dictionary<HexCoord, CellView> _cells = new Dictionary<HexCoord, CellView>();

        private Mesh _hexMesh;
        private Mesh _spokeMesh;
        private Material _material;

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
        /// Updates every cell to match the state.
        /// </summary>
        internal void Refresh(GameState state)
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
            _spokeMesh ??= HexMeshFactory.CreateSpoke(CellView.SpokeLength, CellView.SpokeThickness);

            // Unlit: the board is flat colour, and lighting it would cost frame time
            // for no visual gain until real art arrives.
            _material ??= new Material(Shader.Find("Universal Render Pipeline/Unlit"));
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
