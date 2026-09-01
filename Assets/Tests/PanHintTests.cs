using NUnit.Framework;
using Pathweaver.Game.Presentation;
using UnityEngine;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// The marks that say the board continues past a screen edge.
    /// </summary>
    /// <remarks>
    /// The half that matters most is the withdrawal: offering a player a direction that does nothing is
    /// worse than offering none, so a mark has to reach exactly nothing when the clamp does.
    /// </remarks>
    public class PanHintTests
    {
        /// <summary>The aspect of the phone the game is tested on: 1080 by 2376.</summary>
        private const float PhoneAspect = 1080f / 2376f;

        [Test]
        public void No_room_means_no_mark()
        {
            Assert.That(PanHint.BrightnessFor(0f), Is.EqualTo(0f));
            Assert.That(
                PanHint.ColourFor(0f),
                Is.EqualTo(BoardPalette.Background),
                "A mark with nowhere to go should be indistinguishable from the background.");
        }

        [Test]
        public void Plenty_of_room_means_a_full_mark()
        {
            Assert.That(PanHint.BrightnessFor(PanHint.PlentyOfRoom), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(PanHint.BrightnessFor(PanHint.PlentyOfRoom * 5f), Is.EqualTo(1f));
        }

        [Test]
        public void It_brightens_with_the_room_available()
        {
            Assert.That(
                PanHint.BrightnessFor(PanHint.PlentyOfRoom * 0.75f),
                Is.GreaterThan(PanHint.BrightnessFor(PanHint.PlentyOfRoom * 0.25f)));
        }

        [Test]
        public void Plenty_is_about_two_cells()
        {
            // Enough that a mark is at full strength while there is somewhere worth going, and fades over
            // the last stretch rather than snapping off.
            var cell = HexMetrics.CellSpacing;

            Assert.That(PanHint.PlentyOfRoom, Is.InRange(cell, cell * 3f));
        }

        [Test]
        public void A_board_that_fits_offers_no_direction()
        {
            // Every shipped biome-one level. Nothing about them should change.
            var small = BoardFraming.DefaultZoomHalfExtents() * 0.5f;
            var size = BoardFraming.DefaultSize(small, PhoneAspect);

            var room = BoardFraming.RoomFor(Vector2.zero, Vector2.zero, small, size, PhoneAspect);

            Assert.That(room.IsAnywhere, Is.False);
        }

        [Test]
        public void A_board_larger_than_the_screen_offers_both_ways_from_its_middle()
        {
            var large = BoardFraming.DefaultZoomHalfExtents() * 3f;
            var size = BoardFraming.DefaultSize(large, PhoneAspect);

            var room = BoardFraming.RoomFor(Vector2.zero, Vector2.zero, large, size, PhoneAspect);

            Assert.That(room.Left, Is.GreaterThan(0f));
            Assert.That(room.Right, Is.GreaterThan(0f));
            Assert.That(room.Left, Is.EqualTo(room.Right).Within(0.0001f), "The middle is the middle.");
        }

        [Test]
        public void The_room_runs_out_exactly_where_the_clamp_stops()
        {
            // The two have to agree, or a mark would still be offering a direction the pan refuses — which
            // is the one way this feature could be worse than nothing.
            var large = BoardFraming.DefaultZoomHalfExtents() * 3f;
            var size = BoardFraming.DefaultSize(large, PhoneAspect);

            var pushedFar = BoardFraming.ClampLookAt(
                new Vector2(1000f, 0f), Vector2.zero, large, size, PhoneAspect);

            var room = BoardFraming.RoomFor(pushedFar, Vector2.zero, large, size, PhoneAspect);

            Assert.That(room.Right, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(room.Left, Is.GreaterThan(0f), "There should still be a way back.");
        }

        [Test]
        public void The_marks_sit_inside_the_boards_own_band()
        {
            // The top and bottom of the screen belong to the reporting strip and the drawer. A mark drawn
            // there would sit on the progress bar or under the tray.
            Assert.That(PanHintView.TopRow, Is.LessThan(1f - BoardFraming.TopStripFraction));
            Assert.That(PanHintView.BottomRow, Is.GreaterThan(BoardFraming.TrayHeightFraction));
            Assert.That(PanHintView.TopRow, Is.GreaterThan(PanHintView.BottomRow));
        }

        [Test]
        public void The_marks_sit_near_the_edges_rather_than_in_the_way()
        {
            Assert.That(PanHintView.EdgeInset, Is.InRange(0.01f, 0.1f));
        }
    }
}
