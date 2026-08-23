using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Pathweaver.Core.Atlas
{
    /// <summary>
    /// Reads and writes atlas progress as text.
    /// </summary>
    /// <remarks>
    /// Text, for the same reason campaign progress is: this file has to survive every future build,
    /// and one a person can read and repair beats a compact one when there is nothing to compact.
    /// </remarks>
    public static class AtlasProgressFormat
    {
        private const string Marker = "pathweaver-atlas";
        private const string EssencePrefix = "essence ";

        /// <summary>The version this build writes, and the newest it reads.</summary>
        public const int FormatVersion = 1;

        public static string Write(AtlasProgress progress)
        {
            if (progress is null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            var text = new StringBuilder();
            text.Append(Marker).Append(' ').Append(FormatVersion).Append('\n');
            text.Append(EssencePrefix).Append(progress.Essence.ToString(CultureInfo.InvariantCulture)).Append('\n');

            foreach (var id in progress.UnlockedNodes)
            {
                text.Append(id).Append('\n');
            }

            return text.ToString();
        }

        /// <summary>
        /// Reads progress, returning an empty atlas for anything unreadable.
        /// </summary>
        /// <remarks>
        /// Never throws. An unrecognised node identifier is kept rather than dropped: a node renamed
        /// or removed between builds must not silently refund itself, and must not cost the player the
        /// essence they spent on it either. <see cref="AtlasMap.BonusesFor"/> ignores nodes the build
        /// does not ship, so a kept record grants nothing until its pack returns.
        /// </remarks>
        public static AtlasProgress Read(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return AtlasProgress.Empty;
            }

            var lines = text.Replace("\r\n", "\n").Split('\n');

            var header = lines[0].Trim().Split(' ');
            if (header.Length != 2 || header[0] != Marker)
            {
                return AtlasProgress.Empty;
            }

            if (!int.TryParse(header[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var version)
                || version < 1
                || version > FormatVersion)
            {
                return AtlasProgress.Empty;
            }

            var unlocked = new List<string>();
            var essence = 0;

            for (var index = 1; index < lines.Length; index++)
            {
                var line = lines[index].Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (line.StartsWith(EssencePrefix, StringComparison.Ordinal))
                {
                    int.TryParse(
                        line.Substring(EssencePrefix.Length).Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out essence);

                    continue;
                }

                unlocked.Add(line);
            }

            return AtlasProgress.Of(unlocked, essence);
        }
    }
}
