using NUnit.Framework;
using Pathweaver.Core.Hex;
using Pathweaver.Game.Presentation;
using UnityEngine;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// Turning a thumb position into a cell. This is the maths that decides whether a
    /// tap lands where the player aimed, and the only part of input that can be tested
    /// without a device.
    /// </summary>
    public class HexMetricsRoundTripTests
    {
        [Test]
        public void Every_cell_centre_maps_back_to_its_own_cell()
        {
            for (var q = -4; q <= 4; q++)
            {
                for (var r = -4; r <= 4; r++)
                {
                    var coordinate = new HexCoord(q, r);
                    var world = HexMetrics.ToWorld(coordinate);

                    Assert.That(
                        HexMetrics.FromWorld(world),
                        Is.EqualTo(coordinate),
                        $"{coordinate} did not survive the round trip.");
                }
            }
        }

        [Test]
        public void A_point_near_a_centre_maps_to_that_cell()
        {
            // A thumb never lands exactly on a centre.
            var coordinate = new HexCoord(1, -2);
            var centre = HexMetrics.ToWorld(coordinate);

            foreach (var offset in new[]
                     {
                         new Vector3(0.1f, 0f, 0f),
                         new Vector3(-0.1f, 0f, 0f),
                         new Vector3(0f, 0.1f, 0f),
                         new Vector3(0f, -0.1f, 0f),
                         new Vector3(0.07f, 0.07f, 0f),
                     })
            {
                Assert.That(
                    HexMetrics.FromWorld(centre + offset),
                    Is.EqualTo(coordinate),
                    $"Offset {offset} escaped the cell.");
            }
        }

        [Test]
        public void A_point_just_across_a_border_maps_to_the_neighbour()
        {
            var coordinate = HexCoord.Zero;
            var centre = HexMetrics.ToWorld(coordinate);

            for (var edge = 0; edge < 6; edge++)
            {
                var neighbour = coordinate.Neighbour(edge);
                var justOver = centre + (HexMetrics.EdgeDirection(edge) * HexMetrics.CellSpacing * 0.75f);

                Assert.That(
                    HexMetrics.FromWorld(justOver),
                    Is.EqualTo(neighbour),
                    $"Crossing edge {edge} did not reach {neighbour}.");
            }
        }

        [Test]
        public void Rounding_never_lands_on_a_cell_that_does_not_touch_the_point()
        {
            // Rounding the two axial components independently can pick a cell the point
            // is not inside, because the axes are not perpendicular. Cube rounding is
            // what prevents it, and this is the test that would catch its absence.
            var random = new System.Random(1234);

            for (var sample = 0; sample < 2000; sample++)
            {
                var point = new Vector3(
                    (float)((random.NextDouble() * 8.0) - 4.0),
                    (float)((random.NextDouble() * 8.0) - 4.0),
                    0f);

                var cell = HexMetrics.FromWorld(point);
                var distance = Vector3.Distance(HexMetrics.ToWorld(cell), point);

                // No point inside a hexagon is further from its centre than the
                // centre-to-corner distance.
                Assert.That(
                    distance,
                    Is.LessThanOrEqualTo(HexMetrics.Size + 0.0001f),
                    $"{point} resolved to {cell}, which is {distance} away.");
            }
        }
    }
}
