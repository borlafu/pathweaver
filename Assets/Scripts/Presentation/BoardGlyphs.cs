using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// The marks on the two controls that spend something, as data rather than as drawing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// They used to be built inline by <see cref="PivotButtonView"/> and <see cref="SkipButtonView"/>,
    /// which was fine while each mark had exactly one place to appear. The help screen gives each of them
    /// a second: a page that describes a control the player cannot find is a page that has to show it, and
    /// a picture drawn from a second set of numbers would eventually stop matching the button it claims to
    /// be a picture of.
    /// </para>
    /// <para>
    /// Only the marks live here, not the hexagon behind them. That background carries state — spent,
    /// ready, armed — and state belongs to the control, not to a diagram of it.
    /// </para>
    /// </remarks>
    internal static class BoardGlyphs
    {
        /// <summary>
        /// The radius of a control in the drawer, in world units.
        /// </summary>
        /// <remarks>
        /// Shared by both, because they sit at either end of the same row and a difference between them
        /// would read as one meaning more than the other.
        /// </remarks>
        internal const float ButtonRadius = 0.34f;

        private const float PivotCellRadius = 0.15f;
        private const float PivotBarWidth = 0.22f;
        private const float PivotBarThickness = 0.075f;

        private const float SkipChevronLength = 0.13f;
        private const float SkipChevronHeight = 0.115f;
        private const float SkipChevronThickness = 0.05f;

        /// <summary>How far apart the two chevrons of the skip mark sit, in world units.</summary>
        private const float SkipChevronSpacing = 0.06f;

        /// <summary>
        /// A hexagon with a bar struck through it: this cell, taken off the board.
        /// </summary>
        /// <remarks>
        /// A plain minus would say "less" rather than "remove that tile".
        /// </remarks>
        internal static Part[] Pivot() => new[]
        {
            new Part(
                "Cell", HexMeshFactory.CreateHexagon(PivotCellRadius), BoardPalette.PivotGlyphCell, -0.02f),
            new Part(
                "Bar",
                HexMeshFactory.CreateRectangle(PivotBarWidth, PivotBarThickness),
                BoardPalette.RestartArrow,
                -0.04f),
        };

        /// <summary>
        /// Two chevrons pointing the same way: pass this one along.
        /// </summary>
        /// <remarks>
        /// A double chevron rather than a single one, so it is not confused with the pan marks at the
        /// screen edges, which are one chevron each and mean a direction rather than an action.
        /// </remarks>
        internal static Part[] Skip()
        {
            var chevron = GlyphMeshFactory.CreateChevron(
                SkipChevronLength, SkipChevronHeight, SkipChevronThickness);

            return new[]
            {
                new Part(
                    "Near",
                    chevron,
                    BoardPalette.RestartArrow,
                    -0.02f,
                    new Vector3(-SkipChevronSpacing, 0f, 0f)),
                new Part(
                    "Far",
                    chevron,
                    BoardPalette.RestartArrow,
                    -0.02f,
                    new Vector3(SkipChevronSpacing, 0f, 0f)),
            };
        }

        /// <summary>One mesh of a mark, and where in front of the face it sits.</summary>
        internal readonly struct Part
        {
            internal Part(string name, Mesh mesh, Color colour, float depth, Vector3 offset = default)
            {
                Name = name;
                Mesh = mesh;
                Colour = colour;
                Depth = depth;
                Offset = offset;
            }

            internal string Name { get; }

            internal Mesh Mesh { get; }

            internal Color Colour { get; }

            /// <summary>How far in front of the control's face this piece sits, in world units.</summary>
            internal float Depth { get; }

            /// <summary>Where on the face it sits, relative to the centre.</summary>
            internal Vector3 Offset { get; }
        }
    }
}
