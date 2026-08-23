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
        public const int FormatVersion = 1;

        public static string Write(CampaignProgress progress)
        {
            if (progress is null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            var text = new StringBuilder();
            text.Append(Marker).Append(' ').Append(FormatVersion).Append('\n');

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

            if (!int.TryParse(header[1], out var version) || version < 1 || version > FormatVersion)
            {
                return CampaignProgress.Empty;
            }

            var cleared = new List<string>();
            for (var index = 1; index < lines.Length; index++)
            {
                var id = lines[index].Trim();
                if (id.Length > 0)
                {
                    cleared.Add(id);
                }
            }

            return CampaignProgress.Of(cleared);
        }
    }
}
