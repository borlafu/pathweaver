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
        private BoardTheme _theme;
        private GameObject _background;
        private SpriteRenderer _backgroundSprite;
        private MeshRenderer _backgroundMesh;
        private GameObject _motif;

        internal static float SpokeLength => HexMetrics.CellSpacing * 0.5f;

        internal void Initialise(Mesh hexMesh, Mesh spokeMesh, Material material, BoardTheme theme = null)
        {
            _hexMesh = hexMesh;
            _spokeMesh = spokeMesh;
            _material = material;
            _theme = theme;

            // A theme's cell art replaces the generated hexagon when it exists, and the two
            // paths are otherwise identical, so a partly finished art set still plays.
            var cellSprite = _theme?.CellBackground;

            if (cellSprite != null)
            {
                _background = AddSprite("Background", cellSprite, BoardPalette.EmptyCell, depth: 0f);
                _backgroundSprite = _background.GetComponent<SpriteRenderer>();
            }
            else
            {
                _backgroundMesh = AddQuad("Background", _hexMesh, BoardPalette.EmptyCell, depth: 0f);
                _background = _backgroundMesh.gameObject;
            }
        }

        internal void SetBackground(Color colour)
        {
            if (_backgroundSprite != null)
            {
                _backgroundSprite.color = colour;
                return;
            }

            if (_backgroundMesh != null)
            {
                _backgroundMesh.material.color = colour;
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

        /// <summary>
        /// Marks which resource this is, by shape as well as by colour.
        /// </summary>
        /// <remarks>
        /// Drawn at the centre where the spokes meet, so it reads as belonging to the conduit
        /// rather than sitting on top of it.
        /// </remarks>
        internal void ShowResource(ResourceKind kind, Color colour)
        {
            ClearMotif();

            var sprite = _theme?.MotifFor(kind);

            _motif = sprite != null
                ? AddSprite($"Motif{kind}", sprite, Color.white, depth: -0.02f)
                : AddQuad(
                    $"Motif{kind}",
                    ResourceMotif.Create(kind, HexMetrics.Size),
                    colour,
                    depth: -0.02f).gameObject;
        }

        internal void ClearMotif()
        {
            if (_motif != null)
            {
                Destroy(_motif);
                _motif = null;
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
            // The arm sprite is per resource, so it is looked up by colour's owner rather than
            // held here; a theme without one falls back to the generated bar.
            var spoke = _armSprite != null
                ? AddSprite($"Spoke{edge}", _armSprite, Color.white, depth: -0.01f)
                : AddQuad($"Spoke{edge}", _spokeMesh, colour, depth: -0.01f).gameObject;

            spoke.transform.localRotation =
                Quaternion.FromToRotation(Vector3.right, HexMetrics.EdgeDirection(edge));

            _spokes.Add(spoke);
        }

        private Sprite _armSprite;

        /// <summary>
        /// Chooses the arm artwork for the resource about to be drawn.
        /// </summary>
        /// <remarks>
        /// Set before the spokes so a themed conduit and its motif agree. Cleared for endpoint
        /// marks, which are not conduits and should not borrow a conduit's artwork.
        /// </remarks>
        internal void UseResourceArt(ResourceKind? kind)
        {
            _armSprite = kind.HasValue ? _theme?.ArmFor(kind.Value) : null;
        }

        private GameObject AddSprite(string childName, Sprite sprite, Color colour, float depth)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, worldPositionStays: false);
            child.transform.localPosition = new Vector3(0f, 0f, depth);

            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = colour;

            // Nearer the camera means drawn later, matching how the mesh path is layered.
            renderer.sortingOrder = Mathf.RoundToInt(-depth * 100f);

            return child;
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
            ClearMotif();
        }

        internal static float SpokeThickness => SpokeWidth;
    }
}
