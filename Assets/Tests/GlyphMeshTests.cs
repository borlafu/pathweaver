using System;
using System.Linq;
using NUnit.Framework;
using Pathweaver.Game.Presentation;
using UnityEngine;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// Whether each icon is the shape it claims to be.
    /// </summary>
    /// <remarks>
    /// Whether an icon <em>reads</em> needs a person and a phone. Whether the gear has a hole, whether
    /// the chevron's arms actually meet, and whether the spiral's turns are separate are all decidable
    /// here — and each of those three was the specific defect in the shape this replaces.
    /// </remarks>
    public class GlyphMeshTests
    {
        [Test]
        public void The_gear_has_a_real_hole()
        {
            // The old settings icon was a disc with a smaller disc drawn over it in the background
            // colour. That reads as a disc, and it only worked at all because glyph depths were later
            // stacked. A hole made of geometry cannot be undone by a sorting question.
            const float Root = 0.16f;
            var mesh = GlyphMeshFactory.CreateGear(Root);

            var radii = mesh.vertices.Select(vertex => new Vector2(vertex.x, vertex.y).magnitude).ToList();

            Assert.That(radii.Min(), Is.GreaterThan(Root * 0.3f), "a vertex sits inside the bore");
            CollectionAssert.DoesNotContain(radii.Select(r => Mathf.Approximately(r, 0f)), true);
        }

        [Test]
        public void The_gear_has_the_teeth_it_was_asked_for()
        {
            const int Teeth = 8;
            var mesh = GlyphMeshFactory.CreateGear(0.16f, Teeth);

            // Outer vertices alternate between the root and the tip radius, four boundary points per
            // tooth, so counting the distinct outer radii counts the teeth.
            var outerRadii = mesh.vertices
                .Where((_, index) => index % 2 == 1)
                .Select(vertex => new Vector2(vertex.x, vertex.y).magnitude)
                .ToList();

            var tip = outerRadii.Max();
            var atTip = outerRadii.Count(radius => Mathf.Abs(radius - tip) < 0.001f);

            Assert.That(outerRadii.Count, Is.EqualTo(Teeth * 4));
            Assert.That(atTip, Is.EqualTo(Teeth * 2), "each tooth should reach the tip radius twice");
        }

        [Test]
        public void The_gear_fits_inside_the_radius_it_was_given()
        {
            // A tooth poking past the button's own hexagon would look like a rendering fault.
            const float Root = 0.16f;
            var mesh = GlyphMeshFactory.CreateGear(Root);

            Assert.That(mesh.bounds.extents.x, Is.LessThan(Root * 1.4f));
            Assert.That(mesh.bounds.extents.y, Is.LessThan(Root * 1.4f));
        }

        [Test]
        public void A_gear_with_too_few_teeth_is_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GlyphMeshFactory.CreateGear(0.16f, teeth: 4));
        }

        [Test]
        public void The_chevron_is_one_mesh_with_a_closed_point()
        {
            // Four loose rectangles is what it was: the arms crossed at the apex and left a notch on
            // the outside of the joint. Six vertices and four triangles is a single mitred stroke.
            var mesh = GlyphMeshFactory.CreateChevron(0.16f, 0.11f, 0.05f);

            Assert.That(mesh.vertexCount, Is.EqualTo(6));
            Assert.That(mesh.triangles.Length, Is.EqualTo(12));

            var xs = mesh.vertices.Select(vertex => vertex.x).OrderBy(x => x).ToList();

            // The two tip vertices sit at different distances along the axis — that gap is the mitre.
            Assert.That(xs.Last() - xs[xs.Count - 2], Is.GreaterThan(0.001f));
        }

        [Test]
        public void The_chevron_is_symmetric_about_its_axis()
        {
            var mesh = GlyphMeshFactory.CreateChevron(0.16f, 0.11f, 0.05f);

            var above = mesh.vertices.Where(vertex => vertex.y > 0.0001f).Count();
            var below = mesh.vertices.Where(vertex => vertex.y < -0.0001f).Count();

            Assert.That(above, Is.EqualTo(below));
        }

        [Test]
        public void A_chevron_too_flat_to_mitre_is_refused()
        {
            // As the arms flatten the mitre runs away to infinity, which would draw a spike across the
            // whole button rather than a chevron.
            Assert.Throws<ArgumentOutOfRangeException>(
                () => GlyphMeshFactory.CreateChevron(0.2f, 0.005f, 0.05f));
        }

        [Test]
        public void The_spiral_actually_spirals()
        {
            // The shape this replaces was two concentric circular arrows, so its "turns" were at two
            // fixed radii. A spiral's radius has to keep growing.
            var mesh = GlyphMeshFactory.CreateSpiral(0.06f, 0.1f, 1.75f, 0.04f);

            var innerRadii = mesh.vertices
                .Where((_, index) => index % 2 == 0)
                .Select(vertex => new Vector2(vertex.x, vertex.y).magnitude)
                .ToList();

            for (var index = 1; index < innerRadii.Count; index++)
            {
                Assert.That(
                    innerRadii[index],
                    Is.GreaterThan(innerRadii[index - 1] - 0.0001f),
                    "the spiral's radius stopped growing");
            }

            Assert.That(innerRadii.Last(), Is.GreaterThan(innerRadii.First() * 1.5f));
        }

        [Test]
        public void A_spiral_whose_turns_would_touch_is_refused()
        {
            // Fused turns are a disc with a gap, which is exactly how the old icon read.
            Assert.Throws<ArgumentOutOfRangeException>(
                () => GlyphMeshFactory.CreateSpiral(0.06f, 0.04f, 2f, 0.04f));
        }

        [Test]
        public void A_ring_leaves_its_middle_empty()
        {
            // What lets an endpoint's pulse pass over the resource motif without hiding it.
            const float Radius = 0.4f;
            const float Thickness = 0.09f;

            var mesh = GlyphMeshFactory.CreateRing(Radius, Thickness);
            var radii = mesh.vertices.Select(vertex => new Vector2(vertex.x, vertex.y).magnitude).ToList();

            Assert.That(radii.Min(), Is.GreaterThan(Radius - Thickness));
            Assert.That(radii.Max(), Is.LessThan(Radius + Thickness));
        }

        [Test]
        public void A_ring_thicker_than_its_own_diameter_is_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GlyphMeshFactory.CreateRing(0.1f, 0.5f));
        }

        [Test]
        public void The_play_and_back_triangles_point_opposite_ways()
        {
            // Seven call sites used to repeat the rotation by hand, and one of them had the sign wrong:
            // the level list's back arrow pointed right and read as a second play button.
            var play = GlyphMeshFactory.CreatePlayTriangle(0.3f);
            var back = GlyphMeshFactory.CreateBackTriangle(0.3f);

            var playApex = play.vertices.OrderByDescending(vertex => vertex.x).First();
            var backApex = back.vertices.OrderBy(vertex => vertex.x).First();

            Assert.That(playApex.x, Is.GreaterThan(0.2f));
            Assert.That(backApex.x, Is.LessThan(-0.2f));
        }
    }
}
