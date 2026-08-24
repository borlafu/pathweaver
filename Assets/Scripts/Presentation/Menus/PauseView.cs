using UnityEngine;

namespace Pathweaver.Game.Presentation.Menus
{
    /// <summary>
    /// Paused: carry on, start the level again, or go back to the list.
    /// </summary>
    /// <remarks>
    /// The board stays visible behind it. A player who pauses mid-puzzle is usually still looking
    /// at the board, and hiding it would make pausing feel like leaving.
    /// </remarks>
    internal sealed class PauseView : MonoBehaviour
    {
        internal const string ResumeId = "resume";
        internal const string RestartId = "restart";
        internal const string MenuId = "menu";

        private HexButton _resume;
        private HexButton _restart;
        private HexButton _menu;

        internal void Build(Camera camera, Material material)
        {
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

            return _menu != null && _menu.IsPressed(screenPosition) ? MenuId : null;
        }
    }
}
