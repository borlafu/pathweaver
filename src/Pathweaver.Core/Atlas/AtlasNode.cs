using System;
using System.Collections.Generic;
using Pathweaver.Core.Hex;

namespace Pathweaver.Core.Atlas
{
    /// <summary>
    /// What unlocking a node gives the player.
    /// </summary>
    /// <remarks>
    /// PRD section 4.2 calls these Relic Slots and gives "+1 tile redraw per stage" as an example,
    /// which is a skip. Values appear in atlas pack files and in saved progress, so never renumber an
    /// existing member; append new ones.
    /// </remarks>
    public enum AtlasEffectKind
    {
        /// <summary>Extra skips on every board.</summary>
        Skip = 0,

        /// <summary>Extra Pivot Tokens on every board.</summary>
        Token = 1,

        /// <summary>Extra Star Essence for every board cleared.</summary>
        Essence = 2,
    }

    /// <summary>An effect and how much of it.</summary>
    public readonly struct AtlasEffect : IEquatable<AtlasEffect>
    {
        public AtlasEffect(AtlasEffectKind kind, int amount)
        {
            if (amount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount), amount, "A node that gives nothing is not worth unlocking.");
            }

            Kind = kind;
            Amount = amount;
        }

        public AtlasEffectKind Kind { get; }

        public int Amount { get; }

        public bool Equals(AtlasEffect other) => Kind == other.Kind && Amount == other.Amount;

        public override bool Equals(object? obj) => obj is AtlasEffect other && Equals(other);

        public override int GetHashCode() => HashCode.Combine((int)Kind, Amount);

        public override string ToString() => $"{Kind} +{Amount}";
    }

    /// <summary>
    /// One node of the World Atlas constellation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nodes carry their own position, cost and prerequisites, and name their prerequisites by
    /// identifier rather than by index. That is what lets a biome pack dock onto the outer edge of
    /// the constellation by adding a file: nothing already shipped has to change, and nothing
    /// depends on how many nodes came before it.
    /// </para>
    /// <para>
    /// The position is a hex coordinate, because the constellation is drawn on the same grid the game
    /// is played on — one geometry, one set of maths, and a node that lines up with the board.
    /// </para>
    /// </remarks>
    public sealed class AtlasNode
    {
        private readonly string[] _requires;

        internal AtlasNode(string id, string pack, int cost, HexCoord position, AtlasEffect effect, string[] requires)
        {
            Id = id;
            Pack = pack;
            Cost = cost;
            Position = position;
            Effect = effect;
            _requires = requires;
        }

        public string Id { get; }

        /// <summary>The pack that shipped this node, for grouping and for diagnostics.</summary>
        public string Pack { get; }

        /// <summary>Star Essence this node costs to unlock.</summary>
        public int Cost { get; }

        public HexCoord Position { get; }

        public AtlasEffect Effect { get; }

        /// <summary>Nodes that must be unlocked first, in the order the file listed them.</summary>
        public IReadOnlyList<string> Requires => _requires;

        public override string ToString() => $"{Id} ({Cost} essence, {Effect})";
    }
}
