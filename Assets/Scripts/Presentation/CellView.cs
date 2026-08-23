using System.Collections.Generic;
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
    /// Presentation only. A cell view holds no game rule and never decides anything —
    /// it is told what to show. Keeping that boundary is what lets the simulation stay
    /// testable without Unity.
    /// </remarks>
    internal sealed class CellView : MonoBehaviour
    {
        private const float SpokeWidth = 0.14f;

        private readonly List<GameObject> _spokes = new List<GameObject>();

        private MeshRenderer _background;
        private Material _materialTemplate;
        private Mesh _spokeMesh;

        internal HexCoord Coordinate { get; private set; }

        internal void Initialise(HexCoord coordinate, Mesh hexMesh, Mesh spokeMesh, Material materialTemplate)
        {
            Coordinate = coordinate;
            _spokeMesh = spokeMesh;
            _materialTemplate = materialTemplate;

            transform.localPosition = HexMetrics.ToWorld(coordinate);

            _background = AddQuad("Background", hexMesh, BoardPalette.EmptyCell, sortingOffset: 0f);
            ShowEmpty();
        }

        /// <summary>Shows a cell with nothing on it.</summary>
        internal void ShowEmpty()
        {
            SetBackground(BoardPalette.EmptyCell);
            ClearSpokes();
        }

        /// <summary>Shows a spring or hub.</summary>
        internal void ShowEndpoint(FlowEndpoint endpoint)
        {
            SetBackground(endpoint.Role == EndpointRole.Spring ? BoardPalette.Spring : BoardPalette.Hub);
            ClearSpokes();

            // A small centre mark, so an endpoint reads as a feature rather than as a
            // brightly coloured empty cell.
            AddSpokeMark(BoardPalette.ForKind(endpoint.Kind), 0);
            AddSpokeMark(BoardPalette.ForKind(endpoint.Kind), 3);
        }

        /// <summary>Shows a placed conduit, with a spoke per open edge.</summary>
        internal void ShowConduit(ConduitTile tile)
        {
            SetBackground(BoardPalette.CellOutline);
            ClearSpokes();

            foreach (var edge in tile.Edges.OpenDirections)
            {
                AddSpokeMark(BoardPalette.ForKind(tile.Kind), edge);
            }
        }

        private void AddSpokeMark(Color colour, int edge)
        {
            var spoke = AddQuad($"Spoke{edge}", _spokeMesh, colour, sortingOffset: -0.01f);
            spoke.transform.localPosition = Vector3.zero;

            var direction = HexMetrics.EdgeDirection(edge);
            spoke.transform.localRotation = Quaternion.FromToRotation(Vector3.right, direction);

            _spokes.Add(spoke.gameObject);
        }

        private MeshRenderer AddQuad(string childName, Mesh mesh, Color colour, float sortingOffset)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, worldPositionStays: false);
            child.transform.localPosition = new Vector3(0f, 0f, sortingOffset);

            child.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _materialTemplate;
            renderer.material.color = colour;

            return renderer;
        }

        private void SetBackground(Color colour)
        {
            if (_background != null)
            {
                _background.material.color = colour;
            }
        }

        private void ClearSpokes()
        {
            foreach (var spoke in _spokes)
            {
                if (spoke != null)
                {
                    Destroy(spoke);
                }
            }

            _spokes.Clear();
        }

        internal static float SpokeLength => HexMetrics.CellSpacing * 0.5f;

        internal static float SpokeThickness => SpokeWidth;
    }
}
