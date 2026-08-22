namespace Pathweaver.Core.Determinism
{
    /// <summary>
    /// The independent random streams a run draws from.
    /// </summary>
    /// <remarks>
    /// Each subsystem gets its own stream so that adding a consumer, or changing
    /// how often an existing one draws, cannot shift the numbers another
    /// subsystem sees. Without this, a tweak to objective generation would
    /// silently reshuffle every daily puzzle.
    /// <para>
    /// Values are part of the save and replay format. Never renumber an existing
    /// member; append new ones.
    /// </para>
    /// </remarks>
    public enum PathweaverStream
    {
        /// <summary>Grid shape, source springs, and destination hubs.</summary>
        GridLayout = 0,

        /// <summary>The order conduit tiles are drawn in.</summary>
        TileBag = 1,

        /// <summary>Quota targets and objective selection.</summary>
        Objectives = 2,

        /// <summary>Environmental rules such as frozen rivers or volcanic vents.</summary>
        Environment = 3,
    }
}
