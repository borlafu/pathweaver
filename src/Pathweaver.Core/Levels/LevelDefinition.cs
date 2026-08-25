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
            ulong seed,
            int tokenCapacity = TokenRules.BaseCapacity,
            int skipCapacity = TokenRules.BaseCapacity)
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
            TokenCapacity = tokenCapacity;
            SkipCapacity = skipCapacity;
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
        /// The most Pivot Tokens this board lets the player hold.
        /// </summary>
        /// <remarks>
        /// Per level rather than one number for the game, because relics raise it: a board played
        /// with the whole atlas unlocked has a higher ceiling than the same board played before any
        /// of it. Authored level files do not set it — the ceiling is progression, not level design.
        /// </remarks>
        public int TokenCapacity { get; }

        /// <summary>The most skips this board lets the player hold.</summary>
        public int SkipCapacity { get; }

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
            => WithStartingResources(startingTokens, StartingSkips);

        /// <summary>
        /// The same level, dealt with different numbers of Pivot Tokens and skips in hand.
        /// </summary>
        /// <remarks>
        /// The second way a level's opening resources are raised: carried tokens travel between levels,
        /// and unlocked atlas relics add to every board. Both are the caller's arithmetic, so the level
        /// file keeps saying only what the level grants on its own.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown for a negative count.</exception>
        public LevelDefinition WithStartingResources(int startingTokens, int startingSkips)
            => WithStartingResources(startingTokens, startingSkips, TokenCapacity, SkipCapacity);

        /// <summary>
        /// The same level, dealt with different opening resources and different ceilings on them.
        /// </summary>
        /// <remarks>
        /// Ceilings are raised alongside the opening hand rather than separately, because the two
        /// come from the same place: a relic that deals a fourth token has to raise the ceiling to
        /// four, or it would hand the player something they could not hold. The opening hand is
        /// clamped to the ceiling here rather than refused, since a caller adding a carried count to
        /// a relic bonus has no business knowing where the band ends.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown for a negative count or a ceiling below one.
        /// </exception>
        public LevelDefinition WithStartingResources(
            int startingTokens, int startingSkips, int tokenCapacity, int skipCapacity)
        {
            if (startingTokens < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingTokens), startingTokens, "A level cannot start with fewer than no tokens.");
            }

            if (startingSkips < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingSkips), startingSkips, "A level cannot start with fewer than no skips.");
            }

            RequireCapacity(tokenCapacity, nameof(tokenCapacity));
            RequireCapacity(skipCapacity, nameof(skipCapacity));

            return new LevelDefinition(
                Id, Name, _shape, _endpoints, _bagTiles, BaseRouteScore, TargetScore,
                Math.Min(startingTokens, tokenCapacity), Math.Min(startingSkips, skipCapacity), Seed,
                tokenCapacity, skipCapacity);
        }

        public GameState CreateGame(ulong seed)
            => GameState.Create(
                HexGrid<ConduitTile>.FromShape(_shape),
                _endpoints,
                TileBag.Create(_bagTiles, SeedSource.Stream(seed, PathweaverStream.TileBag)),
                BaseRouteScore,
                TokenPool.Of(StartingTokens, TokenCapacity),
                TokenPool.Of(StartingSkips, SkipCapacity));

        /// <summary>
        /// Keeps a ceiling inside the band the rules define.
        /// </summary>
        /// <remarks>
        /// Checked here as well as where relics are counted, because this is the only door a ceiling
        /// enters a board through. A ceiling outside the band would be a save the next build cannot
        /// read — <see cref="Save.SaveGame"/> validates the same band — so it fails now rather than
        /// on the player's next launch.
        /// </remarks>
        private static void RequireCapacity(int capacity, string name)
        {
            if (capacity < 1 || capacity > TokenRules.MaximumCapacity)
            {
                throw new ArgumentOutOfRangeException(
                    name, capacity, $"A ceiling must lie between 1 and {TokenRules.MaximumCapacity}.");
            }
        }

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
