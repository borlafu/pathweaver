using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// How brightly to mark an edge the board continues past.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pure function of how much room is left, like the other motion in this codebase, so it can be
    /// checked without a device.
    /// </para>
    /// <para>
    /// It exists because a board larger than the screen said nothing about being larger than the screen.
    /// The opening flight shows the whole valley once and is then gone; after that a player has only the
    /// board's cut-off edges to go on, which look the same as a board that simply ends there.
    /// </para>
    /// </remarks>
    internal static class PanHint
    {
        /// <summary>
        /// How much room, in world units, counts as plenty.
        /// </summary>
        /// <remarks>
        /// Roughly two cells. Below this the mark dims toward nothing, so a player pushing against the
        /// clamp sees it fade rather than being told there is more when there is not — which is the half
        /// of this that actually matters, because being offered a direction that does nothing is worse
        /// than being offered none.
        /// </remarks>
        internal const float PlentyOfRoom = 1.7f;

        /// <summary>How visible the mark for a given direction should be, from nothing to fully.</summary>
        internal static float BrightnessFor(float roomInWorldUnits)
            => Mathf.Clamp01(roomInWorldUnits / PlentyOfRoom);

        /// <summary>
        /// The colour to draw the mark in.
        /// </summary>
        /// <remarks>
        /// Lerped from the background rather than faded with alpha: the material the board is drawn with
        /// is opaque, so "invisible" has to mean "the same colour as what is behind" — the same trick the
        /// endpoint ring and the next tile already use.
        /// </remarks>
        internal static Color ColourFor(float roomInWorldUnits)
            => Color.Lerp(BoardPalette.Background, BoardPalette.PanHint, BrightnessFor(roomInWorldUnits));
    }
}
