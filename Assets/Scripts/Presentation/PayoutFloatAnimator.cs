using System.Collections.Generic;
using System.Globalization;
using Pathweaver.Game.App;
using Pathweaver.Game.Platform;
using Pathweaver.Game.Presentation.Text;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Shows what each route paid, at the hub it paid into.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The score curve is the centre of the design and had no interface. A player saw a bar move and a
    /// total change, with no way to connect either to the route they had just finished — and no way at
    /// all to feel that a longer route paid geometrically more, which is the whole trade PRD section
    /// 3.2A is built on.
    /// </para>
    /// <para>
    /// One number per route rather than one per harvest, because two routes completing together is the
    /// case worth telling apart: a single total would hide which of them was worth having.
    /// </para>
    /// </remarks>
    internal sealed class PayoutFloatAnimator : MonoBehaviour
    {
        [SerializeField]
        private GameSession _session;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private FrameRateGovernor _frameRateGovernor;

        private readonly List<Floating> _floating = new List<Floating>();

        private Camera ResolvedCamera => _camera != null ? _camera : Camera.main;

        private void OnEnable()
        {
            if (_session != null)
            {
                _session.RoutesHarvested += OnRoutesHarvested;
            }
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.RoutesHarvested -= OnRoutesHarvested;
            }

            Clear();
        }

        private void OnRoutesHarvested(int count)
        {
            if (_session == null || _boardView == null || ResolvedCamera == null)
            {
                return;
            }

            foreach (var payout in _session.LastPayouts)
            {
                if (payout.Amount <= 0)
                {
                    continue;
                }

                Spawn(payout);
            }
        }

        private void Spawn(Payout payout)
        {
            var world = _boardView.WorldPositionOf(payout.Hub);
            var viewport = ResolvedCamera.WorldToViewportPoint(world);

            var label = TextLabel.Create(
                transform,
                ResolvedCamera,
                $"payout{_floating.Count}",
                new Vector2(viewport.x, viewport.y),
                LabelMetrics.BodyHeightFraction,
                BoardPalette.ForKind(payout.Kind),
                // In front of the board and of the backdrop bands, because a payout at a hub near the
                // top of the board would otherwise be swallowed by the reporting strip.
                depth: Menus.HexButton.LabelDepth);

            // Grouped with separators, matching the score under the bar: a four-figure payout from a long
            // route is the moment the curve becomes visible, and it should read as one number.
            label.SetText($"+{payout.Amount.ToString("N0", CultureInfo.InvariantCulture)}");

            _floating.Add(new Floating(label, new Vector2(viewport.x, viewport.y), payout.Kind));
        }

        private void Update()
        {
            if (_floating.Count == 0)
            {
                return;
            }

            // A payout is a transient, so pinning the active frame rate for its second is fair — unlike
            // the endpoint and flow pulses, which never stop and deliberately never ask.
            _frameRateGovernor?.NotifyActivity();

            var reduceMotion = GameSettings.ReduceMotion;

            for (var index = _floating.Count - 1; index >= 0; index--)
            {
                var floating = _floating[index];
                floating.Elapsed += Time.unscaledDeltaTime;

                var phase = floating.Elapsed / PayoutFloat.DurationSeconds;

                if (phase >= 1f)
                {
                    Destroy(floating.Label.gameObject);
                    _floating.RemoveAt(index);
                    continue;
                }

                var (rise, alpha) = reduceMotion
                    ? PayoutFloat.EvaluateStill(phase)
                    : PayoutFloat.Evaluate(phase);

                floating.Label.SetViewportPosition(
                    new Vector2(floating.Origin.x, floating.Origin.y + rise));

                var colour = BoardPalette.ForKind(floating.Kind);
                floating.Label.SetColour(new Color(colour.r, colour.g, colour.b, alpha));

                _floating[index] = floating;
            }
        }

        private void Clear()
        {
            foreach (var floating in _floating)
            {
                if (floating.Label != null)
                {
                    Destroy(floating.Label.gameObject);
                }
            }

            _floating.Clear();
        }

        private struct Floating
        {
            internal Floating(TextLabel label, Vector2 origin, Pathweaver.Core.Tiles.ResourceKind kind)
            {
                Label = label;
                Origin = origin;
                Kind = kind;
                Elapsed = 0f;
            }

            internal TextLabel Label { get; }

            internal Vector2 Origin { get; }

            internal Pathweaver.Core.Tiles.ResourceKind Kind { get; }

            internal float Elapsed { get; set; }
        }
    }
}
