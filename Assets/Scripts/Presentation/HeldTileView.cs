using Pathweaver.Core.Tiles;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Draws the tile in hand, either resting in its tray or following the thumb.
    /// </summary>
    /// <remarks>
    /// The tray sits low on the screen because that is where a thumb rests. Reaching
    /// the top of a phone one-handed is the awkward part of single-thumb play, so the
    /// thing the player touches most often is kept closest.
    /// </remarks>
    internal sealed class HeldTileView : MonoBehaviour
    {
        /// <summary>
        /// Where the tray sits, in viewport coordinates.
        /// </summary>
        private static readonly Vector2 TrayViewportPosition = new Vector2(0.5f, 0.12f);

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private Camera _camera;

        private TileVisual _visual;
        private bool _isFollowingPointer;

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
                world.z = 0f;
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
            _visual.ShowEdges(tile.Edges, BoardPalette.ForKind(tile.Kind));

            if (!_isFollowingPointer)
            {
                ReturnToTray();
            }
        }

        /// <summary>Moves the tile under the pointer while a drag is in progress.</summary>
        internal void FollowPointer(Vector3 worldPosition)
        {
            _isFollowingPointer = true;
            transform.position = new Vector3(worldPosition.x, worldPosition.y, -0.2f);
        }

        internal void ReturnToTray()
        {
            _isFollowingPointer = false;
            transform.position = TrayWorldPosition;
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
            _visual.Initialise(_boardView.HexMesh, _boardView.SpokeMesh, _boardView.TileMaterial);
        }
    }
}
