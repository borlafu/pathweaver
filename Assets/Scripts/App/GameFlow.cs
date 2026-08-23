using Pathweaver.Core.Campaign;
using Pathweaver.Game.Presentation;
using Pathweaver.Game.Presentation.Menus;
using UnityEngine;

namespace Pathweaver.Game.App
{
    /// <summary>
    /// Moves the player between menus and boards, and records what they finish.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One place that knows what a button means. The screens draw themselves and report which
    /// button was pressed; deciding what that does lives here, so no view has to know about
    /// another.
    /// </para>
    /// <para>
    /// The board is left visible behind the pause and settings screens rather than hidden. A player
    /// who pauses mid-puzzle is usually still looking at the board, and clearing the screen would
    /// make pausing feel like abandoning the level.
    /// </para>
    /// </remarks>
    internal sealed class GameFlow : MonoBehaviour
    {
        [SerializeField]
        private ScreenRouter _router;

        [SerializeField]
        private GameSession _session;

        [SerializeField]
        private MainMenuView _mainMenu;

        [SerializeField]
        private LevelSelectView _levelSelect;

        [SerializeField]
        private PauseView _pause;

        [SerializeField]
        private SettingsView _settings;

        [SerializeField]
        private GameObject _hud;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private Camera _camera;

        private SaveService _saves;
        private CampaignProgressStore _progressStore;
        private Campaign _campaign;
        private CampaignProgress _progress;
        private HexButton _pauseButton;
        private bool _hasRecordedThisClear;

        private void Start()
        {
            _saves = SaveService.ForPlayer();
            _progressStore = CampaignProgressStore.ForPlayer();
            _progress = _progressStore.Load();
            _campaign = CampaignCatalogue.Load();

            var material = _boardView.TileMaterial;

            _mainMenu.Build(_camera, material);
            _pause.Build(_camera, material);
            _settings.Build(_camera, material);
            _levelSelect.Build(_camera, material, _campaign, _progress);

            _pauseButton = HexButton.Create(
                _hud.transform, "pause", _camera, material,
                new Vector2(0.12f, 0.94f), 0.3f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.1f);

            // Two bars: the shape a pause control has had for fifty years.
            _pauseButton.AddGlyph(
                HexMeshFactory.CreateRectangle(0.05f, 0.2f), BoardPalette.MenuGlyph, new Vector3(-0.06f, 0f, 0f));
            _pauseButton.AddGlyph(
                HexMeshFactory.CreateRectangle(0.05f, 0.2f), BoardPalette.MenuGlyph, new Vector3(0.06f, 0f, 0f));

            _session.StateChanged += OnStateChanged;
            _router.ScreenChanged += OnScreenChanged;

            _router.Show(GameScreen.MainMenu);
        }

        private void OnDestroy()
        {
            if (_session != null)
            {
                _session.StateChanged -= OnStateChanged;
            }

            if (_router != null)
            {
                _router.ScreenChanged -= OnScreenChanged;
            }
        }

        /// <summary>
        /// Handles a tap that is not a play action.
        /// </summary>
        /// <returns>Whether the tap was consumed.</returns>
        internal bool HandleTap(Vector2 screenPosition)
        {
            switch (_router.Current)
            {
                case GameScreen.MainMenu:
                    return HandleMainMenu(_mainMenu.ButtonAt(screenPosition));
                case GameScreen.LevelSelect:
                    return HandleLevelSelect(_levelSelect.ButtonAt(screenPosition));
                case GameScreen.Paused:
                    return HandlePause(_pause.ButtonAt(screenPosition));
                case GameScreen.Settings:
                    return HandleSettings(_settings.ButtonAt(screenPosition));
                case GameScreen.Playing:
                    if (_pauseButton != null && _pauseButton.IsPressed(screenPosition))
                    {
                        _router.Show(GameScreen.Paused);
                        return true;
                    }

                    return false;
                default:
                    return false;
            }
        }

        private bool HandleMainMenu(string button)
        {
            switch (button)
            {
                case MainMenuView.ContinueId:
                    // Straight into wherever the player had got to, which is what most players
                    // want most of the time.
                    StartLevel(_campaign.NextLevel(_progress));
                    return true;
                case MainMenuView.LevelsId:
                    _levelSelect.Build(_camera, _boardView.TileMaterial, _campaign, _progress);
                    _router.Show(GameScreen.LevelSelect);
                    return true;
                case MainMenuView.SettingsId:
                    _settings.Refresh();
                    _router.Show(GameScreen.Settings);
                    return true;
                default:
                    return false;
            }
        }

        private bool HandleLevelSelect(string button)
        {
            if (button == null)
            {
                return false;
            }

            if (button == LevelSelectView.BackId)
            {
                _router.Show(GameScreen.MainMenu);
                return true;
            }

            StartLevel(button);
            return true;
        }

        private bool HandlePause(string button)
        {
            switch (button)
            {
                case PauseView.ResumeId:
                    _router.Show(GameScreen.Playing);
                    return true;
                case PauseView.RestartId:
                    _session.Restart();
                    _router.Show(GameScreen.Playing);
                    return true;
                case PauseView.MenuId:
                    _levelSelect.Build(_camera, _boardView.TileMaterial, _campaign, _progress);
                    _router.Show(GameScreen.LevelSelect);
                    return true;
                default:
                    return false;
            }
        }

        private bool HandleSettings(string button)
        {
            switch (button)
            {
                case SettingsView.HapticsId:
                    GameSettings.HapticsEnabled = !GameSettings.HapticsEnabled;
                    _settings.Refresh();
                    return true;
                case SettingsView.ReduceMotionId:
                    GameSettings.ReduceMotion = !GameSettings.ReduceMotion;
                    _settings.Refresh();
                    return true;
                case SettingsView.BackId:
                    _router.Show(GameScreen.MainMenu);
                    return true;
                default:
                    return false;
            }
        }

        private void StartLevel(string levelId)
        {
            _hasRecordedThisClear = false;

            // The screen is shown first so the play interface is active before the level reports
            // its opening state. Starting the level first meant the quota bar was disabled when the
            // only state change it would ever see went past, and it sat empty on a board that was
            // already finished.
            _router.Show(GameScreen.Playing);
            _session.Begin(levelId, _saves);
        }

        private void OnScreenChanged(GameScreen screen)
        {
            if (_hud != null)
            {
                _hud.SetActive(screen == GameScreen.Playing);
            }
        }

        private void OnStateChanged(Pathweaver.Core.State.GameState state)
        {
            if (!_session.IsComplete || _hasRecordedThisClear)
            {
                return;
            }

            // Recorded once per run, not once per state change: clearing the quota does not end the
            // board, so the state keeps changing afterwards.
            _hasRecordedThisClear = true;
            _progress = _progress.WithCleared(_session.LevelId);
            _progressStore.Save(_progress);
        }
    }
}
