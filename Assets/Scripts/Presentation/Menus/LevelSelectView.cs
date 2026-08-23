using System.Collections.Generic;
using Pathweaver.Core.Campaign;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Levels;
using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation.Menus
{
    /// <summary>
    /// Choose a level, shown as a miniature of its own board.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There is no font, so a level cannot be labelled with a number. Drawing each level as a small
    /// version of its shape turns that constraint into something better than a digit: a player
    /// recognises "the tall corridor" faster than they recognise "3", and the preview says something
    /// about the level rather than only where it sits in a list.
    /// </para>
    /// <para>
    /// Locked levels are dimmed rather than hidden, so the length of the campaign is visible from
    /// the start.
    /// </para>
    /// </remarks>
    internal sealed class LevelSelectView : MonoBehaviour
    {
        internal const string BackId = "back";

        /// <summary>
        /// The band of screen the grid may use, in viewport fractions.
        /// </summary>
        /// <remarks>
        /// Stops below the back button rather than at the screen edge, so a growing campaign cannot
        /// push a level underneath a control.
        /// </remarks>
        private const float TopEdge = 0.86f;

        /// <summary>
        /// Where the grid must stop, leaving the back button its corner.
        /// </summary>
        /// <remarks>
        /// Raised after the bottom-left level landed on top of the back button. Reserving the space is
        /// better than nudging the grid, because the collision only appears at certain level counts and
        /// would come back the next time the campaign grew.
        /// </remarks>
        private const float BottomEdge = 0.26f;

        private const float HorizontalMargin = 0.1f;

        private readonly List<HexButton> _levelButtons = new List<HexButton>();

        private HexButton _back;
        private Camera _camera;
        private Material _material;

        internal void Build(Camera camera, Material material, Campaign campaign, CampaignProgress progress)
        {
            _camera = camera;
            _material = material;

            Clear();

            var ids = campaign.LevelIds;
            var layout = Arrange(ids.Count, MenuCamera.WorldExtents(camera));

            for (var index = 0; index < ids.Count; index++)
            {
                var id = ids[index];
                var unlocked = campaign.IsUnlocked(id, progress);
                var cleared = progress.IsCleared(id);

                var column = index % layout.Columns;
                var row = index / layout.Columns;

                var viewport = new Vector2(
                    0.5f + ((column - ((layout.Columns - 1) * 0.5f)) * layout.ColumnStep),
                    layout.FirstRow - (row * layout.RowStep));

                var colour = !unlocked
                    ? BoardPalette.LevelLocked
                    : cleared ? BoardPalette.LevelCleared : BoardPalette.LevelOpen;

                var button = HexButton.Create(
                    transform, id, camera, material, viewport, layout.Radius, colour,
                    touchRadiusFraction: layout.TouchRadiusFraction);

                button.SetEnabled(unlocked, colour);
                AddShapePreview(button, id, unlocked);

                _levelButtons.Add(button);
            }

            _back = HexButton.Create(
                transform, BackId, camera, material,
                new Vector2(0.14f, 0.09f), 0.4f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.12f);

            // A triangle pointing back the way the player came. 90 degrees puts the apex at 180,
            // which is left; 30 was the same shape as the play button's -90 and pointed right, so
            // the back control read as another play control.
            _back.AddGlyph(
                HexMeshFactory.CreateRegularPolygon(3, 0.19f, rotationDegrees: 90f), BoardPalette.MenuGlyph);
        }

        /// <summary>
        /// How many columns to use, how large the buttons are, and where the grid sits.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Computed from the level count rather than fixed. A three-column grid at the original
        /// spacing ran off the bottom of the screen at about twelve levels, which a twenty-level
        /// campaign would have hit — and scrolling would have put a drag gesture in a screen whose
        /// only other gesture is a tap, which is how a mis-swipe becomes a level launch.
        /// </para>
        /// <para>
        /// The two units in play are the reason this needs care: positions are viewport fractions
        /// while a button's radius is world units. Deriving the row spacing from the radius without
        /// converting made the rows a screen-height's worth of world units apart, which on the
        /// device showed three rows of levels marooned in empty space. <paramref name="worldExtents"/>
        /// is what converts between them.
        /// </para>
        /// </remarks>
        private static (int Columns, float Radius, float ColumnStep, float RowStep, float FirstRow,
            float TouchRadiusFraction) Arrange(int levelCount, Vector2 worldExtents)
        {
            // Wider grids for longer campaigns, so everything stays on one screen.
            var columns = levelCount <= 12 ? 3 : levelCount <= 20 ? 4 : 5;
            var rows = Mathf.Max(1, Mathf.CeilToInt(levelCount / (float)columns));

            var columnStep = (1f - (HorizontalMargin * 2f)) / columns;

            // A pointy-top hexagon is radius * sqrt(3) wide, so this leaves a gap of roughly a
            // sixth of a column between neighbours.
            var radius = columnStep * worldExtents.x * 0.48f;

            // A row is two radii tall. Spacing them by rather more than that keeps the grid open
            // without letting it drift apart, and the band caps it when the campaign grows.
            var naturalStep = radius * 3f / worldExtents.y;
            var available = TopEdge - BottomEdge;
            var rowStep = rows > 1 ? Mathf.Min(naturalStep, available / (rows - 1)) : 0f;

            // Centred in the band rather than pinned to the top, so a short campaign does not sit
            // in one corner of a tall screen.
            var firstRow = ((TopEdge + BottomEdge) * 0.5f) + ((rows - 1) * rowStep * 0.5f);

            // Touch radii are a fraction of the shorter screen edge, which in portrait is the width
            // the world extent's x maps onto.
            var touchRadiusFraction = Mathf.Max(radius / worldExtents.x * 0.95f, 0.07f);

            return (columns, radius, columnStep, rowStep, firstRow, touchRadiusFraction);
        }

        /// <summary>
        /// Which level or control a tap landed on, or null.
        /// </summary>
        internal string ButtonAt(Vector2 screenPosition)
        {
            if (_back != null && _back.IsPressed(screenPosition))
            {
                return BackId;
            }

            foreach (var button in _levelButtons)
            {
                if (button.IsPressed(screenPosition))
                {
                    return button.Id;
                }
            }

            return null;
        }

        /// <summary>
        /// Draws the level's own board, shrunk to fit the button.
        /// </summary>
        /// <remarks>
        /// Parsed from the level file rather than stored separately, so a preview can never
        /// disagree with the level it claims to show.
        /// </remarks>
        private void AddShapePreview(HexButton button, string levelId, bool unlocked)
        {
            LevelDefinition level;

            try
            {
                level = LevelCatalogue.Load(levelId);
            }
            catch (LevelFormatException error)
            {
                Debug.LogWarning($"[levels] {levelId} could not be previewed: {error.Message}");
                return;
            }

            var extent = 0.001f;
            foreach (var cell in level.Shape)
            {
                var world = HexMetrics.ToWorld(cell);
                extent = Mathf.Max(extent, Mathf.Max(Mathf.Abs(world.x), Mathf.Abs(world.y)));
            }

            // Scaled so the widest board still fits inside the button, which keeps previews
            // comparable: a bigger drawing means a bigger level.
            var scale = button.Radius * 0.72f / (extent + HexMetrics.Size);
            var mesh = HexMeshFactory.CreateHexagon(HexMetrics.Size * scale * 0.86f);
            var colour = unlocked ? BoardPalette.MenuGlyph : BoardPalette.LevelLockedGlyph;

            foreach (var cell in level.Shape)
            {
                button.AddGlyph(mesh, colour, HexMetrics.ToWorld(cell) * scale);
            }
        }

        private void Clear()
        {
            foreach (var button in _levelButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            _levelButtons.Clear();

            if (_back != null)
            {
                Destroy(_back.gameObject);
                _back = null;
            }
        }
    }
}
