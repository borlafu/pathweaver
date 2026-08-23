using System;

namespace Pathweaver.Core.Save
{
    /// <summary>
    /// Thrown when saved data cannot be trusted.
    /// </summary>
    /// <remarks>
    /// One exception type for every failure — wrong marker, unsupported version,
    /// truncation, corruption — so the calling layer has a single thing to catch
    /// and a single decision to make: start a fresh run rather than load a game
    /// that is subtly wrong.
    /// </remarks>
    public sealed class SaveFormatException : Exception
    {
        public SaveFormatException(string message)
            : base(message)
        {
        }

        public SaveFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
