using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Pathweaver.Core.Hex
{
    /// <summary>
    /// A bounded hex board of arbitrary shape, holding at most one value per
    /// cell.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every mutation returns a new grid rather than modifying this one. That is
    /// what makes Pivot Token retrieval, undo, and replay-from-seed cheap: a
    /// previous board state is simply a value still held somewhere, not
    /// something that must be reconstructed.
    /// </para>
    /// <para>
    /// The shape is a set of coordinates rather than a radius, because levels are
    /// handcrafted and irregular. <see cref="Hexagon"/> covers the regular case
    /// that procedural generation uses.
    /// </para>
    /// <para>
    /// Enumeration order is sorted and therefore stable across instances and
    /// across the order cells were authored in. Generation walks the grid, so an
    /// unstable order would produce different puzzles from the same seed.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">
    /// What occupies a cell. Kept generic so the grid carries no knowledge of
    /// tiles, and can be tested without them.
    /// </typeparam>
    public sealed class HexGrid<T>
        where T : notnull
    {
        private readonly HexCoord[] _coordinates;
        private readonly HashSet<HexCoord> _shape;
        private readonly Dictionary<HexCoord, T> _contents;

        private HexGrid(HexCoord[] coordinates, HashSet<HexCoord> shape, Dictionary<HexCoord, T> contents)
        {
            _coordinates = coordinates;
            _shape = shape;
            _contents = contents;
        }

        /// <summary>
        /// Every cell in the grid, in a stable order.
        /// </summary>
        public IReadOnlyList<HexCoord> Coordinates => _coordinates;

        /// <summary>
        /// The occupied cells and their values, in the same stable order.
        /// </summary>
        public IEnumerable<(HexCoord Coordinate, T Value)> OccupiedCells
        {
            get
            {
                foreach (var coordinate in _coordinates)
                {
                    if (_contents.TryGetValue(coordinate, out var value))
                    {
                        yield return (coordinate, value);
                    }
                }
            }
        }

        public int OccupiedCount => _contents.Count;

        /// <summary>
        /// True when no cell is free. The deadlock detector watches this.
        /// </summary>
        public bool IsFull => _contents.Count == _coordinates.Length;

        /// <summary>
        /// Builds a regular hexagon of the given radius, centred on the origin.
        /// Radius 0 is a single cell.
        /// </summary>
        public static HexGrid<T> Hexagon(int radius)
        {
            if (radius < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(radius), radius, "Radius cannot be negative.");
            }

            var cells = new List<HexCoord>();
            for (var q = -radius; q <= radius; q++)
            {
                var lower = Math.Max(-radius, -q - radius);
                var upper = Math.Min(radius, -q + radius);
                for (var r = lower; r <= upper; r++)
                {
                    cells.Add(new HexCoord(q, r));
                }
            }

            return FromShape(cells);
        }

        /// <summary>
        /// Builds a grid from an explicit set of cells, in any order.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when the shape is empty or contains a duplicate, both of which
        /// mean the level data is wrong rather than merely unusual.
        /// </exception>
        public static HexGrid<T> FromShape(IEnumerable<HexCoord> cells)
        {
            if (cells is null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            var materialised = cells.ToArray();
            if (materialised.Length == 0)
            {
                throw new ArgumentException("A grid needs at least one cell.", nameof(cells));
            }

            var shape = new HashSet<HexCoord>();
            foreach (var cell in materialised)
            {
                if (!shape.Add(cell))
                {
                    throw new ArgumentException(
                        $"Cell {cell} appears more than once in the shape.", nameof(cells));
                }
            }

            return new HexGrid<T>(Sorted(materialised), shape, new Dictionary<HexCoord, T>());
        }

        public bool Contains(HexCoord coordinate) => _shape.Contains(coordinate);

        public bool IsEmpty(HexCoord coordinate)
        {
            RequireInsideGrid(coordinate);
            return !_contents.ContainsKey(coordinate);
        }

        /// <summary>
        /// Reads the value at a cell, if it holds one.
        /// </summary>
        public bool TryGet(HexCoord coordinate, [MaybeNullWhen(false)] out T value)
        {
            RequireInsideGrid(coordinate);
            return _contents.TryGetValue(coordinate, out value);
        }

        /// <summary>
        /// Returns a new grid with <paramref name="value"/> at the given cell.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the cell is already occupied. Overwriting silently would
        /// discard a tile the player placed.
        /// </exception>
        public HexGrid<T> Place(HexCoord coordinate, T value)
        {
            RequireInsideGrid(coordinate);

            if (_contents.ContainsKey(coordinate))
            {
                throw new InvalidOperationException($"Cell {coordinate} is already occupied.");
            }

            var contents = new Dictionary<HexCoord, T>(_contents) { [coordinate] = value };
            return new HexGrid<T>(_coordinates, _shape, contents);
        }

        /// <summary>
        /// Returns a new grid with the given cell cleared.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the cell is already empty, so a Pivot Token spent on a
        /// retrieval cannot quietly do nothing.
        /// </exception>
        public HexGrid<T> Remove(HexCoord coordinate)
        {
            RequireInsideGrid(coordinate);

            if (!_contents.ContainsKey(coordinate))
            {
                throw new InvalidOperationException($"Cell {coordinate} is already empty.");
            }

            var contents = new Dictionary<HexCoord, T>(_contents);
            contents.Remove(coordinate);
            return new HexGrid<T>(_coordinates, _shape, contents);
        }

        /// <summary>
        /// The neighbours of a cell that fall inside the grid, in direction
        /// order.
        /// </summary>
        public IEnumerable<HexCoord> NeighboursOf(HexCoord coordinate)
        {
            RequireInsideGrid(coordinate);

            for (var direction = 0; direction < HexCoord.Directions.Count; direction++)
            {
                var neighbour = coordinate.Neighbour(direction);
                if (_shape.Contains(neighbour))
                {
                    yield return neighbour;
                }
            }
        }

        private static HexCoord[] Sorted(HexCoord[] cells)
        {
            var sorted = (HexCoord[])cells.Clone();
            Array.Sort(sorted, (left, right) =>
            {
                var byQ = left.Q.CompareTo(right.Q);
                return byQ != 0 ? byQ : left.R.CompareTo(right.R);
            });

            return sorted;
        }

        private void RequireInsideGrid(HexCoord coordinate)
        {
            if (!_shape.Contains(coordinate))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(coordinate), coordinate, "Coordinate lies outside the grid.");
            }
        }
    }
}
