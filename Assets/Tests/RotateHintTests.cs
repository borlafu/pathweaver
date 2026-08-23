using NUnit.Framework;
using Pathweaver.Game.Presentation;
using UnityEngine;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// The rotation hint's motion. Whether it communicates anything needs a person, but
    /// whether it leaves the tile straight does not.
    /// </summary>
    public class RotateHintTests
    {
        [Test]
        public void The_shake_starts_at_rest()
        {
            Assert.That(RotateHint.AngleAt(0f), Is.EqualTo(0f));
        }

        [Test]
        public void The_shake_ends_exactly_at_rest()
        {
            // The important one. A shake that does not settle at zero leaves the tile
            // visibly crooked, which reads as a rendering fault rather than a hint.
            Assert.That(RotateHint.AngleAt(RotateHint.DurationSeconds), Is.EqualTo(0f));
            Assert.That(RotateHint.AngleAt(RotateHint.DurationSeconds + 1f), Is.EqualTo(0f));
        }

        [Test]
        public void A_time_before_the_start_is_at_rest()
        {
            Assert.That(RotateHint.AngleAt(-0.1f), Is.EqualTo(0f));
        }

        [Test]
        public void The_tile_actually_moves_during_the_shake()
        {
            var largest = 0f;

            for (var elapsed = 0f; elapsed < RotateHint.DurationSeconds; elapsed += 0.01f)
            {
                largest = Mathf.Max(largest, Mathf.Abs(RotateHint.AngleAt(elapsed)));
            }

            Assert.That(largest, Is.GreaterThan(RotateHint.AmplitudeDegrees * 0.5f));
        }

        [Test]
        public void The_shake_twists_both_ways()
        {
            // A twist in one direction only reads as the tile drifting, not as rotating.
            var sawPositive = false;
            var sawNegative = false;

            for (var elapsed = 0f; elapsed < RotateHint.DurationSeconds; elapsed += 0.01f)
            {
                var angle = RotateHint.AngleAt(elapsed);
                sawPositive |= angle > 1f;
                sawNegative |= angle < -1f;
            }

            Assert.That(sawPositive, Is.True, "Expected a twist one way.");
            Assert.That(sawNegative, Is.True, "Expected a twist the other way.");
        }

        [Test]
        public void The_shake_decays_rather_than_stopping_abruptly()
        {
            // Peak motion belongs early; the tail should be settling.
            var early = Mathf.Abs(RotateHint.AngleAt(RotateHint.DurationSeconds * 0.15f));
            var late = Mathf.Abs(RotateHint.AngleAt(RotateHint.DurationSeconds * 0.9f));

            Assert.That(early, Is.GreaterThan(late));
        }

        [Test]
        public void The_shake_never_exceeds_its_amplitude()
        {
            for (var elapsed = 0f; elapsed < RotateHint.DurationSeconds; elapsed += 0.005f)
            {
                Assert.That(
                    Mathf.Abs(RotateHint.AngleAt(elapsed)),
                    Is.LessThanOrEqualTo(RotateHint.AmplitudeDegrees + 0.001f));
            }
        }

        [Test]
        public void The_wait_between_shakes_is_a_few_seconds()
        {
            // Long enough not to nag, short enough that a player who missed one sees the
            // next while still looking at the same board.
            Assert.That(RotateHint.IntervalSeconds, Is.InRange(2f, 3f));
        }

        [Test]
        public void A_shake_is_brief_next_to_the_wait()
        {
            Assert.That(RotateHint.DurationSeconds, Is.LessThan(RotateHint.IntervalSeconds * 0.5f));
        }
    }
}
