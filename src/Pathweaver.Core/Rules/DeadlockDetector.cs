using System;
using System.Collections.Generic;
using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Rules
{
    /// <summary>
    /// Whether the drawn tile can be played at all.
    /// </summary>
    /// <remarks>
    /// PRD section 3.2B treats deadlock frustration as a design failure rather
    /// than a challenge, so the game needs to know the moment a player is stuck.
    /// That is the trigger for offering a Pivot Token rather than letting someone
    /// stare at an unplayable board.
    /// </remarks>
    public static class DeadlockDetector
    {
        /// <summary>
        /// True when the tile fits nowhere, in any rotation.
        /// </summary>
        /// <remarks>
        /// Rotation is considered before declaring a deadlock. A tile that does not
        /// fit as drawn but fits once turned is a legitimate move, and treating it
        /// as stuck would both rob the player of that move and hand out a rescue
        /// that was not needed.
        /// </remarks>
        public static bool IsDeadlocked(
            HexGrid<ConduitTile> board, IEnumerable<FlowEndpoint> endpoints, ConduitTile tile)
            => PlacementRules.LegalPlacements(board, endpoints, tile).Count == 0;
    }
}
