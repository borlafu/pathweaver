using NUnit.Framework;
using Pathweaver.Game.Presentation;
using UnityEngine;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// How much of a board the camera shows, and how far it may be moved.
    /// </summary>
    /// <remarks>
    /// The class this covers exists because a fixed orthographic size cut the endpoints off both edges
    /// of the first device build while the square preview looked correct. Every number here is one that
    /// mistake would have got wrong.
    /// </remarks>
    public class BoardFramingTests
    {
        /// <summary>The aspect of the phone the previews are rendered at: 1080 by 2376.</summary>
        private const float PhoneAspect = 1080f / 2376f;

        /// <summary>Half extents of a board of the default zoom radius, in world units.</summary>
        private static Vector2 DefaultBoard => BoardFraming.DefaultZoomHalfExtents();

        [Test]
        public void A_board_that_already_fits_is_not_zoomed_past_fitting_it()
        {
            // Every shipped level is this size or smaller, so this is the case that must not change.
            var small = DefaultBoard * 0.5f;

            Assert.That(
                BoardFraming.DefaultSize(small, PhoneAspect),
                Is.EqualTo(BoardFraming.SizeFor(small + BoardFraming.CellReach(), PhoneAspect))
                    .Within(0.0001f));
        }

        [Test]
        public void A_board_that_already_fits_needs_no_panning()
        {
            Assert.That(BoardFraming.NeedsPanning(DefaultBoard * 0.5f, PhoneAspect), Is.False);
        }

        [Test]
        public void A_board_larger_than_the_default_zoom_opens_at_the_default_zoom()
        {
            // Twice the radius. The point of a default zoom: a cell stays the size a thumb learned
            // instead of shrinking every time a biome gets bigger.
            var large = DefaultBoard * 2f;

            Assert.That(
                BoardFraming.DefaultSize(large, PhoneAspect),
                Is.EqualTo(BoardFraming.DefaultSize(DefaultBoard, PhoneAspect)).Within(0.0001f));
        }

        [Test]
        public void A_board_larger_than_the_default_zoom_needs_panning()
        {
            Assert.That(BoardFraming.NeedsPanning(DefaultBoard * 2f, PhoneAspect), Is.True);
        }

        [Test]
        public void A_board_exactly_the_default_size_sits_on_the_boundary_and_does_not_pan()
        {
            // The shipped levels' own size. If this ever reported true, every existing level would gain
            // a camera flight and a pan gesture it does not need.
            Assert.That(BoardFraming.NeedsPanning(DefaultBoard, PhoneAspect), Is.False);
        }

        [Test]
        public void The_width_decides_the_zoom_on_a_portrait_phone()
        {
            // The whole reason a fixed orthographic size failed: the size is a half-height, and on a
            // phone the visible width is that times an aspect near 0.45.
            var square = new Vector2(3f, 3f);

            Assert.That(
                BoardFraming.SizeFor(square, PhoneAspect),
                Is.EqualTo(square.x / PhoneAspect).Within(0.0001f));
        }

        [Test]
        public void The_camera_sits_below_what_it_is_looking_at_so_the_tray_does_not_cover_it()
        {
            var position = BoardFraming.CameraPositionFor(Vector2.zero, 4f);

            Assert.That(position.y, Is.LessThan(0f));
            Assert.That(position.y, Is.EqualTo(-4f * BoardFraming.TrayHeightFraction).Within(0.0001f));
        }

        [Test]
        public void Panning_cannot_push_the_board_off_the_screen()
        {
            var large = DefaultBoard * 3f;
            var size = BoardFraming.DefaultSize(large, PhoneAspect);

            // Far further than any thumb could drag in one gesture.
            var clamped = BoardFraming.ClampLookAt(
                new Vector2(1000f, -1000f), Vector2.zero, large, size, PhoneAspect);

            var reach = large + BoardFraming.CellReach();

            Assert.That(Mathf.Abs(clamped.x), Is.LessThanOrEqualTo(reach.x));
            Assert.That(Mathf.Abs(clamped.y), Is.LessThanOrEqualTo(reach.y));
        }

        [Test]
        public void A_board_smaller_than_the_view_is_pinned_to_its_own_centre()
        {
            // Rather than clamped to an inverted range, which would snap the board to whichever edge
            // the arithmetic reached first — a board that jumps sideways when touched.
            var centre = new Vector2(2f, -1f);

            var clamped = BoardFraming.ClampLookAt(
                new Vector2(50f, 50f), centre, DefaultBoard * 0.25f, 20f, PhoneAspect);

            Assert.That(clamped, Is.EqualTo(centre));
        }

        [Test]
        public void Panning_within_the_slack_moves_the_view()
        {
            // The other half of the clamp: it must not pin a large board in place.
            var large = DefaultBoard * 3f;
            var size = BoardFraming.DefaultSize(large, PhoneAspect);

            var moved = BoardFraming.ClampLookAt(
                new Vector2(0.5f, 0f), Vector2.zero, large, size, PhoneAspect);

            Assert.That(moved.x, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void The_default_zoom_shows_the_largest_shape_any_level_uses()
        {
            // shape: hexagon 3, which biome1-01 uses. Lower it and an existing level starts needing to
            // be panned; raise it and cells shrink for everyone.
            Assert.That(BoardFraming.DefaultZoomRadiusInCells, Is.EqualTo(3));
        }
    }
}
