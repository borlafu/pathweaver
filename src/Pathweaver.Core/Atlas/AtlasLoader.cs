using System;
using System.Collections.Generic;
using System.Globalization;
using Pathweaver.Core.Hex;

namespace Pathweaver.Core.Atlas
{
    /// <summary>
    /// Thrown when an atlas pack file cannot be read.
    /// </summary>
    public sealed class AtlasFormatException : Exception
    {
        public AtlasFormatException(string message, int line = 0)
            : base(line > 0 ? $"Line {line}: {message}" : message)
        {
            Line = line;
        }

        /// <summary>The line the fault was found on, or zero when it is about the whole file.</summary>
        public int Line { get; }
    }

    /// <summary>
    /// Reads an atlas pack: one line per node.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The format is deliberately one line per node rather than a block per node. A pack is a short
    /// list of small facts, and a flat line is both easier to author and easier to point a line number
    /// at when it is wrong.
    /// </para>
    /// <code>
    /// pack: biome1
    /// node: spring-well cost 2 at 0,0 gives skip 1
    /// node: deep-channel cost 3 at 1,0 gives token 1 needs spring-well
    /// </code>
    /// <para>
    /// A later pack docks onto an earlier one by naming the nodes it attaches to:
    /// </para>
    /// <code>
    /// pack: biome2
    /// docks: deep-channel
    /// node: frost-vein cost 5 at 2,0 gives skip 1 needs deep-channel
    /// </code>
    /// <para>
    /// The declaration is what makes a pack readable on its own. Without it, a prerequisite naming a
    /// node from another file is indistinguishable from a typo, and one of the two has to be an error.
    /// A dock that never arrives is caught when the packs are combined, which is at game start.
    /// </para>
    /// </remarks>
    public static class AtlasLoader
    {
        public static AtlasMap Parse(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var lines = text.Replace("\r\n", "\n").Split('\n');
            var nodes = new List<AtlasNode>();
            var docks = new List<string>();
            string? pack = null;

            for (var index = 0; index < lines.Length; index++)
            {
                var number = index + 1;
                var line = lines[index].Trim();

                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var separator = line.IndexOf(':');
                if (separator < 0)
                {
                    throw new AtlasFormatException($"\"{line}\" has no key.", number);
                }

                var key = line.Substring(0, separator).Trim();
                var value = line.Substring(separator + 1).Trim();

                switch (key)
                {
                    case "pack":
                        pack = RequireValue(value, "pack", number);
                        break;
                    case "docks":
                        docks.AddRange(
                            RequireValue(value, "docks", number)
                                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                        break;
                    case "node":
                        if (pack is null)
                        {
                            throw new AtlasFormatException("A node needs a pack declared above it.", number);
                        }

                        nodes.Add(ParseNode(value, pack, number));
                        break;
                    default:
                        throw new AtlasFormatException($"Unknown key \"{key}\".", number);
                }
            }

            if (pack is null)
            {
                throw new AtlasFormatException("An atlas pack must name itself with a pack line.");
            }

            return AtlasMap.Of(nodes, docks);
        }

        /// <summary>
        /// Reads one node line: <c>id cost N at Q,R gives EFFECT N [needs a,b]</c>.
        /// </summary>
        private static AtlasNode ParseNode(string value, string pack, int line)
        {
            var words = value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 8)
            {
                throw new AtlasFormatException(
                    "A node needs an identifier, a cost, a position, and an effect.", line);
            }

            var id = words[0];
            var cost = ReadKeyword(words, 1, "cost", line);
            var position = ReadPosition(words, 3, line);
            var effect = ReadEffect(words, 5, line);
            var requires = ReadRequirements(words, 8, line);

            if (cost < 1)
            {
                throw new AtlasFormatException("A node must cost at least one essence.", line);
            }

            return new AtlasNode(id, pack, cost, position, effect, requires);
        }

        private static int ReadKeyword(string[] words, int index, string keyword, int line)
        {
            if (index + 1 >= words.Length || words[index] != keyword)
            {
                throw new AtlasFormatException($"Expected \"{keyword} <number>\".", line);
            }

            if (!int.TryParse(words[index + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                throw new AtlasFormatException($"\"{words[index + 1]}\" is not a number.", line);
            }

            return value;
        }

        private static HexCoord ReadPosition(string[] words, int index, int line)
        {
            if (index + 1 >= words.Length || words[index] != "at")
            {
                throw new AtlasFormatException("Expected \"at <q>,<r>\".", line);
            }

            var parts = words[index + 1].Split(',');
            if (parts.Length != 2
                || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var q)
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var r))
            {
                throw new AtlasFormatException($"\"{words[index + 1]}\" is not a hex coordinate.", line);
            }

            return new HexCoord(q, r);
        }

        private static AtlasEffect ReadEffect(string[] words, int index, int line)
        {
            if (index + 2 >= words.Length || words[index] != "gives")
            {
                throw new AtlasFormatException(
                    "Expected \"gives <skip|token|essence|discount> <number>\".", line);
            }

            var kind = words[index + 1] switch
            {
                "skip" => AtlasEffectKind.Skip,
                "token" => AtlasEffectKind.Token,
                "essence" => AtlasEffectKind.Essence,
                "discount" => AtlasEffectKind.Discount,
                _ => throw new AtlasFormatException($"Unknown effect \"{words[index + 1]}\".", line),
            };

            if (!int.TryParse(words[index + 2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount))
            {
                throw new AtlasFormatException($"\"{words[index + 2]}\" is not a number.", line);
            }

            if (amount < 1)
            {
                throw new AtlasFormatException("A node that gives nothing is not worth unlocking.", line);
            }

            return new AtlasEffect(kind, amount);
        }

        private static string[] ReadRequirements(string[] words, int index, int line)
        {
            if (index >= words.Length)
            {
                return Array.Empty<string>();
            }

            if (words[index] != "needs" || index + 1 >= words.Length)
            {
                throw new AtlasFormatException("Expected \"needs <id>[,<id>]\" or nothing.", line);
            }

            var requires = words[index + 1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (requires.Length == 0)
            {
                throw new AtlasFormatException("\"needs\" was given nothing to need.", line);
            }

            return requires;
        }

        private static string RequireValue(string value, string key, int line)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new AtlasFormatException($"\"{key}\" needs a value.", line);
            }

            return value;
        }
    }
}
