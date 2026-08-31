using System.Linq;
using NUnit.Framework;
using Pathweaver.Game.Presentation.Menus;
using Pathweaver.Game.Presentation.Text;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// The help screen's content and paging.
    /// </summary>
    /// <remarks>
    /// Whether the words explain anything needs a person, and <c>TextPreview.CaptureHelp</c> renders
    /// each page at phone size for exactly that. What does not need a person is whether a paragraph is
    /// short enough to fit its slot — the wrap depends on the player's screen, and the first draft of
    /// the last page ran to five lines and climbed over its own heading.
    /// </remarks>
    public class HelpViewTests
    {
        [Test]
        public void There_is_a_page_for_each_thing_a_player_has_to_work_out_alone()
        {
            // Springs and hubs, placement, the length curve, and the two token columns. Fewer than four
            // means one of those went back to being learned by trial.
            Assert.That(HelpView.PageCount, Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void Every_page_says_something()
        {
            Assert.That(HelpView.AllHeadings.Any(heading => string.IsNullOrWhiteSpace(heading)), Is.False);
            Assert.That(
                HelpView.AllParagraphs.Any(paragraph => string.IsNullOrWhiteSpace(paragraph)),
                Is.False);
        }

        [Test]
        public void No_paragraph_is_long_enough_to_reach_the_one_below_it()
        {
            foreach (var paragraph in HelpView.AllParagraphs)
            {
                Assert.That(
                    paragraph.Length,
                    Is.LessThanOrEqualTo(HelpView.LongestParagraph),
                    $"This paragraph wraps past its slot: \"{paragraph}\"");
            }
        }

        [Test]
        public void A_paragraph_slot_is_taller_than_the_paragraph_it_holds()
        {
            // Four wrapped lines at body size, against the spacing between slots. If a size changes
            // and this stops holding, LongestParagraph is the number that has to come down.
            const int longestWrappedLines = 4;

            Assert.That(
                longestWrappedLines * LabelMetrics.BodyHeightFraction,
                Is.LessThan(HelpView.LineSpacing));
        }

        [Test]
        public void Every_character_the_help_uses_is_in_the_font_atlas()
        {
            // The atlas is static and stops at Latin-1 plus seven marks. A character outside it renders
            // as nothing, which on this screen would be a sentence with a hole in it — and the em dash
            // that put those seven marks in the atlas is used on the last page.
            var allowed = Enumerable.Range(0x20, 0x7F - 0x20)
                .Concat(Enumerable.Range(0xA0, 0x100 - 0xA0))
                .Select(code => (char)code)
                .Concat("–—‘’“”…")
                .ToHashSet();

            foreach (var text in HelpView.AllParagraphs.Concat(HelpView.AllHeadings))
            {
                foreach (var character in text)
                {
                    Assert.That(
                        allowed.Contains(character),
                        Is.True,
                        $"U+{(int)character:X4} is not in the font atlas, in: \"{text}\"");
                }
            }
        }

        [Test]
        public void The_pages_are_arranged_top_to_bottom_and_stay_on_screen()
        {
            var lastSlot = HelpView.FirstLineViewportY - ((4 - 1) * HelpView.LineSpacing);

            Assert.That(HelpView.FirstLineViewportY, Is.LessThan(HelpView.HeadingViewportY));
            Assert.That(lastSlot, Is.GreaterThan(0.2f), "A fourth paragraph would reach the buttons.");
        }
    }
}
