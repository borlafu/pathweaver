using Pathweaver.Core.Hex;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Game.App
{
    /// <summary>
    /// What one route just earned, and where it earned it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The board could show a score going up and leave a player to work out which of their routes did
    /// it. This carries the hub so the number can appear where the resources arrived, which is the only
    /// place it means anything.
    /// </para>
    /// <para>
    /// The amount is the difference a pair was actually paid rather than what its length is worth, so a
    /// pair extended from three conduits to five shows the gap. Showing the gross figure would credit a
    /// player with something they had already been given.
    /// </para>
    /// </remarks>
    internal readonly struct Payout
    {
        internal Payout(HexCoord hub, long amount, ResourceKind kind)
        {
            Hub = hub;
            Amount = amount;
            Kind = kind;
        }

        /// <summary>Where the resources arrived.</summary>
        internal HexCoord Hub { get; }

        /// <summary>How much this route paid, over what its pair had already been paid.</summary>
        internal long Amount { get; }

        /// <summary>Which resource paid, so the number can be drawn in its colour.</summary>
        internal ResourceKind Kind { get; }
    }
}
