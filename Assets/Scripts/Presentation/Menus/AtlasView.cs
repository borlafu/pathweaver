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
    /// State is carried by colour — unlocked, affordable now, reachable but not yet affordable, and
    /// locked behind something else — and that is still worth having, because it answers "what can I
    /// buy" at a glance. But it was all this screen said, which is why the atlas was withheld from the
    /// closed test: a player met coloured hexagons and guessed. The colours are now the summary and the
    /// words are the answer, in <see cref="Text.AtlasWords"/>.
    /// </para>
    /// <para>
    /// A tap selects a node and says what it costs and what it gives; a second tap on the same node buys
    /// it. Arming before spending is the pattern the Pivot Token and the erase-everything control already
    /// use, and for the same reason: essence is slow to earn, and a screen where one stray tap spends it
    /// is a screen a player learns to be careful on rather than to explore.
    /// </para>
    /// </remarks>
    internal sealed class AtlasView : MonoBehaviour
    {
        internal const string BackId = "back";

        /// <summary>Where the line naming what a node gives sits, as a viewport fraction.</summary>
        internal const float EffectViewportY = 0.25f;

        /// <summary>Where the line saying whether it can be bought sits.</summary>
        internal const float StatusViewportY = 0.18f;

        /// <summary>Where the balance sits.</summary>
        internal const float BalanceViewportY = 0.09f;

        /// <summary>How much of the width a line of this screen's text may use.</summary>
        internal const float WrapWidthFraction = 0.84f;

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

        /// <summary>
        /// The band of screen the constellation may use, in viewport fractions.
        /// </summary>
        /// <remarks>
        /// The bottom edge rose from 0.28 when the two lines of text arrived beneath it: the pips they
        /// replaced took one row, and a sentence that wraps takes two or three.
        /// </remarks>
        private const float TopEdge = 0.95f;
        private const float BottomEdge = 0.34f;

        /// <summary>Breathing room around the constellation, in world units.</summary>
        private const float MarginWorldUnits = 0.12f;

        private readonly List<HexButton> _nodeButtons = new List<HexButton>();
        private readonly List<GameObject> _decorations = new List<GameObject>();

        private HexButton _back;
        private Material _material;
        private Camera _camera;
        private float _spacing = 1f;
        private float _radius = 0.4f;

        private Text.TextLabel _effect;
        private Text.TextLabel _status;
        private Text.TextLabel _balance;

        private AtlasMap _map;
        private AtlasProgress _progress;

        /// <summary>
        /// The node a tap has selected, or null.
        /// </summary>
        /// <remarks>
        /// Kept across a rebuild, because buying a node redraws the whole screen and the node the player
        /// was reading about should still be the one described afterwards.
        /// </remarks>
        internal string SelectedNode { get; private set; }

        /// <summary>
        /// Draws the constellation for the given progress.
        /// </summary>
        internal void Build(Camera camera, Material material, AtlasMap map, AtlasProgress progress)
        {
            _camera = camera;
            _material = material;
            _map = map;
            _progress = progress;

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

            _effect = AddLine("effect", EffectViewportY, BoardPalette.TextPrimary);
            _status = AddLine("status", StatusViewportY, BoardPalette.TextSecondary);
            _balance = AddLine("balance", BalanceViewportY, BoardPalette.AtlasEssence);

            _back = HexButton.Create(
                transform, BackId, camera, material,
                new Vector2(0.14f, 0.09f), 0.4f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.12f);

            MenuGlyphs.AddBack(_back);

            Describe();
        }

        /// <summary>
        /// Selects a node, or confirms the one already selected.
        /// </summary>
        /// <remarks>
        /// Returns whether the caller should go on to buy it, so the spending itself stays where the rest
        /// of the game's state changes are — this screen decides what the player is asking about, not what
        /// they own.
        /// </remarks>
        internal bool Select(string nodeId)
        {
            var confirming = nodeId != null && nodeId == SelectedNode;

            SelectedNode = nodeId;

            // Redrawn rather than only re-described, because the ring that says which node is armed is
            // part of the node. The first version wrote the words and left the constellation unchanged, so
            // a player reading "tap again to unlock it" had nothing on screen saying which node "it" was.
            if (_map != null && _progress != null && _camera != null && _material != null)
            {
                Build(_camera, _material, _map, _progress);
            }
            else
            {
                Describe();
            }

            return confirming;
        }

        /// <summary>Drops the selection, so leaving and returning starts from the introduction.</summary>
        internal void ClearSelection()
        {
            SelectedNode = null;
            Describe();
        }

        /// <summary>
        /// Writes the two lines under the constellation for whatever is selected.
        /// </summary>
        /// <remarks>
        /// With nothing selected they carry the introduction rather than nothing, because an empty half
        /// of the screen is what made the first version of this screen unreadable.
        /// </remarks>
        private void Describe()
        {
            if (_balance != null && _progress != null)
            {
                _balance.SetText(Text.AtlasWords.Balance(_progress.Essence));
            }

            var node = SelectedNode != null && _map != null && _map.Contains(SelectedNode)
                ? _map.Node(SelectedNode)
                : null;

            if (node == null)
            {
                _effect?.SetText(Text.AtlasWords.Introduction);
                _status?.SetText(string.Empty);
                return;
            }

            _effect?.SetText(Text.AtlasWords.Effect(node.Effect));
            _status?.SetText(Text.AtlasWords.Status(node, _map, _progress));
        }

        private Text.TextLabel AddLine(string lineName, float viewportY, Color colour)
        {
            var label = Text.TextLabel.Create(
                transform,
                _camera,
                lineName,
                new Vector2(0.5f, viewportY),
                Text.LabelMetrics.BodyHeightFraction,
                colour,
                TMPro.TextAlignmentOptions.Center,
                HexButton.LabelDepth);

            label.SetWrapWidth(WrapWidthFraction);

            return label;
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
                MarkFor(node.Effect.Kind, _radius),
                EffectColour(node.Effect.Kind, unlocked),
                new Vector3(0f, _radius * 0.22f, 0f));

            if (!unlocked)
            {
                DrawCost(button, node, affordable);
            }

            if (node.Id == SelectedNode)
            {
                DrawSelectionMark(button);
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

        /// <summary>
        /// The mark a node wears, one shape per effect.
        /// </summary>
        /// <remarks>
        /// Every kind is listed and an unknown one throws, rather than falling through to whichever mark
        /// happened to be the default. `Discount` shipped wearing the essence diamond for exactly that
        /// reason: the switch had a catch-all, so a new effect silently borrowed an existing mark and two
        /// different relics looked like the same relic.
        /// </remarks>
        internal static Mesh MarkFor(AtlasEffectKind kind, float radius) => kind switch
        {
            AtlasEffectKind.Token => HexMeshFactory.CreateHexagon(radius * 0.34f),
            AtlasEffectKind.Skip => HexMeshFactory.CreateRectangle(radius * 0.5f, radius * 0.17f),
            AtlasEffectKind.Essence =>
                HexMeshFactory.CreateRegularPolygon(4, radius * 0.36f, rotationDegrees: 45f),

            // A triangle pointing down: the price comes down. Distinct from the bar, the hexagon and the
            // diamond, which is what matters — colour may never carry a fact on its own here.
            AtlasEffectKind.Discount =>
                HexMeshFactory.CreateRegularPolygon(3, radius * 0.38f, rotationDegrees: 180f),

            _ => throw new System.ArgumentOutOfRangeException(
                nameof(kind), kind, "This effect has no mark. Give it one rather than letting it borrow."),
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

                // Essence and discount share a colour on purpose: both are about Star Essence, and the
                // shape is what tells them apart. That is the same division the board uses, where a
                // resource says its kind by motif as well as by colour.
                _ => BoardPalette.AtlasEssence,
            };
        }

        /// <summary>
        /// Draws a node's cost as a numeral in its lower half.
        /// </summary>
        /// <remarks>
        /// <para>
        /// It used to be a row of pips, on the grounds that a row of shapes reads faster than a digit.
        /// That was true of a cost of three and false of a cost of twenty, which took four rows and had to
        /// be counted — and counting is the thing a numeral removes. The first region costs 51 across
        /// eleven nodes, so several of them were never going to be pip-sized.
        /// </para>
        /// <para>
        /// Inside the node rather than below it: below, it collided with whatever node the constellation
        /// happened to put underneath.
        /// </para>
        /// </remarks>
        private void DrawCost(HexButton button, AtlasNode node, bool affordable)
        {
            var viewport = _camera.WorldToViewportPoint(button.transform.position);

            var label = Text.TextLabel.Create(
                transform,
                _camera,
                $"cost-{node.Id}",
                new Vector2(viewport.x, viewport.y - MenuCamera.ViewportHalfHeight(_radius * 0.42f)),
                Text.LabelMetrics.CaptionHeightFraction,
                affordable ? BoardPalette.AtlasEssence : BoardPalette.AtlasCostUnaffordable,
                TMPro.TextAlignmentOptions.Center,
                HexButton.LabelDepth);

            // What the player will be charged rather than what the pack file says, so the numerals visibly
            // fall the moment a discount relic is bought. That is the only feedback a discount has.
            label.SetText(_map.CostOf(node.Id, _progress)
                .ToString(System.Globalization.CultureInfo.InvariantCulture));

            _decorations.Add(label.gameObject);
        }

        /// <summary>
        /// Rings the node a tap has selected.
        /// </summary>
        /// <remarks>
        /// A ring rather than a change of colour, because colour on this screen already means "what state
        /// is this node in" and a fifth meaning would collide with the four it has. It is also the second
        /// tap that spends, so which node is armed has to be unmistakable.
        /// </remarks>
        private void DrawSelectionMark(HexButton button)
        {
            button.AddGlyph(
                GlyphMeshFactory.CreateRing(_radius * 0.92f, _radius * 0.1f),
                BoardPalette.TokenArmed,
                Vector3.zero);
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

            foreach (var label in new[] { _effect, _status, _balance })
            {
                if (label != null)
                {
                    Destroy(label.gameObject);
                }
            }

            _effect = null;
            _status = null;
            _balance = null;

            if (_back != null)
            {
                Destroy(_back.gameObject);
                _back = null;
            }
        }
    }
}
