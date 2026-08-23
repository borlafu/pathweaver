using UnityEngine;

namespace Pathweaver.Game.Platform
{
    /// <summary>
    /// Runs fast while the player is acting and slow while they are thinking.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Anything that constitutes activity calls <see cref="NotifyActivity"/>; the
    /// governor drops back on its own. Nothing else needs to know the current rate.
    /// </para>
    /// <para>
    /// V-sync is switched off because <c>Application.targetFrameRate</c> is ignored
    /// while it is on, which would make the whole mechanism a no-op that still looks
    /// implemented.
    /// </para>
    /// </remarks>
    internal sealed class FrameRateGovernor : MonoBehaviour
    {
        private float _lastActivityTime;
        private bool _isActive;

        /// <summary>The rate this screen gets while the player is acting.</summary>
        internal int ActiveRate { get; private set; }

        /// <summary>What the frame rate is currently capped at.</summary>
        internal int CurrentRate => _isActive ? ActiveRate : FrameRatePlan.IdleHz;

        /// <summary>Reports that the player did something.</summary>
        internal void NotifyActivity()
        {
            _lastActivityTime = Time.unscaledTime;

            if (!_isActive)
            {
                _isActive = true;
                Apply();
            }
        }

        private void Awake()
        {
            // Without this, targetFrameRate is ignored and the throttle silently does
            // nothing.
            QualitySettings.vSyncCount = 0;

            ActiveRate = FrameRatePlan.ActiveRateFor((float)Screen.currentResolution.refreshRateRatio.value);

            _isActive = true;
            _lastActivityTime = Time.unscaledTime;
            Apply();
        }

        private void Update()
        {
            if (!_isActive)
            {
                return;
            }

            if (Time.unscaledTime - _lastActivityTime < FrameRatePlan.IdleAfterSeconds)
            {
                return;
            }

            _isActive = false;
            Apply();
        }

        private void Apply()
        {
            Application.targetFrameRate = CurrentRate;
        }
    }
}
