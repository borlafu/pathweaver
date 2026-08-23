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
            _resume.AddGlyph(
                HexMeshFactory.CreateRegularPolygon(3, 0.24f, rotationDegrees: -90f), BoardPalette.MenuGlyph);

            _restart = HexButton.Create(
                transform, RestartId, camera, material,
                new Vector2(0.33f, 0.38f), 0.45f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.13f);
            _restart.AddGlyph(
                HexMeshFactory.CreateCircularArrow(0.2f, 0.065f, 265f), BoardPalette.MenuGlyph);

            _menu = HexButton.Create(
                transform, MenuId, camera, material,
                new Vector2(0.67f, 0.38f), 0.45f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.13f);
            foreach (var offset in new[] { -0.14f, 0f, 0.14f })
            {
                _menu.AddGlyph(
                    HexMeshFactory.CreateHexagon(0.06f), BoardPalette.MenuGlyph, new Vector3(0f, offset, 0f));
            }
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
