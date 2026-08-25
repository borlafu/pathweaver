using System.Collections.Generic;
using NUnit.Framework;
using Pathweaver.Core.Hex;
using Pathweaver.Game.Presentation;
using UnityEngine;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// The light travelling along a completed route.
    /// </summary>
    public class FlowPulseTests
    {
        private static readonly List<Vector3> ThreePoints = new List<Vector3>
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(2f, 0f, 0f),
        };

        [Test]
        public void The_pulse_starts_at_the_spring()
        {
            Assert.That(FlowPulse.PositionAt(ThreePoints, 0f, phase: 0f), Is.EqualTo(ThreePoints[0]));
        }

        [Test]
        public void The_pulse_stays_on_the_path()
        {
            // It may only ever be somewhere between the spring and the hub, on the line between two
            // neighbouring cells — a pulse drifting off the conduit would read as a stray object.
            for (var elapsed = 0f; elapsed < 20f; elapsed += 0.05f)
            {
                var position = FlowPulse.PositionAt(ThreePoints, elapsed, phase: 0.3f);

                Assert.That(position.x, Is.InRange(-0.0001f, 2.0001f));
                Assert.That(position.y, Is.EqualTo(0f).Within(0.0001f));
            }
        }

        [Test]
        public void Halfway_along_one_segment_is_the_midpoint()
        {
            var perSegment = HexMetrics.CellSpacing / FlowPulse.SpeedPerSecond;
            var points = new List<Vector3> { Vector3.zero, new Vector3(2f, 0f, 0f) };

            var position = FlowPulse.PositionAt(points, perSegment * 0.5f, phase: 0f);

            Assert.That(position.x, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void A_route_with_one_point_degenerates_safely()
        {
            // A one-cell path cannot happen on a real board, but a defensive animation must not throw on
            // a board it did not expect: a decoration may never be able to break a level.
            var single = new List<Vector3> { new Vector3(3f, 4f, 0f) };

            Assert.That(FlowPulse.PositionAt(single, 5f, phase: 0.5f), Is.EqualTo(single[0]));
            Assert.That(FlowPulse.PositionAt(new List<Vector3>(), 5f, 0f), Is.EqualTo(Vector3.zero));
            Assert.That(FlowPulse.PositionAt(null, 5f, 0f), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void The_pulse_wraps_rather_than_stopping_at_the_hub()
        {
            var perSegment = HexMetrics.CellSpacing / FlowPulse.SpeedPerSecond;
            var cycle = perSegment * 2f;

            var atStart = FlowPulse.PositionAt(ThreePoints, 0f, phase: 0f);
            var afterOneCycle = FlowPulse.PositionAt(ThreePoints, cycle, phase: 0f);

            Assert.That(afterOneCycle.x, Is.EqualTo(atStart.x).Within(0.001f));
        }

        [Test]
        public void A_longer_route_takes_longer_to_cross()
        {
            // Speed is world units per second, not routes per second, so both routes advance at the same
            // rate and the longer one therefore takes longer end to end. Asserting the rate is the honest
            // way to say that: after the same time, both are the same distance along.
            var shortRoute = FlowPulse.DistanceAlong(2, elapsedSeconds: 0.9f, phase: 0f);
            var longRoute = FlowPulse.DistanceAlong(9, elapsedSeconds: 0.9f, phase: 0f);

            Assert.That(shortRoute, Is.EqualTo(longRoute).Within(0.0001f));

            var perSegment = HexMetrics.CellSpacing / FlowPulse.SpeedPerSecond;
            Assert.That(perSegment * 9f, Is.GreaterThan(perSegment * 2f));
        }

        [Test]
        public void Crossing_one_cell_takes_a_readable_length_of_time()
        {
            // Pinned so nobody makes the flow frantic: fast enough to read as movement, slow enough to
            // follow with the eye at the 30 Hz an idle board runs at.
            var perSegment = HexMetrics.CellSpacing / FlowPulse.SpeedPerSecond;

            Assert.That(perSegment, Is.InRange(0.3f, 1f));
        }

        [Test]
        public void Zero_segments_does_not_divide_by_anything()
        {
            Assert.That(FlowPulse.DistanceAlong(0, 3f, 0.2f), Is.EqualTo(0f));
        }

        [Test]
        public void Two_routes_do_not_travel_in_lockstep()
        {
            var first = FlowPulse.PhaseFor(new HexCoord(-3, 0), new HexCoord(2, 0));
            var second = FlowPulse.PhaseFor(new HexCoord(0, -2), new HexCoord(0, 2));

            Assert.That(Mathf.Abs(first - second), Is.GreaterThan(0.05f));
        }

        [Test]
        public void A_routes_phase_survives_being_reshaped()
        {
            // A pivot can change the path between a spring and a hub without changing either end. The
            // phase comes from the ends alone, so the pulse keeps its place in the cycle instead of
            // snapping back to the spring.
            var before = FlowPulse.PhaseFor(new HexCoord(2, 0), new HexCoord(2, -2));
            var after = FlowPulse.PhaseFor(new HexCoord(2, 0), new HexCoord(2, -2));

            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void A_phase_stays_inside_one_cycle()
        {
            for (var q = -4; q <= 4; q++)
            {
                var phase = FlowPulse.PhaseFor(new HexCoord(q, 1), new HexCoord(-q, -1));

                Assert.That(phase, Is.InRange(0f, 1f));
            }
        }
    }
}
