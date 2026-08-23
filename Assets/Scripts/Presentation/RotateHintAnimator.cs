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
    /// rotated is the thing that demonstrates rotating, so there is nothing to interpret. It
    /// stops for good at the first rotation, because a hint that keeps arriving after being
    /// understood is just noise.
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
        private bool _isRetired;

        private void OnEnable()
        {
            if (_session != null)
            {
                _session.HeldRotated += Retire;
            }

            _nextShakeTime = Time.unscaledTime + RotateHint.IntervalSeconds;
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.HeldRotated -= Retire;
            }
        }

        private void Update()
        {
            if (_isRetired || _heldTileView == null)
            {
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

        private void Retire()
        {
            _isRetired = true;
            _shakeStartedAt = -1f;

            // Left exactly upright, so retiring the hint cannot leave the tile crooked.
            _heldTileView?.SetHintTwist(0f);
        }
    }
}
