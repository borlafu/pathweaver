using NUnit.Framework;
using Pathweaver.Game.Presentation;
using UnityEngine;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// The opening camera move, which is a pure function of a phase.
    /// </summary>
    /// <remarks>
    /// Being a pure function is what makes it testable and what lets the preview capture freeze it. The
    /// property that matters most is the last one: a flight that does not land exactly on the playing
    /// framing leaves the board slightly misplaced for the whole session.
    /// </remarks>
    public class BoardIntroFlightTests
    {
        private static readonly Vector2 BirdsEye = new Vector2(0f, 0f);
        private static readonly Vector2 Playing = new Vector2(-2.5f, 1.5f);

        private const float BirdsEyeSize = 9f;
        private const float PlayingSize = 3f;

        private static (Vector2 LookAt, float OrthographicSize) At(float phase)
            => BoardIntroFlight.Evaluate(phase, BirdsEye, BirdsEyeSize, Playing, PlayingSize);

        [Test]
        public void It_starts_showing_the_whole_board()
        {
            Assert.That(At(0f).LookAt, Is.EqualTo(BirdsEye));
            Assert.That(At(0f).OrthographicSize, Is.EqualTo(BirdsEyeSize).Within(0.0001f));
        }

        [Test]
        public void It_lands_exactly_on_the_playing_framing()
        {
            // The one that matters. A flight that stops near the destination rather than on it leaves the
            // board a little off for the rest of the session, and the clamp then treats that as where the
            // player panned to.
            Assert.That(At(1f).LookAt.x, Is.EqualTo(Playing.x).Within(0.0001f));
            Assert.That(At(1f).LookAt.y, Is.EqualTo(Playing.y).Within(0.0001f));
            Assert.That(At(1f).OrthographicSize, Is.EqualTo(PlayingSize).Within(0.0001f));
        }

        [Test]
        public void A_phase_past_the_end_rests_on_the_destination()
        {
            Assert.That(At(1.5f).OrthographicSize, Is.EqualTo(PlayingSize).Within(0.0001f));
            Assert.That(At(-0.5f).OrthographicSize, Is.EqualTo(BirdsEyeSize).Within(0.0001f));
        }

        [Test]
        public void The_zoom_only_ever_closes_in()
        {
            // Monotonic. A flight that widened before narrowing would read as the camera changing its
            // mind about where it was going.
            var previous = float.MaxValue;

            for (var phase = 0f; phase <= 1f; phase += 0.02f)
            {
                var size = At(phase).OrthographicSize;
                Assert.That(size, Is.LessThanOrEqualTo(previous + 0.0001f));
                previous = size;
            }
        }

        [Test]
        public void The_zoom_is_even_rather_than_rushing_the_end()
        {
            // Interpolated in the logarithm of the size, so the halfway point is the geometric mean and
            // not the arithmetic one. A linear ramp between 9 and 3 would sit at 6 halfway — most of the
            // flight spent wide, then a lurch — because what the eye reads as speed is the proportional
            // change.
            var halfway = At(0.5f).OrthographicSize;

            Assert.That(halfway, Is.EqualTo(Mathf.Sqrt(BirdsEyeSize * PlayingSize)).Within(0.01f));
            Assert.That(halfway, Is.LessThan((BirdsEyeSize + PlayingSize) * 0.5f));
        }

        [Test]
        public void It_eases_in_and_out_rather_than_cutting()
        {
            // Slow at both ends, fastest in the middle. A camera that starts and stops abruptly reads as
            // a cut, and a cut does not say that where the player ends up is part of where they began.
            var early = BoardIntroFlight.Ease(0.05f);
            var middleStep = BoardIntroFlight.Ease(0.55f) - BoardIntroFlight.Ease(0.45f);
            var endStep = BoardIntroFlight.Ease(1f) - BoardIntroFlight.Ease(0.9f);

            Assert.That(early, Is.LessThan(0.05f), "It should leave slowly.");
            Assert.That(middleStep, Is.GreaterThan(endStep), "It should arrive slowly.");
        }

        [Test]
        public void The_ease_is_pinned_at_both_ends()
        {
            Assert.That(BoardIntroFlight.Ease(0f), Is.EqualTo(0f));
            Assert.That(BoardIntroFlight.Ease(1f), Is.EqualTo(1f));
        }

        [Test]
        public void It_is_brief_enough_not_to_be_in_the_way()
        {
            // Time taken from someone who wants to play, and the board is not interactive during it.
            Assert.That(BoardIntroFlight.DurationSeconds, Is.InRange(0.6f, 1.5f));
        }
    }
}
