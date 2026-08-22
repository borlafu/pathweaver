namespace Pathweaver.Core.Tiles
{
    /// <summary>
    /// The resource a conduit carries, per PRD section 3.1.
    /// </summary>
    /// <remarks>
    /// Each kind flows from its own springs to its own hubs. Kinds never
    /// interconnect, so a route is always of a single kind.
    /// <para>
    /// Values appear in level JSON and in saved runs. Never renumber an existing
    /// member; append new ones.
    /// </para>
    /// </remarks>
    public enum ResourceKind
    {
        Water = 0,
        Wind = 1,
        Crystal = 2,
        Trade = 3,
    }
}
