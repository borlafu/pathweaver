using System.Collections.Generic;
using Pathweaver.Core.State;
using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Which resource a pip row counts.
    /// </summary>
    internal enum TokenKind
    {
        Pivot,
        Skip,
    }

    /// <summary>
    /// Shows how many of a token the player holds.
    /// </summary>
    /// <remarks>
    /// Counted as pips rather than written as a number, for the same reason the progress bar is
    /// a bar: there is no font yet. At these quantities a count of shapes reads faster than a
    /// digit anyway.
    /// <para>
    /// Without this, tokens are earned and spent invisibly — and a resource the player cannot
    /// see is a resource they will not use, which would quietly undo the whole anti-deadlock
    /// mechanism of PRD section 3.2B.
    /// </para>
    /// </remarks>
    internal sealed class TokenPipsView : MonoBehaviour
    {
        private const int MaximumPips = 6;
        private const float PipRadius = 0.11f;
        private const float PipSpacing = 0.3f;

        [SerializeField]
        private TokenKind _kind = TokenKind.Pivot;

        [SerializeField]
        private Vector2 _viewportPosition = new Vector2(0.12f, 0.26f);

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private GameSession _session;

        /// <summary>
        /// How far from the pip column a tap still counts, as a fraction of the shorter screen edge.
        /// </summary>
        /// <remarks>
        /// Generous, because the pips themselves are small and this is the only way to arm a Pivot
        /// Token. A miss here reads as the control not existing.
        /// </remarks>
        private const float TouchRadiusFraction = 0.13f;

        private readonly List<MeshRenderer> _pips = new List<MeshRenderer>();

        private Camera ResolvedCamera => _camera != null ? _camera : Camera.main;

        /// <summary>Whether this row can be tapped to arm a token.</summary>
        /// <remarks>
        /// Only the Pivot row. A skip is spent on the tile in hand and already has its own button;
        /// there is nothing on the board for it to point at.
        /// </remarks>
        internal bool IsArmable => _kind == TokenKind.Pivot;

        /// <summary>Whether a screen position lands on this row of pips.</summary>
        internal bool IsPressed(Vector2 screenPosition)
        {
            if (_pips.Count == 0)
            {
                return false;
            }

            // Measured against the middle of the column rather than a single pip, so the whole row
            // is one control however many pips are lit.
            var centre = transform.position + new Vector3(0f, PipSpacing, 0f);
            var pipScreen = ResolvedCamera.WorldToScreenPoint(centre);
            var radius = Mathf.Min(Screen.width, Screen.height) * TouchRadiusFraction;

            return Vector2.Distance(screenPosition, pipScreen) <= radius;
        }

        private void OnEnable()
        {
            if (_session != null)
            {
                _session.StateChanged += OnStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.StateChanged -= OnStateChanged;
            }
        }

        private void Start()
        {
            Build();

            if (_session?.State != null)
            {
                OnStateChanged(_session.State);
            }
        }

        private void Update()
        {
            var world = ResolvedCamera.ViewportToWorldPoint(
                new Vector3(_viewportPosition.x, _viewportPosition.y, 0f));
            transform.position = new Vector3(world.x, world.y, -0.4f);
        }

        private void OnStateChanged(GameState state)
        {
            var held = state == null
                ? 0
                : _kind == TokenKind.Pivot ? state.PivotTokens.Count : state.SkipTokens.Count;

            var armed = IsArmable && _session != null && _session.IsPivotArmed;

            for (var index = 0; index < _pips.Count; index++)
            {
                // Empty slots stay visible but dim, so the player can see there is something
                // to earn rather than only noticing tokens once they have one.
                var filled = index < held;

                _pips[index].material.color = filled
                    ? (_kind == TokenKind.Pivot
                        ? (armed ? BoardPalette.TokenArmed : BoardPalette.TokenHeld)
                        : BoardPalette.SkipHeld)
                    : BoardPalette.TokenEmpty;

                _pips[index].gameObject.SetActive(index < Mathf.Max(held, 3));
            }
        }

        private void Build()
        {
            if (_boardView == null)
            {
                return;
            }

            var mesh = HexMeshFactory.CreateHexagon(PipRadius);

            for (var index = 0; index < MaximumPips; index++)
            {
                var pip = new GameObject($"Pip{index}");
                pip.transform.SetParent(transform, worldPositionStays: false);
                // Stacked upward from the anchor, so a growing count never collides with the
                // controls below it.
                pip.transform.localPosition = new Vector3(0f, index * PipSpacing, 0f);

                pip.AddComponent<MeshFilter>().sharedMesh = mesh;

                var renderer = pip.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _boardView.TileMaterial;
                renderer.material.color = BoardPalette.TokenEmpty;

                _pips.Add(renderer);
            }
        }
    }
}
