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
            int startingTokens,
            int startingSkips,
            ulong seed)
        {
            Id = id;
            Name = name;
            _shape = shape;
            _endpoints = endpoints;
            _bagTiles = bagTiles;
            BaseRouteScore = baseRouteScore;
            TargetScore = targetScore;
            StartingTokens = startingTokens;
            StartingSkips = startingSkips;
            Seed = seed;
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
        /// Skips the player starts with.
        /// </summary>
        /// <remarks>
        /// Granted up front rather than earned first, because the first awkward draw can
        /// arrive before the first completed route.
        /// </remarks>
        public int StartingSkips { get; }

        /// <summary>
        /// The seed this level is played at.
        /// </summary>
        /// <remarks>
        /// A handcrafted level is authored against one tile order, and that order is part of the
        /// puzzle: the same board with a different draw sequence is a different problem, and not
        /// necessarily a solvable one. Endless Wayfare and the Daily Expedition take their seeds
        /// from elsewhere, because generated boards must work for a seed nobody chose.
        /// </remarks>
        public ulong Seed { get; }

        /// <summary>Builds a fresh game at the level's own seed.</summary>
        public GameState CreateGame() => CreateGame(Seed);

        /// <summary>
        /// The same level, dealt with a different number of Pivot Tokens in hand.
        /// </summary>
        /// <remarks>
        /// How a carried token reaches a level. Progress holds the count that travels between levels
        /// and hands it here, which keeps the level file describing what the level grants on its own
        /// and keeps the carrying rule in one place. Callers take the larger of the two, so an
        /// authored allowance is a floor rather than something a carried count can undercut.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown for a negative count.</exception>
        public LevelDefinition WithStartingTokens(int startingTokens)
        {
            if (startingTokens < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingTokens), startingTokens, "A level cannot start with fewer than no tokens.");
            }

            return new LevelDefinition(
                Id, Name, _shape, _endpoints, _bagTiles, BaseRouteScore, TargetScore,
                startingTokens, StartingSkips, Seed);
        }

        public GameState CreateGame(ulong seed)
            => GameState.Create(
                HexGrid<ConduitTile>.FromShape(_shape),
                _endpoints,
                TileBag.Create(_bagTiles, SeedSource.Stream(seed, PathweaverStream.TileBag)),
                BaseRouteScore,
                TokenPool.Of(StartingTokens),
                TokenPool.Of(StartingSkips));

        /// <summary>
        /// Whether a score clears this level's quota.
        /// </summary>
        /// <remarks>
        /// Here rather than in the presentation layer, which is where it first lived. What
        /// counts as finishing a level is a rule, and rules belong with the simulation where
        /// they can be tested without Unity — otherwise the one thing the whole level is
        /// judged by would be the one thing CI never checks.
        /// </remarks>
        public bool IsClearedBy(long score) => score >= TargetScore;

        public override string ToString() => $"{Id} ({Shape.Count} cells, target {TargetScore})";
    }
}
