using System;
using UnityEngine;

namespace Pathweaver.Game.Platform
{
    /// <summary>
    /// Short vibrations that confirm something happened.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A tile locking into place and a route completing are the two moments worth
    /// feeling: one confirms an action the player took, the other rewards it. Anything
    /// more and the game buzzes constantly, which is why the vocabulary stays at two.
    /// </para>
    /// <para>
    /// Durations are in milliseconds and deliberately short. <c>Handheld.Vibrate</c> is
    /// avoided because it runs for roughly half a second on Android, which reads as an
    /// error rather than a confirmation.
    /// </para>
    /// </remarks>
    internal sealed class HapticsService : MonoBehaviour
    {
        /// <summary>A tile settling onto the board.</summary>
        internal const int TileLockMilliseconds = 12;

        /// <summary>A route completing and paying out.</summary>
        internal const int RouteCompleteMilliseconds = 30;

        private Action<int> _vibrate;

        /// <summary>
        /// Whether haptics fire. Off means silent, not shorter.
        /// </summary>
        /// <remarks>
        /// Some players find vibration unpleasant and some devices have poor motors, so
        /// this is a setting rather than a constant, exposed in #27.
        /// </remarks>
        internal bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Replaces the platform call, so tests can observe what would have fired.
        /// </summary>
        internal void OverrideVibrate(Action<int> vibrate)
        {
            _vibrate = vibrate;
        }

        internal void TileLocked() => Fire(TileLockMilliseconds);

        internal void RouteCompleted() => Fire(RouteCompleteMilliseconds);

        private void Fire(int milliseconds)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (_vibrate != null)
            {
                _vibrate(milliseconds);
                return;
            }

            Vibrate(milliseconds);
        }

        private static void Vibrate(int milliseconds)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

                if (vibrator == null)
                {
                    return;
                }

                // VibrationEffect arrived in API 26, far below the API 36 this ships
                // against, so the legacy path is not worth carrying.
                using var effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                using var effect = effectClass.CallStatic<AndroidJavaObject>(
                    "createOneShot", (long)milliseconds, -1);

                vibrator.Call("vibrate", effect);
            }
            catch (Exception error)
            {
                // A device without a motor, or a manufacturer that moved the service,
                // must not take the game down over a buzz.
                Debug.LogWarning($"[haptics] vibration unavailable: {error.Message}");
            }
#else
            // Nothing to do off-device; the Editor has no motor.
            _ = milliseconds;
#endif
        }
    }
}
