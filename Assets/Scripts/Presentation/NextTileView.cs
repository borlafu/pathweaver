using Pathweaver.Core.State;
using Pathweaver.Core.Tiles;
using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Shows the tile that comes after the one in the tray.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Placement in this game is shape-forced: a corridor cell wants one particular tile and refuses
    /// everything else. Without knowing what comes next, a player cannot plan — they can only react to
    /// what they are handed, and a board that could have been solved is lost to an order they were never
    /// shown. Seeing one tile ahead is the difference between a puzzle and a slot machine.
    /// </para>
    /// <para>
    /// Drawn smaller and dimmer than the held tile, because it is information rather than something to
    /// touch. It is deliberately not tappable: swapping the two would be a second resource to manage
    /// alongside skips, and the skip already exists to refuse a tile.
    /// </para>
    /// </remarks>
    internal sealed class NextTileView : MonoBehaviour
    {
        /// <summary>Where the next tile sits, as a viewport fraction.</summary>
        /// <remarks>
        /// Right of the tray and clear of the skip button at 0.86, on the side the skip button is — the
        /// two are about the same thing, which is what the bag hands over next.
        /// </remarks>
        internal const float ViewportX = 0.68f;

        private static readonly Vector2 ViewportPosition = new Vector2(ViewportX, 0.12f);

        /// <summary>How large it is next to the held tile.</summary>
        /// <remarks>
        /// A fraction of the held tile rather than of the screen, because the held tile is world-sized on
        /// purpose — it should match the cell it is about to become. Sizing this against the screen made
        /// it larger than the tile it is subordinate to whenever the board was zoomed out.
        /// </remarks>
        internal const float RelativeSize = 0.62f;

        /// <summary>How far in front of the board it sits, matching the held tile.</summary>
        private const float Depth = -0.3f;

        [SerializeField]
        private GameSession _session;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private Camera _camera;

        private TileVisual _visual;
        private ConduitTile _shown;
        private bool _hasShown;

        private Camera ResolvedCamera => _camera != null ? _camera : Camera.main;

        private void OnEnable()
        {
            if (_session != null)
            {
                _session.StateChanged += OnStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.StateChanged -= OnStateChanged;
            }
        }

        private void Start()
        {
            if (_session?.State != null)
            {
                OnStateChanged(_session.State);
            }
        }

        private void Update()
        {
            var camera = ResolvedCamera;
            if (camera == null)
            {
                return;
            }

            var world = camera.ViewportToWorldPoint(
                new Vector3(ViewportPosition.x, ViewportPosition.y, 0f));
            transform.position = new Vector3(world.x, world.y, Depth);

            // Sized against the held tile, not against the screen. The held tile is deliberately
            // world-sized so it matches the cell it is about to become, and this has to stay a fixed
            // fraction of it — normalising against the camera made the "secondary" tile 43 per cent
            // *larger* than the primary one on a board zoomed out to fit the valley.
            transform.localScale = Vector3.one * RelativeSize;
        }

        private void OnStateChanged(GameState state)
        {
            if (_boardView == null)
            {
                return;
            }

            if (state == null)
            {
                Hide();
                return;
            }

            // Draw is pure — it returns the next tile and the bag that would follow, without changing
            // anything — so peeking costs nothing and cannot alter the order a player is dealt.
            var next = state.Bag.Draw().Tile;

            if (_hasShown && next.Equals(_shown))
            {
                return;
            }

            Show(next);
        }

        private void Show(ConduitTile tile)
        {
            EnsureVisual();

            var colour = BoardPalette.ForKind(tile.Kind);

            // Dimmed toward the background rather than made transparent: the material the whole board is
            // drawn with is opaque, so "fainter" has to mean "closer to what is behind".
            var faded = Color.Lerp(BoardPalette.Background, colour, 0.55f);

            _visual.gameObject.SetActive(true);
            _visual.SetBackground(BoardPalette.EmptyCell);
            _visual.UseResourceArt(tile.Kind);
            _visual.ShowEdges(tile.Edges, faded);
            _visual.ShowResource(tile.Kind, faded);

            _shown = tile;
            _hasShown = true;
        }

        private void Hide()
        {
            if (_visual != null)
            {
                _visual.gameObject.SetActive(false);
            }

            _hasShown = false;
        }

        private void EnsureVisual()
        {
            if (_visual != null)
            {
                return;
            }

            _visual = new GameObject("NextTileVisual").AddComponent<TileVisual>();
            _visual.transform.SetParent(transform, worldPositionStays: false);
            _visual.Initialise(
                HexMeshFactory.CreateHexagon(HexMetrics.Size * 0.92f),
                HexMeshFactory.CreateSpoke(TileVisual.SpokeLength, TileVisual.SpokeThickness),
                _boardView.TileMaterial);
        }
    }
}
