using Pathweaver.Core.Hex;
using Pathweaver.Game.App;
using Pathweaver.Game.Platform;
using Pathweaver.Game.Presentation.Menus;
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

        [SerializeField]
        private RestartButtonView _restartButton;

        [SerializeField]
        private RestartConfirmView _restartConfirm;

        [SerializeField]
        private SkipButtonView _skipButton;

        [SerializeField]
        private BoardCameraFitter _cameraFitter;

        [SerializeField]
        private ScreenRouter _router;

        [SerializeField]
        private GameFlow _flow;

        [SerializeField]
        private PivotButtonView _pivotButton;

        private bool _isPressed;
        private bool _startedOnRestart;
        private bool _startedOnSkip;
        private bool _startedOnPivot;
        private bool _startedOnTray;
        private bool _startedOnBoard;
        private bool _hasPanned;
        private Vector2 _panFrom;
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

            // And light the path that paid out, so the buzz has something to refer to.
            _boardView?.FlashHarvested(_session.LastHarvestedTiles);
        }

        private void Update()
        {
            // Only the session itself is required. Menus are live before any level has been
            // started, so refusing input while the board is empty would make the main menu
            // untappable — which is exactly what it did.
            if (_session == null)
            {
                return;
            }

            // Android's back gesture arrives as Escape. Read before the pointer, because a back press is
            // never also a tap on the board.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleBack();
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

        /// <summary>
        /// Answers the device's back gesture.
        /// </summary>
        /// <remarks>
        /// A modal question takes it first — dismissing, which is the safe answer, and the same thing a
        /// tap anywhere but the confirmation does. Otherwise the flow routes it to whatever the current
        /// screen's own back control does. Unconsumed on the main menu, so the game leaves rather than
        /// swallowing a gesture and appearing stuck.
        /// </remarks>
        private void HandleBack()
        {
            _frameRateGovernor?.NotifyActivity();

            if (_restartConfirm != null && _restartConfirm.IsOpen)
            {
                _restartConfirm.Close();
                return;
            }

            if (_flow != null && _flow.HandleBack())
            {
                _haptics?.TileLocked();
                return;
            }

            Application.Quit();
        }

        private void BeginPress(Vector2 screenPosition)
        {
            // Touching the screen is activity even if it turns out to do nothing, so the
            // frame rate rises before the first frame of a drag rather than after it.
            _frameRateGovernor?.NotifyActivity();

            // While the question is up, nothing else can be pressed. A modal that lets the
            // board be played behind it is not a question, it is decoration.
            if (_restartConfirm != null && _restartConfirm.IsOpen)
            {
                _isPressed = true;
                _pressPosition = screenPosition;
                _travelled = 0f;
                _startedOnRestart = false;
                _startedOnTray = false;
                return;
            }

            _isPressed = true;
            _pressPosition = screenPosition;
            _travelled = 0f;

            // Buttons are checked before the tray, so a press near a corner cannot be claimed
            // by two things at once.
            _startedOnRestart = _restartButton != null && _restartButton.IsPressed(screenPosition);
            _startedOnSkip = !_startedOnRestart
                             && _skipButton != null
                             && _skipButton.IsPressed(screenPosition);
            _startedOnPivot = !_startedOnRestart
                              && !_startedOnSkip
                              && _pivotButton != null
                              && _pivotButton.IsPressed(screenPosition);
            _startedOnTray = !_startedOnRestart
                             && !_startedOnSkip
                             && !_startedOnPivot
                             && _heldTileView != null
                             && _heldTileView.IsTrayTouch(screenPosition);

            // Whatever is left is the board. A drag that begins there is the one gesture nothing else
            // claims — placement is a tap on a cell or a drag out of the tray — so it becomes the pan.
            _startedOnBoard = !_startedOnRestart
                              && !_startedOnSkip
                              && !_startedOnPivot
                              && !_startedOnTray;

            _panFrom = screenPosition;
            _hasPanned = false;
        }

        private void ContinuePress(Vector2 screenPosition)
        {
            if (!_isPressed)
            {
                return;
            }

            _travelled = Mathf.Max(_travelled, Vector2.Distance(_pressPosition, screenPosition));
            _frameRateGovernor?.NotifyActivity();

            if (_restartConfirm != null && _restartConfirm.IsOpen)
            {
                return;
            }

            if (_session.State == null)
            {
                return;
            }

            if (_startedOnTray && _travelled > TapThresholdPixels && _heldTileView != null)
            {
                _heldTileView.FollowPointer(ToWorld(screenPosition));
                return;
            }

            if (_startedOnBoard && _travelled > TapThresholdPixels)
            {
                Pan(screenPosition);
            }
        }

        /// <summary>
        /// Moves the view by however far the thumb has travelled since the last frame.
        /// </summary>
        /// <remarks>
        /// Measured frame to frame rather than from where the press began, so the board keeps up with
        /// the thumb instead of accelerating away from it once the clamp starts biting at an edge.
        /// </remarks>
        private void Pan(Vector2 screenPosition)
        {
            if (_cameraFitter == null || !_cameraFitter.CanPan)
            {
                return;
            }

            var delta = ToWorld(screenPosition) - ToWorld(_panFrom);
            _panFrom = screenPosition;
            _hasPanned = true;

            _cameraFitter.PanBy(new Vector2(delta.x, delta.y));
        }

        private void EndPress(Vector2 screenPosition)
        {
            if (!_isPressed)
            {
                return;
            }

            _isPressed = false;

            var wasTap = _travelled <= TapThresholdPixels;

            // A menu takes the tap before the board sees it. Without this the board would react to
            // presses landing on a screen drawn in front of it.
            if (wasTap && _flow != null && _flow.HandleTap(screenPosition))
            {
                return;
            }

            if (_router != null && !_router.IsPlaying)
            {
                return;
            }

            // Past this point everything touches the board, which does not exist until a level
            // has been started.
            if (_session.State == null)
            {
                return;
            }

            if (_restartConfirm != null && _restartConfirm.IsOpen)
            {
                AnswerRestartQuestion(screenPosition, wasTap);
                return;
            }

            if (_heldTileView != null)
            {
                _heldTileView.ReturnToTray();
            }

            _frameRateGovernor?.NotifyActivity();

            // A finished board takes no further play. Every control except the button that moves on
            // is hidden by GameFlow, and this is what stops the board itself from answering: without
            // it a tap on a cell would keep placing tiles behind the notice.
            if (_session.IsComplete)
            {
                return;
            }

            if (_startedOnRestart)
            {
                // Only on a tap, so a drag that happens to begin on the button does not
                // throw the board away.
                if (wasTap && _restartButton.IsPressed(screenPosition))
                {
                    RequestRestart();
                }

                return;
            }

            if (_startedOnSkip)
            {
                // Tap only, like the restart button: a drag that happens to start here should
                // not spend a resource.
                if (wasTap && _skipButton.IsPressed(screenPosition) && _session.TrySkipHeld())
                {
                    _haptics?.TileLocked();
                }

                return;
            }

            if (_startedOnPivot)
            {
                // The remove button is the only way in and out of the pivot mode, so a tap there
                // always answers, whether it arms or cancels.
                if (wasTap && _pivotButton.IsPressed(screenPosition) && _session.TogglePivotArmed())
                {
                    _haptics?.TileLocked();
                }

                return;
            }

            if (_startedOnTray && wasTap)
            {
                _session.RotateHeld();
                return;
            }

            // A drag that moved the board is not also a placement. Without this, panning across a board
            // would drop a tile wherever the thumb happened to lift.
            if (_hasPanned)
            {
                return;
            }

            var cell = CellUnder(screenPosition);

            if (_session.IsPivotArmed)
            {
                SpendPivotAt(cell);
                return;
            }

            if (_session.TryPlaceAt(cell))
            {
                _haptics?.TileLocked();
            }
        }

        /// <summary>
        /// Spends the armed Pivot Token on the conduit under the pointer, taking it off the board.
        /// </summary>
        /// <remarks>
        /// One verb, so one gesture. A press that lands anywhere but a conduit cancels the mode
        /// rather than doing nothing, so a player who armed a token by accident is one tap away from
        /// where they were — and the token is still theirs, because arming does not spend it.
        /// </remarks>
        private void SpendPivotAt(HexCoord cell)
        {
            if (_session.TryPivotRetrieve(cell))
            {
                _haptics?.TileLocked();
                return;
            }

            _session.DisarmPivot();
        }

        /// <summary>
        /// Asks before restarting, unless there is nothing left to lose.
        /// </summary>
        /// <remarks>
        /// A dead-ended board has no run worth protecting, and a confirmation there would add a
        /// tap to an already frustrating moment. Anywhere else, an accidental restart is
        /// unrecoverable.
        /// </remarks>
        private void RequestRestart()
        {
            if (_session.State != null && _session.State.IsDeadlocked)
            {
                _session.Restart();
                _haptics?.TileLocked();
                return;
            }

            _restartConfirm?.Open();
        }

        private void AnswerRestartQuestion(Vector2 screenPosition, bool wasTap)
        {
            if (wasTap && _restartConfirm.IsConfirmPressed(screenPosition))
            {
                _restartConfirm.Close();
                _session.Restart();
                _haptics?.TileLocked();
                return;
            }

            // Anything else dismisses. A question the player can back out of by tapping away is
            // less likely to be answered by accident than one with a single exit.
            _restartConfirm.Close();
        }

        private HexCoord CellUnder(Vector2 screenPosition)
        {
            var world = BoardPointUnder(screenPosition);
            var local = _boardView != null
                ? _boardView.transform.InverseTransformPoint(world)
                : world;

            return HexMetrics.FromWorld(local);
        }

        /// <summary>
        /// Where the pointer meets the plane the board's top faces lie in.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A ray against a plane rather than a point at a fixed depth. The board leans, so the depth at
        /// which the pointer crosses it depends on how far up the screen the pointer is; reading a point
        /// at z = 0 and inverse-transforming it gives a cell that drifts further from the thumb the
        /// nearer the top or bottom of the board it is.
        /// </para>
        /// <para>
        /// The plane is the board's own local z = 0, which is where <c>HexMetrics.ToWorld</c> puts every
        /// cell centre, so it follows the board's transform without knowing the angle.
        /// </para>
        /// </remarks>
        private Vector3 BoardPointUnder(Vector2 screenPosition)
        {
            var camera = _camera != null ? _camera : Camera.main;
            var ray = camera.ScreenPointToRay(screenPosition);

            if (_boardView == null)
            {
                return ToWorld(screenPosition);
            }

            var board = _boardView.transform;
            var topFaces = new Plane(board.forward, board.position);

            return topFaces.Raycast(ray, out var distance) ? ray.GetPoint(distance) : ToWorld(screenPosition);
        }

        /// <summary>
        /// Where the pointer is, flat, for anything drawn in front of the board.
        /// </summary>
        /// <remarks>
        /// The tray and the tile that follows the thumb are not on the board and do not lean with it, so
        /// they want the screen position and not a point on the board's plane.
        /// </remarks>
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
