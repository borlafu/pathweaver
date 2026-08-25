using UnityEngine;

namespace Pathweaver.Game.App
{
    /// <summary>
    /// Forgets everything the player has done.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Its own class because "all progress" is a list, and a list kept in a caller's head goes out of
    /// date. Progress lives in four places — cleared levels and carried tokens, the World Atlas, the
    /// endless run, and one save file per board in progress — and a reset that missed any of them
    /// would be worse than none: a player asking for a fresh start and being dropped back onto their
    /// old board would reasonably conclude the game had ignored them.
    /// </para>
    /// <para>
    /// Anything added to that list later belongs here, and the test alongside it fails until it is.
    /// </para>
    /// <para>
    /// Files are deleted rather than overwritten with empty ones. An absent file is what a first
    /// launch looks like, and every store already treats it that way, so a reset leaves the game in a
    /// state it is known to handle rather than a new one.
    /// </para>
    /// </remarks>
    internal static class ProgressReset
    {
        /// <summary>
        /// Deletes every record of past play.
        /// </summary>
        /// <remarks>
        /// Each store swallows its own IO failure and warns, so one unwritable file cannot leave the
        /// wipe half done. Missing arguments are skipped rather than refused: a scene without an
        /// endless store has no endless run to forget.
        /// </remarks>
        internal static void Wipe(
            SaveService saves,
            CampaignProgressStore campaign,
            AtlasProgressStore atlas,
            EndlessRunStore endless)
        {
            var boards = saves?.DeleteAll() ?? 0;

            campaign?.Delete();
            atlas?.Delete();
            endless?.Delete();

            Debug.Log($"[reset] progress cleared, including {boards} board(s) in progress");
        }
    }
}
