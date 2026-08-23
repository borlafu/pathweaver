using System.Collections.Generic;
using NUnit.Framework;
using Pathweaver.Game.Platform;
using UnityEngine;

namespace Pathweaver.Game.EditorTests
{
    public class FrameRatePlanTests
    {
        [TestCase(144f, 120)]
        [TestCase(120f, 120)]
        [TestCase(90f, 90)]
        [TestCase(60f, 60)]
        public void The_active_rate_never_exceeds_the_screen_or_the_ceiling(float screenHz, int expected)
        {
            Assert.That(FrameRatePlan.ActiveRateFor(screenHz), Is.EqualTo(expected));
        }

        [Test]
        public void A_screen_faster_than_the_ceiling_is_capped()
        {
            // 240 Hz panels exist. Rendering a static puzzle board that fast spends
            // battery for nothing a player can see.
            Assert.That(FrameRatePlan.ActiveRateFor(240f), Is.EqualTo(FrameRatePlan.MaximumActiveHz));
        }

        [Test]
        public void An_unknown_screen_refresh_rate_still_gives_a_playable_rate()
        {
            // Some devices report zero. Trusting it would cap the game at the idle rate
            // or worse.
            Assert.That(FrameRatePlan.ActiveRateFor(0f), Is.EqualTo(60));
            Assert.That(FrameRatePlan.ActiveRateFor(float.NaN), Is.EqualTo(60));
        }

        [Test]
        public void The_active_rate_never_falls_below_the_idle_rate()
        {
            // A device reporting something implausibly low should still be playable.
            Assert.That(
                FrameRatePlan.ActiveRateFor(10f),
                Is.GreaterThanOrEqualTo(FrameRatePlan.IdleHz));
        }

        [Test]
        public void The_idle_rate_is_well_below_the_active_ceiling()
        {
            // The whole point is that thinking time costs less than acting time.
            Assert.That(FrameRatePlan.IdleHz, Is.LessThan(FrameRatePlan.MaximumActiveHz / 2));
        }
    }

    public class HapticsServiceTests
    {
        private GameObject _host;
        private HapticsService _haptics;
        private List<int> _fired;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("Haptics");
            _haptics = _host.AddComponent<HapticsService>();
            _fired = new List<int>();
            _haptics.OverrideVibrate(_fired.Add);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void Locking_a_tile_fires_a_short_pulse()
        {
            _haptics.TileLocked();

            Assert.That(_fired, Is.EqualTo(new[] { HapticsService.TileLockMilliseconds }));
        }

        [Test]
        public void Completing_a_route_fires_a_longer_pulse()
        {
            _haptics.RouteCompleted();

            Assert.That(_fired, Is.EqualTo(new[] { HapticsService.RouteCompleteMilliseconds }));
        }

        [Test]
        public void A_route_feels_more_significant_than_a_placement()
        {
            // The two have to be distinguishable through a pocket, or the reward reads as
            // just another placement.
            Assert.That(
                HapticsService.RouteCompleteMilliseconds,
                Is.GreaterThan(HapticsService.TileLockMilliseconds));
        }

        [Test]
        public void Both_pulses_stay_short_enough_to_read_as_confirmation()
        {
            // Anything approaching Handheld.Vibrate's half second reads as an error.
            Assert.That(HapticsService.TileLockMilliseconds, Is.LessThan(50));
            Assert.That(HapticsService.RouteCompleteMilliseconds, Is.LessThan(50));
        }

        [Test]
        public void Disabling_haptics_silences_them_entirely()
        {
            _haptics.IsEnabled = false;

            _haptics.TileLocked();
            _haptics.RouteCompleted();

            Assert.That(_fired, Is.Empty);
        }

        [Test]
        public void Re_enabling_haptics_restores_them()
        {
            _haptics.IsEnabled = false;
            _haptics.TileLocked();

            _haptics.IsEnabled = true;
            _haptics.TileLocked();

            Assert.That(_fired.Count, Is.EqualTo(1));
        }
    }
}
