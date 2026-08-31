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
        internal const string AtlasId = "atlas";
        internal const string SettingsId = "settings";

        /// <summary>The secondary row's height, as a fraction of the viewport.</summary>
        private const float SecondaryRowY = 0.36f;

        /// <summary>Centre-to-centre spacing along the secondary row, in viewport fractions.</summary>
        private const float SecondarySpacing = 0.23f;

        private const float SecondaryRadius = 0.38f;
        private const float SecondaryTouchRadius = 0.11f;

        private HexButton _continue;
        private HexButton _levels;
        private HexButton _endless;
        private HexButton _atlas;
        private HexButton _settings;

        /// <summary>
        /// Whether the World Atlas is offered at all.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The atlas is built, saved, and covered by tests, but nothing on screen says what Star
        /// Essence is, what a node costs, or what a relic does — so a player meets a constellation of
        /// coloured hexagons and has to guess. It is withheld until it can be read rather than
        /// guessed, which needs a font the project does not yet have.
        /// </para>
        /// <para>
        /// Withheld, not disabled: <c>GameFlow.AwardEssence</c> keeps paying on every clear while this
        /// is false, so essence banks up and a player loses nothing by the wait.
        /// </para>
        /// <para>
        /// A property rather than a <c>const</c> deliberately. A constant false condition makes the
        /// guarded branch unreachable code, which the compiler is right to warn about and which would
        /// have to be silenced; this reads the same and does not.
        /// </para>
        /// </remarks>
        internal static bool IsAtlasVisible => false;

        /// <summary>How many buttons the secondary row holds.</summary>
        internal static int SecondaryCount => IsAtlasVisible ? 4 : 3;

        /// <summary>
        /// Where the given secondary button sits horizontally, as a viewport fraction.
        /// </summary>
        /// <remarks>
        /// Spacing is fixed and the row is centred, so removing a button closes the gap rather than
        /// leaving a hole, and the remaining buttons keep the density a thumb learned. With four
        /// buttons this reproduces the hand-placed 0.155, 0.385, 0.615, 0.845 exactly.
        /// </remarks>
        internal static float SecondaryX(int index, int count)
            => 0.5f + (SecondarySpacing * (index - ((count - 1) * 0.5f)));

        internal void Build(Camera camera, Material material)
        {
            _continue = HexButton.Create(
                transform, ContinueId, camera, material,
                new Vector2(0.5f, 0.62f), 0.85f, BoardPalette.MenuPrimary, touchRadiusFraction: 0.22f);

            MenuGlyphs.AddPlay(_continue, 0.34f);

            var count = SecondaryCount;
            var index = 0;

            _levels = CreateSecondary(camera, material, LevelsId, SecondaryX(index++, count));
            MenuGlyphs.AddLevelStack(_levels, pipRadius: 0.07f, spacing: 0.17f);

            _endless = CreateSecondary(camera, material, EndlessId, SecondaryX(index++, count));
            MenuGlyphs.AddEndlessSpiral(_endless);

            if (IsAtlasVisible)
            {
                _atlas = CreateSecondary(camera, material, AtlasId, SecondaryX(index++, count));
                MenuGlyphs.AddConstellation(_atlas);
            }

            _settings = CreateSecondary(camera, material, SettingsId, SecondaryX(index, count));
            MenuGlyphs.AddSettingsGear(_settings);
        }

        private HexButton CreateSecondary(Camera camera, Material material, string id, float viewportX)
            => HexButton.Create(
                transform, id, camera, material,
                new Vector2(viewportX, SecondaryRowY), SecondaryRadius, BoardPalette.MenuSecondary,
                touchRadiusFraction: SecondaryTouchRadius);

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
