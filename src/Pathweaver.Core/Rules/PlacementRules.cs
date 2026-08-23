using System;
using System.Collections.Generic;
using System.Linq;
using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Rules
{
    /// <summary>
    /// A cell and rotation a tile may legally be placed at.
    /// </summary>
    public readonly struct TilePlacement
    {
        internal TilePlacement(HexCoord coordinate, int rotation, ConduitTile tile)
        {
            Coordinate = coordinate;
            Rotation = rotation;
            Tile = tile;
        }

        public HexCoord Coordinate { get; }

        /// <summary>Clockwise rotation steps applied to the drawn tile, 0 to 5.</summary>
        public int Rotation { get; }

        /// <summary>The already-rotated tile, ready to place.</summary>
        public ConduitTile Tile { get; }

        public override string ToString() => $"{Tile} at {Coordinate} turned {Rotation}";
    }

    /// <summary>
    /// Where a drawn conduit may go.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A placement is legal when the cell is empty, is not occupied by a spring or
    /// hub, and the tile connects to at least one neighbouring conduit or endpoint
    /// of its own resource kind with facing open edges.
    /// </para>
    /// <para>
    /// The connection requirement is what gives deadlock its meaning. Under free
    /// placement a player could always drop a tile on any empty cell, so the only
    /// dead end would be a full board and Pivot Tokens would have little to
    /// rescue. Requiring growth from an existing network produces the grid
    /// congestion and deadlock risk PRD section 3.2A describes as the cost of
    /// extended routing.
    /// </para>
    /// </remarks>
    public static class PlacementRules
    {
        /// <summary>
        /// Whether the given tile may be placed at the given cell, exactly as
        /// oriented.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the cell lies outside the board. Asking about a cell that
        /// does not exist is a caller mistake, not a placement that happens to be
        /// illegal.
        /// </exception>
        public static bool IsLegal(
            HexGrid<ConduitTile> board,
            IEnumerable<FlowEndpoint> endpoints,
            HexCoord at,
            ConduitTile tile)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (endpoints is null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (!board.Contains(at))
            {
                throw new ArgumentOutOfRangeException(nameof(at), at, "Cell lies outside the board.");
            }

            var endpointsByCell = ByCell(endpoints);
            return IsLegalCore(board, endpointsByCell, at, tile);
        }

        /// <summary>
        /// Every cell and rotation the tile may be placed at, ordered by cell then
        /// rotation.
        /// </summary>
        /// <remarks>
        /// Rotations are considered because the player may turn a tile freely
        /// before committing it. The ordering is stable so the solver and
        /// generation see the same list on every device.
        /// </remarks>
        public static IReadOnlyList<TilePlacement> LegalPlacements(
            HexGrid<ConduitTile> board, IEnumerable<FlowEndpoint> endpoints, ConduitTile tile)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (endpoints is null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            var endpointsByCell = ByCell(endpoints);
            var placements = new List<TilePlacement>();

            foreach (var coordinate in board.Coordinates)
            {
                for (var rotation = 0; rotation < HexCoord.Directions.Count; rotation++)
                {
                    var rotated = tile.RotateClockwise(rotation);
                    if (!IsLegalCore(board, endpointsByCell, coordinate, rotated))
                    {
                        continue;
                    }

                    placements.Add(new TilePlacement(coordinate, rotation, rotated));
                }
            }

            return placements;
        }

        private static bool IsLegalCore(
            HexGrid<ConduitTile> board,
            Dictionary<HexCoord, FlowEndpoint> endpointsByCell,
            HexCoord at,
            ConduitTile tile)
        {
            if (endpointsByCell.ContainsKey(at) || !board.IsEmpty(at))
            {
                return false;
            }

            foreach (var direction in tile.Edges.OpenDirections)
            {
                var neighbour = at.Neighbour(direction);
                if (!board.Contains(neighbour))
                {
                    continue;
                }

                // Joining a spring or hub: the tile's open edge faces it, and the
                // resource matches.
                if (endpointsByCell.TryGetValue(neighbour, out var endpoint))
                {
                    if (endpoint.Kind == tile.Kind)
                    {
                        return true;
                    }

                    continue;
                }

                // Joining an existing conduit: both edges face each other and the
                // resource matches, which ConnectsTo already checks.
                if (board.TryGet(neighbour, out var neighbourTile)
                    && tile.ConnectsTo(neighbourTile, direction))
                {
                    return true;
                }
            }

            return false;
        }

        private static Dictionary<HexCoord, FlowEndpoint> ByCell(IEnumerable<FlowEndpoint> endpoints)
            => endpoints.ToDictionary(endpoint => endpoint.Coordinate);
    }
}
