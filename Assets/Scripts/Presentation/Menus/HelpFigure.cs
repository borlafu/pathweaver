using System.Collections.Generic;
using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Tiles;
using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation.Menus
{
    /// <summary>
    /// A small picture under each help page, drawn from the game's own cells.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Words alone made the help screen worth shipping, but they left it describing shapes in prose: "a
    /// spring's ring grows outward from its centre" is a sentence about something the player has been
    /// looking at for twenty levels without knowing what it meant. A picture beside the sentence closes
    /// that gap in one glance, and a page about a control the player cannot find has to point at it.
    /// </para>
    /// <para>
    /// The figures are made of <see cref="CellView"/> and <see cref="BoardGlyphs"/> — the board's own cell
    /// and the drawer's own marks — rather than of illustrations of them. That is the whole design: a
    /// diagram drawn from a second set of numbers is a diagram that eventually lies, and this one cannot,
    /// because there is no second set. When the board changes, so does the help.
    /// </para>
    /// <para>
    /// The rings breathe here as they do on a board, in step rather than offset by coordinate: on a board
    /// the offset keeps neighbours from pulsing in lockstep, but here the contrast between an outward ring
    /// and an inward one <em>is</em> the lesson, and it only reads when the two are compared at the same
    /// moment. Reduced motion rests them exactly as the board does.
    /// </para>
    /// </remarks>
    internal sealed class HelpFigure : MonoBehaviour
    {
        /// <summary>
        /// Where the figure is centred, as a viewport fraction.
        /// </summary>
        /// <remarks>
        /// In the band between the last paragraph and the two controls at the bottom. That band is the
        /// only free space on the page, and it is not large, which is why the figure is scaled to fit it
        /// rather than drawn at a fixed size — see <see cref="HeightFraction"/>.
        /// </remarks>
        internal const float ViewportY = 0.27f;

        /// <summary>
        /// How much of the screen the figure may use, as viewport fractions.
        /// </summary>
        /// <remarks>
        /// Whichever of the two runs out first decides the scale, so a wide figure shrinks to fit the
        /// width and a tall one to fit the height. Both are needed: the widest figure here is five cells
        /// across and the height available is a tenth of a portrait screen.
        /// </remarks>
        internal const float WidthFraction = 0.76f;

        internal const float HeightFraction = 0.10f;

        /// <summary>
        /// How far in front of the background the figure sits, in world units.
        /// </summary>
        /// <remarks>
        /// Behind the labels at <c>HexButton.LabelDepth</c>, which is what a figure under a paragraph
        /// should be, and behind the button faces too — it never shares screen space with one, so being
        /// behind them costs nothing and keeps the whole figure on one side of the layer every control is
        /// drawn on.
        /// </remarks>
        internal const float Depth = -1.2f;

        /// <summary>The kind every figure is drawn in, so the pictures read as one set.</summary>
        private const ResourceKind FigureKind = ResourceKind.Water;

        /// <summary>How many conduits the route on the third page has.</summary>
        /// <remarks>
        /// Four, which is exactly what earns a Pivot Token — so the picture of "longer routes pay more"
        /// is also a picture of the rule the last page states.
        /// </remarks>
        internal const int RouteConduits = 4;

        private readonly List<Figure> _figures = new List<Figure>();

        private Camera _camera;
        private int _page = -1;
        private bool _isResting;

        /// <summary>Which figure is showing, or -1 before the first page.</summary>
        internal int CurrentPage => _page;

        /// <summary>How many figures were built.</summary>
        internal int FigureCount => _figures.Count;

        /// <summary>
        /// Builds one figure per help page and shows the first.
        /// </summary>
        /// <remarks>
        /// All of them up front and then shown or hidden, like the help screen's own labels: turning a
        /// page should not create and destroy meshes.
        /// </remarks>
        internal void Build(Camera camera, Material material)
        {
            _camera = camera;

            var hexMesh = HexMeshFactory.CreateHexagon(HexMetrics.Size * 0.92f);
            var spokeMesh = HexMeshFactory.CreateSpoke(TileVisual.SpokeLength, TileVisual.SpokeThickness);
            var ringMesh = GlyphMeshFactory.CreateRing(HexMetrics.Size * 0.92f, HexMetrics.Size * 0.2f);

            var parts = new CellParts(hexMesh, spokeMesh, ringMesh, material);

            _figures.Add(BuildEndpoints(parts));
            _figures.Add(BuildPlacement(parts));
            _figures.Add(BuildRoute(parts));
            _figures.Add(BuildControls(material));

            ShowPage(0);
        }

        /// <summary>Shows the figure belonging to the given page, and hides the rest.</summary>
        internal void ShowPage(int page)
        {
            if (_figures.Count == 0)
            {
                return;
            }

            _page = ((page % _figures.Count) + _figures.Count) % _figures.Count;

            for (var index = 0; index < _figures.Count; index++)
            {
                _figures[index].Root.gameObject.SetActive(index == _page);
            }

            // A figure hidden mid-breath and shown again would otherwise resume wherever it was left.
            _isResting = false;

            Place();
        }

        private void Update()
        {
            Place();
            Breathe();
        }

        /// <summary>
        /// Anchors and scales the visible figure against the camera, every frame.
        /// </summary>
        /// <remarks>
        /// Both derived from the camera rather than fixed, because everything in this codebase that was
        /// positioned once against a camera eventually appeared somewhere else: the tray tile left behind
        /// when the board moved, the wrap box baked at the wrong width, the pause controls drawn at half
        /// size over a large board.
        /// </remarks>
        private void Place()
        {
            if (_camera == null || _page < 0 || _page >= _figures.Count)
            {
                return;
            }

            var figure = _figures[_page];

            var scale = ScaleFor(
                _camera.orthographicSize, _camera.aspect, figure.Width, figure.Height);

            var world = _camera.ViewportToWorldPoint(new Vector3(0.5f, ViewportY, 0f));

            // Offset by the figure's own middle, because none of them is built around its origin: a row of
            // cells is laid out rightward from the first one, and a control has its pips above it. Anchoring
            // the root instead of the middle put the five-cell route almost entirely off the right edge.
            figure.Root.position = new Vector3(
                world.x - (figure.Centre.x * scale),
                world.y - (figure.Centre.y * scale),
                Depth);

            figure.Root.localScale = Vector3.one * scale;
        }

        /// <summary>
        /// How large a figure of the given size has to be drawn to fit its band.
        /// </summary>
        /// <remarks>
        /// Kept separate from the placing so it can be checked without a camera. Scaling to fit is what
        /// lets a two-cell figure and a five-cell one share a slot without either being laid out by hand.
        /// </remarks>
        internal static float ScaleFor(
            float orthographicSize, float aspect, float figureWidth, float figureHeight)
        {
            if (figureWidth <= 0f || figureHeight <= 0f)
            {
                return 1f;
            }

            var worldHeight = orthographicSize * 2f;
            var worldWidth = worldHeight * (aspect > 0f ? aspect : 1f);

            return Mathf.Min(
                worldWidth * WidthFraction / figureWidth,
                worldHeight * HeightFraction / figureHeight);
        }

        /// <summary>Pulses the visible figure's rings, or rests them for reduced motion.</summary>
        private void Breathe()
        {
            if (_page < 0 || _page >= _figures.Count)
            {
                return;
            }

            var cells = _figures[_page].PulsingCells;

            if (cells.Count == 0)
            {
                return;
            }

            if (GameSettings.ReduceMotion)
            {
                if (!_isResting)
                {
                    for (var index = 0; index < cells.Count; index++)
                    {
                        cells[index].RestPulse();
                    }

                    _isResting = true;
                }

                return;
            }

            _isResting = false;

            // No per-cell phase offset. The board spreads its rings out so a field of endpoints does not
            // throb as one; here there are two, side by side, and what they are for is the comparison.
            var now = Time.unscaledTime;

            for (var index = 0; index < cells.Count; index++)
            {
                var cell = cells[index];

                cell.SetPulse(
                    EndpointPulse.ScaleAt(now, cell.PulseRole),
                    EndpointPulse.FadeAt(now, cell.PulseRole));
            }
        }

        /// <summary>
        /// A spring and a hub, apart, each breathing its own way.
        /// </summary>
        /// <remarks>
        /// Not adjacent. Two endpoints of one kind touching would be a completed route with no conduits
        /// in it, which is legal on a board and beside the point on this page — the picture is of two
        /// things being told apart, not of them being joined.
        /// </remarks>
        private Figure BuildEndpoints(CellParts parts)
        {
            var figure = NewFigure("Endpoints");

            AddEndpoint(figure, parts, new HexCoord(0, 0), EndpointRole.Spring);
            AddEndpoint(figure, parts, new HexCoord(2, 0), EndpointRole.Hub);

            return figure.Measured();
        }

        /// <summary>
        /// A spring, one conduit joined to it, and a lit cell where the next may go.
        /// </summary>
        /// <remarks>
        /// The lit cell is what makes this a picture of a rule rather than of a board: it is the cell the
        /// game would light, so it says where the tile is allowed to land as well as that it has to join.
        /// </remarks>
        private Figure BuildPlacement(CellParts parts)
        {
            var figure = NewFigure("Placement");

            AddEndpoint(figure, parts, new HexCoord(0, 0), EndpointRole.Spring);
            AddConduit(figure, parts, new HexCoord(1, 0), isHarvested: false);
            AddCell(figure, parts, new HexCoord(2, 0)).ShowAvailable();

            // And one plain cell past it, because a lit cell on its own is not visibly lit. The first
            // draft ended at the lit one and read as three cells of much the same grey; the contrast
            // between the two on the right is what says where the tile may and may not go — which is
            // also, exactly, the rule the page states.
            AddCell(figure, parts, new HexCoord(3, 0)).ShowEmpty();

            return figure.Measured();
        }

        /// <summary>
        /// A finished route, lit as one, from a spring to a hub.
        /// </summary>
        /// <remarks>
        /// Drawn in the harvest colour, because that is what the board shows the instant a route pays and
        /// it is the only way a still picture can say "this paid" rather than "this exists".
        /// </remarks>
        private Figure BuildRoute(CellParts parts)
        {
            var figure = NewFigure("Route");

            AddEndpoint(figure, parts, new HexCoord(0, 0), EndpointRole.Spring);

            for (var step = 1; step <= RouteConduits; step++)
            {
                AddConduit(figure, parts, new HexCoord(step, 0), isHarvested: true);
            }

            AddEndpoint(figure, parts, new HexCoord(RouteConduits + 1, 0), EndpointRole.Hub);

            return figure.Measured();
        }

        /// <summary>
        /// The two controls in the drawer, each under a row of the pips it spends.
        /// </summary>
        /// <remarks>
        /// The same marks and the same pip spacing the drawer itself uses, at the same relative positions,
        /// so a player who finds this page can then find the controls. Both are drawn ready rather than
        /// spent: a diagram of a control greyed out would be a diagram of not being able to use it.
        /// </remarks>
        private Figure BuildControls(Material material)
        {
            var figure = NewFigure("Controls");

            // Far enough apart to read as two separate controls, and no further: on the board itself they
            // sit at opposite ends of the drawer, which is a distance no figure this size can hold.
            const float separation = 0.62f;

            AddControl(figure, material, new Vector3(-separation, 0f, 0f), BoardGlyphs.Pivot(),
                BoardPalette.PivotReady, BoardPalette.TokenHeld);

            AddControl(figure, material, new Vector3(separation, 0f, 0f), BoardGlyphs.Skip(),
                BoardPalette.SkipReady, BoardPalette.SkipHeld);

            return figure.Measured();
        }

        /// <summary>
        /// One control: its face, its mark, and a full row of pips above it.
        /// </summary>
        /// <remarks>
        /// The pips come from <c>TokenPipsView.PipPosition</c>, so the row sits above the face at exactly
        /// the offset it does in the drawer. A full row rather than a partial one, because the page is
        /// explaining what the column counts, not reporting a total.
        /// </remarks>
        private void AddControl(
            Figure figure,
            Material material,
            Vector3 offset,
            BoardGlyphs.Part[] glyph,
            Color faceColour,
            Color pipColour)
        {
            var control = new GameObject("Control");
            control.transform.SetParent(figure.Root, worldPositionStays: false);
            control.transform.localPosition = offset;

            AddMesh(
                control.transform, material, "Face",
                HexMeshFactory.CreateHexagon(BoardGlyphs.ButtonRadius), faceColour, 0f);

            foreach (var part in glyph)
            {
                AddMesh(
                    control.transform, material, part.Name, part.Mesh, part.Colour, part.Depth,
                    part.Offset);
            }

            var pip = HexMeshFactory.CreateHexagon(TokenPipsView.PipRadius);

            for (var index = 0; index < TokenPipsView.PipsPerRow; index++)
            {
                AddMesh(
                    control.transform, material, $"Pip{index}", pip, pipColour, 0f,
                    TokenPipsView.PipPosition(index));
            }
        }

        private static void AddMesh(
            Transform parent,
            Material material,
            string childName,
            Mesh mesh,
            Color colour,
            float depth,
            Vector3 offset = default)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(parent, worldPositionStays: false);
            child.transform.localPosition = new Vector3(offset.x, offset.y, depth);

            child.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.material.color = colour;
        }

        private Figure NewFigure(string figureName)
        {
            var root = new GameObject($"Figure {figureName}");
            root.transform.SetParent(transform, worldPositionStays: false);

            return new Figure(root.transform);
        }

        private static CellView AddCell(Figure figure, CellParts parts, HexCoord coordinate)
        {
            var cell = new GameObject($"Cell {coordinate.Q},{coordinate.R}").AddComponent<CellView>();
            cell.transform.SetParent(figure.Root, worldPositionStays: false);
            cell.Initialise(coordinate, parts.Hex, parts.Spoke, parts.Material, theme: null);

            return cell;
        }

        private static void AddEndpoint(
            Figure figure, CellParts parts, HexCoord coordinate, EndpointRole role)
        {
            var endpoint = role == EndpointRole.Spring
                ? FlowEndpoint.Spring(coordinate, FigureKind)
                : FlowEndpoint.Hub(coordinate, FigureKind);

            var cell = AddCell(figure, parts, coordinate);

            cell.ShowEndpoint(endpoint);
            cell.AttachPulse(endpoint, parts.Ring, parts.Material);
            cell.RestPulse();

            figure.PulsingCells.Add(cell);
        }

        /// <summary>
        /// A straight conduit running east to west, which is the axis every figure here is laid out on.
        /// </summary>
        private static void AddConduit(
            Figure figure, CellParts parts, HexCoord coordinate, bool isHarvested)
        {
            // Direction 0 is the neighbour at +1 on Q and direction 3 its opposite, so this is the tile
            // that joins the cell either side of it along the row.
            var tile = new ConduitTile(FigureKind, EdgeMask.FromDirections(0, 3));

            var cell = AddCell(figure, parts, coordinate);

            if (isHarvested)
            {
                cell.ShowHarvestedConduit(tile);
                return;
            }

            cell.ShowConduit(tile);
        }

        /// <summary>The meshes and material every cell in a figure shares.</summary>
        private readonly struct CellParts
        {
            internal CellParts(Mesh hex, Mesh spoke, Mesh ring, Material material)
            {
                Hex = hex;
                Spoke = spoke;
                Ring = ring;
                Material = material;
            }

            internal Mesh Hex { get; }

            internal Mesh Spoke { get; }

            internal Mesh Ring { get; }

            internal Material Material { get; }
        }

        /// <summary>
        /// One page's picture: its root, its size, and any rings it has to breathe.
        /// </summary>
        private sealed class Figure
        {
            internal Figure(Transform root)
            {
                Root = root;
            }

            internal Transform Root { get; }

            internal List<CellView> PulsingCells { get; } = new List<CellView>();

            /// <summary>How wide the figure is at scale one, in world units.</summary>
            internal float Width { get; private set; }

            internal float Height { get; private set; }

            /// <summary>
            /// The middle of the figure in its own space, at scale one.
            /// </summary>
            /// <remarks>
            /// Rarely zero. A row of cells starts at the first cell and runs one way; a control carries its
            /// pips above it. Both have to be anchored by their middle rather than by their origin.
            /// </remarks>
            internal Vector2 Centre { get; private set; }

            /// <summary>
            /// Measures the figure from the geometry actually in it.
            /// </summary>
            /// <remarks>
            /// From the meshes rather than from a declared size, so a figure that gains a cell is
            /// rescaled by the act of gaining it. The z extent is ignored: every block's sides extrude
            /// away from the camera, which says nothing about how much of the screen the figure covers.
            /// </remarks>
            internal Figure Measured()
            {
                var min = new Vector2(float.MaxValue, float.MaxValue);
                var max = new Vector2(float.MinValue, float.MinValue);

                foreach (var filter in Root.GetComponentsInChildren<MeshFilter>(includeInactive: true))
                {
                    var mesh = filter.sharedMesh;
                    if (mesh == null)
                    {
                        continue;
                    }

                    var bounds = mesh.bounds;

                    // The four corners in the plane, taken into the figure's own space. The root is at
                    // identity while a figure is being built, so this is the size at scale one.
                    foreach (var corner in Corners(bounds))
                    {
                        var local = Root.InverseTransformPoint(filter.transform.TransformPoint(corner));

                        min = Vector2.Min(min, new Vector2(local.x, local.y));
                        max = Vector2.Max(max, new Vector2(local.x, local.y));
                    }
                }

                if (min.x > max.x)
                {
                    return this;
                }

                Width = max.x - min.x;
                Height = max.y - min.y;
                Centre = (min + max) * 0.5f;

                return this;
            }

            private static IEnumerable<Vector3> Corners(Bounds bounds)
            {
                yield return new Vector3(bounds.min.x, bounds.min.y, 0f);
                yield return new Vector3(bounds.max.x, bounds.min.y, 0f);
                yield return new Vector3(bounds.min.x, bounds.max.y, 0f);
                yield return new Vector3(bounds.max.x, bounds.max.y, 0f);
            }
        }
    }
}
