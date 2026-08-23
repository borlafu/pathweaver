using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Builds the shapes the board is drawn from, at runtime.
    /// </summary>
    /// <remarks>
    /// Meshes are generated rather than imported so the MVP needs no art assets at
    /// all. Real hand-painted tiles replace these later; until then nothing in the
    /// repository is waiting on an artist.
    /// </remarks>
    internal static class HexMeshFactory
    {
        private const int Corners = 6;

        /// <summary>
        /// A filled hexagon centred on the origin, flat in the XY plane.
        /// </summary>
        /// <param name="radius">Centre-to-corner distance.</param>
        internal static Mesh CreateHexagon(float radius)
        {
            var vertices = new List<Vector3>(Corners + 1) { Vector3.zero };
            var triangles = new List<int>(Corners * 3);

            // Pointy-top: the first corner sits straight up.
            for (var corner = 0; corner < Corners; corner++)
            {
                var angle = Mathf.Deg2Rad * ((60f * corner) + 90f);
                vertices.Add(new Vector3(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle), 0f));
            }

            // Wound clockwise as seen from +Z, which is counter-clockwise from where
            // the camera sits at negative Z. Get this backwards and the hexagons are
            // silently culled as back faces — every cell vanishes while the conduit
            // spokes, wound the other way, keep rendering.
            for (var corner = 0; corner < Corners; corner++)
            {
                triangles.Add(0);
                triangles.Add(1 + ((corner + 1) % Corners));
                triangles.Add(1 + corner);
            }

            var mesh = new Mesh { name = "Hexagon" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// A filled regular polygon centred on the origin.
        /// </summary>
        /// <remarks>
        /// One helper covers every resource motif: a triangle, a square, a diamond (a square
        /// turned 45 degrees) and a near-circle are all the same shape with different side
        /// counts and rotations. Drawing them from one function keeps their sizes consistent,
        /// which matters when the whole point is telling them apart at a glance.
        /// </remarks>
        internal static Mesh CreateRegularPolygon(int sides, float radius, float rotationDegrees = 0f)
        {
            if (sides < 3)
            {
                throw new ArgumentOutOfRangeException(nameof(sides), sides, "A polygon needs three sides.");
            }

            var vertices = new List<Vector3>(sides + 1) { Vector3.zero };
            var triangles = new List<int>(sides * 3);

            for (var corner = 0; corner < sides; corner++)
            {
                var angle = Mathf.Deg2Rad * ((360f / sides * corner) + rotationDegrees + 90f);
                vertices.Add(new Vector3(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle), 0f));
            }

            // Wound to face the camera at negative Z, like every other mesh here.
            for (var corner = 0; corner < sides; corner++)
            {
                triangles.Add(0);
                triangles.Add(1 + ((corner + 1) % sides));
                triangles.Add(1 + corner);
            }

            var mesh = new Mesh { name = $"Polygon{sides}" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// A rectangle centred on the origin, for panels and glyph strokes.
        /// </summary>
        internal static Mesh CreateRectangle(float width, float height)
        {
            var halfWidth = width * 0.5f;
            var halfHeight = height * 0.5f;

            var mesh = new Mesh { name = "Rectangle" };
            mesh.SetVertices(new List<Vector3>
            {
                new Vector3(-halfWidth, -halfHeight, 0f),
                new Vector3(-halfWidth, halfHeight, 0f),
                new Vector3(halfWidth, halfHeight, 0f),
                new Vector3(halfWidth, -halfHeight, 0f),
            });

            // Wound to face the camera at negative Z, like every other mesh here.
            mesh.SetTriangles(new List<int> { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// A circular arrow: an arc with a head, for a restart symbol.
        /// </summary>
        /// <remarks>
        /// Built rather than imported for the same reason as everything else here — no art
        /// dependency — and because a rotation symbol has to read without text, which the
        /// project has no means of drawing yet.
        /// </remarks>
        internal static Mesh CreateCircularArrow(
            float radius, float thickness, float sweepDegrees, int segments = 20)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            var inner = radius - (thickness * 0.5f);
            var outer = radius + (thickness * 0.5f);

            // The arc starts at the top and sweeps clockwise, so the gap sits where the head
            // is about to arrive and the eye follows the direction of travel.
            for (var step = 0; step <= segments; step++)
            {
                var angle = Mathf.Deg2Rad * (90f - (sweepDegrees * step / segments));
                var cos = Mathf.Cos(angle);
                var sin = Mathf.Sin(angle);

                vertices.Add(new Vector3(inner * cos, inner * sin, 0f));
                vertices.Add(new Vector3(outer * cos, outer * sin, 0f));

                if (step == 0)
                {
                    continue;
                }

                var previousInner = (step - 1) * 2;
                var previousOuter = previousInner + 1;
                var currentInner = step * 2;
                var currentOuter = currentInner + 1;

                // Wound to face the camera at negative Z, like every other mesh here. The
                // first attempt had the arc facing away and only the arrow head rendered.
                triangles.Add(previousInner);
                triangles.Add(previousOuter);
                triangles.Add(currentInner);

                triangles.Add(previousOuter);
                triangles.Add(currentOuter);
                triangles.Add(currentInner);
            }

            // Arrow head at the end of the sweep, pointing along the tangent.
            var endAngle = Mathf.Deg2Rad * (90f - sweepDegrees);
            var centre = new Vector3(radius * Mathf.Cos(endAngle), radius * Mathf.Sin(endAngle), 0f);
            var tangent = new Vector3(Mathf.Sin(endAngle), -Mathf.Cos(endAngle), 0f);
            var normal = new Vector3(Mathf.Cos(endAngle), Mathf.Sin(endAngle), 0f);

            var headLength = thickness * 2.2f;
            var headHalfWidth = thickness * 1.3f;

            var headTip = vertices.Count;
            vertices.Add(centre + (tangent * headLength));
            vertices.Add(centre + (normal * headHalfWidth));
            vertices.Add(centre - (normal * headHalfWidth));

            triangles.Add(headTip);
            triangles.Add(headTip + 2);
            triangles.Add(headTip + 1);

            var mesh = new Mesh { name = "CircularArrow" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// A rectangle running from the origin along +X, used for a conduit spoke.
        /// </summary>
        /// <remarks>
        /// One mesh serves all six edges: the spoke is rotated into place rather than
        /// generating a separate mesh per direction.
        /// </remarks>
        internal static Mesh CreateSpoke(float length, float width)
        {
            var half = width * 0.5f;

            var mesh = new Mesh { name = "Spoke" };
            mesh.SetVertices(new List<Vector3>
            {
                new Vector3(0f, -half, 0f),
                new Vector3(0f, half, 0f),
                new Vector3(length, half, 0f),
                new Vector3(length, -half, 0f),
            });
            mesh.SetTriangles(new List<int> { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
