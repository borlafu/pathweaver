using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// How much of a board the camera shows, and how far it may be moved.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Extracted from <see cref="BoardCameraFitter"/> when boards became large enough to need panning:
    /// the component moves a camera, and this decides where a camera is allowed to be. Free of
    /// <c>UnityEngine</c> components for the same reason <c>LabelMetrics</c> and <c>BoardTilt</c> are —
    /// the arithmetic that decides whether the board fits on a phone should be checkable without one.
    /// </para>
    /// <para>
    /// The hard-won fact this all rests on: an orthographic size is a **half-height**, so the visible
    /// width is that doubled and multiplied by an aspect ratio near 0.45 on a portrait phone. A board
    /// six world units across shows about three. The first build on hardware filled the screen with
    /// four cells and cut the endpoints off both edges, while the square preview looked correct.
    /// </para>
    /// </remarks>
    internal static class BoardFraming
    {
        /// <summary>
        /// The share of screen height reserved for the tray, matching where <c>HeldTileView</c> puts it.
        /// </summary>
        internal const float TrayHeightFraction = 0.24f;

        /// <summary>
        /// The share of screen height the reporting strip at the top occupies.
        /// </summary>
        /// <remarks>
        /// The progress bar sits at 0.94 and the score beneath it at 0.905, so board content above
        /// about 0.86 collides with them. It never mattered while every board fitted on screen with room
        /// to spare; the first board taller than the screen put cells behind the bar and left the score
        /// unreadable over them.
        /// </remarks>
        internal const float TopStripFraction = 0.14f;

        /// <summary>Breathing room around the board, in world units.</summary>
        internal const float MarginWorldUnits = 0.25f;

        /// <summary>
        /// The share of screen height the board may actually use.
        /// </summary>
        /// <remarks>
        /// What is left between the tray and the reporting strip. Everything the player touches is in
        /// the bottom quarter and everything that reports is at the top; the board gets the middle.
        /// </remarks>
        internal static float BoardHeightFraction => 1f - TrayHeightFraction - TopStripFraction;

        /// <summary>
        /// The board radius the default zoom is chosen to show, in cells.
        /// </summary>
        /// <remarks>
        /// Three, which is the largest shape any authored level uses — <c>shape: hexagon 3</c>, 37
        /// cells. Bigger boards open at this zoom rather than zoomed out to fit, so a cell stays the
        /// size a thumb has already learned and the board is navigated instead of squinted at.
        /// </remarks>
        internal const int DefaultZoomRadiusInCells = 3;

        /// <summary>
        /// The orthographic size that shows a rectangle of the given half extents.
        /// </summary>
        /// <remarks>
        /// Whichever axis runs out of room first decides the zoom. The height is divided by the screen
        /// left over above the tray, because the tray covers the bottom of it.
        /// </remarks>
        internal static float SizeFor(Vector2 halfExtents, float aspect)
        {
            var safeAspect = aspect > 0f ? aspect : 1f;

            var sizeForWidth = halfExtents.x / safeAspect;
            var sizeForHeight = halfExtents.y / BoardHeightFraction;

            return Mathf.Max(sizeForWidth, sizeForHeight);
        }

        /// <summary>
        /// How far a board's drawn edge reaches beyond the outermost cell centres.
        /// </summary>
        /// <remarks>
        /// A cell reaches <see cref="HexMetrics.Size"/> beyond its own centre in every direction. The
        /// vertical reach is foreshortened by the lean, and a leaning block also hangs below the plane
        /// of its own top face — miss that and the near rim of the bottom row is clipped, which reads
        /// as a rendering fault rather than a framing one.
        /// </remarks>
        internal static Vector2 CellReach()
            => new Vector2(
                HexMetrics.Size + MarginWorldUnits,
                (HexMetrics.Size * BoardTilt.VerticalForeshortening)
                + BoardTilt.ScreenOverhang
                + MarginWorldUnits);

        /// <summary>
        /// The orthographic size a board opens at, once the flight has landed.
        /// </summary>
        /// <remarks>
        /// The smaller of "the whole board" and "a board of
        /// <see cref="DefaultZoomRadiusInCells"/>". A board that already fits is never zoomed past
        /// fitting it — there is nothing to navigate — and a board that does not fit opens at the
        /// familiar zoom and is panned.
        /// </remarks>
        internal static float DefaultSize(Vector2 boardHalfExtents, float aspect)
            => Mathf.Min(
                SizeFor(boardHalfExtents + CellReach(), aspect),
                SizeFor(DefaultZoomHalfExtents() + CellReach(), aspect));

        /// <summary>
        /// The half extents of the board shape the default zoom is measured against.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Over a hexagonal board of radius N, the furthest a cell centre reaches horizontally is N
        /// column steps — the widest cell is the one straight out along the q axis, since going around
        /// the rim trades a column for half of one. Vertically it is N row steps, and a row step is
        /// one and a half times the cell radius because rows of pointy-top hexes interlock.
        /// </para>
        /// <para>
        /// Only the vertical reach is foreshortened: the lean is about the X axis, so it shortens
        /// vertical distances and leaves horizontal ones alone.
        /// </para>
        /// </remarks>
        internal static Vector2 DefaultZoomHalfExtents()
            => new Vector2(
                HexMetrics.CellSpacing * DefaultZoomRadiusInCells,
                HexMetrics.Size * 1.5f * DefaultZoomRadiusInCells * BoardTilt.VerticalForeshortening);

        /// <summary>
        /// Whether a board is larger than the camera shows at the default zoom.
        /// </summary>
        internal static bool NeedsPanning(Vector2 boardHalfExtents, float aspect)
            => SizeFor(boardHalfExtents + CellReach(), aspect)
               > DefaultSize(boardHalfExtents, aspect) + 0.0001f;

        /// <summary>
        /// The camera position that centres the board area on a point.
        /// </summary>
        /// <remarks>
        /// The board area is the screen between the tray and the reporting strip, so its centre sits
        /// slightly above the screen centre — which means the camera sits slightly below the point it is
        /// looking at. When the two strips were the same size this offset would be zero; the tray is the
        /// larger, so it is not.
        /// </remarks>
        internal static Vector2 CameraPositionFor(Vector2 lookAt, float orthographicSize)
            => new Vector2(
                lookAt.x,
                lookAt.y - (orthographicSize * (TrayHeightFraction - TopStripFraction)));

        /// <summary>
        /// How much further the view may still be moved, in world units, in each direction.
        /// </summary>
        /// <remarks>
        /// Zero in a direction means the clamp has been reached that way. Nothing on screen said so
        /// before, which left a player on a large board with no way to know the board continued past the
        /// edge — or that they had run out of board.
        /// </remarks>
        internal readonly struct PanRoom
        {
            internal PanRoom(float left, float right, float down, float up)
            {
                Left = left;
                Right = right;
                Down = down;
                Up = up;
            }

            internal float Left { get; }

            internal float Right { get; }

            internal float Down { get; }

            internal float Up { get; }

            /// <summary>Whether there is anywhere left to go at all.</summary>
            internal bool IsAnywhere => Left > 0f || Right > 0f || Down > 0f || Up > 0f;
        }

        /// <summary>
        /// How far the view may still move from where it is, in each direction.
        /// </summary>
        /// <remarks>
        /// The same slack <see cref="ClampLookAt"/> enforces, measured from the current point rather than
        /// applied to it — so the two cannot disagree about where the edge of the board is.
        /// </remarks>
        internal static PanRoom RoomFor(
            Vector2 lookAt, Vector2 boardCentre, Vector2 boardHalfExtents, float orthographicSize, float aspect)
        {
            var reach = boardHalfExtents + CellReach();
            var visible = new Vector2(
                orthographicSize * (aspect > 0f ? aspect : 1f),
                orthographicSize * BoardHeightFraction);

            var slack = new Vector2(
                Mathf.Max(0f, reach.x - visible.x),
                Mathf.Max(0f, reach.y - visible.y));

            return new PanRoom(
                Mathf.Max(0f, lookAt.x - (boardCentre.x - slack.x)),
                Mathf.Max(0f, (boardCentre.x + slack.x) - lookAt.x),
                Mathf.Max(0f, lookAt.y - (boardCentre.y - slack.y)),
                Mathf.Max(0f, (boardCentre.y + slack.y) - lookAt.y));
        }

        /// <summary>
        /// Holds a look-at point inside the board, so panning cannot lose it.
        /// </summary>
        /// <remarks>
        /// Clamped to the board's own extents shrunk by what the camera shows, so the view always
        /// contains board. When the board is smaller than the view on an axis, that axis is pinned to
        /// the board's centre rather than clamped to an inverted range — which would have snapped the
        /// board to whichever edge the maths reached first.
        /// </remarks>
        internal static Vector2 ClampLookAt(
            Vector2 lookAt, Vector2 boardCentre, Vector2 boardHalfExtents, float orthographicSize, float aspect)
        {
            var reach = boardHalfExtents + CellReach();
            var visible = new Vector2(
                orthographicSize * (aspect > 0f ? aspect : 1f),
                orthographicSize * BoardHeightFraction);

            var slack = new Vector2(
                Mathf.Max(0f, reach.x - visible.x),
                Mathf.Max(0f, reach.y - visible.y));

            return new Vector2(
                Mathf.Clamp(lookAt.x, boardCentre.x - slack.x, boardCentre.x + slack.x),
                Mathf.Clamp(lookAt.y, boardCentre.y - slack.y, boardCentre.y + slack.y));
        }
    }
}
