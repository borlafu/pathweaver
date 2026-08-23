using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation.Menus
{
    /// <summary>
    /// The first screen: continue where you left off, choose a level, wander endlessly, or change
    /// settings.
    /// </summary>
    /// <remarks>
    /// Buttons and no text, because the project has no font. A play triangle, a small stack of hexes
    /// for the level list, an endless spiral, and a gear for settings — symbols that carry meaning
    /// without language, which is what a localised game would need anyway.
    /// </remarks>
    internal sealed class MainMenuView : MonoBehaviour
    {
        internal const string ContinueId = "continue";
        internal const string LevelsId = "levels";
        internal const string EndlessId = "endless";
        internal const string SettingsId = "settings";

        private HexButton _continue;
        private HexButton _levels;
        private HexButton _endless;
        private HexButton _settings;

        internal void Build(Camera camera, Material material)
        {
            _continue = HexButton.Create(
                transform, ContinueId, camera, material,
                new Vector2(0.5f, 0.62f), 0.85f, BoardPalette.MenuPrimary, touchRadiusFraction: 0.22f);

            // A play triangle, pointing the way a player expects to go.
            _continue.AddGlyph(
                HexMeshFactory.CreateRegularPolygon(3, 0.34f, rotationDegrees: -90f),
                BoardPalette.MenuGlyph);

            _levels = HexButton.Create(
                transform, LevelsId, camera, material,
                new Vector2(0.2f, 0.36f), 0.45f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.13f);

            // Three small hexes: a list of boards.
            foreach (var offset in new[] { -0.17f, 0f, 0.17f })
            {
                _levels.AddGlyph(
                    HexMeshFactory.CreateHexagon(0.07f), BoardPalette.MenuGlyph,
                    new Vector3(0f, offset, 0f));
            }

            _endless = HexButton.Create(
                transform, EndlessId, camera, material,
                new Vector2(0.5f, 0.36f), 0.45f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.13f);

            // A loop that never closes: a wandering route rather than a level with an end. Drawn
            // from the same arc as the rotation hint, at two radii so it reads as a spiral rather
            // than as another turn symbol.
            _endless.AddGlyph(
                HexMeshFactory.CreateCircularArrow(0.2f, 0.055f, sweepDegrees: 300f),
                BoardPalette.MenuGlyph);
            _endless.AddGlyph(
                HexMeshFactory.CreateCircularArrow(0.1f, 0.05f, sweepDegrees: 240f),
                BoardPalette.MenuGlyph);

            _settings = HexButton.Create(
                transform, SettingsId, camera, material,
                new Vector2(0.8f, 0.36f), 0.45f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.13f);

            // A ring: a filled disc with a hole punched through it. The hole has to be darker than
            // the button rather than the same colour, or the ring reads as a plain disc — which is
            // what it did on the first build.
            _settings.AddGlyph(HexMeshFactory.CreateRegularPolygon(12, 0.22f), BoardPalette.MenuGlyph);
            _settings.AddGlyph(HexMeshFactory.CreateRegularPolygon(12, 0.1f), BoardPalette.MenuGearHole);
        }

        /// <summary>
        /// Which button a tap landed on, or null.
        /// </summary>
        internal string ButtonAt(Vector2 screenPosition)
        {
            if (_continue != null && _continue.IsPressed(screenPosition))
            {
                return ContinueId;
            }

            if (_levels != null && _levels.IsPressed(screenPosition))
            {
                return LevelsId;
            }

            if (_endless != null && _endless.IsPressed(screenPosition))
            {
                return EndlessId;
            }

            return _settings != null && _settings.IsPressed(screenPosition) ? SettingsId : null;
        }
    }
}
