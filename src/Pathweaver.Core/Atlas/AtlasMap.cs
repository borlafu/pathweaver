using System;
using System.Collections.Generic;
using System.Linq;

namespace Pathweaver.Core.Atlas
{
    /// <summary>
    /// What unlocked nodes are worth on every board.
    /// </summary>
    /// <remarks>
    /// Additive rather than replacing a board's own allowance. A permanent upgrade that replaced it
    /// would make a generous level worse than a mean one, which is the opposite of an upgrade.
    /// </remarks>
    public readonly struct AtlasBonuses
    {
        internal AtlasBonuses(int skips, int tokens, int essencePerClear)
        {
            Skips = skips;
            Tokens = tokens;
            EssencePerClear = essencePerClear;
        }

        public static AtlasBonuses None => new AtlasBonuses(0, 0, 0);

        public int Skips { get; }

        public int Tokens { get; }

        public int EssencePerClear { get; }

        public override string ToString()
            => $"+{Skips} skips, +{Tokens} tokens, +{EssencePerClear} essence";
    }

    /// <summary>
    /// The World Atlas constellation: every node the build ships, from every pack.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Built from one or more pack files and validated on load, so an unreachable node, a duplicate
    /// identifier, or a cycle fails in CI rather than in a player's atlas.
    /// </para>
    /// <para>
    /// PRD section 4.2 asks that new biome packs dock onto the outer edge without reworking earlier
    /// biomes. That is what <see cref="Combine"/> is: packs are separate files, nodes name their
    /// prerequisites by identifier, and nothing already shipped changes when another pack arrives.
    /// </para>
    /// </remarks>
    public sealed class AtlasMap
    {
        private readonly Dictionary<string, AtlasNode> _nodes;
        private readonly HashSet<string> _docks;

        private AtlasMap(Dictionary<string, AtlasNode> nodes, HashSet<string> docks)
        {
            _nodes = nodes;
            _docks = docks;
        }

        /// <summary>Every node, ordered by pack and then by identifier.</summary>
        public IReadOnlyList<AtlasNode> Nodes
            => _nodes.Values
                .OrderBy(node => node.Pack, StringComparer.Ordinal)
                .ThenBy(node => node.Id, StringComparer.Ordinal)
                .ToList();

        /// <summary>
        /// Builds a map from validated nodes.
        /// </summary>
        /// <param name="nodes">The nodes this map holds.</param>
        /// <param name="docks">
        /// Identifiers this map expects another pack to supply. A prerequisite naming one of these is
        /// accepted here and checked when the packs are combined.
        /// </param>
        /// <exception cref="AtlasFormatException">
        /// Thrown for a duplicate identifier, a prerequisite that is neither present nor declared as a
        /// dock, or a cycle.
        /// </exception>
        internal static AtlasMap Of(IEnumerable<AtlasNode> nodes, IEnumerable<string>? docks = null)
        {
            var byId = new Dictionary<string, AtlasNode>(StringComparer.Ordinal);

            foreach (var node in nodes)
            {
                if (byId.ContainsKey(node.Id))
                {
                    throw new AtlasFormatException($"Two nodes share the identifier \"{node.Id}\".");
                }

                byId.Add(node.Id, node);
            }

            var expected = new HashSet<string>(docks ?? Enumerable.Empty<string>(), StringComparer.Ordinal);

            RequirePrerequisitesExist(byId, expected);
            RequireNoCycles(byId, expected);

            return new AtlasMap(byId, expected);
        }

        /// <summary>
        /// Merges packs into one constellation.
        /// </summary>
        /// <remarks>
        /// A pack may depend on a node from an earlier pack — a region docking onto the outer edge is
        /// exactly that — so validation happens across the whole set rather than per file.
        /// </remarks>
        public static AtlasMap Combine(params AtlasMap[] maps)
        {
            if (maps is null)
            {
                throw new ArgumentNullException(nameof(maps));
            }

            // No docks are carried forward: once the packs are together, every prerequisite has to be
            // a node somebody actually ships, and a dock that never arrived fails here.
            return Of(maps.SelectMany(map => map._nodes.Values));
        }

        /// <exception cref="KeyNotFoundException">Thrown when no pack ships that node.</exception>
        public AtlasNode Node(string nodeId)
        {
            if (nodeId is null || !_nodes.TryGetValue(nodeId, out var node))
            {
                throw new KeyNotFoundException($"No atlas node called \"{nodeId}\".");
            }

            return node;
        }

        public bool Contains(string nodeId) => nodeId != null && _nodes.ContainsKey(nodeId);

        /// <summary>
        /// Whether a node can be unlocked right now.
        /// </summary>
        /// <remarks>
        /// Prerequisites first, essence second. Both have to hold, and a node already unlocked is not
        /// available again — the caller does not have to know which of the three it failed, because
        /// the atlas shows cost and reachability on the node itself.
        /// </remarks>
        public bool CanUnlock(string nodeId, AtlasProgress progress)
        {
            if (progress is null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            if (!Contains(nodeId) || progress.IsUnlocked(nodeId))
            {
                return false;
            }

            var node = _nodes[nodeId];

            foreach (var required in node.Requires)
            {
                if (!progress.IsUnlocked(required))
                {
                    return false;
                }
            }

            return progress.Essence >= node.Cost;
        }

        /// <summary>
        /// Adds up what the unlocked nodes are worth.
        /// </summary>
        /// <remarks>
        /// Nodes the build does not ship are ignored. A player who unlocked a node from a pack that is
        /// later absent keeps the record, so it costs them nothing when the pack returns, but must not
        /// keep the bonus in the meantime.
        /// </remarks>
        public AtlasBonuses BonusesFor(AtlasProgress progress)
        {
            if (progress is null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            var skips = 0;
            var tokens = 0;
            var essence = 0;

            foreach (var id in progress.UnlockedNodes)
            {
                if (!_nodes.TryGetValue(id, out var node))
                {
                    continue;
                }

                switch (node.Effect.Kind)
                {
                    case AtlasEffectKind.Skip:
                        skips += node.Effect.Amount;
                        break;
                    case AtlasEffectKind.Token:
                        tokens += node.Effect.Amount;
                        break;
                    case AtlasEffectKind.Essence:
                        essence += node.Effect.Amount;
                        break;
                }
            }

            return new AtlasBonuses(skips, tokens, essence);
        }

        private static void RequirePrerequisitesExist(
            Dictionary<string, AtlasNode> byId, HashSet<string> docks)
        {
            foreach (var node in byId.Values)
            {
                foreach (var required in node.Requires)
                {
                    if (!byId.ContainsKey(required) && !docks.Contains(required))
                    {
                        throw new AtlasFormatException(
                            $"Node \"{node.Id}\" needs \"{required}\", which no pack ships and no docks line declares.");
                    }
                }
            }
        }

        /// <summary>
        /// Rejects a constellation containing a cycle.
        /// </summary>
        /// <remarks>
        /// Two nodes each needing the other can never be unlocked by anyone. Found by repeatedly
        /// removing nodes whose prerequisites are already accounted for: whatever is left when nothing
        /// more can be removed is exactly the part that depends on itself.
        /// </remarks>
        private static void RequireNoCycles(Dictionary<string, AtlasNode> byId, HashSet<string> docks)
        {
            // Docked nodes count as settled: they live in another pack, and whether they are reachable
            // is that pack's business and is checked once the two are combined.
            var settled = new HashSet<string>(docks, StringComparer.Ordinal);
            var remaining = new List<AtlasNode>(byId.Values);

            while (remaining.Count > 0)
            {
                var progressed = remaining
                    .Where(node => node.Requires.All(settled.Contains))
                    .ToList();

                if (progressed.Count == 0)
                {
                    var stuck = string.Join(", ", remaining.Select(node => node.Id).OrderBy(id => id, StringComparer.Ordinal));
                    throw new AtlasFormatException($"These nodes depend on each other and can never unlock: {stuck}.");
                }

                foreach (var node in progressed)
                {
                    settled.Add(node.Id);
                    remaining.Remove(node);
                }
            }
        }
    }
}
