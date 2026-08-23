using System;
using System.Collections.Generic;
using System.Text;

namespace Pathweaver.Core.Campaign
{
    /// <summary>
    /// Reads and writes campaign progress as text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Text rather than the binary format used for a run in progress. Progress is a short list
    /// of identifiers that has to survive every future version of the game, and a format a human
    /// can read and repair by hand is worth more here than a compact one.
    /// </para>
    /// <para>
    /// Unrecognised identifiers are kept rather than dropped. A player who moves between builds
    /// should not lose credit for a level that was renamed or temporarily removed.
    /// </para>
    /// </remarks>
    public static class CampaignProgressFormat
    {
        private const string Marker = "pathweaver-progress";

        /// <summary>The version this build writes, and the newest it reads.</summary>
        /// <remarks>
        /// Version 2 adds a line for the Pivot Tokens the player is carrying between levels. Version
        /// 1 files still read, with nothing carried: a player updating mid-campaign keeps every level
        /// they cleared, which is the part that took time.
        /// </remarks>
        public const int FormatVersion = 2;

        /// <summary>The oldest version this build can still read.</summary>
        public const int MinimumReadableVersion = 1;

        /// <summary>Prefix of the line carrying the token count.</summary>
        private const string TokensPrefix = "tokens ";

        public static string Write(CampaignProgress progress)
        {
            if (progress is null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            var text = new StringBuilder();
            text.Append(Marker).Append(' ').Append(FormatVersion).Append('\n');

            // Prefixed rather than positional, so a level identifier can never be mistaken for the
            // count and the file stays repairable by hand.
            text.Append(TokensPrefix).Append(progress.PivotTokens).Append('\n');

            foreach (var id in progress.ClearedLevels)
            {
                text.Append(id).Append('\n');
            }

            return text.ToString();
        }

        /// <summary>
        /// Reads progress, returning empty progress for anything unreadable.
        /// </summary>
        /// <remarks>
        /// Never throws. Losing a campaign to a damaged file is bad; refusing to start the game
        /// because of one is worse, and there is nothing a player could do about it either way.
        /// </remarks>
        public static CampaignProgress Read(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return CampaignProgress.Empty;
            }

            var lines = text.Replace("\r\n", "\n").Split('\n');

            var header = lines[0].Trim().Split(' ');
            if (header.Length != 2 || header[0] != Marker)
            {
                return CampaignProgress.Empty;
            }

            if (!int.TryParse(header[1], out var version)
                || version < MinimumReadableVersion
                || version > FormatVersion)
            {
                return CampaignProgress.Empty;
            }

            var cleared = new List<string>();
            var pivotTokens = 0;

            for (var index = 1; index < lines.Length; index++)
            {
                var line = lines[index].Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (line.StartsWith(TokensPrefix, StringComparison.Ordinal))
                {
                    // A damaged count means nothing carried, not a lost campaign.
                    int.TryParse(line.Substring(TokensPrefix.Length).Trim(), out pivotTokens);
                    continue;
                }

                cleared.Add(line);
            }

            return CampaignProgress.Of(cleared, pivotTokens);
        }
    }
}
