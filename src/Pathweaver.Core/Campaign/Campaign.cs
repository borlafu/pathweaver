using System;
using System.Collections.Generic;
using System.Linq;

namespace Pathweaver.Core.Campaign
{
    /// <summary>
    /// The ordered run of levels, and which of them a player may enter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlocking is linear: the first level is always open, and every other opens once the one
    /// before it is cleared. PRD section 4.2 describes a non-linear constellation, which is the
    /// Atlas screen's job; until that exists a straight line is the honest version rather than a
    /// half-built graph.
    /// </para>
    /// <para>
    /// A level that has been cleared stays open, so a player can return for a better score.
    /// </para>
    /// </remarks>
    public sealed class Campaign
    {
        private readonly string[] _levelIds;

        private Campaign(string[] levelIds)
        {
            _levelIds = levelIds;
        }

        /// <summary>The levels in play order.</summary>
        public IReadOnlyList<string> LevelIds => _levelIds;

        /// <summary>
        /// Builds a campaign from level identifiers in play order.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when there are no levels, or an identifier repeats. A duplicate would make
        /// unlocking ambiguous, since the same level would sit in two places in the order.
        /// </exception>
        public static Campaign Of(IEnumerable<string> levelIds)
        {
            if (levelIds is null)
            {
                throw new ArgumentNullException(nameof(levelIds));
            }

            var ordered = levelIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()).ToArray();

            if (ordered.Length == 0)
            {
                throw new ArgumentException("A campaign needs at least one level.", nameof(levelIds));
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ordered)
            {
                if (!seen.Add(id))
                {
                    throw new ArgumentException($"Level \"{id}\" appears more than once.", nameof(levelIds));
                }
            }

            return new Campaign(ordered);
        }

        /// <summary>
        /// Whether a player with the given progress may enter a level.
        /// </summary>
        public bool IsUnlocked(string levelId, CampaignProgress progress)
        {
            if (progress is null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            var index = Array.IndexOf(_levelIds, levelId);
            if (index < 0)
            {
                return false;
            }

            return index == 0 || progress.IsCleared(_levelIds[index - 1]);
        }

        /// <summary>
        /// The level a player should be offered next: the first unlocked one they have not
        /// cleared, or the last level once everything is done.
        /// </summary>
        public string NextLevel(CampaignProgress progress)
        {
            if (progress is null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            foreach (var id in _levelIds)
            {
                if (!progress.IsCleared(id))
                {
                    return IsUnlocked(id, progress) ? id : _levelIds[0];
                }
            }

            return _levelIds[_levelIds.Length - 1];
        }
    }
}
