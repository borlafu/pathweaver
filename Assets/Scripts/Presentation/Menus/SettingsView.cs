using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation.Menus
{
    /// <summary>
    /// Two switches and one thing that cannot be undone.
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
    /// <para>
    /// The reset control is the odd one out, and it is deliberately below the two switches and
    /// smaller than them: it is not a preference, and nothing about it should invite a stray thumb.
    /// It arms on the first tap and acts on the second, which is the only confirmation available
    /// without a font to write a question in — and the same mode-then-act shape the Pivot Token
    /// already uses for the other tap in this game that spends something scarce.
    /// </para>
    /// </remarks>
    internal sealed class SettingsView : MonoBehaviour
    {
        internal const string HapticsId = "haptics";
        internal const string ReduceMotionId = "reduce-motion";
        internal const string ResetId = "reset";
        internal const string BackId = "back";

        private HexButton _haptics;
        private HexButton _reduceMotion;
        private HexButton _reset;
        private HexButton _back;

        /// <summary>
        /// Whether the next tap on the reset control would actually erase everything.
        /// </summary>
        /// <remarks>
        /// Held by the view rather than the flow because it is a property of what is on screen: it
        /// must not survive leaving the settings screen, and the drawing and the state have to agree.
        /// </remarks>
        internal bool IsResetArmed { get; private set; }

        internal void Build(Camera camera, Material material)
        {
            _haptics = HexButton.Create(
                transform, HapticsId, camera, material,
                new Vector2(0.5f, 0.66f), 0.55f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.16f);

            _reduceMotion = HexButton.Create(
                transform, ReduceMotionId, camera, material,
                new Vector2(0.5f, 0.48f), 0.55f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.16f);

            _reset = HexButton.Create(
                transform, ResetId, camera, material,
                new Vector2(0.5f, 0.26f), 0.42f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.12f);

            _back = HexButton.Create(
                transform, BackId, camera, material,
                new Vector2(0.14f, 0.09f), 0.4f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.12f);
            MenuGlyphs.AddBack(_back);

            Refresh();
        }

        /// <summary>
        /// Redraws the switches from the stored settings, and the reset control from its arming.
        /// </summary>
        internal void Refresh()
        {
            DrawSwitch(_haptics, GameSettings.HapticsEnabled, MenuGlyphs.Haptics());
            DrawSwitch(_reduceMotion, GameSettings.ReduceMotion, MenuGlyphs.Motion());
            DrawReset();
        }

        /// <summary>
        /// Arms or disarms the reset, redrawing it.
        /// </summary>
        /// <remarks>
        /// Disarmed whenever the screen is opened or left, so an arming cannot wait around for a tap
        /// the player makes on their next visit believing they are somewhere else.
        /// </remarks>
        internal void SetResetArmed(bool isArmed)
        {
            IsResetArmed = isArmed;
            DrawReset();
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

            if (_reset != null && _reset.IsPressed(screenPosition))
            {
                return ResetId;
            }

            return _back != null && _back.IsPressed(screenPosition) ? BackId : null;
        }

        private void DrawReset()
        {
            if (_reset == null)
            {
                return;
            }

            _reset.ClearGlyphs();
            _reset.SetColour(IsResetArmed ? BoardPalette.Destructive : BoardPalette.SwitchOff);
            MenuGlyphs.AddErase(_reset, IsResetArmed);
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
