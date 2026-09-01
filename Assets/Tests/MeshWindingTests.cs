using System.Collections.Generic;
using NUnit.Framework;
using Pathweaver.Game.Presentation;
using UnityEngine;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// Whether every generated mesh faces the camera.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The one rendering fault this project has shipped twice. Back faces are culled and the camera sits
    /// at negative Z, so a mesh wound the wrong way does not look wrong — it is not drawn at all. The
    /// first time, every hexagon on the board vanished while the conduit spokes, wound the other way,
    /// kept rendering; CLAUDE.md records it as something "no test would have noticed".
    /// </para>
    /// <para>
    /// A test can notice. The expected sign is taken from <c>CreateHexagon</c> rather than derived from
    /// first principles, because a hand-derived expectation can be wrong in the same direction as the
    /// code it checks — and the hexagon is known good, since the board is visibly on screen.
    /// </para>
    /// </remarks>
    public class MeshWindingTests
    {
        /// <summary>Triangles smaller than this are ignored: their sign is numerical noise.</summary>
        private const float DegenerateArea = 1e-7f;

        [Test]
        public void The_reference_hexagon_is_wound_consistently()
        {
            // Establishes the convention the rest of this file compares against. If this fails, the
            // board is invisible and every other test here is meaningless.
            var signs = SignsOf(HexMeshFactory.CreateHexagon(0.5f));

            Assert.That(signs, Is.Not.Empty);
            CollectionAssert.DoesNotContain(signs, 1, "some hexagon triangles face away from the camera");
        }

        [Test]
        public void Every_board_mesh_faces_the_camera()
        {
            AssertFacesCamera("Hexagon", HexMeshFactory.CreateHexagon(0.5f));
            AssertFacesCamera("Polygon3", HexMeshFactory.CreateRegularPolygon(3, 0.3f, rotationDegrees: -90f));
            AssertFacesCamera("Polygon16", HexMeshFactory.CreateRegularPolygon(16, 0.2f));
            AssertFacesCamera("Rectangle", HexMeshFactory.CreateRectangle(0.4f, 0.1f));
            AssertFacesCamera("Spoke", HexMeshFactory.CreateSpoke(0.43f, 0.14f));
            AssertFacesCamera("CircularArrow", HexMeshFactory.CreateCircularArrow(0.21f, 0.07f, 265f));
        }

        [Test]
        public void Every_glyph_mesh_faces_the_camera()
        {
            // The two paired-ring shapes and the mitred one are where this could plausibly go wrong,
            // which is why GlyphMeshFactory orients its triangles by measurement rather than by hand.
            AssertFacesCamera("Ring", GlyphMeshFactory.CreateRing(0.4f, 0.09f));
            AssertFacesCamera("Ring12", GlyphMeshFactory.CreateRing(0.4f, 0.06f, sides: 12));
            AssertFacesCamera("Gear", GlyphMeshFactory.CreateGear(0.16f));
            AssertFacesCamera("Chevron", GlyphMeshFactory.CreateChevron(0.16f, 0.11f, 0.05f));
            AssertFacesCamera("Spiral", GlyphMeshFactory.CreateSpiral(0.06f, 0.1f, 1.75f, 0.04f));
            AssertFacesCamera("Play", GlyphMeshFactory.CreatePlayTriangle(0.3f));
            AssertFacesCamera("Back", GlyphMeshFactory.CreateBackTriangle(0.19f));
            AssertFacesCamera("Disc", GlyphMeshFactory.CreateDisc(0.1f));
        }

        [Test]
        public void A_blocks_sides_face_outward()
        {
            // The one mesh in the game that must not face the camera. A skirt is checked against the
            // radial direction instead: a side face pointing at the viewer would be invisible from every
            // angle the board is ever seen from, and its XY area is zero, so the test above cannot
            // measure it at all.
            var mesh = HexMeshFactory.CreateHexagonSkirt(0.46f, 0.25f);
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            Assert.That(triangles.Length, Is.EqualTo(6 * 6), "A hexagonal skirt is six quads.");

            for (var index = 0; index < triangles.Length; index += 3)
            {
                var a = vertices[triangles[index]];
                var b = vertices[triangles[index + 1]];
                var c = vertices[triangles[index + 2]];

                var normal = Vector3.Cross(b - a, c - a).normalized;

                // The face's own middle, pointing away from the prism's axis.
                var middle = (a + b + c) / 3f;
                var outward = new Vector3(middle.x, middle.y, 0f).normalized;

                Assert.That(
                    Vector3.Dot(normal, outward),
                    Is.GreaterThan(0.5f),
                    $"A side face at {middle} points inward or along the axis.");
            }
        }

        [Test]
        public void A_block_stands_behind_its_own_top_face()
        {
            // Extruded away from the camera, so every decal the board draws at z near zero — spokes,
            // motifs, the endpoint ring — stays in front of the block rather than inside it.
            var mesh = HexMeshFactory.CreateHexagonSkirt(0.46f, 0.25f);

            Assert.That(mesh.bounds.min.z, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(mesh.bounds.max.z, Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void No_generated_mesh_folds_over_itself()
        {
            // The check the winding test cannot make. GlyphMeshFactory.AddTriangle re-winds every triangle
            // to face the camera whichever order it is given, so for any mesh built through it the winding
            // assertion above is guaranteed to pass and proves nothing.
            //
            // What it hides is a quad given in the wrong order, which draws as a bow tie: two triangles
            // that cross rather than sitting side by side. The chevron's lower arm was built that way and
            // rendered as a twisted band on the skip control and the pan hints for as long as it existed.
            //
            // The gear is deliberately absent. It is a union of a bored body and eight teeth, and a tooth
            // root overlapping the body is how it is drawn rather than a fault — which is harmless in an
            // opaque mesh. This invariant belongs to a single stroke that should not fold, not to every
            // shape that happens to be one mesh.
            AssertSimple("Chevron", GlyphMeshFactory.CreateChevron(0.16f, 0.11f, 0.05f));
            AssertSimple("ChevronNarrow", GlyphMeshFactory.CreateChevron(0.075f, 0.065f, 0.028f));
            AssertSimple("ChevronTall", GlyphMeshFactory.CreateChevron(0.11f, 0.16f, 0.035f));
            AssertSimple("Ring", GlyphMeshFactory.CreateRing(0.4f, 0.09f));
            AssertSimple("Spiral", GlyphMeshFactory.CreateSpiral(0.06f, 0.1f, 1.75f, 0.04f));
            AssertSimple("CircularArrow", HexMeshFactory.CreateCircularArrow(0.21f, 0.07f, 265f));
            AssertSimple("Play", GlyphMeshFactory.CreatePlayTriangle(0.3f));
            AssertSimple("Back", GlyphMeshFactory.CreateBackTriangle(0.19f));
        }

        /// <summary>
        /// Asserts no triangle of the mesh lies on top of another.
        /// </summary>
        /// <remarks>
        /// Tested by asking whether one triangle's middle falls strictly inside another. That is not a
        /// general overlap test — two triangles can cross without either centre being inside the other —
        /// but it catches a quad given in the wrong order, which is the mistake that actually happens
        /// here, and it costs nothing.
        /// </remarks>
        private static void AssertSimple(string what, Mesh mesh)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var count = triangles.Length / 3;

            for (var i = 0; i < count; i++)
            {
                var centre = (vertices[triangles[i * 3]]
                              + vertices[triangles[(i * 3) + 1]]
                              + vertices[triangles[(i * 3) + 2]]) / 3f;

                for (var j = 0; j < count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    Assert.That(
                        IsInside(
                            centre,
                            vertices[triangles[j * 3]],
                            vertices[triangles[(j * 3) + 1]],
                            vertices[triangles[(j * 3) + 2]]),
                        Is.False,
                        $"{what}: triangle {i} sits on top of triangle {j}, so the mesh folds over itself.");
                }
            }
        }

        /// <summary>Whether a point is strictly inside a triangle, in the XY plane.</summary>
        private static bool IsInside(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
        {
            var first = Side(point, a, b);
            var second = Side(point, b, c);
            var third = Side(point, c, a);

            return (first > 0f && second > 0f && third > 0f)
                   || (first < 0f && second < 0f && third < 0f);
        }

        private static float Side(Vector3 point, Vector3 from, Vector3 to)
            => ((to.x - from.x) * (point.y - from.y)) - ((to.y - from.y) * (point.x - from.x));

        private static void AssertFacesCamera(string what, Mesh mesh)
        {
            var signs = SignsOf(mesh);

            Assert.That(signs, Is.Not.Empty, $"{what} has no triangles with any area at all");
            CollectionAssert.DoesNotContain(signs, 1, $"{what} has triangles facing away from the camera");
        }

        /// <summary>
        /// The signed-area sign of every triangle worth measuring: -1 towards the camera, 1 away.
        /// </summary>
        private static List<int> SignsOf(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var signs = new List<int>(triangles.Length / 3);

            for (var index = 0; index < triangles.Length; index += 3)
            {
                var a = vertices[triangles[index]];
                var b = vertices[triangles[index + 1]];
                var c = vertices[triangles[index + 2]];

                var first = b - a;
                var second = c - a;
                var signedArea = (first.x * second.y) - (first.y * second.x);

                if (Mathf.Abs(signedArea) < DegenerateArea)
                {
                    continue;
                }

                signs.Add(signedArea < 0f ? -1 : 1);
            }

            return signs;
        }
    }
}
