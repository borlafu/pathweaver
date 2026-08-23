using System.Collections.Generic;
using Pathweaver.Core.Tiles;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Draws a hexagon with a spoke per open edge.
    /// </summary>
    /// <remarks>
    /// Shared by the board's cells and by the tile in hand, so a conduit looks the
    /// same wherever it appears. A player about to place a tile has to recognise it
    /// as the thing that will land on the board.
    /// </remarks>
    internal sealed class TileVisual : MonoBehaviour
    {
        private const float SpokeWidth = 0.14f;

        private readonly List<GameObject> _spokes = new List<GameObject>();

        private Mesh _hexMesh;
        private Mesh _spokeMesh;
        private Material _material;
        private MeshRenderer _background;

        internal static float SpokeLength => HexMetrics.CellSpacing * 0.5f;

        internal void Initialise(Mesh hexMesh, Mesh spokeMesh, Material material)
        {
            _hexMesh = hexMesh;
            _spokeMesh = spokeMesh;
            _material = material;

            _background = AddQuad("Background", _hexMesh, BoardPalette.EmptyCell, depth: 0f);
        }

        internal void SetBackground(Color colour)
        {
            if (_background != null)
            {
                _background.material.color = colour;
            }
        }

        /// <summary>Replaces the spokes with one per open edge of the tile.</summary>
        internal void ShowEdges(EdgeMask edges, Color colour)
        {
            ClearSpokes();

            foreach (var edge in edges.OpenDirections)
            {
                AddSpoke(edge, colour);
            }
        }

        /// <summary>Draws spokes on specific edges, for endpoint markers.</summary>
        internal void ShowEdges(IEnumerable<int> edges, Color colour)
        {
            ClearSpokes();

            foreach (var edge in edges)
            {
                AddSpoke(edge, colour);
            }
        }

        internal void ClearSpokes()
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

        private void AddSpoke(int edge, Color colour)
        {
            var spoke = AddQuad($"Spoke{edge}", _spokeMesh, colour, depth: -0.01f);
            spoke.transform.localRotation =
                Quaternion.FromToRotation(Vector3.right, HexMetrics.EdgeDirection(edge));

            _spokes.Add(spoke.gameObject);
        }

        private MeshRenderer AddQuad(string childName, Mesh mesh, Color colour, float depth)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, worldPositionStays: false);
            child.transform.localPosition = new Vector3(0f, 0f, depth);

            child.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.material.color = colour;

            return renderer;
        }

        private void OnDestroy()
        {
            ClearSpokes();
        }

        internal static float SpokeThickness => SpokeWidth;
    }
}
