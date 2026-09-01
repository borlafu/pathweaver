using UnityEngine;

namespace Pathweaver.Game.App
{
    /// <summary>Which one-off hint a player has not been shown yet.</summary>
    internal enum CoachMark
    {
        /// <summary>Nothing to say.</summary>
        None,

        /// <summary>How a tile gets onto the board.</summary>
        Place,

        /// <summary>That the tile in the tray can be turned.</summary>
        Turn,

        /// <summary>Why a placement was refused.</summary>
        Join,
    }

    /// <summary>
    /// Remembers which first-run hints have been shown.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The help screen explains every rule, and a player has to go and find it. These three are the
    /// ones a player needs before they have any reason to look: that a tile is dragged from the tray,
    /// that it can be turned, and that it only joins its own kind. Everything else can wait for the
    /// help screen or for curiosity.
    /// </para>
    /// <para>
    /// Kept in <c>PlayerPrefs</c> alongside <see cref="GameSettings"/> rather than in a save file,
    /// because having been taught something belongs to the player and not to a game in progress. It is
    /// still forgotten by <see cref="ProgressReset"/>: a player asking for a fresh start means it.
    /// </para>
    /// <para>
    /// Each mark is shown once ever, not once per level and not once per mode. A hint that reappears is
    /// no longer a hint, and a player who learned to drag a tile in Endless does not need telling again
    /// in the campaign.
    /// </para>
    /// </remarks>
    internal static class CoachMarks
    {
        private const string Prefix = "coach.";

        /// <summary>Whether the given hint has been shown.</summary>
        internal static bool HasSeen(CoachMark mark)
            => mark == CoachMark.None || PlayerPrefs.GetInt(KeyFor(mark), 0) == 1;

        /// <summary>Records that the given hint has been shown, for good.</summary>
        internal static void MarkSeen(CoachMark mark)
        {
            if (mark == CoachMark.None)
            {
                return;
            }

            PlayerPrefs.SetInt(KeyFor(mark), 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Forgets every hint, so a fresh start teaches again.
        /// </summary>
        /// <remarks>
        /// Called by <see cref="ProgressReset"/>. Erasing progress and then withholding the tutorial
        /// would leave a player who wanted a clean slate with a board and no explanation.
        /// </remarks>
        internal static void Forget()
        {
            foreach (var mark in All)
            {
                PlayerPrefs.DeleteKey(KeyFor(mark));
            }

            PlayerPrefs.Save();
        }

        /// <summary>Every hint there is, for the reset and for the tests.</summary>
        internal static readonly CoachMark[] All =
        {
            CoachMark.Place,
            CoachMark.Turn,
            CoachMark.Join,
        };

        /// <summary>What each hint says.</summary>
        /// <remarks>
        /// One sentence each, in the imperative for the two that ask for an action and in the indicative
        /// for the one that explains a refusal. Short enough to read while a thumb is already moving.
        /// </remarks>
        internal static string TextFor(CoachMark mark) => mark switch
        {
            CoachMark.Place => "Drag a tile from the tray onto a lit cell.",
            CoachMark.Turn => "Tap the tile in the tray to turn it.",
            CoachMark.Join => "A tile only joins its own kind, edge to edge.",
            _ => string.Empty,
        };

        private static string KeyFor(CoachMark mark) => Prefix + mark;
    }
}
