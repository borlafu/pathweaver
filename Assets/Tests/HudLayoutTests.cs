using NUnit.Framework;
using Pathweaver.Game.Presentation;
using Pathweaver.Game.Presentation.Menus;
using Pathweaver.Game.Presentation.Text;
using UnityEngine;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// Where things sit on screen relative to each other.
    /// </summary>
    /// <remarks>
    /// Every control in the game is anchored by viewport fraction, and every collision between two of
    /// them has so far been found on a device: a level button landing on the back button, the board
    /// framed for a square and cut off on a phone, a bar running under the buttons at either end. The
    /// anchors are numbers, so the collisions are arithmetic, so they belong here.
    /// </remarks>
    public class HudLayoutTests
    {
        /// <summary>The aspect of the phone the game is tested on: 1080 by 2376.</summary>
        private const float PhoneAspect = 1080f / 2376f;

        /// <summary>Where the restart button sits, from <c>RestartButtonView</c>.</summary>
        private static readonly Vector2 Restart = new Vector2(0.12f, 0.94f);

        /// <summary>Where the pause button sits, from <c>GameFlow</c>.</summary>
        private static readonly Vector2 Pause = new Vector2(0.88f, 0.94f);

        /// <summary>How much of the width the progress bar spans, from <c>ProgressBarView</c>.</summary>
        private const float BarWidthFraction = 0.52f;

        /// <summary>The share of screen height the tile tray occupies, from <c>BoardCameraFitter</c>.</summary>
        private const float TrayHeightFraction = 0.24f;

        [Test]
        public void The_score_sits_below_the_bar_it_belongs_to()
        {
            Assert.That(ProgressBarView.ScoreViewportY, Is.LessThan(ProgressBarView.ViewportY));
        }

        [Test]
        public void The_score_clears_the_bar_rather_than_touching_it()
        {
            // A body line is about 0.022 of screen height, and the bar's own track has thickness. Less
            // than a full line of separation and the two read as one smudged row.
            var separation = ProgressBarView.ViewportY - ProgressBarView.ScoreViewportY;

            Assert.That(separation, Is.GreaterThan(LabelMetrics.BodyHeightFraction));
        }

        [Test]
        public void The_score_stays_clear_of_the_buttons_in_the_top_corners()
        {
            // Restart and pause sit at the bar's own height. The bar itself is narrow enough to run
            // between them; the score is centred under it, so it must be too.
            var barLeft = 0.5f - (BarWidthFraction * 0.5f);
            var barRight = 0.5f + (BarWidthFraction * 0.5f);

            Assert.That(Restart.x, Is.LessThan(barLeft));
            Assert.That(Pause.x, Is.GreaterThan(barRight));
        }

        [Test]
        public void The_score_is_above_the_board_and_the_tray()
        {
            // Not a collision test so much as a statement of where the top strip ends. Everything the
            // player touches is in the bottom quarter; everything that reports is at the top.
            Assert.That(ProgressBarView.ScoreViewportY, Is.GreaterThan(1f - TrayHeightFraction));
        }

        [Test]
        public void The_level_name_sits_above_the_resume_button()
        {
            // Resume is at 0.58 with a radius of 0.6 world units. The title is a heading, so it needs
            // its own line's worth of room above whatever is beneath it.
            const float resumeViewportY = 0.58f;

            Assert.That(
                PauseView.TitleViewportY - resumeViewportY,
                Is.GreaterThan(LabelMetrics.HeadingHeightFraction));
        }

        [Test]
        public void The_level_name_is_on_screen()
        {
            Assert.That(PauseView.TitleViewportY, Is.InRange(0.05f, 0.95f));
        }

        [Test]
        public void A_settings_label_begins_clear_of_the_control_it_names()
        {
            // The test that was missing. The previous one compared the label's offset against the height
            // of a line of text and against the gap between rows, and passed — while every label sat
            // inside the switch it named, because a switch of radius 0.55 covers far more of the screen
            // than a caption does and nothing here had converted the two into the same units.
            var switchRightEdge = SettingsView.ControlViewportX
                                  + MenuCamera.ViewportHalfWidth(SettingsView.SwitchRadius, PhoneAspect);

            Assert.That(
                SettingsView.LabelLeftEdge,
                Is.GreaterThan(switchRightEdge),
                "A settings label starts inside the control it names.");
        }

        [Test]
        public void A_settings_label_fits_the_width_left_over()
        {
            Assert.That(
                SettingsView.LabelLeftEdge + SettingsView.LabelWidth,
                Is.LessThanOrEqualTo(1f),
                "A settings label runs off the right of the screen.");
        }

        [Test]
        public void A_settings_control_stays_on_screen_at_its_own_width()
        {
            // The same conversion applied to the left edge. A hexagon of radius 0.55 reaches nearly two
            // fifths of the way across a portrait phone, which is not obvious from the number 0.55.
            var halfWidth = MenuCamera.ViewportHalfWidth(SettingsView.SwitchRadius, PhoneAspect);

            Assert.That(SettingsView.ControlViewportX - halfWidth, Is.GreaterThan(0f));
        }

        [Test]
        public void A_label_on_a_button_is_drawn_in_front_of_it()
        {
            // The bug that hid the help screen's question mark and half of every settings label on the
            // device while each rendered perfectly in isolation. The camera looks along +Z from negative
            // Z, so nearer the viewer is a smaller number.
            Assert.That(HexButton.LabelDepth, Is.LessThan(HexButton.FaceDepth));
        }

        [Test]
        public void A_row_of_pips_is_centred_on_its_button()
        {
            // It grew inward from the button's centre at first, which left the block off to one side of
            // the control it belongs to and made the two columns mirror images rather than the same thing
            // twice. Checked on the outermost pair, since that is what a centre means.
            var pips = new GameObject("pips").AddComponent<TokenPipsView>();

            try
            {
                var first = pips.PipPosition(0).x;
                var last = pips.PipPosition(TokenPipsView.PipsPerRow - 1).x;

                Assert.That(first + last, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(first, Is.LessThan(0f), "A centred row should start left of its button.");
            }
            finally
            {
                Object.DestroyImmediate(pips.gameObject);
            }
        }

        [Test]
        public void A_row_of_pips_is_about_as_wide_as_the_button_it_sits_on()
        {
            // A row much wider than its button would stop reading as belonging to it. The button's radius
            // is 0.34 and a pointy-top hexagon is radius * sqrt(3) across.
            const float buttonRadius = 0.34f;
            var buttonWidth = buttonRadius * Mathf.Sqrt(3f);

            var pips = new GameObject("pips").AddComponent<TokenPipsView>();

            try
            {
                var span = pips.PipPosition(TokenPipsView.PipsPerRow - 1).x - pips.PipPosition(0).x;

                Assert.That(span, Is.LessThan(buttonWidth * 1.3f));
                Assert.That(span, Is.GreaterThan(buttonWidth * 0.6f));
            }
            finally
            {
                Object.DestroyImmediate(pips.gameObject);
            }
        }

        [Test]
        public void A_pip_block_is_three_across_and_two_deep_at_most()
        {
            // Three per row, so the base allowance of three is one row and the five a full set of relics
            // allows is two. A single column of five reached far enough up the screen to sit over the board.
            Assert.That(TokenPipsView.PipsPerRow, Is.EqualTo(3));
            Assert.That(
                Pathweaver.Core.Rules.TokenRules.MaximumCapacity,
                Is.LessThanOrEqualTo(TokenPipsView.PipsPerRow * 2),
                "The ceiling no longer fits two rows of three.");
        }

        [Test]
        public void The_next_tile_sits_between_the_tray_and_the_skip_button()
        {
            // Right of the tray at 0.5 and clear of the skip button at 0.86, on the side the skip button
            // is — the two are about the same thing, which is what the bag hands over next.
            const float trayX = 0.5f;
            const float skipX = 0.86f;

            Assert.That(NextTileView.ViewportX, Is.GreaterThan(trayX));
            Assert.That(NextTileView.ViewportX, Is.LessThan(skipX));
        }

        [Test]
        public void The_next_tile_reads_as_secondary_to_the_one_in_the_tray()
        {
            // Small enough to be clearly not the tile being placed, large enough that its shape is
            // unambiguous — which is the entire content of the thing.
            Assert.That(NextTileView.RelativeSize, Is.InRange(0.4f, 0.8f));
        }

        [Test]
        public void A_control_keeps_its_size_on_screen_whatever_the_camera_shows()
        {
            // At the menu camera nothing changes, which is why every menu is unaffected. The pause screen
            // keeps the board's framing on purpose, and over a large board that is more than twice the
            // menu size — where its controls were drawn less than half their intended size.
            Assert.That(
                HexButton.ScaleFor(MenuCamera.OrthographicSize), Is.EqualTo(1f).Within(0.0001f));

            Assert.That(
                HexButton.ScaleFor(MenuCamera.OrthographicSize * 2f), Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void A_control_grows_with_the_world_the_camera_shows()
        {
            // The direction matters and is easy to invert. Showing more world means a world-sized object
            // covers less screen, so it has to be made larger, not smaller.
            Assert.That(
                HexButton.ScaleFor(8f), Is.GreaterThan(HexButton.ScaleFor(4f)));
        }

        [Test]
        public void The_held_tile_is_in_front_of_the_backdrop_that_hides_the_board()
        {
            // It rested at zero and jumped to -0.2 only while being dragged, which was invisible until an
            // opaque band appeared at -0.1 between the two: the tile in the tray disappeared and came
            // back the instant a thumb touched it. Reported from the device.
            Assert.That(HeldTileView.Depth, Is.LessThan(HudBackdrop.Depth));
        }

        [Test]
        public void The_backdrop_is_the_boundary_between_the_board_and_the_interface()
        {
            // Board content behind, interface in front. The flow pulse is board content and should be
            // covered by the bands; the labels are interface and should not.
            Assert.That(
                FlowPulseAnimator.Depth,
                Is.GreaterThan(HudBackdrop.Depth),
                "The flow pulse would draw over the tray.");

            Assert.That(
                TextLabel.DefaultDepth,
                Is.LessThan(HudBackdrop.Depth),
                "A label over the board would be hidden by the backdrop.");
        }

        [Test]
        public void A_labels_default_depth_is_not_enough_for_a_button()
        {
            // Why every button label passes a depth explicitly. The default is chosen for labels over the
            // board, where it sits in front of the tray at -0.2 and the pip columns at -0.4 — and it is
            // a long way behind a button face at -1.5. Both are right for their own case, and the trap is
            // that the wrong one renders perfectly in isolation.
            Assert.That(TextLabel.DefaultDepth, Is.GreaterThan(HexButton.FaceDepth));
        }

        [Test]
        public void A_label_on_a_button_is_in_front_of_the_glyphs_too()
        {
            // Glyphs stack forward from the face in steps of 0.005, and the settings gear uses several.
            // A label only just in front of the face would end up behind them.
            const float deepestGlyphStack = 0.05f;

            Assert.That(
                HexButton.LabelDepth,
                Is.LessThan(HexButton.FaceDepth - deepestGlyphStack));
        }

        [Test]
        public void A_level_number_clears_its_hexagon_without_reaching_the_row_below()
        {
            // Twenty levels give a four-column grid of five rows in a band 0.60 tall, so about 0.15
            // between rows. The number has to be further from its own button than a line of text is
            // tall, and nearer to it than to the button beneath.
            const float rowStepAtTwentyLevels = 0.15f;

            Assert.That(
                LevelSelectView.NumberOffset, Is.GreaterThan(LabelMetrics.CaptionHeightFraction));
            Assert.That(
                LevelSelectView.NumberOffset, Is.LessThan(rowStepAtTwentyLevels * 0.5f));
        }
    }
}
