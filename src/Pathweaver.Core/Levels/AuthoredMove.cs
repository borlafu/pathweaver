using Pathweaver.Core.Hex;

namespace Pathweaver.Core.Levels
{
    /// <summary>
    /// One step of a solution written down by whoever authored the level.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Either a placement at a cell or a skip. A rotation may be given, but usually is not: on a board
    /// that has one route, exactly one rotation of the held tile fits a given cell, so writing it down
    /// is transcription rather than information. <see cref="AuthoredSolution"/> finds it.
    /// </para>
    /// <para>
    /// Retrieving a conduit with a Pivot Token is deliberately not expressible. A solution that has to
    /// take a tile back off the board is a solution that took a wrong turn, and an authored one has no
    /// reason to.
    /// </para>
    /// </remarks>
    public readonly struct AuthoredMove
    {
        private AuthoredMove(bool isSkip, HexCoord at, int? rotation)
        {
            IsSkip = isSkip;
            At = at;
            Rotation = rotation;
        }

        /// <summary>Whether this step discards the held tile rather than placing it.</summary>
        public bool IsSkip { get; }

        /// <summary>Where the tile goes. Meaningless when <see cref="IsSkip"/> is true.</summary>
        public HexCoord At { get; }

        /// <summary>
        /// Which rotation to use, or null to use whichever one fits.
        /// </summary>
        public int? Rotation { get; }

        /// <summary>A placement, at whichever rotation of the held tile is legal there.</summary>
        public static AuthoredMove Place(HexCoord at) => new AuthoredMove(false, at, null);

        /// <summary>A placement at a rotation the author insisted on.</summary>
        public static AuthoredMove Place(HexCoord at, int rotation)
            => new AuthoredMove(false, at, rotation);

        /// <summary>Throwing the held tile away.</summary>
        public static AuthoredMove Skip() => new AuthoredMove(true, HexCoord.Zero, null);

        public override string ToString()
            => IsSkip
                ? "skip"
                : Rotation.HasValue ? $"place {At} turned {Rotation.Value}" : $"place {At}";
    }
}
