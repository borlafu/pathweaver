using System.Linq;
using NUnit.Framework;
using Pathweaver.Game.App;
using Pathweaver.Game.Presentation;
using Pathweaver.Game.Presentation.Text;
using UnityEngine;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// The three first-run hints: what they say, how long they last, and that they never come back.
    /// </summary>
    /// <remarks>
    /// Whether the sentences teach anything needs a person. That a hint is shown once and not twice does
    /// not — and a hint that reappeared would be worse than none, because a player would stop reading
    /// the ones that matter.
    /// </remarks>
    public class CoachMarkTests
    {
        [SetUp]
        [TearDown]
        public void ForgetEverything()
        {
            // These live in PlayerPrefs, which is shared with the Editor, so each test starts and leaves
            // it clean rather than depending on whatever ran before.
            CoachMarks.Forget();
        }

        [Test]
        public void A_hint_is_unseen_until_it_is_shown()
        {
            Assert.That(CoachMarks.HasSeen(CoachMark.Place), Is.False);

            CoachMarks.MarkSeen(CoachMark.Place);

            Assert.That(CoachMarks.HasSeen(CoachMark.Place), Is.True);
        }

        [Test]
        public void Seeing_one_hint_does_not_spend_the_others()
        {
            CoachMarks.MarkSeen(CoachMark.Place);

            Assert.That(CoachMarks.HasSeen(CoachMark.Turn), Is.False);
            Assert.That(CoachMarks.HasSeen(CoachMark.Join), Is.False);
        }

        [Test]
        public void Forgetting_brings_every_hint_back()
        {
            // What a progress reset does. Erasing progress and then withholding the tutorial would leave
            // a player who asked for a clean slate with a board and no explanation.
            foreach (var mark in CoachMarks.All)
            {
                CoachMarks.MarkSeen(mark);
            }

            CoachMarks.Forget();

            Assert.That(CoachMarks.All.Any(CoachMarks.HasSeen), Is.False);
        }

        [Test]
        public void Every_hint_says_something_and_nothing_else_does()
        {
            foreach (var mark in CoachMarks.All)
            {
                Assert.That(
                    string.IsNullOrWhiteSpace(CoachMarks.TextFor(mark)),
                    Is.False,
                    $"{mark} has nothing to say.");
            }

            Assert.That(CoachMarks.TextFor(CoachMark.None), Is.Empty);
        }

        [Test]
        public void Nothing_is_owed_to_the_absence_of_a_hint()
        {
            // None is the resting state, so treating it as unseen would make the view try to show it.
            Assert.That(CoachMarks.HasSeen(CoachMark.None), Is.True);
        }

        [Test]
        public void A_hint_is_short_enough_to_read_while_a_thumb_is_moving()
        {
            // One line at the wrap width, or two at most. A paragraph over the board is not a hint.
            foreach (var mark in CoachMarks.All)
            {
                Assert.That(
                    CoachMarks.TextFor(mark).Length,
                    Is.LessThanOrEqualTo(60),
                    $"{mark} is too long to be read in passing.");
            }
        }

        [Test]
        public void Every_character_a_hint_uses_is_in_the_font_atlas()
        {
            // The atlas is static and stops at Latin-1 plus seven marks. Anything outside it renders as
            // nothing, which on a one-line hint would be a sentence with a hole in it.
            var allowed = Enumerable.Range(0x20, 0x7F - 0x20)
                .Concat(Enumerable.Range(0xA0, 0x100 - 0xA0))
                .Select(code => (char)code)
                .Concat("–—‘’“”…")
                .ToHashSet();

            foreach (var mark in CoachMarks.All)
            {
                foreach (var character in CoachMarks.TextFor(mark))
                {
                    Assert.That(
                        allowed.Contains(character),
                        Is.True,
                        $"U+{(int)character:X4} is not in the font atlas, in: \"{CoachMarks.TextFor(mark)}\"");
                }
            }
        }

        [Test]
        public void It_appears_and_leaves_at_nothing()
        {
            Assert.That(CoachMarkFade.AlphaAt(0f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(CoachMarkFade.AlphaAt(1f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void It_holds_fully_readable_through_the_middle()
        {
            // A hint that fades continuously is one a player reads while it is disappearing. Unlike a
            // payout, which is a reward, this is a sentence someone is not expecting.
            Assert.That(CoachMarkFade.AlphaAt(0.3f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(CoachMarkFade.AlphaAt(0.5f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(CoachMarkFade.AlphaAt(0.7f), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void It_never_goes_below_nothing_or_above_full()
        {
            for (var phase = -0.5f; phase <= 1.5f; phase += 0.02f)
            {
                Assert.That(CoachMarkFade.AlphaAt(phase), Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void Reduced_motion_keeps_the_hint()
        {
            // A fade is not motion in the sense that setting is about — nothing travels. Removing the
            // hint would remove information, which is never the right half to drop.
            for (var phase = 0f; phase <= 1f; phase += 0.1f)
            {
                Assert.That(
                    CoachMarkFade.AlphaAtStill(phase),
                    Is.EqualTo(CoachMarkFade.AlphaAt(phase)).Within(0.0001f));
            }
        }

        [Test]
        public void It_lasts_long_enough_to_read_twice_and_no_longer()
        {
            Assert.That(CoachMarkFade.DurationSeconds, Is.InRange(3.5f, 8f));
        }

        [Test]
        public void A_hint_sits_above_the_drawer_and_below_the_boards_middle()
        {
            // Near the tray it is usually talking about, without covering the cells a player is about to
            // look at.
            Assert.That(CoachMarkView.ViewportY, Is.GreaterThan(BoardFraming.TrayHeightFraction));
            Assert.That(CoachMarkView.ViewportY, Is.LessThan(0.5f));
        }

        [Test]
        public void A_hint_fits_the_width_it_is_given()
        {
            Assert.That(CoachMarkView.WrapWidthFraction, Is.InRange(0.5f, 0.95f));
        }
    }
}
