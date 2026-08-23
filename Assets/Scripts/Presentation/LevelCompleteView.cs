using Pathweaver.Core.State;
using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Says so when the level's quota is met.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Added because a player finished a level and the game said nothing. The simulation had
    /// scored the route, banked the points, and granted a Pivot Token, and none of it was
    /// visible — so the correct behaviour was indistinguishable from a broken game.
    /// </para>
    /// <para>
    /// Dismissable rather than blocking, and it does not reappear for the same run. Clearing the
    /// quota is not the end of the board: PRD section 3.2A rewards extending routes, so a player
    /// who wants a longer route and a bigger score should be able to carry on. The restart button
    /// stays where it always is rather than being offered here, so there is one way to start over
    /// rather than two.
    /// </para>
    /// </remarks>
    internal sealed class LevelCompleteView : MonoBehaviour
    {
        private const float PanelWidth = 2.2f;
        private const float PanelHeight = 1.0f;
        private const float TickRadius = 0.34f;
        private const float GlyphThickness = 0.085f;
        private const float RiseSeconds = 0.35f;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private GameSession _session;

        private Transform _panel;
        private bool _hasShownForThisRun;
        private float _shownAt = -1f;

        internal bool IsOpen => _panel != null && _panel.gameObject.activeSelf;

        private Camera ResolvedCamera => _camera != null ? _camera : Camera.main;

        /// <summary>Dismisses the notice, leaving the board playable.</summary>
        internal void Dismiss()
        {
            if (_panel != null)
            {
                _panel.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_session != null)
            {
                _session.StateChanged += OnStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(GameState state)
        {
            if (_session == null)
            {
                return;
            }

            // Restarting clears the flag, so a replay can be congratulated again.
            if (!_session.IsComplete)
            {
                _hasShownForThisRun = false;
                Dismiss();
                return;
            }

            if (_hasShownForThisRun)
            {
                return;
            }

            _hasShownForThisRun = true;
            Show();
        }

        private void Show()
        {
            EnsureBuilt();

            if (_panel == null)
            {
                return;
            }

            _panel.gameObject.SetActive(true);
            _shownAt = Time.unscaledTime;
        }

        private void LateUpdate()
        {
            if (!IsOpen)
            {
                return;
            }

            var centre = ResolvedCamera.transform.position;

            // Rises into place rather than appearing: arriving is what makes it read as a
            // response to what the player just did.
            var progress = Mathf.Clamp01((Time.unscaledTime - _shownAt) / RiseSeconds);
            var eased = 1f - ((1f - progress) * (1f - progress));
            var offset = Mathf.Lerp(-0.45f, 0f, eased);

            _panel.position = new Vector3(centre.x, centre.y + 1.1f + offset, -1f);
            _panel.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, eased);
        }

        private void EnsureBuilt()
        {
            if (_panel != null || _boardView == null)
            {
                return;
            }

            _panel = new GameObject("Panel").transform;
            _panel.SetParent(transform, worldPositionStays: false);

            AddPart(_panel, "Background", HexMeshFactory.CreateRectangle(PanelWidth, PanelHeight),
                BoardPalette.DialogPanel, 0f);

            var badge = new GameObject("Badge").transform;
            badge.SetParent(_panel, worldPositionStays: false);
            badge.localPosition = new Vector3(0f, 0f, -0.02f);

            AddPart(badge, "Face", HexMeshFactory.CreateHexagon(TickRadius),
                BoardPalette.ProgressComplete, 0f);

            var shortArm = HexMeshFactory.CreateRectangle(TickRadius * 0.55f, GlyphThickness);
            var longArm = HexMeshFactory.CreateRectangle(TickRadius * 0.95f, GlyphThickness);

            AddPart(badge, "Short", shortArm, BoardPalette.RestartArrow, -0.02f,
                new Vector3(-0.1f, -0.055f, 0f), -50f);
            AddPart(badge, "Long", longArm, BoardPalette.RestartArrow, -0.02f,
                new Vector3(0.05f, 0.02f, 0f), 52f);

            _panel.gameObject.SetActive(false);
        }

        private void AddPart(
            Transform parent,
            string childName,
            Mesh mesh,
            Color colour,
            float depth,
            Vector3 offset = default,
            float rotationDegrees = 0f)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(parent, worldPositionStays: false);
            child.transform.localPosition = new Vector3(offset.x, offset.y, depth);
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);

            child.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _boardView.TileMaterial;
            renderer.material.color = colour;
        }
    }
}
