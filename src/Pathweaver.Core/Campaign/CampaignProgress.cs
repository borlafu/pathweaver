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

        private CampaignProgress(HashSet<string> cleared, int pivotTokens)
        {
            _cleared = cleared;
            PivotTokens = pivotTokens;
        }

        /// <summary>A player who has cleared nothing.</summary>
        public static CampaignProgress Empty
            => new CampaignProgress(new HashSet<string>(StringComparer.Ordinal), pivotTokens: 0);

        /// <summary>
        /// Pivot Tokens the player is carrying between levels.
        /// </summary>
        /// <remarks>
        /// A token is earned by completing a route of four conduits or more, and clearing a level
        /// ends the board — so a token earned by the route that clears a level could never be spent
        /// unless it travelled with the player. Endless rounds carry them for the same reason.
        /// <para>
        /// Carried in progress rather than in the run save, because a run is one level's business
        /// and this outlives it. Restarting a level therefore hands the tokens back rather than
        /// destroying them.
        /// </para>
        /// </remarks>
        public int PivotTokens { get; }

        /// <summary>The cleared level identifiers, in a stable order.</summary>
        public IReadOnlyList<string> ClearedLevels
            => _cleared.OrderBy(id => id, StringComparer.Ordinal).ToList();

        public int ClearedCount => _cleared.Count;

        /// <summary>
        /// Builds progress from a set of identifiers, ignoring blanks and duplicates.
        /// </summary>
        public static CampaignProgress Of(IEnumerable<string> clearedLevels, int pivotTokens = 0)
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

            return new CampaignProgress(cleared, Math.Max(0, pivotTokens));
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
            return new CampaignProgress(cleared, PivotTokens);
        }

        /// <summary>
        /// Returns progress carrying the given number of Pivot Tokens.
        /// </summary>
        /// <remarks>
        /// A negative count is treated as none rather than rejected: only a damaged file can produce
        /// one, and refusing to load a campaign over it would cost the player far more.
        /// </remarks>
        public CampaignProgress WithPivotTokens(int pivotTokens)
            => new CampaignProgress(
                new HashSet<string>(_cleared, StringComparer.Ordinal), Math.Max(0, pivotTokens));

        public override string ToString()
            => $"{ClearedCount} levels cleared, {PivotTokens} tokens carried";
    }
}
