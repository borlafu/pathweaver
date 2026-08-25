using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Breathes every spring outward and every hub inward, for as long as a board is on screen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One component for the whole board rather than one per cell: there are two to seven endpoints on a
    /// level, and a single loop over a cached list costs less than the same number of MonoBehaviour
    /// callbacks.
    /// </para>
    /// <para>
    /// It never calls <see cref="FrameRateGovernor.NotifyActivity"/>. That is deliberate and load-bearing:
    /// notifying would pin the frame rate at its active ceiling for as long as the game is open, which is
    /// the opposite of PRD section 5.2. The pulse runs at the 30 Hz idle rate, which is what its period is
    /// chosen for.
    /// </para>
    /// </remarks>
    internal sealed class EndpointPulseAnimator : MonoBehaviour
    {
        [SerializeField]
        private BoardView _boardView;

        /// <summary>Whether the rings have already been put away for reduced motion.</summary>
        private bool _isResting;

        private void Update()
        {
            if (_boardView == null)
            {
                return;
            }

            var cells = _boardView.PulsingCells;

            if (GameSettings.ReduceMotion)
            {
                // Reduced motion means off rather than slow, following the rotation hint — and the rings
                // are put away once rather than every frame, so the setting costs nothing while it is on.
                if (!_isResting)
                {
                    for (var index = 0; index < cells.Count; index++)
                    {
                        cells[index].RestPulse();
                    }

                    _isResting = true;
                }

                return;
            }

            _isResting = false;

            var now = Time.unscaledTime;

            for (var index = 0; index < cells.Count; index++)
            {
                var cell = cells[index];
                var elapsed = now + EndpointPulse.PhaseOffsetFor(cell.Coordinate.Q, cell.Coordinate.R);

                cell.SetPulse(
                    EndpointPulse.ScaleAt(elapsed, cell.PulseRole),
                    EndpointPulse.FadeAt(elapsed, cell.PulseRole));
            }
        }
    }
}
