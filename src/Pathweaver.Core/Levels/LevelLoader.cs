using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Levels
{
    /// <summary>
    /// Reads the hand-authored level format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A line-oriented <c>key: value</c> format rather than JSON. netstandard2.1
    /// carries no JSON reader in its BCL, and adding one as a package would mean a
    /// DLL to hand-manage inside Unity and another thing IL2CPP might strip. The
    /// schema is small enough that a bespoke reader is less risk than either a
    /// dependency or a hand-rolled JSON parser, and the result diffs cleanly in
    /// review.
    /// </para>
    /// <para>
    /// Every failure names the offending line, because levels are written by hand
    /// and an error that does not say where it is costs more than the mistake.
    /// </para>
    /// <example>
    /// <code>
    /// # comments and blank lines are ignored
    /// id: biome1-01
    /// name: First Waters
    /// base-score: 100
    /// target-score: 246
    /// tokens: 0
    /// shape: hexagon 3      # or one "cell: q,r" line per cell
    /// spring: -3,0 water
    /// hub: 2,0 water
    /// tile: 0,3 water x4    # edges, kind, and how many go in the bag
    /// </code>
    /// </example>
    /// </remarks>
    public static class LevelLoader
    {
        private const int MaximumRadius = 32;
        private const int MaximumTileCount = 256;

        public static LevelDefinition Parse(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new LevelFormatException("A level file cannot be empty.");
            }

            string? id = null;
            string? name = null;
            long? baseScore = null;
            long? targetScore = null;
            var startingTokens = 0;
            int? radius = null;
            var cells = new List<HexCoord>();
            var endpoints = new List<FlowEndpoint>();
            var bagTiles = new List<ConduitTile>();

            var lines = text.Replace("\r\n", "\n").Split('\n');
            for (var index = 0; index < lines.Length; index++)
            {
                var lineNumber = index + 1;
                var line = StripComment(lines[index]).Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                var separator = line.IndexOf(':');
                if (separator <= 0)
                {
                    throw new LevelFormatException(
                        $"Expected \"key: value\" but found \"{line}\".", lineNumber);
                }

                var key = line.Substring(0, separator).Trim().ToLowerInvariant();
                var value = line.Substring(separator + 1).Trim();

                switch (key)
                {
                    case "id":
                        id = RequireText(value, "id", lineNumber);
                        break;
                    case "name":
                        name = RequireText(value, "name", lineNumber);
                        break;
                    case "base-score":
                        baseScore = ParsePositiveNumber(value, "base-score", lineNumber);
                        break;
                    case "target-score":
                        targetScore = ParsePositiveNumber(value, "target-score", lineNumber);
                        break;
                    case "tokens":
                        startingTokens = (int)ParseNonNegativeNumber(value, "tokens", lineNumber);
                        break;
                    case "shape":
                        radius = ParseHexagonShape(value, lineNumber);
                        break;
                    case "cell":
                        cells.Add(ParseCoordinate(value, lineNumber));
                        break;
                    case "spring":
                        endpoints.Add(ParseEndpoint(value, EndpointRole.Spring, lineNumber));
                        break;
                    case "hub":
                        endpoints.Add(ParseEndpoint(value, EndpointRole.Hub, lineNumber));
                        break;
                    case "tile":
                        bagTiles.AddRange(ParseTiles(value, lineNumber));
                        break;
                    default:
                        throw new LevelFormatException($"Unknown key \"{key}\".", lineNumber);
                }
            }

            return Build(id, name, baseScore, targetScore, startingTokens, radius, cells, endpoints, bagTiles);
        }

        private static LevelDefinition Build(
            string? id,
            string? name,
            long? baseScore,
            long? targetScore,
            int startingTokens,
            int? radius,
            List<HexCoord> cells,
            List<FlowEndpoint> endpoints,
            List<ConduitTile> bagTiles)
        {
            if (id is null)
            {
                throw new LevelFormatException("A level needs an id.");
            }

            if (baseScore is null)
            {
                throw new LevelFormatException("A level needs a base-score.");
            }

            if (targetScore is null)
            {
                throw new LevelFormatException("A level needs a target-score.");
            }

            var shape = ResolveShape(radius, cells);

            if (endpoints.Count == 0)
            {
                throw new LevelFormatException("A level needs endpoints.");
            }

            if (!endpoints.Any(endpoint => endpoint.Role == EndpointRole.Spring))
            {
                throw new LevelFormatException("A level with no spring can never be completed.");
            }

            if (!endpoints.Any(endpoint => endpoint.Role == EndpointRole.Hub))
            {
                throw new LevelFormatException("A level with no hub can never be completed.");
            }

            if (bagTiles.Count == 0)
            {
                throw new LevelFormatException("A level needs at least one tile in its bag.");
            }

            var shapeCells = new HashSet<HexCoord>(shape);
            var endpointCells = new HashSet<HexCoord>();
            foreach (var endpoint in endpoints)
            {
                if (!shapeCells.Contains(endpoint.Coordinate))
                {
                    throw new LevelFormatException(
                        $"Endpoint at {endpoint.Coordinate} lies outside the board.");
                }

                if (!endpointCells.Add(endpoint.Coordinate))
                {
                    throw new LevelFormatException(
                        $"Cell {endpoint.Coordinate} carries more than one endpoint.");
                }
            }

            return new LevelDefinition(
                id,
                name ?? id,
                shape.ToArray(),
                endpoints.ToArray(),
                bagTiles.ToArray(),
                baseScore.Value,
                targetScore.Value,
                startingTokens);
        }

        private static List<HexCoord> ResolveShape(int? radius, List<HexCoord> cells)
        {
            // Explicit cells win, so a level can start from a hexagon and then be
            // reshaped by hand without the two definitions fighting.
            if (cells.Count > 0)
            {
                var seen = new HashSet<HexCoord>();
                foreach (var cell in cells)
                {
                    if (!seen.Add(cell))
                    {
                        throw new LevelFormatException($"Cell {cell} is listed more than once.");
                    }
                }

                return cells;
            }

            if (radius is null)
            {
                throw new LevelFormatException("A level needs a shape or at least one cell.");
            }

            var shape = new List<HexCoord>();
            for (var q = -radius.Value; q <= radius.Value; q++)
            {
                var lower = Math.Max(-radius.Value, -q - radius.Value);
                var upper = Math.Min(radius.Value, -q + radius.Value);
                for (var r = lower; r <= upper; r++)
                {
                    shape.Add(new HexCoord(q, r));
                }
            }

            return shape;
        }

        private static string StripComment(string line)
        {
            var hash = line.IndexOf('#');
            return hash < 0 ? line : line.Substring(0, hash);
        }

        private static string RequireText(string value, string key, int line)
        {
            if (value.Length == 0)
            {
                throw new LevelFormatException($"\"{key}\" needs a value.", line);
            }

            return value;
        }

        private static long ParsePositiveNumber(string value, string key, int line)
        {
            var number = ParseNumber(value, key, line);
            if (number < 1)
            {
                throw new LevelFormatException($"\"{key}\" must be at least 1, found {number}.", line);
            }

            return number;
        }

        private static long ParseNonNegativeNumber(string value, string key, int line)
        {
            var number = ParseNumber(value, key, line);
            if (number < 0)
            {
                throw new LevelFormatException($"\"{key}\" cannot be negative, found {number}.", line);
            }

            return number;
        }

        private static long ParseNumber(string value, string key, int line)
        {
            if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            {
                throw new LevelFormatException($"\"{key}\" expects a whole number, found \"{value}\".", line);
            }

            return number;
        }

        private static int ParseHexagonShape(string value, int line)
        {
            var parts = Split(value);
            if (parts.Length != 2 || !parts[0].Equals("hexagon", StringComparison.OrdinalIgnoreCase))
            {
                throw new LevelFormatException(
                    $"\"shape\" expects \"hexagon <radius>\", found \"{value}\".", line);
            }

            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var radius)
                || radius < 0
                || radius > MaximumRadius)
            {
                throw new LevelFormatException(
                    $"A hexagon radius must be between 0 and {MaximumRadius}, found \"{parts[1]}\".", line);
            }

            return radius;
        }

        private static HexCoord ParseCoordinate(string value, int line)
        {
            var parts = value.Split(',');
            if (parts.Length != 2
                || !int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var q)
                || !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var r))
            {
                throw new LevelFormatException(
                    $"Expected a coordinate as \"q,r\", found \"{value}\".", line);
            }

            return new HexCoord(q, r);
        }

        private static FlowEndpoint ParseEndpoint(string value, EndpointRole role, int line)
        {
            var parts = Split(value);
            if (parts.Length != 2)
            {
                throw new LevelFormatException(
                    $"Expected \"q,r <kind>\", found \"{value}\".", line);
            }

            var coordinate = ParseCoordinate(parts[0], line);
            var kind = ParseKind(parts[1], line);

            return role == EndpointRole.Spring
                ? FlowEndpoint.Spring(coordinate, kind)
                : FlowEndpoint.Hub(coordinate, kind);
        }

        private static IEnumerable<ConduitTile> ParseTiles(string value, int line)
        {
            var parts = Split(value);
            if (parts.Length < 2 || parts.Length > 3)
            {
                throw new LevelFormatException(
                    $"Expected \"<edges> <kind> [xN]\", found \"{value}\".", line);
            }

            var edges = ParseEdges(parts[0], line);
            var kind = ParseKind(parts[1], line);
            var count = parts.Length == 3 ? ParseRepeat(parts[2], line) : 1;

            ConduitTile tile;
            try
            {
                tile = new ConduitTile(kind, edges);
            }
            catch (ArgumentException error)
            {
                throw new LevelFormatException(error.Message, line, error);
            }

            return Enumerable.Repeat(tile, count);
        }

        private static EdgeMask ParseEdges(string value, int line)
        {
            var directions = new List<int>();
            foreach (var part in value.Split(','))
            {
                if (!int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var direction))
                {
                    throw new LevelFormatException(
                        $"Expected edge directions as \"0,3\", found \"{value}\".", line);
                }

                directions.Add(direction);
            }

            try
            {
                return EdgeMask.FromDirections(directions.ToArray());
            }
            catch (ArgumentException error)
            {
                throw new LevelFormatException(error.Message, line, error);
            }
        }

        private static int ParseRepeat(string value, int line)
        {
            if (value.Length < 2
                || (value[0] != 'x' && value[0] != 'X')
                || !int.TryParse(
                    value.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
            {
                throw new LevelFormatException(
                    $"Expected a tile count as \"x4\", found \"{value}\".", line);
            }

            if (count < 1 || count > MaximumTileCount)
            {
                throw new LevelFormatException(
                    $"A tile count must be between 1 and {MaximumTileCount}, found {count}.", line);
            }

            return count;
        }

        private static ResourceKind ParseKind(string value, int line)
        {
            foreach (ResourceKind kind in Enum.GetValues(typeof(ResourceKind)))
            {
                if (kind.ToString().Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    return kind;
                }
            }

            throw new LevelFormatException($"Unknown resource kind \"{value}\".", line);
        }

        private static string[] Split(string value)
            => value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }
}
