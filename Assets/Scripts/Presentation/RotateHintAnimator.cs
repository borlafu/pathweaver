using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Twists the tile in hand every few seconds, until the player turns it themselves.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Added after device testing found rotation hard to discover. That matters more than a
    /// missing affordance usually would: the bag deals bends that are useless until turned,
    /// so a player who has not found rotation concludes the game dealt them a dead tile.
    /// </para>
    /// <para>
    /// The tile itself moves, rather than an icon appearing beside it. The thing that can be
    /// rotated is the thing that demonstrates rotating, so there is nothing to interpret.
    /// </para>
    /// <para>
    /// It keeps going rather than retiring after the first rotation. A tile that is always
    /// visibly turnable reads as a property of the tile, not as a tutorial that has been
    /// completed and withdrawn — and a player returning after a few days should not have to
    /// remember.
    /// </para>
    /// <para>
    /// This only touches the transform. The pending rotation is expressed by redrawing the
    /// tile's edges, so a decorative twist cannot be mistaken for game state.
    /// </para>
    /// </remarks>
    internal sealed class RotateHintAnimator : MonoBehaviour
    {
        [SerializeField]
        private HeldTileView _heldTileView;

        [SerializeField]
        private GameSession _session;

        private float _nextShakeTime;
        private float _shakeStartedAt = -1f;

        private void OnEnable()
        {
            if (_session != null)
            {
                // A rotation restarts the wait rather than stopping the hint, so the tile does
                // not twist in the player's face immediately after they turned it themselves.
                _session.HeldRotated += DeferNextShake;
            }

            _nextShakeTime = Time.unscaledTime + RotateHint.IntervalSeconds;
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.HeldRotated -= DeferNextShake;
            }
        }

        private void Update()
        {
            if (_heldTileView == null)
            {
                return;
            }

            // Reduced motion silences it entirely rather than slowing it. A hint that repeats every
            // two seconds forever is exactly the kind of motion a player turns that setting on to
            // stop, and a slower version is still motion.
            if (GameSettings.ReduceMotion)
            {
                _heldTileView.SetHintTwist(0f);
                return;
            }

            // Never while the tile is under a thumb: the player is already interacting, and
            // twisting what they are holding would read as a glitch.
            if (_heldTileView.IsFollowingPointer)
            {
                _shakeStartedAt = -1f;
                _heldTileView.SetHintTwist(0f);
                _nextShakeTime = Time.unscaledTime + RotateHint.IntervalSeconds;
                return;
            }

            var now = Time.unscaledTime;

            if (_shakeStartedAt < 0f)
            {
                if (now >= _nextShakeTime)
                {
                    _shakeStartedAt = now;
                }

                return;
            }

            var elapsed = now - _shakeStartedAt;
            if (elapsed >= RotateHint.DurationSeconds)
            {
                _shakeStartedAt = -1f;
                _nextShakeTime = now + RotateHint.IntervalSeconds;
                _heldTileView.SetHintTwist(0f);
                return;
            }

            _heldTileView.SetHintTwist(RotateHint.AngleAt(elapsed));
        }

        private void DeferNextShake()
        {
            _shakeStartedAt = -1f;
            _nextShakeTime = Time.unscaledTime + RotateHint.IntervalSeconds;

            // Left exactly upright, so interrupting a shake cannot leave the tile crooked.
            _heldTileView?.SetHintTwist(0f);
        }
    }
}
