using System;

namespace Pathweaver.Core.Levels
{
    /// <summary>
    /// Thrown when a level file cannot be loaded.
    /// </summary>
    /// <remarks>
    /// Carries the line number because level files are hand-authored: an error
    /// that does not say where it is costs the author more than the error itself.
    /// <see cref="Line"/> is zero when the fault is the file as a whole rather
    /// than one line, such as a missing key or a level with no spring.
    /// </remarks>
    public sealed class LevelFormatException : Exception
    {
        public LevelFormatException(string message, int line = 0)
            : base(line > 0 ? $"Line {line}: {message}" : message)
        {
            Line = line;
        }

        public LevelFormatException(string message, int line, Exception innerException)
            : base(line > 0 ? $"Line {line}: {message}" : message, innerException)
        {
            Line = line;
        }

        /// <summary>The one-based line at fault, or zero for a whole-file problem.</summary>
        public int Line { get; }
    }
}
