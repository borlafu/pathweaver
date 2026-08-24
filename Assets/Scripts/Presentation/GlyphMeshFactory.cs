using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// The shapes the interface is drawn from: gears, chevrons, spirals, rings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Separate from <see cref="HexMeshFactory"/>, which builds the shapes the board itself is made of.
    /// These are icons, and they answer to a different question: whether a player can tell at a glance
    /// what a control does. There is still no font, so every icon has to carry its meaning in silhouette.
    /// </para>
    /// <para>
    /// Every mesh here is wound to face the camera at negative Z. Rather than deriving that by hand for
    /// each shape — which is how a mesh ends up silently culled, twice in this project's history — the
    /// triangles go through <see cref="AddTriangle"/>, which measures the signed area and emits whichever
    /// order faces the camera.
    /// </para>
    /// </remarks>
    internal static class GlyphMeshFactory
    {
        /// <summary>
        /// The rotation that points a triangle to the right, as a play symbol does.
        /// </summary>
        /// <remarks>
        /// Named because it was a magic number repeated at seven call sites, and because the sign is not
        /// guessable: the level list's back arrow was written as 30 degrees, which is the same shape as
        /// this one and pointed the wrong way, so the control to leave a screen read as a second play
        /// button.
        /// </remarks>
        private const float PointingRight = -90f;

        private const float PointingLeft = 90f;

        /// <summary>A triangle pointing the way a player expects to go.</summary>
        internal static Mesh CreatePlayTriangle(float radius)
            => HexMeshFactory.CreateRegularPolygon(3, radius, PointingRight);

        /// <summary>A triangle pointing back the way the player came.</summary>
        internal static Mesh CreateBackTriangle(float radius)
            => HexMeshFactory.CreateRegularPolygon(3, radius, PointingLeft);

        /// <summary>A small many-sided disc, for a travelling pulse or a pip.</summary>
        internal static Mesh CreateDisc(float radius, int sides = 12)
            => HexMeshFactory.CreateRegularPolygon(sides, radius);

        /// <summary>
        /// A closed ring, hollow in the middle.
        /// </summary>
        /// <remarks>
        /// What an endpoint's pulse is drawn with. A ring rather than a filled shape for two reasons: it
        /// never covers the resource motif in the middle of the cell, so it cannot depend on depth
        /// ordering to stay out of the way, and its stroke thins as it grows, which reads as travelling
        /// where a growing solid reads as a flicker.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the ring has fewer than three sides, or a stroke too thick for its radius.
        /// </exception>
        internal static Mesh CreateRing(float radius, float thickness, int sides = 6)
        {
            if (sides < 3)
            {
                throw new ArgumentOutOfRangeException(nameof(sides), sides, "A ring needs three sides.");
            }

            if (thickness <= 0f || thickness >= radius * 2f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(thickness), thickness, "A ring's stroke must be thinner than its diameter.");
            }

            var inner = radius - (thickness * 0.5f);
            var outer = radius + (thickness * 0.5f);

            var vertices = new List<Vector3>(sides * 2);
            var triangles = new List<int>(sides * 6);

            // Clockwise from the top, matching HexMeshFactory.CreateCircularArrow, and pointy-topped
            // like every hexagon on the board.
            for (var step = 0; step < sides; step++)
            {
                var angle = Mathf.Deg2Rad * (90f - (360f * step / sides));
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

                vertices.Add(direction * inner);
                vertices.Add(direction * outer);
            }

            for (var step = 0; step < sides; step++)
            {
                var thisInner = step * 2;
                var thisOuter = thisInner + 1;
                var nextInner = ((step + 1) % sides) * 2;
                var nextOuter = nextInner + 1;

                AddTriangle(triangles, vertices, thisInner, thisOuter, nextInner);
                AddTriangle(triangles, vertices, thisOuter, nextOuter, nextInner);
            }

            return Build("Ring", vertices, triangles);
        }

        /// <summary>
        /// A gear: a ring of trapezoidal teeth around a real hole.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The settings icon. Its predecessor was a twelve-sided polygon with a smaller one drawn over
        /// it in the background colour, which read as a plain disc — the punched hole only worked at all
        /// because a later fix stacked glyph depths, and a disc with a dot in it is not a gear.
        /// </para>
        /// <para>
        /// Teeth are narrower at the tip than at the root, because a gear whose teeth are square blocks
        /// reads as a cog in a diagram rather than as a machine part. The hole is geometry, not a shape
        /// laid on top: no vertex sits inside the bore, which is what makes it survive any depth or
        /// sorting question.
        /// </para>
        /// </remarks>
        /// <param name="rootRadius">Where the teeth begin.</param>
        /// <param name="teeth">How many teeth. Six reads as a hexagon and twelve as a saw blade.</param>
        internal static Mesh CreateGear(float rootRadius, int teeth = 8)
        {
            if (teeth < 5)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(teeth), teeth, "Fewer than five teeth reads as a polygon rather than a gear.");
            }

            if (rootRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(rootRadius), rootRadius, "A radius must be positive.");
            }

            var tipRadius = rootRadius * 1.34f;
            var boreRadius = rootRadius * 0.42f;

            // The share of one tooth's pitch that the tip spans. Below about a third the teeth read as
            // spikes; above a half the gaps disappear.
            const float TipFraction = 0.42f;

            var vertices = new List<Vector3>(teeth * 8);
            var triangles = new List<int>(teeth * 24);

            var pitch = 2f * Mathf.PI / teeth;

            for (var tooth = 0; tooth < teeth; tooth++)
            {
                // Clockwise, so the four angles of a tooth descend: root, tip, tip, root.
                var centre = (Mathf.PI * 0.5f) - (tooth * pitch);

                AddSpoke(vertices, centre + (pitch * 0.5f), boreRadius, rootRadius);
                AddSpoke(vertices, centre + (pitch * TipFraction * 0.5f), boreRadius, tipRadius);
                AddSpoke(vertices, centre - (pitch * TipFraction * 0.5f), boreRadius, tipRadius);
                AddSpoke(vertices, centre - (pitch * 0.5f), boreRadius, rootRadius);
            }

            var pairs = teeth * 4;
            for (var pair = 0; pair < pairs; pair++)
            {
                var thisInner = pair * 2;
                var thisOuter = thisInner + 1;
                var nextInner = ((pair + 1) % pairs) * 2;
                var nextOuter = nextInner + 1;

                AddTriangle(triangles, vertices, thisInner, thisOuter, nextInner);
                AddTriangle(triangles, vertices, thisOuter, nextOuter, nextInner);
            }

            return Build("Gear", vertices, triangles);
        }

        /// <summary>
        /// A chevron: two arms meeting at a mitred point, as one mesh.
        /// </summary>
        /// <remarks>
        /// The skip button drew this as four separate rectangles rotated by forty degrees, which left the
        /// arms crossing at the apex and a visible notch on the outside of the joint. Mitring means
        /// extending both arms to where their outer edges actually meet, which is what a stroked corner
        /// does — and it makes the chevron one object instead of two.
        /// </remarks>
        /// <param name="width">Distance from the tail to the tip along the pointing axis.</param>
        /// <param name="height">How far each tail sits from the axis.</param>
        /// <param name="thickness">Stroke width.</param>
        internal static Mesh CreateChevron(float width, float height, float thickness)
        {
            if (width <= 0f || height <= 0f || thickness <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(thickness), thickness, "A chevron needs a positive width, height and stroke.");
            }

            var halfWidth = width * 0.5f;
            var halfStroke = thickness * 0.5f;

            var armAngle = Mathf.Atan2(height, width);

            // How far past the tip the outer corner reaches. As the arms flatten this grows without
            // bound, which is why a near-flat chevron is refused rather than drawn as a spike.
            var sine = Mathf.Sin(armAngle);
            if (sine < 0.2f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height), height, "A chevron this flat mitres into a spike; raise its height.");
            }

            var mitre = halfStroke / sine;

            var vertices = new List<Vector3>(6);
            var triangles = new List<int>(12);

            var outerTip = new Vector3(halfWidth + mitre, 0f, 0f);
            var innerTip = new Vector3(halfWidth - mitre, 0f, 0f);

            // Upper arm, then lower, each offset along its own normal so the stroke keeps an even width
            // rather than pinching at the tails.
            var upperTail = new Vector3(-halfWidth, height, 0f);
            var upperNormal = Normal(upperTail, outerTip);

            var lowerTail = new Vector3(-halfWidth, -height, 0f);
            var lowerNormal = Normal(lowerTail, outerTip);

            vertices.Add(upperTail + (upperNormal * halfStroke));  // 0 upper outer tail
            vertices.Add(upperTail - (upperNormal * halfStroke));  // 1 upper inner tail
            vertices.Add(outerTip);                                // 2
            vertices.Add(innerTip);                                // 3
            vertices.Add(lowerTail - (lowerNormal * halfStroke));  // 4 lower inner tail
            vertices.Add(lowerTail + (lowerNormal * halfStroke));  // 5 lower outer tail

            AddTriangle(triangles, vertices, 0, 1, 3);
            AddTriangle(triangles, vertices, 0, 3, 2);
            AddTriangle(triangles, vertices, 2, 3, 4);
            AddTriangle(triangles, vertices, 2, 4, 5);

            return Build("Chevron", vertices, triangles);
        }

        /// <summary>
        /// A spiral: a stroke whose radius grows as it turns.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The endless mode's icon, and deliberately headless. A restart symbol is a circular arrow
        /// <em>with</em> a head; an endless one is a spiral without. The previous version was two
        /// concentric circular arrows, so the two controls carried the same symbol at two sizes.
        /// </para>
        /// <para>
        /// The stroke is offset radially rather than along the true curve normal, which widens it by a
        /// couple of per cent where the spiral is steepest — invisible at icon scale, and it keeps the
        /// construction identical to the arc in <see cref="HexMeshFactory.CreateCircularArrow"/>.
        /// </para>
        /// </remarks>
        /// <param name="innerRadius">Where the spiral starts.</param>
        /// <param name="growthPerTurn">How much further out each full turn travels.</param>
        /// <param name="turns">Turns to draw.</param>
        /// <param name="thickness">Stroke width.</param>
        internal static Mesh CreateSpiral(
            float innerRadius, float growthPerTurn, float turns, float thickness, int segmentsPerTurn = 24)
        {
            if (turns <= 0f || segmentsPerTurn < 6)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(turns), turns, "A spiral needs at least a fraction of a turn and six segments.");
            }

            // Turns that touch each other are a disc, which is what the shape this replaces looked like.
            if (growthPerTurn < thickness * 2.2f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(growthPerTurn),
                    growthPerTurn,
                    "Turns this close together fuse into a disc; grow faster or draw a thinner stroke.");
            }

            var growthPerRadian = growthPerTurn / (2f * Mathf.PI);
            var totalRadians = turns * 2f * Mathf.PI;
            var segments = Mathf.CeilToInt(segmentsPerTurn * turns);
            var halfStroke = thickness * 0.5f;

            var vertices = new List<Vector3>((segments + 1) * 2);
            var triangles = new List<int>(segments * 6);

            for (var step = 0; step <= segments; step++)
            {
                var swept = totalRadians * step / segments;
                var radius = innerRadius + (growthPerRadian * swept);

                // Clockwise from the top: (sin, cos) rather than (cos, sin).
                var direction = new Vector3(Mathf.Sin(swept), Mathf.Cos(swept), 0f);

                vertices.Add(direction * (radius - halfStroke));
                vertices.Add(direction * (radius + halfStroke));

                if (step == 0)
                {
                    continue;
                }

                var previousInner = (step - 1) * 2;
                var previousOuter = previousInner + 1;
                var currentInner = step * 2;
                var currentOuter = currentInner + 1;

                AddTriangle(triangles, vertices, previousInner, previousOuter, currentInner);
                AddTriangle(triangles, vertices, previousOuter, currentOuter, currentInner);
            }

            return Build("Spiral", vertices, triangles);
        }

        /// <summary>Adds an inner and an outer vertex along one direction.</summary>
        private static void AddSpoke(List<Vector3> vertices, float angle, float inner, float outer)
        {
            var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            vertices.Add(direction * inner);
            vertices.Add(direction * outer);
        }

        /// <summary>The unit normal to the left of the line from one point to another.</summary>
        private static Vector3 Normal(Vector3 from, Vector3 to)
        {
            var direction = (to - from).normalized;
            return new Vector3(-direction.y, direction.x, 0f);
        }

        /// <summary>
        /// Adds a triangle wound to face the camera, whichever order it was given in.
        /// </summary>
        /// <remarks>
        /// The camera sits at negative Z with back faces culled, so a triangle must have a negative
        /// signed area to be seen — the convention every mesh in <see cref="HexMeshFactory"/> follows by
        /// hand. Measuring it here instead means a new shape cannot join the two occasions this project
        /// has silently culled its own geometry, and the sign is asserted once in the mesh tests rather
        /// than reasoned about per shape.
        /// </remarks>
        private static void AddTriangle(List<int> triangles, List<Vector3> vertices, int a, int b, int c)
        {
            var first = vertices[b] - vertices[a];
            var second = vertices[c] - vertices[a];

            var signedArea = (first.x * second.y) - (first.y * second.x);

            triangles.Add(a);

            if (signedArea > 0f)
            {
                triangles.Add(c);
                triangles.Add(b);
                return;
            }

            triangles.Add(b);
            triangles.Add(c);
        }

        private static Mesh Build(string name, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
