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
    /// State is shown by fill as well as named. A filled hexagon reads as on and a hollow one as
    /// off, which survives being looked at by someone who does not read the game's language; the
    /// label beside it says which setting it is, which the fill never could.
    /// </para>
    /// <para>
    /// Laid out as rows — control on the left, label to its right — rather than as a centred column with
    /// captions beneath. The captions beneath were tried and did not fit: a switch of radius 0.55 covers
    /// 0.086 of screen height either side of its centre, and the rows are only 0.18 apart, so any offset
    /// large enough to clear the switch put the label nearer the switch below it. Rows use the width,
    /// which a portrait phone has spare and no vertical layout can borrow.
    /// </para>
    /// <para>
    /// The reset control is the odd one out, and it is deliberately below the two switches and
    /// smaller than them: it is not a preference, and nothing about it should invite a stray thumb.
    /// It arms on the first tap and acts on the second, the same mode-then-act shape the Pivot Token
    /// uses for the other tap in this game that spends something scarce. Its label is now the
    /// confirmation question that used to be impossible to write: armed, it reads as a warning, in
    /// the destructive colour and in words, because neither on its own should have to carry it.
    /// </para>
    /// </remarks>
    internal sealed class SettingsView : MonoBehaviour
    {
        internal const string HapticsId = "haptics";
        internal const string ReduceMotionId = "reduce-motion";
        internal const string ResetId = "reset";
        internal const string BackId = "back";

        /// <summary>Where each row sits, and therefore where its label goes.</summary>
        internal const float HapticsViewportY = 0.66f;

        internal const float ReduceMotionViewportY = 0.48f;

        internal const float ResetViewportY = 0.26f;

        /// <summary>Where a row's control sits horizontally.</summary>
        internal const float ControlViewportX = 0.26f;

        /// <summary>
        /// Where a label's text begins, as a viewport fraction.
        /// </summary>
        /// <remarks>
        /// Clear of the widest control's right edge. A switch of radius 0.55 centred at 0.26 reaches
        /// about 0.45 on a portrait phone — see <c>MenuCamera.ViewportHalfWidth</c>, which is the
        /// conversion that makes that checkable rather than guessed at.
        /// </remarks>
        internal const float LabelLeftEdge = 0.5f;

        /// <summary>How much width a label may use before wrapping.</summary>
        internal const float LabelWidth = 0.46f;

        internal const float SwitchRadius = 0.55f;

        internal const float ResetRadius = 0.42f;

        private HexButton _haptics;
        private HexButton _reduceMotion;
        private HexButton _reset;
        private HexButton _back;
        private Text.TextLabel _hapticsLabel;
        private Text.TextLabel _reduceMotionLabel;
        private Text.TextLabel _resetLabel;

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
                new Vector2(ControlViewportX, HapticsViewportY), SwitchRadius,
                BoardPalette.MenuSecondary, touchRadiusFraction: 0.16f);

            _hapticsLabel = Label(camera, HapticsId, HapticsViewportY, "Vibration");

            _reduceMotion = HexButton.Create(
                transform, ReduceMotionId, camera, material,
                new Vector2(ControlViewportX, ReduceMotionViewportY), SwitchRadius,
                BoardPalette.MenuSecondary, touchRadiusFraction: 0.16f);

            _reduceMotionLabel = Label(camera, ReduceMotionId, ReduceMotionViewportY, "Reduced motion");

            _reset = HexButton.Create(
                transform, ResetId, camera, material,
                new Vector2(ControlViewportX, ResetViewportY), ResetRadius,
                BoardPalette.MenuSecondary, touchRadiusFraction: 0.12f);

            _resetLabel = Label(camera, ResetId, ResetViewportY, string.Empty);

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

            // The label follows the fill, so a switch that is off is dimmer in both places rather
            // than half-lit in one.
            _hapticsLabel?.SetColour(
                GameSettings.HapticsEnabled ? BoardPalette.TextPrimary : BoardPalette.TextSecondary);
            _reduceMotionLabel?.SetColour(
                GameSettings.ReduceMotion ? BoardPalette.TextPrimary : BoardPalette.TextSecondary);

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

        private Text.TextLabel Label(Camera camera, string id, float controlViewportY, string content)
        {
            // Anchored at the middle of its own box so that left-aligning the text puts its first
            // character on LabelLeftEdge, and at the button's own depth so the control cannot cover it.
            var label = Text.TextLabel.Create(
                transform,
                camera,
                id,
                new Vector2(LabelLeftEdge + (LabelWidth * 0.5f), controlViewportY),
                Text.LabelMetrics.BodyHeightFraction,
                BoardPalette.TextSecondary,
                TMPro.TextAlignmentOptions.Left,
                HexButton.LabelDepth);

            label.SetWrapWidth(LabelWidth);
            label.SetText(content);
            return label;
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

            // The question the game could not ask before. Armed, it says what the next tap does; the
            // colour agrees with it rather than replacing it.
            _resetLabel?.SetText(IsResetArmed ? "Tap again to erase everything" : "Erase all progress");
            _resetLabel?.SetColour(
                IsResetArmed ? BoardPalette.Destructive : BoardPalette.TextSecondary);
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
