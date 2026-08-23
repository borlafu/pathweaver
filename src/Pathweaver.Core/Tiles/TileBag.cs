using System;
using System.Collections.Generic;
using System.Linq;
using Pathweaver.Core.Determinism;

namespace Pathweaver.Core.Tiles
{
    /// <summary>
    /// The endless supply a player draws conduit tiles from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Draws are a shuffled cycle rather than independent picks: the bag deals
    /// every tile in its definition once, then reshuffles and starts again. That
    /// bounds droughts. Independent picks could withhold a needed tile for an
    /// arbitrarily long run, manufacturing exactly the deadlock frustration PRD
    /// section 3.2B sets out to remove.
    /// </para>
    /// <para>
    /// A level weights its supply by repeating a tile in the definition. Three
    /// copies of a straight conduit against one bend means three straights per
    /// cycle, which is easier to reason about than a separate weight table.
    /// </para>
    /// <para>
    /// The bag is immutable. <see cref="Draw"/> returns the next bag alongside the
    /// tile, so holding a bag and drawing from it twice yields the same tile —
    /// which is what makes undo and replay-from-seed safe.
    /// </para>
    /// </remarks>
    public sealed class TileBag
    {
        private readonly ConduitTile[] _definition;
        private readonly ConduitTile[] _cycle;
        private readonly int _position;
        private readonly Pcg32 _generator;

        private TileBag(ConduitTile[] definition, ConduitTile[] cycle, int position, Pcg32 generator)
        {
            _definition = definition;
            _cycle = cycle;
            _position = position;
            _generator = generator;
        }

        /// <summary>
        /// How many tiles remain before the bag reshuffles.
        /// </summary>
        public int Remaining => _cycle.Length - _position;

        /// <summary>
        /// The distinct tiles this bag can ever deal.
        /// </summary>
        /// <remarks>
        /// Every cycle deals the whole definition, so this is what a player could reach by
        /// skipping — which is what makes it possible to ask whether skipping would help at all.
        /// Duplicates are collapsed because a repeated tile changes how often it appears, not
        /// whether it can appear.
        /// </remarks>
        public IEnumerable<ConduitTile> PossibleTiles => _definition.Distinct();

        /// <summary>
        /// Creates a bag from a tile definition and a generator.
        /// </summary>
        /// <param name="tiles">
        /// The supply for one cycle. Repeat a tile to make it more common.
        /// </param>
        /// <param name="generator">
        /// Should come from <see cref="SeedSource.Stream"/> with
        /// <see cref="PathweaverStream.TileBag"/>, so draw order is reproducible
        /// and independent of every other subsystem.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when no tiles are given, since an empty supply cannot deal.
        /// </exception>
        public static TileBag Create(IEnumerable<ConduitTile> tiles, Pcg32 generator)
        {
            if (tiles is null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            var definition = tiles.ToArray();
            if (definition.Length == 0)
            {
                throw new ArgumentException("A tile bag needs at least one tile.", nameof(tiles));
            }

            var (shuffled, advanced) = Shuffle(definition, generator);
            return new TileBag(definition, shuffled, 0, advanced);
        }

        /// <summary>
        /// The tiles one cycle deals, for serialisation.
        /// </summary>
        internal ConduitTile[] Definition => _definition;

        /// <summary>
        /// The current shuffled cycle, for serialisation. Saving this rather than
        /// reshuffling on load is what keeps a resumed run identical.
        /// </summary>
        internal ConduitTile[] Cycle => _cycle;

        internal int Position => _position;

        internal Pcg32 Generator => _generator;

        /// <summary>
        /// Rebuilds a bag mid-cycle from a save.
        /// </summary>
        internal static TileBag FromSnapshot(
            ConduitTile[] definition, ConduitTile[] cycle, int position, Pcg32 generator)
            => new TileBag(definition, cycle, position, generator);

        /// <summary>
        /// Deals the next tile, returning it alongside the bag that follows.
        /// </summary>
        public (TileBag Bag, ConduitTile Tile) Draw()
        {
            var cycle = _cycle;
            var position = _position;
            var generator = _generator;

            if (position >= cycle.Length)
            {
                (cycle, generator) = Shuffle(_definition, generator);
                position = 0;
            }

            var tile = cycle[position];
            return (new TileBag(_definition, cycle, position + 1, generator), tile);
        }

        /// <summary>
        /// Fisher-Yates, driven by the supplied generator.
        /// </summary>
        /// <remarks>
        /// Walking downward and drawing a bounded index gives a uniform
        /// permutation. The generator is threaded through and returned so the
        /// caller keeps the advanced state — nothing here draws from a shared
        /// mutable source.
        /// </remarks>
        private static (ConduitTile[] Shuffled, Pcg32 Generator) Shuffle(
            ConduitTile[] source, Pcg32 generator)
        {
            var shuffled = (ConduitTile[])source.Clone();

            for (var index = shuffled.Length - 1; index > 0; index--)
            {
                uint swapWith;
                (generator, swapWith) = generator.NextUInt32((uint)(index + 1));

                var held = shuffled[index];
                shuffled[index] = shuffled[swapWith];
                shuffled[swapWith] = held;
            }

            return (shuffled, generator);
        }
    }
}
