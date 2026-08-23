using NUnit.Framework;
using Pathweaver.Core.Hex;
using Pathweaver.Game.Presentation;
using UnityEngine;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// The layout maths, which is the one part of the presentation layer that can be
    /// wrong in a way tests can catch. Everything else about rendering needs eyes.
    /// </summary>
    public class HexMetricsTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void The_origin_sits_at_the_world_origin()
        {
            Assert.That(HexMetrics.ToWorld(HexCoord.Zero), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Every_neighbour_is_one_cell_spacing_away()
        {
            for (var edge = 0; edge < 6; edge++)
            {
                var neighbour = HexMetrics.ToWorld(HexCoord.Zero.Neighbour(edge));

                Assert.That(
                    neighbour.magnitude,
                    Is.EqualTo(HexMetrics.CellSpacing).Within(Tolerance),
                    $"Edge {edge} landed {neighbour.magnitude} away.");
            }
        }

        [Test]
        public void Direction_zero_points_due_east()
        {
            var east = HexMetrics.EdgeDirection(0);

            Assert.That(east.x, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(east.y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Rotating_clockwise_in_the_simulation_looks_clockwise_on_screen()
        {
            // The reason the vertical axis is negated in HexMetrics. If this fails,
            // HexCoord.RotateClockwise is a misnomer and every rotation animation will
            // run the wrong way.
            var east = HexMetrics.EdgeDirection(0);
            var next = HexMetrics.EdgeDirection(1);

            // A clockwise turn from due east must move downward on screen.
            Assert.That(next.y, Is.LessThan(0f), $"Edge 1 pointed at {next}, expected downward.");

            // Cross product z of (east x next) is negative for a clockwise sweep.
            var cross = (east.x * next.y) - (east.y * next.x);
            Assert.That(cross, Is.LessThan(0f));
        }

        [Test]
        public void Opposite_edges_point_in_opposite_directions()
        {
            for (var edge = 0; edge < 6; edge++)
            {
                var forward = HexMetrics.EdgeDirection(edge);
                var back = HexMetrics.EdgeDirection((edge + 3) % 6);

                Assert.That(Vector3.Dot(forward, back), Is.EqualTo(-1f).Within(Tolerance));
            }
        }

        [Test]
        public void Cells_in_a_row_are_evenly_spaced()
        {
            var left = HexMetrics.ToWorld(new HexCoord(-1, 0));
            var middle = HexMetrics.ToWorld(HexCoord.Zero);
            var right = HexMetrics.ToWorld(new HexCoord(1, 0));

            Assert.That(middle.x - left.x, Is.EqualTo(right.x - middle.x).Within(Tolerance));
            Assert.That(left.y, Is.EqualTo(right.y).Within(Tolerance));
        }

        [Test]
        public void A_generated_hexagon_has_six_triangles()
        {
            var mesh = HexMeshFactory.CreateHexagon(1f);

            Assert.That(mesh.vertexCount, Is.EqualTo(7), "Six corners plus a centre.");
            Assert.That(mesh.triangles.Length, Is.EqualTo(18), "Six triangles of three indices.");
        }

        [Test]
        public void A_generated_hexagon_is_pointy_topped()
        {
            // Rows have to nest vertically for a portrait phone layout.
            var mesh = HexMeshFactory.CreateHexagon(1f);
            var highest = 0f;
            var widest = 0f;

            foreach (var vertex in mesh.vertices)
            {
                highest = Mathf.Max(highest, Mathf.Abs(vertex.y));
                widest = Mathf.Max(widest, Mathf.Abs(vertex.x));
            }

            Assert.That(highest, Is.GreaterThan(widest));
        }

        [Test]
        public void A_spoke_reaches_from_the_centre_towards_an_edge()
        {
            var mesh = HexMeshFactory.CreateSpoke(2f, 0.5f);

            Assert.That(mesh.vertexCount, Is.EqualTo(4));
            Assert.That(mesh.bounds.min.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(mesh.bounds.max.x, Is.EqualTo(2f).Within(Tolerance));
        }
    }
}
