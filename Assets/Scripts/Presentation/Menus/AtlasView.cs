using System.Collections.Generic;
using Pathweaver.Core.Atlas;
using Pathweaver.Core.Hex;
using UnityEngine;

namespace Pathweaver.Game.Presentation.Menus
{
    /// <summary>
    /// The World Atlas: nodes on a constellation, bought with Star Essence.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Drawn on the same hex grid the game is played on, because the nodes carry hex coordinates and
    /// reusing <see cref="HexMetrics"/> means one set of geometry for both. PRD section 4.2 asks for a
    /// constellation rather than a ladder, and the arrangement comes from the pack files, so a docking
    /// biome pack extends the picture without this screen knowing about it.
    /// </para>
    /// <para>
    /// State is carried by colour, and cost by pips on the node: unlocked, affordable now, reachable
    /// but not yet affordable, and locked behind something else. Four states is the most this can say
    /// without a font, and it is enough to answer "what can I buy" at a glance.
    /// </para>
    /// </remarks>
    internal sealed class AtlasView : MonoBehaviour
    {
        internal const string BackId = "back";

        /// <summary>
        /// A node's radius as a share of the distance between two nodes.
        /// </summary>
        /// <remarks>
        /// Everything scales from the spacing, which is computed from the constellation's own extents
        /// so a pack of any shape fits the screen. The first version used fixed world units and the
        /// outer nodes ran off both edges the moment the region was three columns wide.
        /// </remarks>
        private const float NodeRadiusFactor = 0.40f;

        private const float LinkThicknessFactor = 0.14f;

        /// <summary>The band of screen the constellation may use, in viewport fractions.</summary>
        private const float TopEdge = 0.95f;
        private const float BottomEdge = 0.28f;

        /// <summary>Breathing room around the constellation, in world units.</summary>
        private const float MarginWorldUnits = 0.12f;

        /// <summary>Essence pips shown before the row is capped.</summary>
        private const int MaximumEssencePips = 12;

        private readonly List<HexButton> _nodeButtons = new List<HexButton>();
        private readonly List<GameObject> _decorations = new List<GameObject>();
        private readonly List<MeshRenderer> _essencePips = new List<MeshRenderer>();

        private HexButton _back;
        private Material _material;
        private Camera _camera;
        private float _spacing = 1f;
        private float _radius = 0.4f;

        /// <summary>
        /// Draws the constellation for the given progress.
        /// </summary>
        internal void Build(Camera camera, Material material, AtlasMap map, AtlasProgress progress)
        {
            _camera = camera;
            _material = material;

            Clear();

            var centre = Centre(map);
            Fit(map, centre);

            // Links first, so a node is drawn over the lines that reach it.
            foreach (var node in map.Nodes)
            {
                foreach (var required in node.Requires)
                {
                    if (!map.Contains(required))
                    {
                        continue;
                    }

                    DrawLink(
                        WorldPositionOf(map.Node(required).Position, centre),
                        WorldPositionOf(node.Position, centre),
                        progress.IsUnlocked(required) && progress.IsUnlocked(node.Id));
                }
            }

            foreach (var node in map.Nodes)
            {
                DrawNode(node, map, progress, centre);
            }

            DrawEssence(progress.Essence);

            _back = HexButton.Create(
                transform, BackId, camera, material,
                new Vector2(0.14f, 0.09f), 0.4f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.12f);

            // Left, matching every other back control in the game.
            _back.AddGlyph(
                HexMeshFactory.CreateRegularPolygon(3, 0.19f, rotationDegrees: 90f), BoardPalette.MenuGlyph);
        }

        /// <summary>
        /// Which node or control a tap landed on, or null.
        /// </summary>
        internal string ButtonAt(Vector2 screenPosition)
        {
            if (_back != null && _back.IsPressed(screenPosition))
            {
                return BackId;
            }

            foreach (var button in _nodeButtons)
            {
                if (button.IsPressed(screenPosition))
                {
                    return button.Id;
                }
            }

            return null;
        }

        /// <summary>
        /// The midpoint of the constellation, so it sits centred however the packs are laid out.
        /// </summary>
        private static Vector3 Centre(AtlasMap map)
        {
            if (map.Nodes.Count == 0)
            {
                return Vector3.zero;
            }

            var minimum = new Vector2(float.MaxValue, float.MaxValue);
            var maximum = new Vector2(float.MinValue, float.MinValue);

            foreach (var node in map.Nodes)
            {
                var world = HexMetrics.ToWorld(node.Position);
                minimum = Vector2.Min(minimum, new Vector2(world.x, world.y));
                maximum = Vector2.Max(maximum, new Vector2(world.x, world.y));
            }

            var middle = (minimum + maximum) * 0.5f;
            return new Vector3(middle.x, middle.y, 0f);
        }

        /// <summary>
        /// Chooses a spacing that fits the whole constellation on screen.
        /// </summary>
        /// <remarks>
        /// Measured against the width and the height separately, and the tighter of the two wins —
        /// the same reasoning as <see cref="BoardCameraFitter"/>, except the layout scales rather than
        /// the camera, because the menu camera has to keep its framing for the controls drawn on top.
        /// </remarks>
        private void Fit(AtlasMap map, Vector3 centre)
        {
            var extentX = 0.001f;
            var extentY = 0.001f;

            foreach (var node in map.Nodes)
            {
                var world = HexMetrics.ToWorld(node.Position) - centre;
                extentX = Mathf.Max(extentX, Mathf.Abs(world.x));
                extentY = Mathf.Max(extentY, Mathf.Abs(world.y));
            }

            var visible = MenuCamera.WorldExtents(_camera);
            var halfWidth = (visible.x * 0.5f) - MarginWorldUnits;
            var halfHeight = (visible.y * (TopEdge - BottomEdge) * 0.5f) - MarginWorldUnits;

            var forWidth = halfWidth / (extentX + NodeRadiusFactor);
            var forHeight = halfHeight / (extentY + NodeRadiusFactor);

            _spacing = Mathf.Max(0.1f, Mathf.Min(forWidth, forHeight));
            _radius = _spacing * NodeRadiusFactor;
        }

        private Vector3 WorldPositionOf(HexCoord coordinate, Vector3 centre)
        {
            var world = (HexMetrics.ToWorld(coordinate) - centre) * _spacing;

            // Centred in the band, which leaves the essence row and the back control their own space
            // below rather than drawing the constellation over them.
            var band = _camera.ViewportToWorldPoint(new Vector3(0.5f, (TopEdge + BottomEdge) * 0.5f, 0f));

            return new Vector3(world.x + band.x, world.y + band.y, -1.4f);
        }

        private void DrawNode(AtlasNode node, AtlasMap map, AtlasProgress progress, Vector3 centre)
        {
            var unlocked = progress.IsUnlocked(node.Id);
            var affordable = map.CanUnlock(node.Id, progress);
            var reachable = IsReachable(node, progress);

            var colour = unlocked
                ? BoardPalette.AtlasUnlocked
                : affordable
                    ? BoardPalette.AtlasAffordable
                    : reachable ? BoardPalette.AtlasReachable : BoardPalette.LevelLocked;

            var position = WorldPositionOf(node.Position, centre);
            var viewport = _camera.WorldToViewportPoint(new Vector3(position.x, position.y, 0f));

            var button = HexButton.Create(
                transform, node.Id, _camera, _material,
                new Vector2(viewport.x, viewport.y), _radius, colour,
                touchRadiusFraction: Mathf.Max(_radius / MenuCamera.WorldExtents(_camera).x * 0.9f, 0.06f));

            // What the node gives, as its own mark: a hexagon for a token, a chevron pair for a skip,
            // a small star for essence. The same three shapes the HUD already uses for those things.
            button.AddGlyph(
                EffectGlyph(node.Effect.Kind, _radius),
                EffectColour(node.Effect.Kind, unlocked),
                new Vector3(0f, _radius * 0.22f, 0f));

            if (!unlocked)
            {
                DrawCost(button, node.Cost, affordable);
            }

            _nodeButtons.Add(button);
        }

        /// <summary>Whether every prerequisite is unlocked, whatever the essence balance is.</summary>
        private static bool IsReachable(AtlasNode node, AtlasProgress progress)
        {
            foreach (var required in node.Requires)
            {
                if (!progress.IsUnlocked(required))
                {
                    return false;
                }
            }

            return true;
        }

        private static Mesh EffectGlyph(AtlasEffectKind kind, float radius) => kind switch
        {
            AtlasEffectKind.Token => HexMeshFactory.CreateHexagon(radius * 0.34f),
            AtlasEffectKind.Skip => HexMeshFactory.CreateRectangle(radius * 0.5f, radius * 0.17f),
            _ => HexMeshFactory.CreateRegularPolygon(4, radius * 0.36f, rotationDegrees: 45f),
        };

        private static Color EffectColour(AtlasEffectKind kind, bool unlocked)
        {
            if (!unlocked)
            {
                return BoardPalette.AtlasGlyphLocked;
            }

            return kind switch
            {
                AtlasEffectKind.Token => BoardPalette.TokenHeld,
                AtlasEffectKind.Skip => BoardPalette.SkipHeld,
                _ => BoardPalette.AtlasEssence,
            };
        }

        /// <summary>
        /// Draws a node's cost as pips beneath it.
        /// </summary>
        /// <remarks>
        /// Pips rather than a number, because there is still no font — and at these costs a row of
        /// shapes reads faster than a digit would anyway. They sit inside the node's lower half: below
        /// it, they collided with whatever node the constellation put underneath.
        /// </remarks>
        private void DrawCost(HexButton button, int cost, bool affordable)
        {
            const int perRow = 5;
            var colour = affordable ? BoardPalette.AtlasEssence : BoardPalette.AtlasCostUnaffordable;

            var pipRadius = _radius * 0.11f;
            var pipSpacing = _radius * 0.29f;
            var mesh = HexMeshFactory.CreateHexagon(pipRadius);
            var rows = Mathf.CeilToInt(cost / (float)perRow);

            for (var index = 0; index < cost; index++)
            {
                var row = index / perRow;
                var column = index % perRow;
                var inRow = Mathf.Min(cost - (row * perRow), perRow);

                var x = (column - ((inRow - 1) * 0.5f)) * pipSpacing;
                var y = (-_radius * 0.34f) - ((row - ((rows - 1) * 0.5f)) * pipSpacing * 0.95f);

                button.AddGlyph(mesh, colour, new Vector3(x, y, 0f));
            }
        }

        private void DrawLink(Vector3 from, Vector3 to, bool lit)
        {
            var difference = to - from;
            var length = difference.magnitude;
            if (length < 0.001f)
            {
                return;
            }

            var link = new GameObject("Link");
            link.transform.SetParent(transform, worldPositionStays: false);
            link.transform.position = new Vector3(
                (from.x + to.x) * 0.5f, (from.y + to.y) * 0.5f, -1.3f);
            link.transform.rotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg);

            link.AddComponent<MeshFilter>().sharedMesh =
                HexMeshFactory.CreateRectangle(length, _radius * LinkThicknessFactor);

            var renderer = link.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.material.color = lit ? BoardPalette.AtlasLinkLit : BoardPalette.AtlasLinkDim;

            _decorations.Add(link);
        }

        /// <summary>
        /// Draws the essence balance as a row of pips along the bottom.
        /// </summary>
        /// <remarks>
        /// Capped at twelve. A balance can grow past that, and a row of forty shapes says less than a
        /// full row does — what a player needs from this is whether the next node is within reach,
        /// which the node's own cost pips answer.
        /// </remarks>
        private void DrawEssence(int essence)
        {
            var shown = Mathf.Min(essence, MaximumEssencePips);
            var mesh = HexMeshFactory.CreateRegularPolygon(4, 0.075f, rotationDegrees: 45f);

            for (var index = 0; index < shown; index++)
            {
                var pip = new GameObject($"Essence{index}");
                pip.transform.SetParent(transform, worldPositionStays: false);

                var viewport = new Vector3(0.5f + ((index - ((shown - 1) * 0.5f)) * 0.055f), 0.16f, 0f);
                var world = _camera.ViewportToWorldPoint(viewport);
                pip.transform.position = new Vector3(world.x, world.y, -1.4f);

                pip.AddComponent<MeshFilter>().sharedMesh = mesh;

                var renderer = pip.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _material;
                renderer.material.color = essence > MaximumEssencePips
                    ? BoardPalette.AtlasEssenceFull
                    : BoardPalette.AtlasEssence;

                _essencePips.Add(renderer);
                _decorations.Add(pip);
            }
        }

        private void Clear()
        {
            foreach (var button in _nodeButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            _nodeButtons.Clear();

            foreach (var decoration in _decorations)
            {
                if (decoration != null)
                {
                    Destroy(decoration);
                }
            }

            _decorations.Clear();
            _essencePips.Clear();

            if (_back != null)
            {
                Destroy(_back.gameObject);
                _back = null;
            }
        }
    }
}
