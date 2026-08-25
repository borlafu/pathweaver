using UnityEngine;

namespace Pathweaver.Game.Presentation.Menus
{
    /// <summary>
    /// The marks that go on a button, each defined once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every icon in the game used to be assembled at the point of use, which meant the same symbol was
    /// defined several times over and drifted: the play triangle appeared at four call sites, the back
    /// arrow at three, and the level-list stack existed twice at different sizes — the main menu drew it
    /// at radius 0.07 spaced 0.17, the pause screen at 0.06 spaced 0.14. Same icon, two definitions,
    /// already diverged.
    /// </para>
    /// <para>
    /// Naming them here also makes the icon vocabulary reviewable in one place, which matters when the
    /// project has no font and a silhouette is the only thing telling a player what a control does.
    /// </para>
    /// </remarks>
    internal static class MenuGlyphs
    {
        /// <summary>A play triangle: continue, resume, or go on to the next board.</summary>
        internal static void AddPlay(HexButton button, float radius)
        {
            button.AddGlyph(GlyphMeshFactory.CreatePlayTriangle(radius), BoardPalette.MenuGlyph);
        }

        /// <summary>A triangle pointing back the way the player came.</summary>
        internal static void AddBack(HexButton button, float radius = 0.19f)
        {
            button.AddGlyph(GlyphMeshFactory.CreateBackTriangle(radius), BoardPalette.MenuGlyph);
        }

        /// <summary>Two bars: the shape a pause control has had for fifty years.</summary>
        internal static void AddPause(HexButton button, float height = 0.2f, float gap = 0.06f)
        {
            var bar = HexMeshFactory.CreateRectangle(height * 0.25f, height);

            button.AddGlyph(bar, BoardPalette.MenuGlyph, new Vector3(-gap, 0f, 0f));
            button.AddGlyph(bar, BoardPalette.MenuGlyph, new Vector3(gap, 0f, 0f));
        }

        /// <summary>A stack of small hexagons: a list of boards.</summary>
        internal static void AddLevelStack(HexButton button, float pipRadius, float spacing)
        {
            var pip = HexMeshFactory.CreateHexagon(pipRadius);

            foreach (var offset in new[] { -spacing, 0f, spacing })
            {
                button.AddGlyph(pip, BoardPalette.MenuGlyph, new Vector3(0f, offset, 0f));
            }
        }

        /// <summary>A gear with teeth and a hole through the middle.</summary>
        internal static void AddSettingsGear(HexButton button, float rootRadius = 0.17f)
        {
            button.AddGlyph(GlyphMeshFactory.CreateGear(rootRadius), BoardPalette.MenuGlyph);
        }

        /// <summary>
        /// A spiral: a route that keeps going.
        /// </summary>
        /// <remarks>
        /// Headless on purpose. Restart is a circular arrow <em>with</em> a head, and when this was drawn
        /// as two concentric circular arrows the two controls carried the same symbol at two sizes.
        /// </remarks>
        internal static void AddEndlessSpiral(HexButton button)
        {
            button.AddGlyph(
                GlyphMeshFactory.CreateSpiral(
                    innerRadius: 0.055f, growthPerTurn: 0.105f, turns: 1.85f, thickness: 0.042f),
                BoardPalette.MenuGlyph);
        }

        /// <summary>A circular arrow: start this board again.</summary>
        internal static void AddRestartArrow(HexButton button, float radius = 0.2f, float thickness = 0.065f)
        {
            button.AddGlyph(
                HexMeshFactory.CreateCircularArrow(radius, thickness, sweepDegrees: 265f),
                BoardPalette.MenuGlyph);
        }

        /// <summary>
        /// A node with three others around it, joined: a map rather than a board.
        /// </summary>
        internal static void AddConstellation(HexButton button)
        {
            var star = GlyphMeshFactory.CreateDisc(0.07f, sides: 4);
            button.AddGlyph(star, BoardPalette.MenuGlyph);

            foreach (var offset in new[]
                     {
                         new Vector3(-0.14f, 0.1f, 0f),
                         new Vector3(0.14f, 0.1f, 0f),
                         new Vector3(0f, -0.16f, 0f),
                     })
            {
                button.AddGlyph(GlyphMeshFactory.CreateDisc(0.05f, sides: 4), BoardPalette.MenuGlyph, offset);

                button.AddGlyph(
                    HexMeshFactory.CreateRectangle(offset.magnitude, 0.022f),
                    BoardPalette.AtlasLinkLit,
                    offset * 0.5f,
                    Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
            }
        }

        /// <summary>
        /// A cross, ringed once the control is armed: erase everything.
        /// </summary>
        /// <remarks>
        /// The ring is the point. A player who cannot see the red still has to be able to tell an
        /// armed control from a resting one, and this game does not let colour carry a fact on its
        /// own — the springs and hubs breathe for the same reason. The ring also reads as a halo
        /// around the one tap that cannot be undone.
        /// </remarks>
        internal static void AddErase(HexButton button, bool isArmed)
        {
            var colour = isArmed ? BoardPalette.MenuGlyph : BoardPalette.SwitchOffGlyph;
            var bar = HexMeshFactory.CreateRectangle(0.3f, 0.07f);

            button.AddGlyph(bar, colour, default, 45f);
            button.AddGlyph(bar, colour, default, -45f);

            if (isArmed)
            {
                button.AddGlyph(GlyphMeshFactory.CreateRing(0.26f, 0.04f, sides: 12), colour);
            }
        }

        /// <summary>Three bars of rising height, like a buzz.</summary>
        internal static (Mesh Mesh, Vector3 Offset)[] Haptics()
            => new[]
            {
                (HexMeshFactory.CreateRectangle(0.05f, 0.12f), new Vector3(-0.13f, 0f, 0f)),
                (HexMeshFactory.CreateRectangle(0.05f, 0.24f), new Vector3(0f, 0f, 0f)),
                (HexMeshFactory.CreateRectangle(0.05f, 0.12f), new Vector3(0.13f, 0f, 0f)),
            };

        /// <summary>Two chevrons in flight, for motion.</summary>
        /// <remarks>
        /// Chevrons rather than the two loose triangles this used to be: a mitred stroke reads as
        /// movement, where a pair of solid wedges reads as a fast-forward button.
        /// </remarks>
        internal static (Mesh Mesh, Vector3 Offset)[] Motion()
        {
            var chevron = GlyphMeshFactory.CreateChevron(width: 0.11f, height: 0.11f, thickness: 0.042f);

            return new[]
            {
                (chevron, new Vector3(-0.08f, 0f, 0f)),
                (chevron, new Vector3(0.08f, 0f, 0f)),
            };
        }
    }
}
