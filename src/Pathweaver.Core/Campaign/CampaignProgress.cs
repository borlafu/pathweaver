using System;
using System.Collections.Generic;
using System.Linq;

namespace Pathweaver.Core.Campaign
{
    /// <summary>
    /// Which levels a player has cleared.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Progress is a value, like everything else in the simulation: clearing a level returns a
    /// new progress rather than mutating one. That keeps it saveable, comparable, and testable
    /// without a device.
    /// </para>
    /// <para>
    /// It stores identifiers rather than indices. A level's position in the campaign can change
    /// when levels are inserted or reordered, and progress recorded by position would silently
    /// re-lock or unlock the wrong things when it did.
    /// </para>
    /// </remarks>
    public sealed class CampaignProgress
    {
        private readonly HashSet<string> _cleared;

        private CampaignProgress(HashSet<string> cleared)
        {
            _cleared = cleared;
        }

        /// <summary>A player who has cleared nothing.</summary>
        public static CampaignProgress Empty => new CampaignProgress(new HashSet<string>(StringComparer.Ordinal));

        /// <summary>The cleared level identifiers, in a stable order.</summary>
        public IReadOnlyList<string> ClearedLevels
            => _cleared.OrderBy(id => id, StringComparer.Ordinal).ToList();

        public int ClearedCount => _cleared.Count;

        /// <summary>
        /// Builds progress from a set of identifiers, ignoring blanks and duplicates.
        /// </summary>
        public static CampaignProgress Of(IEnumerable<string> clearedLevels)
        {
            if (clearedLevels is null)
            {
                throw new ArgumentNullException(nameof(clearedLevels));
            }

            var cleared = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in clearedLevels)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    cleared.Add(id.Trim());
                }
            }

            return new CampaignProgress(cleared);
        }

        public bool IsCleared(string levelId)
            => levelId != null && _cleared.Contains(levelId);

        /// <summary>
        /// Returns progress with the given level marked cleared.
        /// </summary>
        /// <remarks>
        /// Clearing an already-cleared level is not an error. A player may replay one for a
        /// better score, and treating that as a fault would make replaying a level something the
        /// game has to special-case.
        /// </remarks>
        public CampaignProgress WithCleared(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                throw new ArgumentException("A level identifier is required.", nameof(levelId));
            }

            var cleared = new HashSet<string>(_cleared, StringComparer.Ordinal) { levelId.Trim() };
            return new CampaignProgress(cleared);
        }

        public override string ToString() => $"{ClearedCount} levels cleared";
    }
}
