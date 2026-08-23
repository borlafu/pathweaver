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

        private const int Columns = 3;
        private const float ButtonRadius = 0.44f;

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
            for (var index = 0; index < ids.Count; index++)
            {
                var id = ids[index];
                var unlocked = campaign.IsUnlocked(id, progress);
                var cleared = progress.IsCleared(id);

                var column = index % Columns;
                var row = index / Columns;

                var viewport = new Vector2(
                    0.5f + ((column - (Columns - 1) * 0.5f) * 0.26f),
                    0.78f - (row * 0.19f));

                var colour = !unlocked
                    ? BoardPalette.LevelLocked
                    : cleared ? BoardPalette.LevelCleared : BoardPalette.LevelOpen;

                var button = HexButton.Create(
                    transform, id, camera, material, viewport, ButtonRadius, colour,
                    touchRadiusFraction: 0.11f);

                button.SetEnabled(unlocked, colour);
                AddShapePreview(button, id, unlocked);

                _levelButtons.Add(button);
            }

            _back = HexButton.Create(
                transform, BackId, camera, material,
                new Vector2(0.14f, 0.09f), 0.4f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.12f);

            // A triangle pointing back the way the player came.
            _back.AddGlyph(
                HexMeshFactory.CreateRegularPolygon(3, 0.19f, rotationDegrees: 30f), BoardPalette.MenuGlyph);
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
