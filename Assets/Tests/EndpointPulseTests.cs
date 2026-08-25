using NUnit.Framework;
using Pathweaver.Core.Flow;
using Pathweaver.Game.Presentation;
using UnityEngine;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// The endpoint breath. Whether it reads as emitting or receiving needs a person; whether it stays
    /// inside its own cell and whether it loops without a flicker do not.
    /// </summary>
    public class EndpointPulseTests
    {
        [Test]
        public void A_spring_grows_and_a_hub_shrinks()
        {
            // The whole point of the animation: the two roles are told apart by direction of travel, so
            // that source and destination stop depending on colour alone.
            var springStart = EndpointPulse.ScaleAt(0f, EndpointRole.Spring);
            var springLate = EndpointPulse.ScaleAt(EndpointPulse.PeriodSeconds * 0.9f, EndpointRole.Spring);

            var hubStart = EndpointPulse.ScaleAt(0f, EndpointRole.Hub);
            var hubLate = EndpointPulse.ScaleAt(EndpointPulse.PeriodSeconds * 0.9f, EndpointRole.Hub);

            Assert.That(springLate, Is.GreaterThan(springStart));
            Assert.That(hubLate, Is.LessThan(hubStart));
        }

        [Test]
        public void A_spring_never_stops_growing_within_a_cycle()
        {
            // A ring that grew, shrank a little and grew again would read as a wobble rather than as
            // something being emitted.
            var previous = EndpointPulse.ScaleAt(0f, EndpointRole.Spring);

            for (var elapsed = 0f; elapsed < EndpointPulse.PeriodSeconds; elapsed += 0.02f)
            {
                var scale = EndpointPulse.ScaleAt(elapsed, EndpointRole.Spring);

                Assert.That(scale, Is.GreaterThanOrEqualTo(previous - 0.0001f));
                previous = scale;
            }
        }

        [Test]
        public void A_hub_is_exactly_the_mirror_of_a_spring()
        {
            // Written as a mirror rather than as a second easing, so the two cannot drift apart.
            for (var elapsed = 0f; elapsed < EndpointPulse.PeriodSeconds; elapsed += 0.05f)
            {
                var spring = EndpointPulse.ScaleAt(elapsed, EndpointRole.Spring);
                var hub = EndpointPulse.ScaleAt(elapsed, EndpointRole.Hub);

                Assert.That(spring + hub, Is.EqualTo(EndpointPulse.ScaleAt(0f, EndpointRole.Spring)
                    + EndpointPulse.ScaleAt(0f, EndpointRole.Hub)).Within(0.0001f));
            }
        }

        [Test]
        public void The_ring_never_leaves_its_own_cell()
        {
            // The ring is built at the drawn hexagon's radius, so a scale above one spills into the
            // neighbouring cell — which looks like a fault, not a flourish.
            for (var elapsed = 0f; elapsed < EndpointPulse.PeriodSeconds * 2f; elapsed += 0.02f)
            {
                Assert.That(EndpointPulse.ScaleAt(elapsed, EndpointRole.Spring), Is.LessThanOrEqualTo(1f));
                Assert.That(EndpointPulse.ScaleAt(elapsed, EndpointRole.Hub), Is.LessThanOrEqualTo(1f));
                Assert.That(EndpointPulse.ScaleAt(elapsed, EndpointRole.Spring), Is.GreaterThan(0f));
            }
        }

        [Test]
        public void The_cycle_repeats_without_a_step()
        {
            // The equivalent of the rotation hint ending exactly at rest: the ring has to be invisible
            // at the moment it jumps back to the start, or the loop shows as a flicker.
            Assert.That(EndpointPulse.FadeAt(EndpointPulse.PeriodSeconds), Is.EqualTo(0f));
            Assert.That(
                EndpointPulse.FadeAt(EndpointPulse.PeriodSeconds - 0.0001f),
                Is.EqualTo(1f).Within(0.001f));

            Assert.That(
                EndpointPulse.ScaleAt(EndpointPulse.PeriodSeconds, EndpointRole.Spring),
                Is.EqualTo(EndpointPulse.ScaleAt(0f, EndpointRole.Spring)).Within(0.0001f));
        }

        [Test]
        public void Reduced_motion_still_says_which_role_a_cell_plays()
        {
            // The edge marks are gone, so this ring is the only thing besides colour that separates a
            // source from a destination. Switching motion off may take the movement away; it may not
            // take the distinction away from the players who need it most.
            var spring = EndpointPulse.RestingScaleFor(EndpointRole.Spring);
            var hub = EndpointPulse.RestingScaleFor(EndpointRole.Hub);

            Assert.That(spring, Is.Not.EqualTo(hub));
            Assert.That(spring, Is.GreaterThan(hub));
            Assert.That(hub, Is.GreaterThan(0f), "a hidden ring says nothing at all");
        }

        [Test]
        public void A_resting_ring_sits_where_its_travel_ends()
        {
            // So switching motion off looks like the animation stopping, not like a different board.
            Assert.That(
                EndpointPulse.RestingScaleFor(EndpointRole.Spring),
                Is.EqualTo(EndpointPulse.ScaleAt(EndpointPulse.PeriodSeconds * 0.999f, EndpointRole.Spring))
                    .Within(0.01f));
        }

        [Test]
        public void The_ring_is_fully_lit_when_it_sets_out()
        {
            Assert.That(EndpointPulse.FadeAt(0f), Is.EqualTo(0f));
        }

        [Test]
        public void The_fade_never_leaves_its_range()
        {
            for (var elapsed = 0f; elapsed < EndpointPulse.PeriodSeconds * 2f; elapsed += 0.01f)
            {
                Assert.That(EndpointPulse.FadeAt(elapsed), Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void The_period_is_slow_enough_for_the_idle_frame_rate()
        {
            // At 30 Hz — where an idle board lives — a cycle needs enough frames not to look stepped.
            // The fix for a stepped pulse must never be raising the idle rate, which is a PRD budget.
            var framesPerCycle = EndpointPulse.PeriodSeconds * 30f;

            Assert.That(framesPerCycle, Is.GreaterThanOrEqualTo(36f));
        }

        [Test]
        public void Two_endpoints_on_one_board_do_not_beat_in_unison()
        {
            // Otherwise a board of three springs looks like one animation drawn three times.
            var first = EndpointPulse.PhaseOffsetFor(-3, 0);
            var second = EndpointPulse.PhaseOffsetFor(2, 0);
            var third = EndpointPulse.PhaseOffsetFor(0, 2);

            Assert.That(Mathf.Abs(first - second), Is.GreaterThan(0.1f));
            Assert.That(Mathf.Abs(second - third), Is.GreaterThan(0.1f));
        }

        [Test]
        public void An_offset_depends_only_on_where_the_endpoint_is()
        {
            // The board is rebuilt on every state change, including a mere rotation of the held tile.
            // An offset derived from anything else would make every endpoint jump on every move.
            Assert.That(EndpointPulse.PhaseOffsetFor(1, -2), Is.EqualTo(EndpointPulse.PhaseOffsetFor(1, -2)));
        }

        [Test]
        public void An_offset_stays_inside_one_cycle()
        {
            for (var q = -4; q <= 4; q++)
            {
                for (var r = -4; r <= 4; r++)
                {
                    Assert.That(EndpointPulse.PhaseOffsetFor(q, r), Is.InRange(0f, EndpointPulse.PeriodSeconds));
                }
            }
        }
    }
}
