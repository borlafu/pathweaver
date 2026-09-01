using Pathweaver.Core.State;
using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Frames a board, flies the camera in, and lets a thumb move it afterwards.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The arithmetic lives in <see cref="BoardFraming"/> and <see cref="BoardIntroFlight"/>; this owns
    /// the camera and the state that cannot be a pure function — where the player has panned to, and how
    /// far through the flight they are.
    /// </para>
    /// <para>
    /// A board small enough to fit does none of it. There is nothing to fly to and nowhere to pan, so
    /// the camera is simply placed, exactly as it was before boards could be larger than a screen.
    /// </para>
    /// </remarks>
    internal sealed class BoardCameraFitter : MonoBehaviour
    {
        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private BoardView _boardView;

        /// <summary>
        /// Told when the flight is running, so it is not drawn at the idle rate.
        /// </summary>
        /// <remarks>
        /// Unlike the endpoint and flow pulses, which deliberately never do this, the flight is
        /// transient and ends. Pinning the active rate for a second is not the same as pinning it for as
        /// long as the game is open, which is why that rule reads "no animator" and this is not one.
        /// </remarks>
        [SerializeField]
        private Pathweaver.Game.Platform.FrameRateGovernor _frameRateGovernor;

        private Vector2 _boardCentre;
        private Vector2 _boardHalfExtents;
        private Vector2 _lookAt;
        private Vector2 _birdsEyeLookAt;
        private float _birdsEyeSize;
        private float _playingSize;
        private float _flightElapsed = -1f;

        /// <summary>Whether this board is larger than the screen shows at the playing zoom.</summary>
        internal bool CanPan { get; private set; }

        /// <summary>Whether the opening flight is still running.</summary>
        internal bool IsFlying => _flightElapsed >= 0f;

        /// <summary>
        /// Sizes and positions the camera for the given board, and starts the flight if there is one.
        /// </summary>
        internal void Fit(GameState state)
        {
            var camera = ResolvedCamera;
            if (camera == null || state == null)
            {
                return;
            }

            Measure(state, camera.aspect);

            camera.orthographic = true;

            if (!CanPan)
            {
                _flightElapsed = -1f;
                Apply(_lookAt, _playingSize);
                return;
            }

            // Reduced motion is shown the destination rather than the journey. The flight is the one
            // animation in this game that moves the whole screen, which is the kind most likely to make
            // someone ill — so it is skipped outright rather than slowed.
            if (GameSettings.ReduceMotion)
            {
                _flightElapsed = -1f;
                Apply(_lookAt, _playingSize);
                return;
            }

            _flightElapsed = 0f;
            _frameRateGovernor?.NotifyActivity();
            ApplyFlight();
        }

        /// <summary>
        /// Moves the view by a drag, in world units.
        /// </summary>
        /// <remarks>
        /// The drag is subtracted rather than added: dragging the board left moves the camera right, so
        /// the board follows the thumb instead of fleeing it.
        /// </remarks>
        internal void PanBy(Vector2 worldDelta)
        {
            if (!CanPan || IsFlying)
            {
                return;
            }

            _lookAt = BoardFraming.ClampLookAt(
                _lookAt - worldDelta, _boardCentre, _boardHalfExtents, _playingSize, ResolvedCamera.aspect);

            Apply(_lookAt, _playingSize);
        }

        private Camera ResolvedCamera => _camera != null ? _camera : Camera.main;

        private void Update()
        {
            if (!IsFlying)
            {
                return;
            }

            _flightElapsed += Time.deltaTime;
            _frameRateGovernor?.NotifyActivity();

            if (_flightElapsed >= BoardIntroFlight.DurationSeconds)
            {
                _flightElapsed = -1f;
                Apply(_lookAt, _playingSize);
                return;
            }

            ApplyFlight();
        }

        /// <summary>
        /// Works out where the board is, how large it is, and where the flight begins and ends.
        /// </summary>
        private void Measure(GameState state, float aspect)
        {
            var minimum = new Vector2(float.MaxValue, float.MaxValue);
            var maximum = new Vector2(float.MinValue, float.MinValue);

            var board = _boardView != null ? _boardView.transform : null;

            foreach (var coordinate in state.Board.Coordinates)
            {
                // Measured where the cell actually is rather than where the hex maths puts it. The board
                // leans, so its own coordinates are no longer its screen extents — and going through the
                // transform means any future change to the lean needs no change here.
                var local = HexMetrics.ToWorld(coordinate);
                var centre = board != null ? board.TransformPoint(local) : local;

                minimum = Vector2.Min(minimum, new Vector2(centre.x, centre.y));
                maximum = Vector2.Max(maximum, new Vector2(centre.x, centre.y));
            }

            _boardCentre = (minimum + maximum) * 0.5f;
            _boardHalfExtents = (maximum - minimum) * 0.5f;

            _birdsEyeLookAt = _boardCentre;
            _birdsEyeSize = BoardFraming.SizeFor(_boardHalfExtents + BoardFraming.CellReach(), aspect);
            _playingSize = BoardFraming.DefaultSize(_boardHalfExtents, aspect);
            CanPan = BoardFraming.NeedsPanning(_boardHalfExtents, aspect);

            _lookAt = BoardFraming.ClampLookAt(
                CanPan ? OpeningLookAt(state, board) : _boardCentre,
                _boardCentre,
                _boardHalfExtents,
                _playingSize,
                aspect);
        }

        /// <summary>
        /// Where a large board opens: on a spring.
        /// </summary>
        /// <remarks>
        /// The first spring in board order, so the same level always opens the same way — the board is
        /// generated deterministically and the camera should not be the one thing that is not. A spring
        /// rather than a hub because a route is built forwards from one, so it is where the player's
        /// first placement goes.
        /// </remarks>
        private Vector2 OpeningLookAt(GameState state, Transform board)
        {
            foreach (var endpoint in state.Endpoints)
            {
                if (endpoint.Role != Pathweaver.Core.Flow.EndpointRole.Spring)
                {
                    continue;
                }

                var local = HexMetrics.ToWorld(endpoint.Coordinate);
                var world = board != null ? board.TransformPoint(local) : local;

                return new Vector2(world.x, world.y);
            }

            return _boardCentre;
        }

        private void ApplyFlight()
        {
            var (lookAt, size) = BoardIntroFlight.Evaluate(
                _flightElapsed / BoardIntroFlight.DurationSeconds,
                _birdsEyeLookAt,
                _birdsEyeSize,
                _lookAt,
                _playingSize);

            Apply(lookAt, size);
        }

        private void Apply(Vector2 lookAt, float orthographicSize)
        {
            var camera = ResolvedCamera;
            if (camera == null)
            {
                return;
            }

            camera.orthographicSize = orthographicSize;

            var position = BoardFraming.CameraPositionFor(lookAt, orthographicSize);
            camera.transform.position = new Vector3(position.x, position.y, camera.transform.position.z);
        }
    }
}
