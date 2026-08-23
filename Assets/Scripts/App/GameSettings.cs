using UnityEngine;

namespace Pathweaver.Game.App
{
    /// <summary>
    /// Player preferences that are not part of a run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Kept in <c>PlayerPrefs</c> rather than in the save file, because they belong to the player
    /// and the device rather than to a game in progress. Wiping a run should not reset them, and
    /// they should survive a corrupt save.
    /// </para>
    /// <para>
    /// There is no audio setting because there is no audio. Offering a toggle for something that
    /// does nothing is worse than offering nothing: it tells the player the game is broken rather
    /// than unfinished.
    /// </para>
    /// </remarks>
    internal static class GameSettings
    {
        private const string HapticsKey = "settings.haptics";
        private const string ReduceMotionKey = "settings.reduceMotion";

        /// <summary>
        /// Whether the phone vibrates on placements and completed routes.
        /// </summary>
        internal static bool HapticsEnabled
        {
            get => PlayerPrefs.GetInt(HapticsKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(HapticsKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Whether animation is kept to a minimum.
        /// </summary>
        /// <remarks>
        /// Covers the tile's rotation hint and the flash along a harvested route. Repeated motion
        /// is a genuine accessibility problem for some players, and the hint in particular runs
        /// every two seconds forever — which is exactly the kind of thing that needs an off
        /// switch rather than an apology.
        /// </remarks>
        internal static bool ReduceMotion
        {
            get => PlayerPrefs.GetInt(ReduceMotionKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(ReduceMotionKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
