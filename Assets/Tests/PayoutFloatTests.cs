using NUnit.Framework;
using Pathweaver.Game.Presentation;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// How a payout number rises and fades.
    /// </summary>
    /// <remarks>
    /// Whether the number is readable needs a person. Whether it is on screen long enough to read, and
    /// whether it ever disappears, do not — and a number that faded before it could be read would make
    /// the score curve *less* legible than showing nothing, which is the thing this is for.
    /// </remarks>
    public class PayoutFloatTests
    {
        [Test]
        public void It_starts_where_the_route_paid_and_fully_opaque()
        {
            var (rise, alpha) = PayoutFloat.Evaluate(0f);

            Assert.That(rise, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(alpha, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void It_is_gone_by_the_end()
        {
            // Otherwise the last frame leaves a number sitting on the board until the object is destroyed.
            Assert.That(PayoutFloat.Evaluate(1f).Alpha, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void It_stays_fully_readable_for_the_first_half()
        {
            // A number that begins fading immediately is one a player has to hurry to read, and the whole
            // point is that they read it.
            Assert.That(PayoutFloat.Evaluate(0.25f).Alpha, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(PayoutFloat.Evaluate(0.5f).Alpha, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void It_only_ever_fades()
        {
            var previous = 1.0001f;

            for (var phase = 0f; phase <= 1f; phase += 0.02f)
            {
                var alpha = PayoutFloat.Evaluate(phase).Alpha;
                Assert.That(alpha, Is.LessThanOrEqualTo(previous));
                previous = alpha;
            }
        }

        [Test]
        public void It_only_ever_rises()
        {
            // A number that drifted back down would read as falling into the board rather than leaving it.
            var previous = -0.0001f;

            for (var phase = 0f; phase <= 1f; phase += 0.02f)
            {
                var rise = PayoutFloat.Evaluate(phase).Rise;
                Assert.That(rise, Is.GreaterThanOrEqualTo(previous));
                previous = rise;
            }
        }

        [Test]
        public void Most_of_the_travel_happens_before_the_fade_begins()
        {
            // So the movement reads as the number leaving rather than drifting: it should be most of the
            // way up by the time it starts to disappear.
            var atHalfway = PayoutFloat.Evaluate(0.5f).Rise;

            Assert.That(atHalfway, Is.GreaterThan(PayoutFloat.RiseHeightFraction * 0.6f));
        }

        [Test]
        public void The_rise_is_short_enough_to_stay_near_the_hub()
        {
            // The rise separates the number from the cell it came from. A number that travelled far would
            // stop saying which route paid, which is the only reason it is drawn at the hub at all.
            Assert.That(PayoutFloat.RiseHeightFraction, Is.LessThan(0.1f));
        }

        [Test]
        public void Reduced_motion_holds_the_number_still_but_keeps_it()
        {
            // The number is information, so it is never removed — only the movement is, which is exactly
            // the kind of motion to drop first.
            for (var phase = 0f; phase <= 1f; phase += 0.1f)
            {
                Assert.That(PayoutFloat.EvaluateStill(phase).Rise, Is.EqualTo(0f));
                Assert.That(
                    PayoutFloat.EvaluateStill(phase).Alpha,
                    Is.EqualTo(PayoutFloat.Evaluate(phase).Alpha).Within(0.0001f));
            }
        }

        [Test]
        public void It_is_readable_but_gone_before_the_next_placement()
        {
            // Long enough for four digits, short enough not to still be there when the next tile lands.
            Assert.That(PayoutFloat.DurationSeconds, Is.InRange(0.7f, 1.5f));
        }
    }
}
