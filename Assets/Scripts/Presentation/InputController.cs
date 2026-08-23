using Pathweaver.Core.Hex;
using Pathweaver.Game.App;
using Pathweaver.Game.Platform;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Turns one thumb into placements and rotations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two ways to place a tile, deliberately:
    /// </para>
    /// <list type="bullet">
    /// <item>drag it out of the tray and release over a cell, which is the gesture PRD
    /// section 3.1 describes and which shows the tile before committing</item>
    /// <item>tap a highlighted cell, which places without the reach a drag to the top of
    /// a phone demands</item>
    /// </list>
    /// <para>
    /// Both exist because reaching the far edge of a large screen one-handed is the
    /// awkward part of single-thumb play, and which gesture wins is a question for a
    /// real device rather than an argument to have here. Tapping the tray rotates.
    /// </para>
    /// <para>
    /// Nothing here decides legality: it asks the session, which asks the simulation.
    /// </para>
    /// </remarks>
    internal sealed class InputController : MonoBehaviour
    {
        /// <summary>
        /// How far a pointer may travel and still count as a tap rather than a drag,
        /// as a fraction of the shorter screen edge. A thumb wobbles.
        /// </summary>
        private const float TapMovementFraction = 0.02f;

        [SerializeField]
        private GameSession _session;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private HeldTileView _heldTileView;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private FrameRateGovernor _frameRateGovernor;

        [SerializeField]
        private HapticsService _haptics;

        private bool _isPressed;
        private bool _startedOnTray;
        private Vector2 _pressPosition;
        private float _travelled;

        private float TapThresholdPixels => Mathf.Min(Screen.width, Screen.height) * TapMovementFraction;

        private void OnEnable()
        {
            if (_session != null)
            {
                _session.RoutesHarvested += OnRoutesHarvested;
            }
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.RoutesHarvested -= OnRoutesHarvested;
            }
        }

        private void OnRoutesHarvested(int count)
        {
            // One buzz per harvest, not per route: several routes completing at once is a
            // good moment, not a reason to rattle the phone.
            _haptics?.RouteCompleted();
        }

        private void Update()
        {
            if (_session == null || _session.State == null)
            {
                return;
            }

            if (TryReadPointer(out var screenPosition, out var phase))
            {
                switch (phase)
                {
                    case PointerPhase.Pressed:
                        BeginPress(screenPosition);
                        break;
                    case PointerPhase.Held:
                        ContinuePress(screenPosition);
                        break;
                    case PointerPhase.Released:
                        EndPress(screenPosition);
                        break;
                }
            }
        }

        private void BeginPress(Vector2 screenPosition)
        {
            // Touching the screen is activity even if it turns out to do nothing, so the
            // frame rate rises before the first frame of a drag rather than after it.
            _frameRateGovernor?.NotifyActivity();

            _isPressed = true;
            _pressPosition = screenPosition;
            _travelled = 0f;
            _startedOnTray = _heldTileView != null && _heldTileView.IsTrayTouch(screenPosition);
        }

        private void ContinuePress(Vector2 screenPosition)
        {
            if (!_isPressed)
            {
                return;
            }

            _travelled = Mathf.Max(_travelled, Vector2.Distance(_pressPosition, screenPosition));
            _frameRateGovernor?.NotifyActivity();

            if (_startedOnTray && _travelled > TapThresholdPixels && _heldTileView != null)
            {
                _heldTileView.FollowPointer(ToWorld(screenPosition));
            }
        }

        private void EndPress(Vector2 screenPosition)
        {
            if (!_isPressed)
            {
                return;
            }

            _isPressed = false;

            var wasTap = _travelled <= TapThresholdPixels;

            if (_heldTileView != null)
            {
                _heldTileView.ReturnToTray();
            }

            _frameRateGovernor?.NotifyActivity();

            if (_startedOnTray && wasTap)
            {
                _session.RotateHeld();
                return;
            }

            var cell = CellUnder(screenPosition);
            if (_session.TryPlaceAt(cell))
            {
                _haptics?.TileLocked();
            }
        }

        private HexCoord CellUnder(Vector2 screenPosition)
        {
            var world = ToWorld(screenPosition);
            var local = _boardView != null
                ? _boardView.transform.InverseTransformPoint(world)
                : world;

            return HexMetrics.FromWorld(local);
        }

        private Vector3 ToWorld(Vector2 screenPosition)
        {
            var camera = _camera != null ? _camera : Camera.main;
            var world = camera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, -camera.transform.position.z));
            world.z = 0f;

            return world;
        }

        private static bool TryReadPointer(out Vector2 position, out PointerPhase phase)
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                position = touch.position;

                phase = touch.phase switch
                {
                    TouchPhase.Began => PointerPhase.Pressed,
                    TouchPhase.Moved => PointerPhase.Held,
                    TouchPhase.Stationary => PointerPhase.Held,
                    TouchPhase.Ended => PointerPhase.Released,
                    TouchPhase.Canceled => PointerPhase.Released,
                    _ => PointerPhase.None,
                };

                return phase != PointerPhase.None;
            }

            // The mouse path exists so the game can be driven in the Editor without a
            // device attached.
            position = Input.mousePosition;

            if (Input.GetMouseButtonDown(0))
            {
                phase = PointerPhase.Pressed;
                return true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                phase = PointerPhase.Released;
                return true;
            }

            if (Input.GetMouseButton(0))
            {
                phase = PointerPhase.Held;
                return true;
            }

            phase = PointerPhase.None;
            return false;
        }

        private enum PointerPhase
        {
            None,
            Pressed,
            Held,
            Released,
        }
    }
}
