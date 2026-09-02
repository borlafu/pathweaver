using Pathweaver.Core.Tiles;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Draws the tile in hand, either resting in its tray or following the thumb.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The tray sits low on the screen because that is where a thumb rests. Reaching
    /// the top of a phone one-handed is the awkward part of single-thumb play, so the
    /// thing the player touches most often is kept closest.
    /// </para>
    /// <para>
    /// This is the one thing on the interface that is deliberately <em>not</em> scaled against the camera.
    /// Every control is, so that a button keeps its size on screen whatever the board shows — but this is
    /// not a control, it is a preview of a cell. It has to be the size of the space it is about to occupy,
    /// so it tracks the board rather than the screen, and it therefore looks larger on a small level than
    /// on a valley. That difference is the feature: a ghost that did not match the gap it fits would be
    /// a worse lie than a tile that changes size between levels.
    /// </para>
    /// </remarks>
    internal sealed class HeldTileView : MonoBehaviour
    {
        /// <summary>
        /// Where the tray sits, in viewport coordinates.
        /// </summary>
        private static readonly Vector2 TrayViewportPosition = new Vector2(0.5f, 0.12f);

        /// <summary>
        /// How far in front of the board the held tile sits, in world units.
        /// </summary>
        /// <remarks>
        /// One depth for both resting and being dragged, so the tile does not change plane when a thumb
        /// picks it up. It used to rest at zero and jump to -0.2 on a drag, which was invisible until
        /// <see cref="HudBackdrop"/> put an opaque band at -0.1 between them: the tile in the tray
        /// disappeared behind the band and came back the moment it was dragged.
        /// </remarks>
        internal const float Depth = -0.3f;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private Camera _camera;

        private TileVisual _visual;
        private bool _isFollowingPointer;

        /// <summary>Whether the tile is currently under the player's thumb.</summary>
        internal bool IsFollowingPointer => _isFollowingPointer;

        /// <summary>
        /// How large a screen area counts as touching the tray. Generous on purpose:
        /// a thumb is a blunt instrument, and a missed grab feels like the game
        /// ignoring the player.
        /// </summary>
        internal float TrayTouchRadiusPixels => Mathf.Min(Screen.width, Screen.height) * 0.18f;

        internal Vector3 TrayWorldPosition
        {
            get
            {
                var viewport = new Vector3(TrayViewportPosition.x, TrayViewportPosition.y, 0f);
                var world = ResolvedCamera.ViewportToWorldPoint(viewport);
                world.z = Depth;
                return world;
            }
        }

        internal Vector2 TrayScreenPosition
            => ResolvedCamera.WorldToScreenPoint(TrayWorldPosition);

        private Camera ResolvedCamera => _camera != null ? _camera : Camera.main;

        internal void Show(ConduitTile tile)
        {
            EnsureVisual();

            _visual.SetBackground(BoardPalette.CellOutline);
            _visual.UseResourceArt(tile.Kind);
            _visual.ShowEdges(tile.Edges, BoardPalette.ForKind(tile.Kind));
            _visual.ShowResource(tile.Kind, BoardPalette.ForKind(tile.Kind));

            if (!_isFollowingPointer)
            {
                ReturnToTray();
            }
        }

        /// <summary>Moves the tile under the pointer while a drag is in progress.</summary>
        internal void FollowPointer(Vector3 worldPosition)
        {
            _isFollowingPointer = true;
            transform.position = new Vector3(worldPosition.x, worldPosition.y, Depth);
        }

        internal void ReturnToTray()
        {
            _isFollowingPointer = false;
            transform.position = TrayWorldPosition;
        }

        /// <summary>
        /// Keeps the tile in the tray as the camera moves.
        /// </summary>
        /// <remarks>
        /// Every other view anchored by viewport fraction does this — <c>TokenPipsView</c>,
        /// <c>ProgressBarView</c>, <c>HexButton</c> — and this one did not, because until a board could
        /// be larger than a screen the camera never moved after a tile had been dealt. The opening
        /// flight moves it, and the tray tile stayed at the world position the tray used to occupy:
        /// on the first large level it ended up clipped off the bottom edge.
        /// </remarks>
        private void Update()
        {
            // Only when the tile is not under a thumb. Overriding a drag would drag the tile back to
            // the tray while the player was still holding it.
            if (!_isFollowingPointer)
            {
                transform.position = TrayWorldPosition;
            }
        }

        /// <summary>
        /// Twists the tile for the rotation hint, in degrees.
        /// </summary>
        /// <remarks>
        /// Decoration only. The pending rotation is expressed by redrawing the tile's edges,
        /// so this cannot be confused with game state — and it must always be able to return
        /// to zero, or the tile would sit visibly crooked.
        /// </remarks>
        internal void SetHintTwist(float degrees)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }

        /// <summary>Whether a screen position counts as grabbing the tile.</summary>
        internal bool IsTrayTouch(Vector2 screenPosition)
            => Vector2.Distance(screenPosition, TrayScreenPosition) <= TrayTouchRadiusPixels;

        private void EnsureVisual()
        {
            if (_visual != null)
            {
                return;
            }

            _visual = new GameObject("Visual").AddComponent<TileVisual>();
            _visual.transform.SetParent(transform, worldPositionStays: false);
            _visual.Initialise(
                _boardView.HexMesh, _boardView.SpokeMesh, _boardView.TileMaterial, _boardView.Theme);
        }
    }
}
