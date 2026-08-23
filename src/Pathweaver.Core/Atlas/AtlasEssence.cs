using System;

namespace Pathweaver.Core.Atlas
{
    /// <summary>
    /// How much Star Essence a cleared board is worth.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One essence per base score harvested, which is a rule a player can work out from the two
    /// numbers already on screen: a level with a base of 100 and a final score of 246 pays two. It
    /// also means a longer route pays more essence as well as more points, so the length curve pulls
    /// in one direction rather than two.
    /// </para>
    /// <para>
    /// Integer division, and no rounding: two devices are allowed to disagree about the last bit of a
    /// double, and they are not allowed to disagree about a reward.
    /// </para>
    /// </remarks>
    public static class AtlasEssence
    {
        /// <summary>
        /// Essence for a board cleared at the given score.
        /// </summary>
        /// <param name="score">The score the board finished on.</param>
        /// <param name="baseRouteScore">What one unmultiplied route is worth on that board.</param>
        /// <param name="essenceBonus">Extra essence from unlocked nodes.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown for a base score below one, which would divide by zero, and for a negative score.
        /// </exception>
        public static int ForClear(long score, long baseRouteScore, int essenceBonus = 0)
        {
            if (baseRouteScore < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseRouteScore), baseRouteScore, "A route must be worth at least one point.");
            }

            if (score < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(score), score, "A score cannot be negative.");
            }

            // The bonus is flat rather than a share of the score, so it helps a player who is
            // struggling instead of compounding for one who is not.
            return (int)(score / baseRouteScore) + Math.Max(0, essenceBonus);
        }
    }
}
