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

        /// <summary>Breathing room around the board, in world units.</summary>
        internal const float MarginWorldUnits = 0.25f;

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
            var sizeForHeight = halfExtents.y / (1f - TrayHeightFraction);

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
        /// The board area is the screen above the tray, so its centre sits above the screen centre by
        /// the tray's share of the half-height — which means the camera sits below the point it is
        /// looking at.
        /// </remarks>
        internal static Vector2 CameraPositionFor(Vector2 lookAt, float orthographicSize)
            => new Vector2(lookAt.x, lookAt.y - (orthographicSize * TrayHeightFraction));

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
                orthographicSize * (1f - TrayHeightFraction));

            var slack = new Vector2(
                Mathf.Max(0f, reach.x - visible.x),
                Mathf.Max(0f, reach.y - visible.y));

            return new Vector2(
                Mathf.Clamp(lookAt.x, boardCentre.x - slack.x, boardCentre.x + slack.x),
                Mathf.Clamp(lookAt.y, boardCentre.y - slack.y, boardCentre.y + slack.y));
        }
    }
}
