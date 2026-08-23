using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Pathweaver.Core.Determinism;
using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Rules;
using Pathweaver.Core.State;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Save
{
    /// <summary>
    /// Reads and writes an in-progress run as a compact versioned binary payload.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A full snapshot, not a replay log. Since the simulation is deterministic, a
    /// save could have been just the seed plus the command list — far smaller and
    /// self-validating. It was rejected because any rules change in a future update
    /// would invalidate every in-progress run, and losing a player's board to an
    /// app update is a worse outcome than carrying a few hundred bytes.
    /// </para>
    /// <para>
    /// The bag's generator state travels with the save. Reshuffling on load would
    /// make a resumed Daily Expedition diverge from every other player's the moment
    /// someone suspended the app.
    /// </para>
    /// <para>
    /// No serialisation library is used, keeping Pathweaver.Core free of NuGet
    /// dependencies so nothing extra has to be imported into Unity and nothing extra
    /// can be stripped by IL2CPP.
    /// </para>
    /// </remarks>
    public static class SaveGame
    {
        /// <summary>The version this build writes.</summary>
        public const int FormatVersion = 2;

        /// <summary>
        /// The oldest version this build can still read.
        /// </summary>
        /// <remarks>
        /// Version 1 predates skips. Reading it rather than rejecting it is the point of
        /// having a version field: a player mid-run through an update keeps their board,
        /// and the missing skips are filled in from the level's own starting count.
        /// </remarks>
        public const int MinimumReadableVersion = 1;

        /// <summary>The version that first carried a skip count.</summary>
        private const int SkipsAddedInVersion = 2;

        private const int HeaderLength = 8;

        /// <summary>
        /// Skips granted when loading a save written before they existed.
        /// </summary>
        /// <remarks>
        /// Matches the loader's default, so a resumed old run is no worse off than a fresh
        /// one.
        /// </remarks>
        private const int DefaultSkipsForOldSaves = 3;

        /// <summary>
        /// Guards against a corrupt length field allocating wildly. No level comes
        /// close.
        /// </summary>
        private const int MaximumCount = 4096;

        private static readonly byte[] Marker = Encoding.ASCII.GetBytes("PWSV");

        /// <summary>
        /// Encodes a game. The same state always produces the same bytes, so an
        /// autosave can skip writing when nothing changed.
        /// </summary>
        public static byte[] Write(GameState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            using var buffer = new MemoryStream();
            using var writer = new BinaryWriter(buffer, Encoding.ASCII, leaveOpen: true);

            writer.Write(Marker);
            writer.Write(FormatVersion);

            writer.Write(state.BaseRouteScore);
            writer.Write(state.Score);
            writer.Write(state.PivotTokens.Count);
            writer.Write(state.SkipTokens.Count);

            WriteShape(writer, state.Board);
            WritePlacedTiles(writer, state.Board);
            WriteEndpoints(writer, state.Endpoints);
            WriteTile(writer, state.HeldTile);
            WriteBag(writer, state.Bag);
            WriteCompletedRoutes(writer, state.CompletedRoutes);

            writer.Flush();
            return buffer.ToArray();
        }

        /// <summary>
        /// Reads the version a payload declares, without decoding the rest.
        /// </summary>
        /// <remarks>
        /// Lets a caller tell "written by a newer build" from "corrupt" before
        /// attempting a load, so the message shown to the player can be accurate.
        /// </remarks>
        /// <exception cref="SaveFormatException">
        /// Thrown when the payload is too short or lacks the marker.
        /// </exception>
        public static int ReadFormatVersion(byte[] data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.Length < HeaderLength)
            {
                throw new SaveFormatException(
                    $"Save data is {data.Length} bytes, too short to contain a header.");
            }

            for (var index = 0; index < Marker.Length; index++)
            {
                if (data[index] != Marker[index])
                {
                    throw new SaveFormatException("Data is not a Pathweaver save.");
                }
            }

            return BitConverter.ToInt32(data, Marker.Length);
        }

        /// <summary>
        /// Decodes a game.
        /// </summary>
        /// <exception cref="SaveFormatException">
        /// Thrown for anything that is not a wholly valid save of a supported
        /// version. Failing outright is deliberate: a partially loaded game would
        /// look playable while being wrong, which is worse than starting fresh.
        /// </exception>
        public static GameState Read(byte[] data)
        {
            var version = ReadFormatVersion(data);
            if (version < MinimumReadableVersion || version > FormatVersion)
            {
                throw new SaveFormatException(
                    $"Save format version {version} is not supported; this build reads " +
                    $"{MinimumReadableVersion} to {FormatVersion}.");
            }

            try
            {
                return ReadPayload(data, version);
            }
            catch (SaveFormatException)
            {
                throw;
            }
            catch (Exception error)
            {
                // Truncation surfaces as EndOfStreamException, corruption as
                // argument failures from the types being rebuilt. Both mean the
                // same thing to the caller.
                throw new SaveFormatException("Save data is corrupt or truncated.", error);
            }
        }

        private static GameState ReadPayload(byte[] data, int version)
        {
            using var buffer = new MemoryStream(data, writable: false);
            using var reader = new BinaryReader(buffer, Encoding.ASCII, leaveOpen: true);

            reader.ReadBytes(HeaderLength);

            var baseRouteScore = reader.ReadInt64();
            var score = reader.ReadInt64();
            var pivotTokens = reader.ReadInt32();

            // A version 1 save has no skip count. Defaulting rather than failing is what
            // keeps an in-progress run alive across the update that added them.
            var skipTokens = version >= SkipsAddedInVersion ? reader.ReadInt32() : DefaultSkipsForOldSaves;

            var shape = ReadShape(reader);
            var board = HexGrid<ConduitTile>.FromShape(shape);

            var placedCount = ReadCount(reader, "placed conduits");
            for (var index = 0; index < placedCount; index++)
            {
                var coordinate = ReadCoordinate(reader);
                var tile = ReadTile(reader);
                board = board.Place(coordinate, tile);
            }

            var endpoints = ReadEndpoints(reader);
            var heldTile = ReadTile(reader);
            var bag = ReadBag(reader);
            var completedRoutes = ReadCompletedRoutes(reader, endpoints);

            return GameState.Restore(
                board, endpoints, bag, heldTile, TokenPool.Of(pivotTokens), TokenPool.Of(skipTokens),
                score, baseRouteScore, completedRoutes);
        }

        private static void WriteShape(BinaryWriter writer, HexGrid<ConduitTile> board)
        {
            writer.Write(board.Coordinates.Count);
            foreach (var coordinate in board.Coordinates)
            {
                WriteCoordinate(writer, coordinate);
            }
        }

        private static List<HexCoord> ReadShape(BinaryReader reader)
        {
            var count = ReadCount(reader, "board cells");
            if (count == 0)
            {
                throw new SaveFormatException("A saved board has no cells.");
            }

            var shape = new List<HexCoord>(count);
            for (var index = 0; index < count; index++)
            {
                shape.Add(ReadCoordinate(reader));
            }

            return shape;
        }

        private static void WritePlacedTiles(BinaryWriter writer, HexGrid<ConduitTile> board)
        {
            writer.Write(board.OccupiedCount);
            foreach (var (coordinate, tile) in board.OccupiedCells)
            {
                WriteCoordinate(writer, coordinate);
                WriteTile(writer, tile);
            }
        }

        private static void WriteEndpoints(BinaryWriter writer, IReadOnlyList<FlowEndpoint> endpoints)
        {
            writer.Write(endpoints.Count);
            foreach (var endpoint in endpoints)
            {
                WriteCoordinate(writer, endpoint.Coordinate);
                writer.Write((int)endpoint.Kind);
                writer.Write((int)endpoint.Role);
            }
        }

        private static FlowEndpoint[] ReadEndpoints(BinaryReader reader)
        {
            var count = ReadCount(reader, "endpoints");
            if (count == 0)
            {
                throw new SaveFormatException("A saved level has no endpoints.");
            }

            var endpoints = new FlowEndpoint[count];
            for (var index = 0; index < count; index++)
            {
                var coordinate = ReadCoordinate(reader);
                var kind = ReadResourceKind(reader);
                var role = reader.ReadInt32();

                endpoints[index] = role switch
                {
                    (int)EndpointRole.Spring => FlowEndpoint.Spring(coordinate, kind),
                    (int)EndpointRole.Hub => FlowEndpoint.Hub(coordinate, kind),
                    _ => throw new SaveFormatException($"Unknown endpoint role {role}."),
                };
            }

            return endpoints;
        }

        private static void WriteBag(BinaryWriter writer, TileBag bag)
        {
            WriteTiles(writer, bag.Definition);
            WriteTiles(writer, bag.Cycle);
            writer.Write(bag.Position);

            var (state, increment) = bag.Generator.Snapshot();
            writer.Write(state);
            writer.Write(increment);
        }

        private static TileBag ReadBag(BinaryReader reader)
        {
            var definition = ReadTiles(reader, "bag definition");
            if (definition.Length == 0)
            {
                throw new SaveFormatException("A saved tile bag has no tiles.");
            }

            var cycle = ReadTiles(reader, "bag cycle");
            var position = reader.ReadInt32();
            if (position < 0 || position > cycle.Length)
            {
                throw new SaveFormatException(
                    $"Bag position {position} lies outside a cycle of {cycle.Length} tiles.");
            }

            var generator = Pcg32.FromSnapshot(reader.ReadUInt64(), reader.ReadUInt64());
            return TileBag.FromSnapshot(definition, cycle, position, generator);
        }

        private static void WriteTiles(BinaryWriter writer, ConduitTile[] tiles)
        {
            writer.Write(tiles.Length);
            foreach (var tile in tiles)
            {
                WriteTile(writer, tile);
            }
        }

        private static ConduitTile[] ReadTiles(BinaryReader reader, string what)
        {
            var count = ReadCount(reader, what);
            var tiles = new ConduitTile[count];
            for (var index = 0; index < count; index++)
            {
                tiles[index] = ReadTile(reader);
            }

            return tiles;
        }

        private static void WriteCompletedRoutes(
            BinaryWriter writer, IReadOnlyCollection<CompletedRoute> routes)
        {
            // Ordered so the encoding stays stable: a hash set enumerates in
            // whatever order it likes, which would make identical games produce
            // different bytes.
            var ordered = routes
                .OrderBy(route => route.Spring.Q)
                .ThenBy(route => route.Spring.R)
                .ThenBy(route => route.Hub.Q)
                .ThenBy(route => route.Hub.R)
                .ToList();

            writer.Write(ordered.Count);
            foreach (var route in ordered)
            {
                WriteCoordinate(writer, route.Spring);
                WriteCoordinate(writer, route.Hub);
            }
        }

        private static List<CompletedRoute> ReadCompletedRoutes(
            BinaryReader reader, FlowEndpoint[] endpoints)
        {
            var count = ReadCount(reader, "completed routes");
            var springs = endpoints
                .Where(endpoint => endpoint.Role == EndpointRole.Spring)
                .Select(endpoint => endpoint.Coordinate)
                .ToHashSet();
            var hubs = endpoints
                .Where(endpoint => endpoint.Role == EndpointRole.Hub)
                .Select(endpoint => endpoint.Coordinate)
                .ToHashSet();

            var routes = new List<CompletedRoute>(count);
            for (var index = 0; index < count; index++)
            {
                var spring = ReadCoordinate(reader);
                var hub = ReadCoordinate(reader);

                // A payout recorded against a pair the level does not have means the
                // data is wrong, and letting it through would silently suppress a
                // payout the player is owed.
                if (!springs.Contains(spring) || !hubs.Contains(hub))
                {
                    throw new SaveFormatException(
                        $"Completed route {spring} to {hub} does not match this level's endpoints.");
                }

                routes.Add(new CompletedRoute(spring, hub));
            }

            return routes;
        }

        private static void WriteCoordinate(BinaryWriter writer, HexCoord coordinate)
        {
            writer.Write(coordinate.Q);
            writer.Write(coordinate.R);
        }

        private static HexCoord ReadCoordinate(BinaryReader reader)
            => new HexCoord(reader.ReadInt32(), reader.ReadInt32());

        private static void WriteTile(BinaryWriter writer, ConduitTile tile)
        {
            writer.Write((int)tile.Kind);
            writer.Write(tile.Edges.Bits);
        }

        private static ConduitTile ReadTile(BinaryReader reader)
        {
            var kind = ReadResourceKind(reader);
            var bits = reader.ReadByte();

            try
            {
                return new ConduitTile(kind, EdgeMask.FromBits(bits));
            }
            catch (ArgumentException error)
            {
                throw new SaveFormatException($"Conduit with edge bits {bits} is not valid.", error);
            }
        }

        private static ResourceKind ReadResourceKind(BinaryReader reader)
        {
            var value = reader.ReadInt32();
            if (!Enum.IsDefined(typeof(ResourceKind), value))
            {
                throw new SaveFormatException($"Unknown resource kind {value}.");
            }

            return (ResourceKind)value;
        }

        private static int ReadCount(BinaryReader reader, string what)
        {
            var count = reader.ReadInt32();
            if (count < 0 || count > MaximumCount)
            {
                throw new SaveFormatException($"Implausible {what} count of {count}.");
            }

            return count;
        }
    }
}
