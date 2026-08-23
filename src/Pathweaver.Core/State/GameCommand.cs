using System;
using Pathweaver.Core.Hex;

namespace Pathweaver.Core.State
{
    /// <summary>
    /// Something a player does. The only way a game advances.
    /// </summary>
    /// <remarks>
    /// Routing every change through a command gives three things at once: a run
    /// replays from a seed and a command list, undo is a matter of keeping earlier
    /// states, and a bug report can carry the exact sequence that produced it.
    /// Commands validate their own shape on construction, so an impossible command
    /// cannot be built and then queued.
    /// </remarks>
    public abstract class GameCommand
    {
        internal GameCommand()
        {
        }
    }

    /// <summary>
    /// Places the held tile, turned clockwise by <see cref="Rotation"/> steps.
    /// </summary>
    public sealed class PlaceTile : GameCommand
    {
        public PlaceTile(HexCoord at, int rotation)
        {
            if (rotation < 0 || rotation > 5)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rotation), rotation, "Rotation must be between 0 and 5 steps.");
            }

            At = at;
            Rotation = rotation;
        }

        public HexCoord At { get; }

        public int Rotation { get; }

        public override string ToString() => $"Place at {At} turned {Rotation}";
    }

    /// <summary>
    /// Spends a Pivot Token to turn a conduit already on the board.
    /// </summary>
    public sealed class PivotRotate : GameCommand
    {
        public PivotRotate(HexCoord at, int rotation)
        {
            // Zero and six are the same non-move, and would charge a token for
            // nothing.
            if (rotation < 1 || rotation > 5)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rotation), rotation, "A pivot must turn the conduit by 1 to 5 steps.");
            }

            At = at;
            Rotation = rotation;
        }

        public HexCoord At { get; }

        public int Rotation { get; }

        public override string ToString() => $"Pivot {At} by {Rotation}";
    }

    /// <summary>
    /// Spends a Pivot Token to take a conduit off the board.
    /// </summary>
    /// <remarks>
    /// The retrieved conduit is discarded rather than returned to hand: the token
    /// buys back the space, not the tile. Returning it would mean deciding what
    /// happens to the tile already held, and no reading of PRD section 3.2B
    /// requires that complication.
    /// </remarks>
    public sealed class PivotRetrieve : GameCommand
    {
        public PivotRetrieve(HexCoord at)
        {
            At = at;
        }

        public HexCoord At { get; }

        public override string ToString() => $"Retrieve {At}";
    }
}
