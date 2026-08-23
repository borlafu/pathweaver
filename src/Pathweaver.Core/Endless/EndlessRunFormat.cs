using System;
using System.Globalization;

namespace Pathweaver.Core.Endless
{
    /// <summary>
    /// Reads and writes an endless run as text.
    /// </summary>
    /// <remarks>
    /// One line of three numbers, for the same reason campaign progress is text: it has to survive
    /// every future build, and a file a person can read and repair beats a compact one when there is
    /// nothing to compact.
    /// </remarks>
    public static class EndlessRunFormat
    {
        private const string Marker = "pathweaver-endless";

        /// <summary>The version this build writes, and the newest it reads.</summary>
        public const int FormatVersion = 1;

        public static string Write(EndlessRun run)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1}\n{2} {3} {4}\n",
                Marker,
                FormatVersion,
                run.Seed,
                run.Round,
                run.BestRound);
        }

        /// <summary>
        /// Reads a run, returning a fresh one for anything unreadable.
        /// </summary>
        /// <remarks>
        /// Never throws, following the campaign progress file. A damaged file costs the player their
        /// place in a run, which is recoverable by playing; refusing to open the mode is not.
        /// </remarks>
        /// <param name="text">The stored text, which may be empty or damaged.</param>
        /// <param name="fallbackSeed">The seed a fresh run starts with when nothing can be read.</param>
        public static EndlessRun Read(string text, ulong fallbackSeed = 1UL)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return EndlessRun.Start(fallbackSeed);
            }

            var lines = text.Replace("\r\n", "\n").Split('\n');
            if (lines.Length < 2)
            {
                return EndlessRun.Start(fallbackSeed);
            }

            var header = lines[0].Trim().Split(' ');
            if (header.Length != 2 || header[0] != Marker)
            {
                return EndlessRun.Start(fallbackSeed);
            }

            if (!int.TryParse(header[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var version)
                || version < 1
                || version > FormatVersion)
            {
                return EndlessRun.Start(fallbackSeed);
            }

            var fields = lines[1].Trim().Split(' ');
            if (fields.Length != 3)
            {
                return EndlessRun.Start(fallbackSeed);
            }

            if (!ulong.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed)
                || !int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var round)
                || !int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var best))
            {
                return EndlessRun.Start(fallbackSeed);
            }

            // A stored round below one means a damaged file rather than a choice, so it is treated
            // as one: keep the seed, which is still usable, and start the run again.
            if (round < 1)
            {
                return EndlessRun.Start(seed);
            }

            return EndlessRun.Of(seed, round, best);
        }
    }
}
