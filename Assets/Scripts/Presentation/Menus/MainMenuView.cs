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
    /// for the level list, an endless spiral, a small constellation for the World Atlas, and a gear for
    /// settings — symbols that carry meaning without language, which is what a localised game would
    /// need anyway.
    /// </remarks>
    internal sealed class MainMenuView : MonoBehaviour
    {
        internal const string ContinueId = "continue";
        internal const string LevelsId = "levels";
        internal const string EndlessId = "endless";
        internal const string AtlasId = "atlas";
        internal const string SettingsId = "settings";

        private HexButton _continue;
        private HexButton _levels;
        private HexButton _endless;
        private HexButton _atlas;
        private HexButton _settings;

        internal void Build(Camera camera, Material material)
        {
            _continue = HexButton.Create(
                transform, ContinueId, camera, material,
                new Vector2(0.5f, 0.62f), 0.85f, BoardPalette.MenuPrimary, touchRadiusFraction: 0.22f);

            MenuGlyphs.AddPlay(_continue, 0.34f);

            _levels = HexButton.Create(
                transform, LevelsId, camera, material,
                new Vector2(0.155f, 0.36f), 0.38f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.11f);

            MenuGlyphs.AddLevelStack(_levels, pipRadius: 0.07f, spacing: 0.17f);

            _endless = HexButton.Create(
                transform, EndlessId, camera, material,
                new Vector2(0.385f, 0.36f), 0.38f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.11f);

            MenuGlyphs.AddEndlessSpiral(_endless);

            _atlas = HexButton.Create(
                transform, AtlasId, camera, material,
                new Vector2(0.615f, 0.36f), 0.38f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.11f);

            MenuGlyphs.AddConstellation(_atlas);

            _settings = HexButton.Create(
                transform, SettingsId, camera, material,
                new Vector2(0.845f, 0.36f), 0.38f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.11f);

            MenuGlyphs.AddSettingsGear(_settings);
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

            if (_atlas != null && _atlas.IsPressed(screenPosition))
            {
                return AtlasId;
            }

            return _settings != null && _settings.IsPressed(screenPosition) ? SettingsId : null;
        }
    }
}
