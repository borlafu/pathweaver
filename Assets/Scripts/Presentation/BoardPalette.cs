using Pathweaver.Core.Tiles;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Placeholder colours for the board.
    /// </summary>
    /// <remarks>
    /// Programmer art, and knowingly incomplete: colour alone must not be the only
    /// way to tell resources apart, since four kinds flow through visually similar
    /// conduits and colour-blind players would lose exactly that distinction. The
    /// shape or pattern encoding that fixes it belongs to the accessibility pass in
    /// #28; these values are chosen to stay distinguishable under the common forms of
    /// colour blindness in the meantime.
    /// </remarks>
    internal static class BoardPalette
    {
        internal static readonly Color EmptyCell = new Color(0.22f, 0.24f, 0.29f);
        internal static readonly Color CellOutline = new Color(0.32f, 0.35f, 0.41f);
        /// <summary>
        /// An empty cell the held tile could legally occupy.
        /// </summary>
        /// <remarks>
        /// Showing where a tile may go is not a hint, it is the placement rule made
        /// visible. Requiring conduits to join the network is invisible otherwise, and
        /// a player who cannot see it reads a refused tap as the game ignoring them.
        /// </remarks>
        internal static readonly Color LegalCell = new Color(0.30f, 0.36f, 0.44f);

        /// <summary>The restart button at rest.</summary>
        /// <summary>
        /// A cell that has just refused a tile.
        /// </summary>
        /// <remarks>
        /// Brighter than an empty cell and cooler than a legal one, so it cannot be mistaken for either.
        /// Not red: the player asked a question and the answer is no, which is not a mistake worth
        /// scolding, and red belongs to the one control that destroys something.
        /// </remarks>
        internal static readonly Color RefusedCell = new Color(0.42f, 0.40f, 0.46f);

        internal static readonly Color RestartIdle = new Color(0.28f, 0.30f, 0.35f);

        /// <summary>
        /// The restart button when the board has no moves left, at which point it is not one
        /// option among several but the only thing left to press.
        /// </summary>
        internal static readonly Color RestartUrgent = new Color(0.78f, 0.36f, 0.30f);

        /// <summary>The skip button with skips remaining.</summary>
        internal static readonly Color SkipReady = new Color(0.26f, 0.38f, 0.46f);

        /// <summary>The skip button with none left: dimmed, not hidden.</summary>
        internal static readonly Color SkipSpent = new Color(0.19f, 0.20f, 0.23f);

        internal static readonly Color RestartArrow = new Color(0.88f, 0.90f, 0.94f);

        /// <summary>The confirmation panel behind the question.</summary>
        internal static readonly Color DialogPanel = new Color(0.13f, 0.14f, 0.17f);

        internal static readonly Color DialogConfirm = new Color(0.30f, 0.62f, 0.40f);

        internal static readonly Color DialogCancel = new Color(0.34f, 0.36f, 0.42f);

        /// <summary>The quota bar's empty track.</summary>
        internal static readonly Color ProgressTrack = new Color(0.20f, 0.22f, 0.26f);

        internal static readonly Color ProgressFill = new Color(0.35f, 0.68f, 0.92f);

        /// <summary>The quota bar once the level's target is met.</summary>
        internal static readonly Color ProgressComplete = new Color(0.42f, 0.78f, 0.48f);

        internal static readonly Color TokenHeld = new Color(0.92f, 0.78f, 0.38f);

        /// <summary>A held skip, kept distinct from a Pivot Token at a glance.</summary>
        internal static readonly Color SkipHeld = new Color(0.55f, 0.80f, 0.90f);

        internal static readonly Color TokenEmpty = new Color(0.24f, 0.26f, 0.30f);

        /// <summary>
        /// The backing of a conduit an armed Pivot Token could turn or retrieve.
        /// </summary>
        /// <remarks>
        /// A dimmed version of <see cref="TokenHeld"/> rather than a new colour, so the armed pip
        /// and the cells it reaches read as one thing without the board turning gold.
        /// </remarks>
        internal static readonly Color PivotTarget = new Color(0.55f, 0.46f, 0.24f);

        /// <summary>A held Pivot Token that is armed and waiting for a conduit to be chosen.</summary>
        internal static readonly Color TokenArmed = new Color(1f, 0.95f, 0.72f);

        /// <summary>The remove button with a token to spend.</summary>
        internal static readonly Color PivotReady = new Color(0.46f, 0.39f, 0.20f);

        /// <summary>The remove button with nothing to spend, or nothing to spend it on.</summary>
        internal static readonly Color PivotSpent = new Color(0.19f, 0.20f, 0.23f);

        /// <summary>The little hexagon on the remove button, standing for the cell being freed.</summary>
        internal static readonly Color PivotGlyphCell = new Color(0.72f, 0.62f, 0.36f);

        /// <summary>A conduit on a route that has just paid out.</summary>
        internal static readonly Color HarvestFlash = new Color(0.95f, 0.97f, 1f);

        /// <summary>The action a player is most likely to want.</summary>
        internal static readonly Color MenuPrimary = new Color(0.30f, 0.55f, 0.72f);

        internal static readonly Color MenuSecondary = new Color(0.26f, 0.28f, 0.33f);


        internal static readonly Color MenuGlyph = new Color(0.92f, 0.94f, 0.97f);

        /// <summary>A level that can be played but has not been cleared.</summary>
        internal static readonly Color LevelOpen = new Color(0.28f, 0.34f, 0.42f);

        internal static readonly Color LevelCleared = new Color(0.30f, 0.52f, 0.38f);

        /// <summary>
        /// A level not yet reachable: dimmed rather than hidden, so the length of the campaign is
        /// visible from the first screen.
        /// </summary>
        internal static readonly Color LevelLocked = new Color(0.17f, 0.18f, 0.21f);

        internal static readonly Color LevelLockedGlyph = new Color(0.30f, 0.32f, 0.36f);

        /// <summary>An atlas node already bought.</summary>
        internal static readonly Color AtlasUnlocked = new Color(0.30f, 0.52f, 0.38f);

        /// <summary>An atlas node reachable and affordable right now.</summary>
        internal static readonly Color AtlasAffordable = new Color(0.30f, 0.55f, 0.72f);

        /// <summary>An atlas node whose prerequisites are met but whose cost is not.</summary>
        internal static readonly Color AtlasReachable = new Color(0.24f, 0.27f, 0.33f);

        /// <summary>The mark on a node not yet bought.</summary>
        internal static readonly Color AtlasGlyphLocked = new Color(0.44f, 0.47f, 0.53f);

        /// <summary>
        /// Star Essence, wherever it is named or counted.
        /// </summary>
        /// <remarks>
        /// One colour across the atlas balance, a node's cost, the line a cleared board pays out on, and
        /// the relics named while paused — so a player can follow one currency by eye through four screens.
        /// </remarks>
        internal static readonly Color AtlasEssence = new Color(0.86f, 0.82f, 0.52f);

        /// <summary>A cost the player cannot yet meet.</summary>
        internal static readonly Color AtlasCostUnaffordable = new Color(0.40f, 0.40f, 0.42f);

        /// <summary>A link between two nodes, both of them bought.</summary>
        internal static readonly Color AtlasLinkLit = new Color(0.32f, 0.50f, 0.40f);

        /// <summary>A link waiting on one of its ends.</summary>
        internal static readonly Color AtlasLinkDim = new Color(0.20f, 0.22f, 0.26f);

        internal static readonly Color SwitchOn = new Color(0.30f, 0.55f, 0.72f);

        internal static readonly Color SwitchOff = new Color(0.22f, 0.23f, 0.27f);

        internal static readonly Color SwitchOffGlyph = new Color(0.45f, 0.47f, 0.52f);

        /// <summary>
        /// A control that destroys something, once it is armed and one tap from doing it.
        /// </summary>
        /// <remarks>
        /// The only red in the game, and it appears on exactly one control. Colour is not what tells
        /// the player it is armed — a ring appears around the cross, because this game may never rely
        /// on colour alone — but red is what makes a mis-tap hesitate.
        /// </remarks>
        internal static readonly Color Destructive = new Color(0.74f, 0.29f, 0.27f);

        /// <summary>
        /// What the camera clears to, and therefore what every colour here is read against.
        /// </summary>
        /// <remarks>
        /// Named because contrast is a relationship rather than a property: a text colour cannot be
        /// judged without it, and it was previously a bare triple repeated in two capture tools.
        /// </remarks>
        internal static readonly Color Background = new Color(0.08f, 0.09f, 0.11f);

        /// <summary>
        /// Text that carries the meaning of a screen.
        /// </summary>
        /// <remarks>
        /// About 16:1 against <see cref="Background"/>, far above the 4.5:1 WCAG asks of body text.
        /// Text gets its own colours rather than borrowing a control's because the requirement is
        /// different: a dim pip is still legibly a pip, and dim text is not legibly anything. The
        /// first text preview was drawn in <see cref="TokenEmpty"/> and came out at 1.8:1.
        /// </remarks>
        internal static readonly Color TextPrimary = new Color(0.93f, 0.95f, 0.97f);

        /// <summary>
        /// Text that qualifies something else — a unit, a level id, a caption.
        /// </summary>
        /// <remarks>
        /// About 7:1, which still clears 4.5:1 with room for the smallest size the game draws.
        /// </remarks>
        internal static readonly Color TextSecondary = new Color(0.62f, 0.65f, 0.71f);

        /// <summary>
        /// A mark at a screen edge the board continues past.
        /// </summary>
        /// <remarks>
        /// Dim on purpose. It is a standing offer rather than an instruction, and it sits at the edge of
        /// the screen where a bright mark would pull the eye away from the board for the whole session.
        /// </remarks>
        internal static readonly Color PanHint = new Color(0.38f, 0.42f, 0.50f);

        internal static readonly Color Spring = new Color(0.95f, 0.85f, 0.35f);
        internal static readonly Color Hub = new Color(0.55f, 0.40f, 0.85f);

        /// <summary>
        /// The colour of a resource.
        /// </summary>
        /// <remarks>
        /// Deliberately spread across brightness as well as hue. The first attempt put water
        /// and crystal within 0.002 of the same relative luminance — distinguishable only by
        /// hue, which is precisely what the most common forms of colour blindness remove. A
        /// test now enforces a brightness gap between every pair, so the palette carries some
        /// of the information even before the motifs do.
        /// </remarks>
        internal static Color ForKind(ResourceKind kind) => kind switch
        {
            ResourceKind.Water => new Color(0.20f, 0.45f, 0.85f),
            ResourceKind.Crystal => new Color(0.80f, 0.50f, 0.75f),
            ResourceKind.Trade => new Color(0.95f, 0.65f, 0.30f),
            ResourceKind.Wind => new Color(0.70f, 0.92f, 0.82f),
            _ => Color.magenta,
        };
    }
}
