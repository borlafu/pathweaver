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
        private LevelCompleteView _levelComplete;

        [SerializeField]
        private SkipButtonView _skipButton;

        [SerializeField]
        private ScreenRouter _router;

        [SerializeField]
        private GameFlow _flow;

        [SerializeField]
        private TokenPipsView _pivotPips;

        private bool _isPressed;
        private bool _startedOnRestart;
        private bool _startedOnSkip;
        private bool _startedOnPivotPips;
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
            _startedOnPivotPips = !_startedOnRestart
                                  && !_startedOnSkip
                                  && _pivotPips != null
                                  && _pivotPips.IsArmable
                                  && _pivotPips.IsPressed(screenPosition);
            _startedOnTray = !_startedOnRestart
                             && !_startedOnSkip
                             && !_startedOnPivotPips
                             && _heldTileView != null
                             && _heldTileView.IsTrayTouch(screenPosition);
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

            // The completion notice is dismissed by any tap and blocks nothing else, since
            // clearing the quota does not end the board.
            if (wasTap && _levelComplete != null && _levelComplete.IsOpen)
            {
                _levelComplete.Dismiss();
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

            if (_startedOnPivotPips)
            {
                // The pips are the only way in and the only way out of the pivot mode, so a tap
                // there always answers, whether it arms or cancels.
                if (wasTap && _pivotPips.IsPressed(screenPosition) && _session.TogglePivotArmed())
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
