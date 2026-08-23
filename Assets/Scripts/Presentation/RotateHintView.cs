using System.Collections.Generic;
using UnityEngine;
using Pathweaver.Game.App;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Shows that the tile in hand can be turned.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Added after device testing: tapping the tray to rotate took a real player a while to
    /// find. An interaction nobody discovers is an interaction that does not exist, and the
    /// tile bag deals bends that are useless until turned — so a player who has not found
    /// rotation concludes the game dealt them a dead tile.
    /// </para>
    /// <para>
    /// Three chevrons around the tray tile, pulsing until the player rotates for the first
    /// time, then still and dimmer. Motion is what draws the eye; a static icon on a board
    /// full of static shapes would not. It stops pulsing rather than disappearing, because
    /// the affordance still needs to read as available afterwards.
    /// </para>
    /// <para>
    /// Shown as geometry rather than text: there is no font, no UI canvas, and no
    /// localisation in the project yet, and a symbol needs none of the three.
    /// </para>
    /// </remarks>
    internal sealed class RotateHintView : MonoBehaviour
    {
        private const int ChevronCount = 3;
        private const float OrbitRadius = 0.62f;
        private const float ChevronSize = 0.14f;
        private const float PulsesPerSecond = 1.1f;
        private const float PulseDepth = 0.28f;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private HeldTileView _heldTileView;

        [SerializeField]
        private GameSession _session;

        private readonly List<Transform> _chevrons = new List<Transform>();

        private bool _hasRotated;

        private void OnEnable()
        {
            if (_session != null)
            {
                _session.HeldRotated += OnHeldRotated;
            }
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.HeldRotated -= OnHeldRotated;
            }
        }

        private void Start()
        {
            Build();
        }

        private void Update()
        {
            if (_heldTileView != null)
            {
                transform.position = _heldTileView.TrayWorldPosition;
            }

            // Once the player knows, the hint stops moving but stays visible.
            var scale = _hasRotated
                ? 1f
                : 1f + (Mathf.Sin(Time.unscaledTime * PulsesPerSecond * Mathf.PI * 2f) * PulseDepth);

            foreach (var chevron in _chevrons)
            {
                chevron.localScale = Vector3.one * scale;
            }
        }

        private void OnHeldRotated()
        {
            _hasRotated = true;
        }

        private void Build()
        {
            if (_boardView == null)
            {
                return;
            }

            var mesh = BuildChevron(ChevronSize);

            for (var index = 0; index < ChevronCount; index++)
            {
                // Spaced around the tile rather than clustered, so the arrangement itself
                // suggests turning.
                var angle = (360f / ChevronCount * index) + 30f;
                var radians = angle * Mathf.Deg2Rad;

                var pivot = new GameObject($"Chevron{index}").transform;
                pivot.SetParent(transform, worldPositionStays: false);
                pivot.localPosition = new Vector3(
                    Mathf.Cos(radians) * OrbitRadius,
                    Mathf.Sin(radians) * OrbitRadius,
                    -0.05f);

                // Pointing along the circle, which is the direction a turn travels.
                pivot.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);

                pivot.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;

                var renderer = pivot.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _boardView.TileMaterial;
                renderer.material.color = BoardPalette.Hint;

                _chevrons.Add(pivot);
            }
        }

        /// <summary>A small triangle, pointing along +X.</summary>
        private static Mesh BuildChevron(float size)
        {
            var mesh = new Mesh { name = "Chevron" };

            mesh.SetVertices(new List<Vector3>
            {
                new Vector3(size, 0f, 0f),
                new Vector3(-size * 0.6f, size * 0.7f, 0f),
                new Vector3(-size * 0.6f, -size * 0.7f, 0f),
            });

            // Wound to face the camera at negative Z, the same way the hexagons are.
            mesh.SetTriangles(new List<int> { 0, 2, 1 }, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
