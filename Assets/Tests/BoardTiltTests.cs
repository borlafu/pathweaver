using NUnit.Framework;
using Pathweaver.Game.Presentation;
using UnityEngine;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// The lean, and the two things it costs.
    /// </summary>
    /// <remarks>
    /// A leaning board takes less vertical room on screen and hangs below the plane its top faces lie
    /// in. Both are arithmetic, and both were going to be found by looking at a clipped bottom row on a
    /// phone otherwise.
    /// </remarks>
    public class BoardTiltTests
    {
        /// <summary>Half-height of a biome-one board, in world units.</summary>
        private const float SmallBoardHalfHeight = 2.5f;

        /// <summary>Half-height of the valley in biome two, which is where the constant broke.</summary>
        private const float LargeBoardHalfHeight = 6.5f;

        [Test]
        public void The_lean_is_enough_to_read_and_not_enough_to_cost_the_board()
        {
            // Below about ten degrees the depth stops being visible; above about twenty-five a hexagon
            // foreshortens into a shape that is no longer obviously a hexagon.
            Assert.That(BoardTilt.Degrees, Is.InRange(10f, 25f));
        }

        [Test]
        public void A_cell_stands_half_an_edge_tall()
        {
            // A regular hexagon's edge equals its circumradius, and HexMetrics.Size is that radius.
            Assert.That(BoardTilt.BlockHeight, Is.EqualTo(HexMetrics.Size * 0.5f).Within(0.0001f));
        }

        [Test]
        public void The_lean_costs_less_than_a_twentieth_of_the_boards_height()
        {
            // The whole reason 15 degrees is affordable on a screen with no room to spare.
            Assert.That(BoardTilt.VerticalForeshortening, Is.GreaterThan(0.95f));
            Assert.That(BoardTilt.VerticalForeshortening, Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void A_leaning_block_hangs_below_its_own_top_face()
        {
            // Small, but not nothing: about 0.065 world units, against a margin of 0.25. Zero would mean
            // the lean had been dropped and the camera fitter was padding for nothing.
            Assert.That(BoardTilt.ScreenOverhang, Is.GreaterThan(0f));
            Assert.That(BoardTilt.ScreenOverhang, Is.LessThan(BoardTilt.BlockHeight));
        }

        [TestCase(SmallBoardHalfHeight)]
        [TestCase(LargeBoardHalfHeight)]
        [TestCase(20f)]
        public void The_whole_board_stays_behind_the_interface(float boardHalfHeight)
        {
            // The lean swings the near edge toward the viewer, in proportion to how tall the board is.
            // This was a constant 1.5 chosen against a half height of 2.5, and the first valley large
            // enough to need panning has 6.5 — its southern rim reached z = -0.18, in front of the
            // backdrop that keeps the board out from behind the interface.
            const float nearestInterfaceDepth = -0.1f;

            var nearestBoardDepth = BoardTilt.DepthOffsetFor(boardHalfHeight)
                                    - (boardHalfHeight * Mathf.Sin(BoardTilt.Degrees * Mathf.Deg2Rad));

            Assert.That(
                nearestBoardDepth,
                Is.GreaterThan(nearestInterfaceDepth),
                "The near edge of the board reaches in front of the interface.");
        }

        [Test]
        public void A_small_board_is_not_pushed_further_back_than_it_needs()
        {
            // The floor still applies, so nothing about a biome-one board changes.
            Assert.That(
                BoardTilt.DepthOffsetFor(SmallBoardHalfHeight),
                Is.EqualTo(BoardTilt.MinimumDepthOffset).Within(0.0001f));
        }

        [Test]
        public void A_taller_board_sits_further_back()
        {
            Assert.That(
                BoardTilt.DepthOffsetFor(LargeBoardHalfHeight),
                Is.GreaterThan(BoardTilt.DepthOffsetFor(SmallBoardHalfHeight)));
        }

        [Test]
        public void The_far_rows_lean_away_and_the_near_rows_lean_toward_the_viewer()
        {
            // The sign of the rotation. The other one tips the board over backwards and shows it from
            // underneath, which looks like a bug in the hex maths rather than in one angle.
            var top = BoardTilt.Rotation * new Vector3(0f, 1f, 0f);
            var bottom = BoardTilt.Rotation * new Vector3(0f, -1f, 0f);

            Assert.That(top.z, Is.GreaterThan(0f), "The top of the board should lean away.");
            Assert.That(bottom.z, Is.LessThan(0f), "The bottom of the board should lean toward.");
        }

        [Test]
        public void A_blocks_side_stays_brighter_than_the_background_it_sits_on()
        {
            // The darkest side in the game is an empty cell's. If it fell below the background the board
            // would read as floating lids rather than as blocks.
            var darkestSide = BoardPalette.EmptyCell.g * TileVisual.SideShade;

            Assert.That(darkestSide, Is.GreaterThan(BoardPalette.Background.g));
        }
    }
}
