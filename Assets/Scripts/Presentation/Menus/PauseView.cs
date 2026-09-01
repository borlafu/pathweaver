using UnityEngine;

namespace Pathweaver.Game.Presentation.Menus
{
    /// <summary>
    /// Paused: carry on, start the level again, or go back to the list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The board stays visible behind it. A player who pauses mid-puzzle is usually still looking
    /// at the board, and hiding it would make pausing feel like leaving.
    /// </para>
    /// <para>
    /// It is also where the level's name goes. Every level file carries one and there was nowhere to
    /// show it; the board itself is the wrong place, because a line of text over the top rows would
    /// cover the puzzle permanently to answer a question asked once. Pausing is when a player asks
    /// which level they are on.
    /// </para>
    /// </remarks>
    internal sealed class PauseView : MonoBehaviour
    {
        internal const string ResumeId = "resume";
        internal const string RestartId = "restart";
        internal const string MenuId = "menu";
        internal const string HelpId = "help";

        /// <summary>
        /// Where the help control sits, below the row that leaves or restarts.
        /// </summary>
        /// <remarks>
        /// Reachable from here as well as from the main menu, because a player who is stuck is stuck
        /// mid-level. Making them abandon the board to read how it works is the one moment help is
        /// least affordable.
        /// </remarks>
        internal const float HelpViewportY = 0.22f;

        /// <summary>
        /// Where the level's name sits, clear of the resume button below it.
        /// </summary>
        internal const float TitleViewportY = 0.75f;

        private HexButton _resume;
        private HexButton _restart;
        private HexButton _menu;
        private HexButton _help;
        private Text.TextLabel _title;
        private Text.TextLabel _helpMark;

        internal void Build(Camera camera, Material material)
        {
            _title = Text.TextLabel.Create(
                transform,
                camera,
                "level-name",
                new Vector2(0.5f, TitleViewportY),
                Text.LabelMetrics.HeadingHeightFraction,
                BoardPalette.TextPrimary,
                TMPro.TextAlignmentOptions.Center,
                HexButton.LabelDepth);

            _resume = HexButton.Create(
                transform, ResumeId, camera, material,
                new Vector2(0.5f, 0.58f), 0.6f, BoardPalette.MenuPrimary, touchRadiusFraction: 0.16f);
            MenuGlyphs.AddPlay(_resume, 0.24f);

            _restart = HexButton.Create(
                transform, RestartId, camera, material,
                new Vector2(0.33f, 0.38f), 0.45f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.13f);
            MenuGlyphs.AddRestartArrow(_restart);

            _menu = HexButton.Create(
                transform, MenuId, camera, material,
                new Vector2(0.67f, 0.38f), 0.45f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.13f);
            MenuGlyphs.AddLevelStack(_menu, pipRadius: 0.06f, spacing: 0.14f);

            _help = HexButton.Create(
                transform, HelpId, camera, material,
                new Vector2(0.5f, HelpViewportY), 0.36f, BoardPalette.MenuSecondary,
                touchRadiusFraction: 0.11f);

            _helpMark = Text.TextLabel.Create(
                transform,
                camera,
                HelpId,
                new Vector2(0.5f, HelpViewportY),
                Text.LabelMetrics.BodyHeightFraction,
                BoardPalette.MenuGlyph,
                TMPro.TextAlignmentOptions.Center,
                HexButton.LabelDepth);

            _helpMark.SetText("?");
        }

        /// <summary>
        /// Names the level being paused.
        /// </summary>
        /// <remarks>
        /// Set when the screen opens rather than at build time, because the screen is built once and
        /// the level changes underneath it.
        /// </remarks>
        internal void SetLevelName(string name)
        {
            _title?.SetText(name ?? string.Empty);
        }

        internal string ButtonAt(Vector2 screenPosition)
        {
            if (_resume != null && _resume.IsPressed(screenPosition))
            {
                return ResumeId;
            }

            if (_restart != null && _restart.IsPressed(screenPosition))
            {
                return RestartId;
            }

            if (_menu != null && _menu.IsPressed(screenPosition))
            {
                return MenuId;
            }

            return _help != null && _help.IsPressed(screenPosition) ? HelpId : null;
        }
    }
}
