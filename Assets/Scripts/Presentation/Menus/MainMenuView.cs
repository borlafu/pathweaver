using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation.Menus
{
    /// <summary>
    /// The first screen: continue where you left off, choose a level, wander endlessly, or change
    /// settings.
    /// </summary>
    /// <remarks>
    /// Symbols rather than words, which is what a localised game would need anyway: a play triangle,
    /// a small stack of hexes for the level list, an endless spiral, a question mark for help, and a
    /// gear for settings.
    /// </remarks>
    internal sealed class MainMenuView : MonoBehaviour
    {
        internal const string ContinueId = "continue";
        internal const string LevelsId = "levels";
        internal const string EndlessId = "endless";
        internal const string HelpId = "help";
        internal const string AtlasId = "atlas";
        internal const string SettingsId = "settings";

        /// <summary>The secondary row's height, as a fraction of the viewport.</summary>
        private const float SecondaryRowY = 0.36f;

        /// <summary>
        /// The widest centre-to-centre spacing the secondary row uses, in viewport fractions.
        /// </summary>
        /// <remarks>
        /// A maximum rather than a fixed value. Four buttons at this spacing exactly reproduce the
        /// hand-placed row; a fifth at the same spacing would put the outermost two at 0.04 and 0.96,
        /// half off the screen. The row closes up instead, so the atlas returning is a change to one
        /// boolean rather than to a layout.
        /// </remarks>
        private const float MaximumSecondarySpacing = 0.23f;

        /// <summary>
        /// The span the row may occupy, from the leftmost centre to the rightmost.
        /// </summary>
        /// <remarks>
        /// 0.155 to 0.845, which is where the four buttons sat when they were placed by hand.
        /// </remarks>
        private const float SecondarySpan = 0.69f;

        private const float SecondaryRadius = 0.38f;
        private const float SecondaryTouchRadius = 0.11f;

        private HexButton _continue;
        private HexButton _levels;
        private HexButton _endless;
        private HexButton _help;
        private HexButton _atlas;
        private Text.TextLabel _helpMark;
        private HexButton _settings;

        /// <summary>
        /// Whether the World Atlas is offered at all.
        /// </summary>
        /// <remarks>
        /// <para>
        /// True again. It was withheld from the closed test because nothing on screen said what Star
        /// Essence was, what a node cost, or what a relic did, so a player met a constellation of coloured
        /// hexagons and guessed. None of that was a fault in the model — it was that the game had no font
        /// to write a number or a sentence with.
        /// </para>
        /// <para>
        /// It now says all three, in <c>Text.AtlasWords</c>: a balance as a figure, a cost as a numeral on
        /// the node, and what a node gives and whether it can be bought as sentences under it. Essence
        /// banked while it was hidden is spendable, which was the promise made when it was withheld.
        /// </para>
        /// <para>
        /// Kept as a property rather than becoming a plain expression, because a flag that has been turned
        /// off once may need turning off again, and the argument for either state belongs in one place.
        /// </para>
        /// </remarks>
        internal static bool IsAtlasVisible => true;

        /// <summary>
        /// How many buttons the secondary row holds.
        /// </summary>
        /// <remarks>
        /// Levels, endless, help, settings — and the atlas when it returns, which is why the row's
        /// spacing is computed rather than placed.
        /// </remarks>
        internal static int SecondaryCount => IsAtlasVisible ? 5 : 4;

        /// <summary>
        /// Where the given secondary button sits horizontally, as a viewport fraction.
        /// </summary>
        /// <remarks>
        /// The row is centred and closes up rather than leaving a hole, so the buttons keep the density
        /// a thumb learned when one arrives or leaves. With four buttons this reproduces the
        /// hand-placed 0.155, 0.385, 0.615, 0.845 exactly.
        /// </remarks>
        internal static float SecondaryX(int index, int count)
        {
            var spacing = count > 1
                ? Mathf.Min(MaximumSecondarySpacing, SecondarySpan / (count - 1))
                : 0f;

            return 0.5f + (spacing * (index - ((count - 1) * 0.5f)));
        }

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

            var helpX = SecondaryX(index++, count);
            _help = CreateSecondary(camera, material, HelpId, helpX);

            // A question mark, drawn as a question mark. Every other glyph in this menu is a mesh
            // because there was no font to draw a character with; this one is a character, and
            // building it out of an arc, a stem, and a dot would be a worse drawing of the same thing.
            _helpMark = Text.TextLabel.Create(
                transform,
                camera,
                HelpId,
                new Vector2(helpX, SecondaryRowY),
                Text.LabelMetrics.HeadingHeightFraction,
                BoardPalette.MenuGlyph,
                TMPro.TextAlignmentOptions.Center,
                HexButton.LabelDepth);

            _helpMark.SetText("?");

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

            if (_help != null && _help.IsPressed(screenPosition))
            {
                return HelpId;
            }

            return _settings != null && _settings.IsPressed(screenPosition) ? SettingsId : null;
        }
    }
}
