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
        /// <summary>A spring pushes outward, so it is marked on every edge.</summary>
        private static readonly int[] SpringMarkEdges = { 0, 1, 2, 3, 4, 5 };

        /// <summary>A hub receives, so it is marked only across.</summary>
        private static readonly int[] HubMarkEdges = { 0, 3 };

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
            _visual.ClearMotif();
        }

        /// <summary>
        /// Shows an empty cell the held tile could legally occupy.
        /// </summary>
        internal void ShowAvailable()
        {
            _visual.SetBackground(BoardPalette.LegalCell);
            _visual.ClearSpokes();
            _visual.ClearMotif();
        }

        internal void ShowEndpoint(FlowEndpoint endpoint)
        {
            // A spring reaches outward on every edge, a hub only across. The pattern of marks
            // says which role a cell plays without relying on its colour, so the two remain
            // distinguishable when yellow and purple are not.
            var isSpring = endpoint.Role == EndpointRole.Spring;

            _visual.SetBackground(isSpring ? BoardPalette.Spring : BoardPalette.Hub);
            _visual.ShowEdges(
                isSpring ? SpringMarkEdges : HubMarkEdges, BoardPalette.ForKind(endpoint.Kind));
            _visual.ShowResource(endpoint.Kind, BoardPalette.ForKind(endpoint.Kind));
        }

        internal void ShowConduit(ConduitTile tile)
        {
            _visual.SetBackground(BoardPalette.CellOutline);
            _visual.ShowEdges(tile.Edges, BoardPalette.ForKind(tile.Kind));
            _visual.ShowResource(tile.Kind, BoardPalette.ForKind(tile.Kind));
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
            _visual.ShowResource(tile.Kind, BoardPalette.ForKind(tile.Kind));
        }
    }
}
