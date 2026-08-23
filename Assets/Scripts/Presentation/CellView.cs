using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Tiles;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Draws one cell: its background, and the conduit or endpoint on it.
    /// </summary>
    /// <remarks>
    /// Presentation only. A cell holds no game rule and decides nothing — it is told
    /// what to show. Keeping that boundary is what lets the simulation stay testable
    /// without Unity.
    /// </remarks>
    internal sealed class CellView : MonoBehaviour
    {
        private static readonly int[] EndpointMarkEdges = { 0, 3 };

        private TileVisual _visual;

        internal HexCoord Coordinate { get; private set; }

        internal void Initialise(HexCoord coordinate, Mesh hexMesh, Mesh spokeMesh, Material material)
        {
            Coordinate = coordinate;
            transform.localPosition = HexMetrics.ToWorld(coordinate);

            _visual = new GameObject("Visual").AddComponent<TileVisual>();
            _visual.transform.SetParent(transform, worldPositionStays: false);
            _visual.Initialise(hexMesh, spokeMesh, material);

            ShowEmpty();
        }

        internal void ShowEmpty()
        {
            _visual.SetBackground(BoardPalette.EmptyCell);
            _visual.ClearSpokes();
        }

        /// <summary>
        /// Shows an empty cell the held tile could legally occupy.
        /// </summary>
        internal void ShowAvailable()
        {
            _visual.SetBackground(BoardPalette.LegalCell);
            _visual.ClearSpokes();
        }

        internal void ShowEndpoint(FlowEndpoint endpoint)
        {
            _visual.SetBackground(
                endpoint.Role == EndpointRole.Spring ? BoardPalette.Spring : BoardPalette.Hub);
            _visual.ShowEdges(EndpointMarkEdges, BoardPalette.ForKind(endpoint.Kind));
        }

        internal void ShowConduit(ConduitTile tile)
        {
            _visual.SetBackground(BoardPalette.CellOutline);
            _visual.ShowEdges(tile.Edges, BoardPalette.ForKind(tile.Kind));
        }

        /// <summary>
        /// Draws a conduit as part of a route that has just harvested.
        /// </summary>
        /// <remarks>
        /// The whole path lights up rather than the last tile placed, because what paid out is
        /// the route, and a player needs to see which one.
        /// </remarks>
        internal void ShowHarvestedConduit(ConduitTile tile)
        {
            _visual.SetBackground(BoardPalette.HarvestFlash);
            _visual.ShowEdges(tile.Edges, BoardPalette.ForKind(tile.Kind));
        }
    }
}
