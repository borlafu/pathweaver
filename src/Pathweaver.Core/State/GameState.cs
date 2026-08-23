using System;
using System.Collections.Generic;
using System.Linq;
using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Rules;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.State
{
    /// <summary>
    /// A spring and hub pair whose route has already paid out.
    /// </summary>
    public readonly struct CompletedRoute : IEquatable<CompletedRoute>
    {
        internal CompletedRoute(HexCoord spring, HexCoord hub)
        {
            Spring = spring;
            Hub = hub;
        }

        public HexCoord Spring { get; }

        public HexCoord Hub { get; }

        public bool Equals(CompletedRoute other)
            => Spring.Equals(other.Spring) && Hub.Equals(other.Hub);

        public override bool Equals(object? obj) => obj is CompletedRoute other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Spring, Hub);

        public override string ToString() => $"{Spring} to {Hub}";
    }

    /// <summary>
    /// Everything about a game in progress, as one immutable value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing here can be modified. <see cref="GameEngine"/> produces the next
    /// state from a command, so an earlier state stays valid forever. Undo keeps
    /// old values, replay reapplies commands to a fresh start, and the level solver
    /// explores branches without needing to unwind anything.
    /// </para>
    /// <para>
    /// <see cref="CompletedRoutes"/> records which pairs have paid out, because
    /// <see cref="FlowResolver"/> reports every currently completed route each time
    /// it runs. Without that record, a player could retrieve one conduit and
    /// replace it to be paid again for the same route.
    /// </para>
    /// </remarks>
    public sealed class GameState
    {
        private readonly FlowEndpoint[] _endpoints;
        private readonly HashSet<CompletedRoute> _completedRoutes;

        private GameState(
            HexGrid<ConduitTile> board,
            FlowEndpoint[] endpoints,
            TileBag bag,
            ConduitTile heldTile,
            TokenPool pivotTokens,
            TokenPool skipTokens,
            long score,
            long baseRouteScore,
            HashSet<CompletedRoute> completedRoutes)
        {
            Board = board;
            _endpoints = endpoints;
            Bag = bag;
            HeldTile = heldTile;
            PivotTokens = pivotTokens;
            SkipTokens = skipTokens;
            Score = score;
            BaseRouteScore = baseRouteScore;
            _completedRoutes = completedRoutes;
        }

        public HexGrid<ConduitTile> Board { get; }

        public IReadOnlyList<FlowEndpoint> Endpoints => _endpoints;

        public TileBag Bag { get; }

        /// <summary>The tile awaiting placement. A game always holds one.</summary>
        public ConduitTile HeldTile { get; }

        public TokenPool PivotTokens { get; }

        /// <summary>
        /// Skips available, each discarding the tile in hand for the next one.
        /// </summary>
        /// <remarks>
        /// A second way out of an awkward draw, alongside rotation. Without it the only
        /// answer to a tile that fits nowhere useful is to place it somewhere wasteful,
        /// which is a decision with no thought in it.
        /// </remarks>
        public TokenPool SkipTokens { get; }

        public long Score { get; }

        /// <summary>
        /// The unmultiplied score a completed route earns, before the length curve.
        /// </summary>
        public long BaseRouteScore { get; }

        public IReadOnlyCollection<CompletedRoute> CompletedRoutes => _completedRoutes;

        /// <summary>
        /// Where the held tile may go, including rotations.
        /// </summary>
        /// <remarks>
        /// Computed on demand rather than cached, so it cannot fall out of step
        /// with the board. Boards are small enough that this costs nothing worth
        /// optimising.
        /// </remarks>
        public IReadOnlyList<TilePlacement> LegalPlacements
            => PlacementRules.LegalPlacements(Board, _endpoints, HeldTile);

        /// <summary>
        /// Whether the held tile fits nowhere.
        /// </summary>
        /// <remarks>
        /// A deadlock is about this tile, not about the run. A player holding a skip may well have
        /// a way forward — see <see cref="IsStuck"/> for whether they actually do.
        /// </remarks>
        public bool IsDeadlocked => LegalPlacements.Count == 0;

        /// <summary>
        /// Whether any tile the bag could deal has a legal placement.
        /// </summary>
        /// <remarks>
        /// The question a skip really asks. Every cycle deals the whole definition, so if nothing in
        /// it fits, skipping only spends tokens on the same answer.
        /// </remarks>
        public bool CanAnyTileBePlaced
        {
            get
            {
                foreach (var tile in Bag.PossibleTiles)
                {
                    if (PlacementRules.LegalPlacements(Board, _endpoints, tile).Count > 0)
                    {
                        return true;
                    }
                }

                return LegalPlacements.Count > 0;
            }
        }

        /// <summary>
        /// Whether the run is over: nothing can be placed and nothing the player holds changes that.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Distinct from <see cref="IsDeadlocked"/>, and the distinction matters. Holding a skip is
        /// only an option if some tile in the bag fits somewhere; on a board where none do, skipping
        /// spends a token to be told the same thing again. Holding a Pivot Token is only an option if
        /// there is a placed conduit to rotate or retrieve.
        /// </para>
        /// <para>
        /// This was originally judged by counting tokens, which meant a player with skips on a board
        /// that could accept nothing was told they had options they did not have.
        /// </para>
        /// </remarks>
        public bool IsStuck
            => IsDeadlocked
               && !(SkipTokens.CanSpend && CanAnyTileBePlaced)
               && !(PivotTokens.CanSpend && Board.OccupiedCount > 0);

        /// <summary>
        /// Starts a game and draws the first tile.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when there are no endpoints, or two share a cell.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when an endpoint lies outside the board, or the base score is
        /// below one.
        /// </exception>
        public static GameState Create(
            HexGrid<ConduitTile> board,
            IEnumerable<FlowEndpoint> endpoints,
            TileBag bag,
            long baseRouteScore,
            TokenPool startingPivotTokens,
            TokenPool startingSkipTokens)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (endpoints is null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (bag is null)
            {
                throw new ArgumentNullException(nameof(bag));
            }

            if (baseRouteScore < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseRouteScore), baseRouteScore, "A route must be worth at least one point.");
            }

            var materialised = endpoints.ToArray();
            if (materialised.Length == 0)
            {
                throw new ArgumentException(
                    "A level needs at least one spring and one hub.", nameof(endpoints));
            }

            var cells = new HashSet<HexCoord>();
            foreach (var endpoint in materialised)
            {
                if (!board.Contains(endpoint.Coordinate))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(endpoints), endpoint.Coordinate, $"Endpoint {endpoint} lies outside the board.");
                }

                if (!cells.Add(endpoint.Coordinate))
                {
                    throw new ArgumentException(
                        $"Cell {endpoint.Coordinate} carries more than one endpoint.", nameof(endpoints));
                }
            }

            var (remainingBag, heldTile) = bag.Draw();

            return new GameState(
                board,
                materialised,
                remainingBag,
                heldTile,
                startingPivotTokens,
                startingSkipTokens,
                score: 0,
                baseRouteScore,
                new HashSet<CompletedRoute>());
        }

        /// <summary>
        /// Rebuilds a game from a save, without drawing a tile.
        /// </summary>
        /// <remarks>
        /// Distinct from <see cref="Create"/> because a resumed game must keep the
        /// tile the player was already holding rather than draw a fresh one, which
        /// would both change the tile and advance the bag.
        /// </remarks>
        internal static GameState Restore(
            HexGrid<ConduitTile> board,
            FlowEndpoint[] endpoints,
            TileBag bag,
            ConduitTile heldTile,
            TokenPool pivotTokens,
            TokenPool skipTokens,
            long score,
            long baseRouteScore,
            IEnumerable<CompletedRoute> completedRoutes)
            => new GameState(
                board,
                endpoints,
                bag,
                heldTile,
                pivotTokens,
                skipTokens,
                score,
                baseRouteScore,
                new HashSet<CompletedRoute>(completedRoutes));

        /// <summary>
        /// Produces the next state. Used by <see cref="GameEngine"/> only, so that
        /// commands remain the single route by which a game changes.
        /// </summary>
        internal GameState With(
            HexGrid<ConduitTile>? board = null,
            TileBag? bag = null,
            ConduitTile? heldTile = null,
            TokenPool? pivotTokens = null,
            TokenPool? skipTokens = null,
            long? score = null,
            IEnumerable<CompletedRoute>? completedRoutes = null)
            => new GameState(
                board ?? Board,
                _endpoints,
                bag ?? Bag,
                heldTile ?? HeldTile,
                pivotTokens ?? PivotTokens,
                skipTokens ?? SkipTokens,
                score ?? Score,
                BaseRouteScore,
                completedRoutes is null
                    ? new HashSet<CompletedRoute>(_completedRoutes)
                    : new HashSet<CompletedRoute>(completedRoutes));

        internal bool HasPaidOut(CompletedRoute route) => _completedRoutes.Contains(route);

        internal IEnumerable<CompletedRoute> PaidOutRoutes() => _completedRoutes;
    }
}
