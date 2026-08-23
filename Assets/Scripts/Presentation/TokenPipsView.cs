using System.Collections.Generic;
using Pathweaver.Core.State;
using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Shows how many Pivot Tokens the player holds.
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
        private static readonly Vector2 ViewportPosition = new Vector2(0.86f, 0.10f);

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private GameSession _session;

        private readonly List<MeshRenderer> _pips = new List<MeshRenderer>();

        private Camera ResolvedCamera => _camera != null ? _camera : Camera.main;

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
                new Vector3(ViewportPosition.x, ViewportPosition.y, 0f));
            transform.position = new Vector3(world.x, world.y, -0.4f);
        }

        private void OnStateChanged(GameState state)
        {
            var held = state?.PivotTokens.Count ?? 0;

            for (var index = 0; index < _pips.Count; index++)
            {
                // Empty slots stay visible but dim, so the player can see there is something
                // to earn rather than only noticing tokens once they have one.
                _pips[index].material.color = index < held
                    ? BoardPalette.TokenHeld
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
                pip.transform.localPosition = new Vector3(0f, -index * PipSpacing, 0f);

                pip.AddComponent<MeshFilter>().sharedMesh = mesh;

                var renderer = pip.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _boardView.TileMaterial;
                renderer.material.color = BoardPalette.TokenEmpty;

                _pips.Add(renderer);
            }
        }
    }
}
