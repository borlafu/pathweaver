using Pathweaver.Core.Tiles;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Placeholder colours for the board.
    /// </summary>
    /// <remarks>
    /// Programmer art, and knowingly incomplete: colour alone must not be the only
    /// way to tell resources apart, since four kinds flow through visually similar
    /// conduits and colour-blind players would lose exactly that distinction. The
    /// shape or pattern encoding that fixes it belongs to the accessibility pass in
    /// #28; these values are chosen to stay distinguishable under the common forms of
    /// colour blindness in the meantime.
    /// </remarks>
    internal static class BoardPalette
    {
        internal static readonly Color EmptyCell = new Color(0.22f, 0.24f, 0.29f);
        internal static readonly Color CellOutline = new Color(0.32f, 0.35f, 0.41f);
        /// <summary>
        /// An empty cell the held tile could legally occupy.
        /// </summary>
        /// <remarks>
        /// Showing where a tile may go is not a hint, it is the placement rule made
        /// visible. Requiring conduits to join the network is invisible otherwise, and
        /// a player who cannot see it reads a refused tap as the game ignoring them.
        /// </remarks>
        internal static readonly Color LegalCell = new Color(0.30f, 0.36f, 0.44f);

        internal static readonly Color Spring = new Color(0.95f, 0.85f, 0.35f);
        internal static readonly Color Hub = new Color(0.55f, 0.40f, 0.85f);

        internal static Color ForKind(ResourceKind kind) => kind switch
        {
            ResourceKind.Water => new Color(0.25f, 0.60f, 0.90f),
            ResourceKind.Wind => new Color(0.60f, 0.85f, 0.75f),
            ResourceKind.Crystal => new Color(0.85f, 0.45f, 0.65f),
            ResourceKind.Trade => new Color(0.90f, 0.60f, 0.25f),
            _ => Color.magenta,
        };
    }
}
