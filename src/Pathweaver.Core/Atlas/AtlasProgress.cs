using System;
using System.Collections.Generic;
using System.Linq;

namespace Pathweaver.Core.Atlas
{
    /// <summary>
    /// Star Essence in hand, and which nodes it has already bought.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A value, like every other kind of progress here: unlocking a node returns a new progress
    /// rather than mutating one, which keeps it saveable, comparable, and testable without a device.
    /// </para>
    /// <para>
    /// It records node identifiers rather than positions or indices, so a pack can add nodes, and a
    /// future build can reorder them, without re-locking or mis-crediting anything.
    /// </para>
    /// </remarks>
    public sealed class AtlasProgress
    {
        private readonly HashSet<string> _unlocked;

        private AtlasProgress(HashSet<string> unlocked, int essence)
        {
            _unlocked = unlocked;
            Essence = essence;
        }

        public static AtlasProgress Empty
            => new AtlasProgress(new HashSet<string>(StringComparer.Ordinal), essence: 0);

        /// <summary>Star Essence not yet spent.</summary>
        public int Essence { get; }

        /// <summary>Unlocked node identifiers, in a stable order.</summary>
        public IReadOnlyList<string> UnlockedNodes
            => _unlocked.OrderBy(id => id, StringComparer.Ordinal).ToList();

        /// <summary>
        /// Rebuilds progress from stored values, correcting anything impossible.
        /// </summary>
        public static AtlasProgress Of(IEnumerable<string> unlockedNodes, int essence)
        {
            if (unlockedNodes is null)
            {
                throw new ArgumentNullException(nameof(unlockedNodes));
            }

            var unlocked = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in unlockedNodes)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    unlocked.Add(id.Trim());
                }
            }

            return new AtlasProgress(unlocked, Math.Max(0, essence));
        }

        public bool IsUnlocked(string nodeId) => nodeId != null && _unlocked.Contains(nodeId);

        /// <summary>Adds harvested essence.</summary>
        public AtlasProgress WithEssence(int harvested)
            => new AtlasProgress(
                new HashSet<string>(_unlocked, StringComparer.Ordinal),
                Essence + Math.Max(0, harvested));

        /// <summary>
        /// Records a node as unlocked and spends its cost.
        /// </summary>
        /// <remarks>
        /// Unlocking one already unlocked changes nothing rather than charging again, and a cost
        /// beyond the balance clamps to zero rather than going negative. Neither should happen —
        /// <see cref="AtlasMap.CanUnlock"/> is what callers ask first — but a damaged file can
        /// produce both, and neither is worth refusing to open the atlas over.
        /// </remarks>
        public AtlasProgress WithUnlocked(string nodeId, int cost)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                throw new ArgumentException("A node identifier is required.", nameof(nodeId));
            }

            var id = nodeId.Trim();
            if (_unlocked.Contains(id))
            {
                return this;
            }

            var unlocked = new HashSet<string>(_unlocked, StringComparer.Ordinal) { id };
            return new AtlasProgress(unlocked, Math.Max(0, Essence - Math.Max(0, cost)));
        }

        public override string ToString() => $"{_unlocked.Count} nodes, {Essence} essence";
    }
}
