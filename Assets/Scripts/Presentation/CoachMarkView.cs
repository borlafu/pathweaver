using Pathweaver.Core.Hex;
using Pathweaver.Core.State;
using Pathweaver.Game.App;
using Pathweaver.Game.Presentation.Text;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Says the three things a player needs before they would think to look them up.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The help screen explains every rule and has to be sought out. These are the ones a player needs
    /// first: that a tile is dragged from the tray, that it can be turned, and that it only joins its
    /// own kind. Each is shown once ever and is dismissed the moment the player does the thing it asked
    /// for — a hint that outlives its lesson has become furniture.
    /// </para>
    /// <para>
    /// The third is shown on a refused placement rather than up front. A player who has just been told
    /// no is the one player guaranteed to be looking, and the rule means far more as an answer than as
    /// an announcement.
    /// </para>
    /// <para>
    /// No arrows, no highlight, no modal. A sentence over the board, in the middle of the space between
    /// the tray and the board's centre, that goes away by itself. Anything that has to be dismissed is
    /// a thing standing between a player and the game.
    /// </para>
    /// </remarks>
    internal sealed class CoachMarkView : MonoBehaviour
    {
        /// <summary>Where the sentence sits, as a viewport fraction.</summary>
        /// <remarks>
        /// Above the drawer at 0.24 and below the board's middle, so it is near the tray it is usually
        /// talking about without covering the cells a player is about to look at.
        /// </remarks>
        internal const float ViewportY = 0.32f;

        /// <summary>How much of the width a sentence may use before wrapping.</summary>
        internal const float WrapWidthFraction = 0.8f;

        [SerializeField]
        private GameSession _session;

        [SerializeField]
        private Camera _camera;

        private TextLabel _label;
        private CoachMark _showing = CoachMark.None;
        private float _elapsed;

        private Camera ResolvedCamera => _camera != null ? _camera : Camera.main;

        private void OnEnable()
        {
            if (_session == null)
            {
                return;
            }

            _session.StateChanged += OnStateChanged;
            _session.TilePlaced += OnTilePlaced;
            _session.HeldRotated += OnRotated;
            _session.PlacementRefused += OnRefused;
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.StateChanged -= OnStateChanged;
                _session.TilePlaced -= OnTilePlaced;
                _session.HeldRotated -= OnRotated;
                _session.PlacementRefused -= OnRefused;
            }

            Dismiss();
        }

        private void OnStateChanged(GameState state)
        {
            // Only on a board nobody has touched, so a resumed game does not open with a tutorial.
            if (state != null && state.Board.OccupiedCount == 0)
            {
                Show(CoachMark.Place);
            }
        }

        private void OnTilePlaced()
        {
            // The first placement answers the first hint and earns the second: a player who has managed
            // to place a tile is ready to hear that it could have been turned first.
            Retire(CoachMark.Place);
            Show(CoachMark.Turn);
        }

        private void OnRotated() => Retire(CoachMark.Turn);

        private void OnRefused(HexCoord cell) => Show(CoachMark.Join);

        /// <summary>
        /// Shows a hint, unless it has been seen or something else is already speaking.
        /// </summary>
        /// <remarks>
        /// One at a time, and never interrupting: two sentences at once is a wall of text, and a hint
        /// replaced halfway through was never read.
        /// </remarks>
        private void Show(CoachMark mark)
        {
            if (_showing != CoachMark.None || CoachMarks.HasSeen(mark))
            {
                return;
            }

            var camera = ResolvedCamera;
            if (camera == null)
            {
                return;
            }

            _label ??= Build(camera);

            _label.gameObject.SetActive(true);
            _label.SetText(CoachMarks.TextFor(mark));

            _showing = mark;
            _elapsed = 0f;

            // Recorded as seen when it appears, not when it ends. A hint interrupted by the player
            // closing the game has still been shown, and showing it again would be worse than losing it.
            CoachMarks.MarkSeen(mark);
        }

        /// <summary>Ends a hint early, because the player has just done what it asked.</summary>
        private void Retire(CoachMark mark)
        {
            if (_showing == mark)
            {
                Dismiss();
            }
        }

        private void Dismiss()
        {
            _showing = CoachMark.None;

            if (_label != null)
            {
                _label.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (_showing == CoachMark.None || _label == null)
            {
                return;
            }

            _elapsed += Time.unscaledDeltaTime;

            var phase = _elapsed / CoachMarkFade.DurationSeconds;

            if (phase >= 1f)
            {
                Dismiss();
                return;
            }

            var alpha = GameSettings.ReduceMotion
                ? CoachMarkFade.AlphaAtStill(phase)
                : CoachMarkFade.AlphaAt(phase);

            var colour = BoardPalette.TextPrimary;
            _label.SetColour(new Color(colour.r, colour.g, colour.b, alpha));
        }

        private TextLabel Build(Camera camera)
        {
            var label = TextLabel.Create(
                transform,
                camera,
                "coach",
                new Vector2(0.5f, ViewportY),
                LabelMetrics.BodyHeightFraction,
                BoardPalette.TextPrimary,
                // In front of the backdrop bands, so a hint is not swallowed by one at either edge.
                depth: Menus.HexButton.LabelDepth);

            label.SetWrapWidth(WrapWidthFraction);
            return label;
        }
    }
}
