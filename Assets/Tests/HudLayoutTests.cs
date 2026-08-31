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
        public void A_settings_label_sits_between_its_own_control_and_the_next()
        {
            // Below its switch, and clear of the switch beneath it. The three controls are 0.18 and
            // 0.22 apart, so an offset that grew past half of the smaller gap would put a label nearer
            // the wrong control than the right one.
            var smallestGap = Mathf.Min(
                SettingsView.HapticsViewportY - SettingsView.ReduceMotionViewportY,
                SettingsView.ReduceMotionViewportY - SettingsView.ResetViewportY);

            Assert.That(SettingsView.LabelOffset, Is.GreaterThan(LabelMetrics.CaptionHeightFraction));
            Assert.That(SettingsView.LabelOffset, Is.LessThan(smallestGap * 0.5f));
        }

        [Test]
        public void The_reset_label_stays_on_screen_below_its_control()
        {
            Assert.That(SettingsView.ResetViewportY - SettingsView.LabelOffset, Is.GreaterThan(0.05f));
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
