using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation.Menus
{
    /// <summary>
    /// Two switches, and no more than the game actually has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Haptics and reduced motion. There is no audio setting because there is no audio: a toggle
    /// for something that does nothing tells a player the game is broken rather than unfinished.
    /// </para>
    /// <para>
    /// State is shown by fill rather than by a label. A filled hexagon reads as on and a hollow one
    /// as off, which needs no words and survives being looked at by someone who does not read the
    /// game's language — of which there currently is none.
    /// </para>
    /// </remarks>
    internal sealed class SettingsView : MonoBehaviour
    {
        internal const string HapticsId = "haptics";
        internal const string ReduceMotionId = "reduce-motion";
        internal const string BackId = "back";

        private HexButton _haptics;
        private HexButton _reduceMotion;
        private HexButton _back;

        internal void Build(Camera camera, Material material)
        {
            _haptics = HexButton.Create(
                transform, HapticsId, camera, material,
                new Vector2(0.5f, 0.62f), 0.55f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.16f);

            _reduceMotion = HexButton.Create(
                transform, ReduceMotionId, camera, material,
                new Vector2(0.5f, 0.42f), 0.55f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.16f);

            _back = HexButton.Create(
                transform, BackId, camera, material,
                new Vector2(0.14f, 0.09f), 0.4f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.12f);
            MenuGlyphs.AddBack(_back);

            Refresh();
        }

        /// <summary>
        /// Redraws both switches from the stored settings.
        /// </summary>
        internal void Refresh()
        {
            DrawSwitch(_haptics, GameSettings.HapticsEnabled, MenuGlyphs.Haptics());
            DrawSwitch(_reduceMotion, GameSettings.ReduceMotion, MenuGlyphs.Motion());
        }

        internal string ButtonAt(Vector2 screenPosition)
        {
            if (_haptics != null && _haptics.IsPressed(screenPosition))
            {
                return HapticsId;
            }

            if (_reduceMotion != null && _reduceMotion.IsPressed(screenPosition))
            {
                return ReduceMotionId;
            }

            return _back != null && _back.IsPressed(screenPosition) ? BackId : null;
        }

        private static void DrawSwitch(HexButton button, bool isOn, (Mesh Mesh, Vector3 Offset)[] glyph)
        {
            if (button == null)
            {
                return;
            }

            button.ClearGlyphs();
            button.SetColour(isOn ? BoardPalette.SwitchOn : BoardPalette.SwitchOff);

            foreach (var (mesh, offset) in glyph)
            {
                button.AddGlyph(mesh, isOn ? BoardPalette.MenuGlyph : BoardPalette.SwitchOffGlyph, offset);
            }
        }
    }
}
