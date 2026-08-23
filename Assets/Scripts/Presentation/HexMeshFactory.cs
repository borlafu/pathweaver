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
