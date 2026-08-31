using NUnit.Framework;
using Pathweaver.Game.Presentation.Text;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// How large text comes out, which is the one thing about a label that can make it useless.
    /// </summary>
    /// <remarks>
    /// Judging legibility needs a person, and <c>TextPreview.Capture</c> renders a sheet for exactly
    /// that. What does not need a person is whether the arithmetic behaves: that a size is a fraction
    /// of the screen and not of the board, and that the three named sizes stay ordered.
    /// </remarks>
    public class LabelMetricsTests
    {
        /// <summary>The board camera on a small level, roughly.</summary>
        private const float SmallBoardSize = 3f;

        /// <summary>The board camera zoomed out for a large one.</summary>
        private const float LargeBoardSize = 9f;

        [Test]
        public void Text_keeps_the_same_share_of_the_screen_on_any_board()
        {
            // The point of sizing against the screen. Every other control is a fixed world size, so it
            // shrinks as the camera pulls back to fit a bigger board; a pip that shrinks is still a
            // pip, but text that shrinks is unreadable.
            var small = LabelMetrics.WorldHeight(SmallBoardSize, LabelMetrics.BodyHeightFraction);
            var large = LabelMetrics.WorldHeight(LargeBoardSize, LabelMetrics.BodyHeightFraction);

            Assert.That(
                small / (SmallBoardSize * 2f),
                Is.EqualTo(large / (LargeBoardSize * 2f)).Within(0.0001f));
        }

        [Test]
        public void A_larger_board_needs_larger_world_text_to_look_the_same()
        {
            Assert.That(
                LabelMetrics.FontSize(LargeBoardSize, LabelMetrics.BodyHeightFraction),
                Is.GreaterThan(LabelMetrics.FontSize(SmallBoardSize, LabelMetrics.BodyHeightFraction)));
        }

        [Test]
        public void An_orthographic_size_is_a_half_height()
        {
            // The doubling. Forgetting it is the mistake BoardCameraFitter's remarks were written to
            // remember, and it would make every label exactly half the intended size.
            Assert.That(LabelMetrics.WorldHeight(5f, 0.1f), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void A_font_size_is_a_world_height_in_tenths()
        {
            Assert.That(
                LabelMetrics.FontSize(5f, 0.1f),
                Is.EqualTo(LabelMetrics.PointsPerWorldUnit).Within(0.0001f));
        }

        [Test]
        public void The_named_sizes_are_ordered()
        {
            Assert.That(
                LabelMetrics.HeadingHeightFraction, Is.GreaterThan(LabelMetrics.BodyHeightFraction));
            Assert.That(
                LabelMetrics.BodyHeightFraction, Is.GreaterThan(LabelMetrics.CaptionHeightFraction));
        }

        [Test]
        public void Every_named_size_clears_the_legible_minimum()
        {
            // Including the caption, which is the one most likely to be nudged down to make a long
            // level name fit. The fix for text that does not fit is fewer words.
            Assert.That(
                LabelMetrics.CaptionHeightFraction,
                Is.GreaterThanOrEqualTo(LabelMetrics.MinimumHeightFraction));
        }

        [Test]
        public void The_minimum_is_about_fourteen_scaled_pixels_on_the_preview_phone()
        {
            // 2376 pixels tall, which is the phone the previews are rendered at and the one the game
            // is tested on. Android treats roughly 14sp as the floor for body text; below that this
            // stops being a game a person can read on a bus.
            const int previewPhoneHeightPixels = 2376;

            var pixels = LabelMetrics.MinimumHeightFraction * previewPhoneHeightPixels;

            Assert.That(pixels, Is.InRange(30f, 50f));
        }
    }
}
