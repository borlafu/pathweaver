using System;
using System.Collections.Generic;
using System.Linq;
using Pathweaver.Core.Determinism;
using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Rules;
using Pathweaver.Core.State;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Levels
{
    /// <summary>
    /// A level as authored: its board, its endpoints, its tile supply, and what
    /// counts as finishing it.
    /// </summary>
    /// <remarks>
    /// Authored data only. Turning it into a playable game needs a seed, which is
    /// what keeps the Daily Expedition reproducible while letting the same level
    /// be replayed with a different tile order elsewhere.
    /// </remarks>
    public sealed class LevelDefinition
    {
        private readonly HexCoord[] _shape;
        private readonly FlowEndpoint[] _endpoints;
        private readonly ConduitTile[] _bagTiles;

        internal LevelDefinition(
            string id,
            string name,
            HexCoord[] shape,
            FlowEndpoint[] endpoints,
            ConduitTile[] bagTiles,
            long baseRouteScore,
            long targetScore,
            int startingTokens)
        {
            Id = id;
            Name = name;
            _shape = shape;
            _endpoints = endpoints;
            _bagTiles = bagTiles;
            BaseRouteScore = baseRouteScore;
            TargetScore = targetScore;
            StartingTokens = startingTokens;
        }

        /// <summary>Stable identifier, used by progression and save data.</summary>
        public string Id { get; }

        /// <summary>Display name. Falls back to the identifier when unset.</summary>
        public string Name { get; }

        public IReadOnlyList<HexCoord> Shape => _shape;

        public IReadOnlyList<FlowEndpoint> Endpoints => _endpoints;

        /// <summary>
        /// One cycle of the tile supply. A repeated tile is a more common tile.
        /// </summary>
        public IReadOnlyList<ConduitTile> BagTiles => _bagTiles;

        public long BaseRouteScore { get; }

        /// <summary>The score that clears the level's quota.</summary>
        public long TargetScore { get; }

        public int StartingTokens { get; }

        /// <summary>
        /// Builds a fresh game of this level for the given seed.
        /// </summary>
        public GameState CreateGame(ulong seed)
            => GameState.Create(
                HexGrid<ConduitTile>.FromShape(_shape),
                _endpoints,
                TileBag.Create(_bagTiles, SeedSource.Stream(seed, PathweaverStream.TileBag)),
                BaseRouteScore,
                PivotTokenPool.Of(StartingTokens));

        public override string ToString() => $"{Id} ({Shape.Count} cells, target {TargetScore})";
    }
}
