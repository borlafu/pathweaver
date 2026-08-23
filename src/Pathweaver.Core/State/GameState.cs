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
            PivotTokenPool pivotTokens,
            long score,
            long baseRouteScore,
            HashSet<CompletedRoute> completedRoutes)
        {
            Board = board;
            _endpoints = endpoints;
            Bag = bag;
            HeldTile = heldTile;
            PivotTokens = pivotTokens;
            Score = score;
            BaseRouteScore = baseRouteScore;
            _completedRoutes = completedRoutes;
        }

        public HexGrid<ConduitTile> Board { get; }

        public IReadOnlyList<FlowEndpoint> Endpoints => _endpoints;

        public TileBag Bag { get; }

        /// <summary>The tile awaiting placement. A game always holds one.</summary>
        public ConduitTile HeldTile { get; }

        public PivotTokenPool PivotTokens { get; }

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
        /// Whether the held tile fits nowhere, which is when a Pivot Token becomes
        /// the player's way out.
        /// </summary>
        public bool IsDeadlocked => LegalPlacements.Count == 0;

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
            PivotTokenPool startingTokens)
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
                startingTokens,
                score: 0,
                baseRouteScore,
                new HashSet<CompletedRoute>());
        }

        /// <summary>
        /// Produces the next state. Used by <see cref="GameEngine"/> only, so that
        /// commands remain the single route by which a game changes.
        /// </summary>
        internal GameState With(
            HexGrid<ConduitTile>? board = null,
            TileBag? bag = null,
            ConduitTile? heldTile = null,
            PivotTokenPool? pivotTokens = null,
            long? score = null,
            IEnumerable<CompletedRoute>? completedRoutes = null)
            => new GameState(
                board ?? Board,
                _endpoints,
                bag ?? Bag,
                heldTile ?? HeldTile,
                pivotTokens ?? PivotTokens,
                score ?? Score,
                BaseRouteScore,
                completedRoutes is null
                    ? new HashSet<CompletedRoute>(_completedRoutes)
                    : new HashSet<CompletedRoute>(completedRoutes));

        internal bool HasPaidOut(CompletedRoute route) => _completedRoutes.Contains(route);

        internal IEnumerable<CompletedRoute> PaidOutRoutes() => _completedRoutes;
    }
}
