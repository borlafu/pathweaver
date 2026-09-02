using System.Globalization;
using Pathweaver.Core.Atlas;

namespace Pathweaver.Game.Presentation.Text
{
    /// <summary>
    /// What the World Atlas says about itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The atlas was withheld from the closed test because it explained nothing: Star Essence, a node's
    /// cost, and what a relic actually does were all numbers the game had no font to write, so a player
    /// met a constellation of coloured hexagons and guessed. Nothing about the model was wrong, which is
    /// why nothing in <c>Pathweaver.Core.Atlas</c> changes — the whole gap was words.
    /// </para>
    /// <para>
    /// Copy rather than rules, so it lives in the presentation assembly. Every sentence is derived from
    /// the node it describes rather than authored per node, because a docking biome pack adds nodes to a
    /// file and must not have to add sentences to a switch.
    /// </para>
    /// </remarks>
    internal static class AtlasWords
    {
        /// <summary>What the game calls the currency the atlas is bought with.</summary>
        internal const string Essence = "Star Essence";

        /// <summary>
        /// The balance, as a line.
        /// </summary>
        /// <remarks>
        /// A number and its name, because the row of pips it replaces could say "some" and never "how
        /// many" — and the thing a player needs from a balance is whether it reaches a cost.
        /// </remarks>
        internal static string Balance(int essence)
            => $"{Number(essence)} {Essence}";

        /// <summary>
        /// A cost, as a phrase that can sit in a sentence.
        /// </summary>
        internal static string Cost(int cost) => $"{Number(cost)} {Essence}";

        /// <summary>
        /// What a node gives, in words.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Phrased as what changes on every board rather than as a quantity, because the effects are
        /// additive on top of a board's own allowance and "one more" says that where "1" does not.
        /// </para>
        /// <para>
        /// Essence is worded differently on purpose: the other two are things a player holds during a
        /// board, and this one is a rate paid at the end of one.
        /// </para>
        /// </remarks>
        internal static string Effect(AtlasEffect effect)
        {
            var count = Amount(effect.Amount);

            return effect.Kind switch
            {
                AtlasEffectKind.Token =>
                    $"{count} more Pivot {Plural(effect.Amount, "Token", "Tokens")} on every board.",
                AtlasEffectKind.Skip =>
                    $"{count} more {Plural(effect.Amount, "skip", "skips")} on every board.",
                _ => $"{count} more {Essence} for every board you clear.",
            };
        }

        /// <summary>
        /// Why a node can or cannot be bought right now.
        /// </summary>
        /// <remarks>
        /// A tap on an unaffordable node used to do nothing at all, on the grounds that its colour and
        /// its cost pips had already said so. They had not: the two unbuyable states looked alike, and
        /// silence is the one answer a player cannot learn anything from.
        /// </remarks>
        internal static string Status(AtlasNode node, AtlasMap map, AtlasProgress progress)
        {
            if (node == null || map == null || progress == null)
            {
                return string.Empty;
            }

            if (progress.IsUnlocked(node.Id))
            {
                return "Unlocked.";
            }

            if (!IsReachable(node, progress))
            {
                return "Unlock the node it grows from first.";
            }

            if (progress.Essence < node.Cost)
            {
                return $"Costs {Cost(node.Cost)}. You have {Number(progress.Essence)}.";
            }

            return $"Costs {Cost(node.Cost)}. Tap again to unlock it.";
        }

        /// <summary>
        /// What the atlas is for, shown before any node has been chosen.
        /// </summary>
        /// <remarks>
        /// Two facts and no more: where the currency comes from, and that a tap explains a node. A
        /// player who reads nothing else on this screen should still be able to start using it.
        /// </remarks>
        internal static string Introduction
            => $"Clearing a board earns {Essence}. Tap a node to see what it costs and what it gives.";

        /// <summary>
        /// The relics in force on a board, as one line, or empty when there are none.
        /// </summary>
        /// <remarks>
        /// Shown while paused rather than on the board itself. A fourth skip pip appearing in the drawer
        /// looked like the game being inconsistent, and the moment a player wants that explained is the
        /// moment they stop to ask how it is going — which is exactly what pausing is.
        /// </remarks>
        internal static string Relics(AtlasBonuses bonuses)
        {
            var parts = new System.Collections.Generic.List<string>(3);

            if (bonuses.Tokens > 0)
            {
                parts.Add($"+{Number(bonuses.Tokens)} Pivot {Plural(bonuses.Tokens, "Token", "Tokens")}");
            }

            if (bonuses.Skips > 0)
            {
                parts.Add($"+{Number(bonuses.Skips)} {Plural(bonuses.Skips, "skip", "skips")}");
            }

            if (bonuses.EssencePerClear > 0)
            {
                parts.Add($"+{Number(bonuses.EssencePerClear)} {Essence}");
            }

            return parts.Count == 0 ? string.Empty : $"Relics: {string.Join(", ", parts)}";
        }

        /// <summary>
        /// What a cleared board paid into the atlas.
        /// </summary>
        /// <remarks>
        /// The one thing the issue that reopened this screen asked for first: essence was paid silently,
        /// so a player had no way to connect a balance to anything they had done.
        /// </remarks>
        internal static string Earned(int harvested)
            => harvested <= 0 ? string.Empty : $"+{Number(harvested)} {Essence}";

        /// <summary>Whether every prerequisite of a node is already bought.</summary>
        private static bool IsReachable(AtlasNode node, AtlasProgress progress)
        {
            foreach (var required in node.Requires)
            {
                if (!progress.IsUnlocked(required))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// A count at the start of a sentence, where a digit reads worse than a word.
        /// </summary>
        /// <remarks>
        /// Only for one, and only here. Costs and balances stay numerals throughout, because those are
        /// figures a player compares rather than reads.
        /// </remarks>
        private static string Amount(int amount) => amount == 1 ? "One" : Number(amount);

        private static string Plural(int amount, string one, string many) => amount == 1 ? one : many;

        /// <summary>
        /// A number, grouped, in the one culture the font atlas covers.
        /// </summary>
        /// <remarks>
        /// The same formatting the score under the progress bar uses. Two renderings of a figure in one
        /// game read as two different kinds of figure.
        /// </remarks>
        private static string Number(int value)
            => value.ToString("N0", CultureInfo.InvariantCulture);
    }
}
