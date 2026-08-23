using System;
using UnityEngine;

namespace Pathweaver.Game.Platform
{
    /// <summary>
    /// Short vibrations that confirm something happened.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two events are worth feeling: a tile locking into place, which confirms an action,
    /// and a route completing, which rewards one. Keeping the vocabulary at two is what
    /// stops the phone buzzing constantly.
    /// </para>
    /// <para>
    /// Durations were raised and amplitudes made explicit after device testing: the first
    /// values, 12 and 30 milliseconds at default amplitude, were reported as barely
    /// noticeable. A confirmation nobody feels is worse than none, because it costs battery
    /// and delivers nothing.
    /// </para>
    /// <para>
    /// A route now plays two pulses rather than one longer one. Length alone is hard to
    /// judge through a pocket, while a count is not: two buzzes read as different in kind
    /// from one, rather than as the same buzz held slightly longer.
    /// </para>
    /// </remarks>
    internal sealed class HapticsService : MonoBehaviour
    {
        /// <summary>
        /// A tile settling onto the board: one firm tap.
        /// </summary>
        internal static readonly int[] TileLockPattern = { 25 };

        /// <summary>
        /// A route completing: two pulses, so it is distinguishable by count rather than
        /// by duration.
        /// </summary>
        /// <remarks>
        /// Read as on, off, on in milliseconds.
        /// </remarks>
        internal static readonly int[] RouteCompletePattern = { 45, 55, 70 };

        /// <summary>
        /// Full strength. The default amplitude is device-defined and was too weak to
        /// notice on the phone this was tested on.
        /// </summary>
        private const int Amplitude = 255;

        private Action<int[]> _vibrate;

        /// <summary>
        /// Whether haptics fire. Off means silent, not weaker.
        /// </summary>
        internal bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Replaces the platform call, so tests can observe what would have fired.
        /// </summary>
        internal void OverrideVibrate(Action<int[]> vibrate)
        {
            _vibrate = vibrate;
        }

        internal void TileLocked() => Fire(TileLockPattern);

        internal void RouteCompleted() => Fire(RouteCompletePattern);

        private void Fire(int[] pattern)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (_vibrate != null)
            {
                _vibrate(pattern);
                return;
            }

            Vibrate(pattern);
        }

        private static void Vibrate(int[] pattern)
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
                // against, so no legacy path is needed.
                using var effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                using var effect = pattern.Length == 1
                    ? effectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)pattern[0], Amplitude)
                    : CreateWaveform(effectClass, pattern);

                vibrator.Call("vibrate", effect);
            }
            catch (Exception error)
            {
                // A device without a motor, or a manufacturer that moved the service, must
                // not take the game down over a buzz.
                Debug.LogWarning($"[haptics] vibration unavailable: {error.Message}");
            }
#else
            // Nothing to do off-device; the Editor has no motor.
            _ = pattern;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject CreateWaveform(AndroidJavaClass effectClass, int[] pattern)
        {
            // Waveforms start with a delay, so the leading entry is zero and the pattern
            // alternates on and off from there.
            var timings = new long[pattern.Length + 1];
            var amplitudes = new int[pattern.Length + 1];

            for (var index = 0; index < pattern.Length; index++)
            {
                timings[index + 1] = pattern[index];
                amplitudes[index + 1] = index % 2 == 0 ? Amplitude : 0;
            }

            return effectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1);
        }
#endif
    }
}
