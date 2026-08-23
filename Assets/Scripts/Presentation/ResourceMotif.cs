using System;
using Pathweaver.Core.Tiles;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// The shape that identifies a resource, alongside its colour.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Four resources flow through visually identical conduits, and they must never
    /// interconnect — so telling them apart is not decoration, it is how the board is read.
    /// Colour alone cannot carry that: the most common forms of colour blindness affect
    /// roughly one in twelve men, and a player who cannot separate two kinds sees a board
    /// that appears to break its own rules.
    /// </para>
    /// <para>
    /// Each kind therefore gets a distinct silhouette as well as a distinct colour, so the
    /// information survives when the hue does not. The shapes are chosen to differ in side
    /// count rather than only in proportion, because a triangle against a square reads at a
    /// glance where a wide rectangle against a narrow one does not.
    /// </para>
    /// </remarks>
    internal static class ResourceMotif
    {
        /// <summary>
        /// How large a motif is, relative to the hex's centre-to-corner distance.
        /// </summary>
        /// <remarks>
        /// Small enough to sit inside the conduit spokes without touching them, large enough
        /// to identify on a phone held at arm's length.
        /// </remarks>
        internal const float RadiusFraction = 0.34f;

        /// <summary>Sides of the shape drawn for a resource.</summary>
        internal static int SidesFor(ResourceKind kind) => kind switch
        {
            ResourceKind.Water => 16,   // a circle: fluid, no corners
            ResourceKind.Wind => 3,     // a triangle: directional, like a gust
            ResourceKind.Crystal => 4,  // a diamond, via the rotation below
            ResourceKind.Trade => 6,    // a hexagon: a coin, or a crate seen end-on
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown resource kind."),
        };

        /// <summary>
        /// How far the shape is turned, which is what separates a diamond from a square.
        /// </summary>
        internal static float RotationFor(ResourceKind kind) => kind switch
        {
            ResourceKind.Crystal => 45f,
            _ => 0f,
        };

        /// <summary>Builds the motif mesh for a resource at the given hex radius.</summary>
        internal static Mesh Create(ResourceKind kind, float hexRadius)
            => HexMeshFactory.CreateRegularPolygon(
                SidesFor(kind), hexRadius * RadiusFraction, RotationFor(kind));
    }
}
