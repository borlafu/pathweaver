using Pathweaver.Core.Hex;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Converts axial hex coordinates to world positions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pointy-top hexes, which suit a portrait phone: rows stack more tightly
    /// vertically than columns do horizontally, so more of the board fits above the
    /// thumb.
    /// </para>
    /// <para>
    /// The vertical axis is negated deliberately. With the textbook mapping and y
    /// pointing up, direction 1 lands north-east and stepping 0, 1, 2 sweeps
    /// counter-clockwise — which would make <c>HexCoord.RotateClockwise</c> a lie on
    /// screen. Negating y here keeps the simulation's names honest, at the cost of
    /// one sign in one place.
    /// </para>
    /// </remarks>
    internal static class HexMetrics
    {
        /// <summary>Distance from a hex centre to a corner, in world units.</summary>
        internal const float Size = 0.5f;

        private static readonly float Sqrt3 = Mathf.Sqrt(3f);

        /// <summary>
        /// The centre of a cell in world space.
        /// </summary>
        internal static Vector3 ToWorld(HexCoord coordinate)
            => new Vector3(
                Size * Sqrt3 * (coordinate.Q + (coordinate.R * 0.5f)),
                -Size * 1.5f * coordinate.R,
                0f);

        /// <summary>
        /// The direction a given edge faces, as a unit vector in world space.
        /// </summary>
        /// <remarks>
        /// Derived from the neighbour offset rather than from an angle table, so it
        /// cannot drift out of step with <c>HexCoord.Directions</c>.
        /// </remarks>
        internal static Vector3 EdgeDirection(int edge)
        {
            var neighbour = ToWorld(HexCoord.Zero.Neighbour(edge));
            return neighbour.normalized;
        }

        /// <summary>
        /// The centre-to-centre distance between neighbouring cells.
        /// </summary>
        internal static float CellSpacing => Size * Sqrt3;

        /// <summary>
        /// The cell containing a world position.
        /// </summary>
        /// <remarks>
        /// Inverts <see cref="ToWorld"/>, then rounds in cube space: rounding the two
        /// axial components independently can land on a cell that does not touch the
        /// point, because axial axes are not perpendicular. Cube rounding picks the
        /// nearest centre by discarding whichever component moved furthest and
        /// rebuilding it from the other two.
        /// </remarks>
        internal static HexCoord FromWorld(Vector3 position)
        {
            var fractionalR = -position.y / (Size * 1.5f);
            var fractionalQ = (position.x / (Size * Sqrt3)) - (fractionalR * 0.5f);

            return RoundToCell(fractionalQ, fractionalR);
        }

        private static HexCoord RoundToCell(float fractionalQ, float fractionalR)
        {
            // Axial to cube, where the three components always sum to zero.
            var cubeX = fractionalQ;
            var cubeZ = fractionalR;
            var cubeY = -cubeX - cubeZ;

            var roundedX = Mathf.RoundToInt(cubeX);
            var roundedY = Mathf.RoundToInt(cubeY);
            var roundedZ = Mathf.RoundToInt(cubeZ);

            var driftX = Mathf.Abs(roundedX - cubeX);
            var driftY = Mathf.Abs(roundedY - cubeY);
            var driftZ = Mathf.Abs(roundedZ - cubeZ);

            // Rebuild the component that moved furthest, so the sum returns to zero
            // without pulling the result away from the nearest centre.
            if (driftX > driftY && driftX > driftZ)
            {
                roundedX = -roundedY - roundedZ;
            }
            else if (driftZ > driftY)
            {
                roundedZ = -roundedX - roundedY;
            }

            return new HexCoord(roundedX, roundedZ);
        }
    }
}
