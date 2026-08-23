using Pathweaver.Core.Campaign;
using Pathweaver.Core.Endless;
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
    /// The board stays visible behind the pause panel, because a player who pauses mid-puzzle is
    /// usually still looking at it and clearing the screen would make pausing feel like abandoning
    /// the level. It is hidden behind the menus, which are whole screens rather than overlays: a
    /// half-built board showing through a level list reads as a rendering fault.
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
        private EndlessRunStore _endlessStore;
        private EndlessRun _endlessRun;
        private HexButton _pauseButton;
        private HexButton _nextRoundButton;
        private bool _hasRecordedThisClear;

        /// <summary>
        /// Whether the board on screen is a generated endless round rather than a campaign level.
        /// </summary>
        /// <remarks>
        /// Tracked rather than inferred from the identifier. A level called "endless-3" would be an
        /// odd thing to author, but deciding what mode the player is in by matching a string is the
        /// kind of rule that quietly stops being true.
        /// </remarks>
        private bool _isEndlessRound;

        /// <summary>The endless board most recently started, so its save can be cleared later.</summary>
        private string _lastEndlessLevelId;

        private void Start()
        {
            _saves = SaveService.ForPlayer();
            _progressStore = CampaignProgressStore.ForPlayer();
            _progress = _progressStore.Load();
            _campaign = CampaignCatalogue.Load();
            _endlessStore = EndlessRunStore.ForPlayer();
            _endlessRun = _endlessStore.Load();

            var material = _boardView.TileMaterial;

            MenuCamera.Frame(_camera);

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

            // Only ever visible on a finished endless round. A campaign level has a level list to go
            // back to; an endless run has nowhere to go but forward, so it needs a way to say so.
            _nextRoundButton = HexButton.Create(
                _hud.transform, "next-round", _camera, material,
                new Vector2(0.86f, 0.94f), 0.3f, BoardPalette.MenuPrimary, touchRadiusFraction: 0.1f);
            _nextRoundButton.AddGlyph(
                HexMeshFactory.CreateRegularPolygon(3, 0.16f, rotationDegrees: -90f), BoardPalette.MenuGlyph);
            _nextRoundButton.gameObject.SetActive(false);

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

                    if (_nextRoundButton != null
                        && _nextRoundButton.gameObject.activeSelf
                        && _nextRoundButton.IsPressed(screenPosition))
                    {
                        StartEndlessRound();
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
                    ShowLevelSelect();
                    return true;
                case MainMenuView.EndlessId:
                    StartEndlessRound();
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
                    ShowLevelSelect();
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

        /// <summary>
        /// Rebuilds the level list and shows it.
        /// </summary>
        /// <remarks>
        /// Rebuilt each time so a level cleared moments ago is not still drawn as locked, and framed
        /// before it is built because the grid's button size is computed from the camera.
        /// </remarks>
        private void ShowLevelSelect()
        {
            MenuCamera.Frame(_camera);
            _levelSelect.Build(_camera, _boardView.TileMaterial, _campaign, _progress);
            _router.Show(GameScreen.LevelSelect);
        }

        /// <summary>
        /// Plays the round the endless run is on, generating it now.
        /// </summary>
        /// <remarks>
        /// Generation is cheap because the generator plans the routes and derives the board from
        /// them rather than searching for a solution, so a round can be built between taps rather
        /// than shipped in the package.
        /// </remarks>
        private void StartEndlessRound()
        {
            _hasRecordedThisClear = false;
            _isEndlessRound = true;

            var round = _endlessRun.CurrentRound();

            // The board just left behind is finished and will never be dealt again, so its save is
            // dead weight in the player's storage. Cleared here rather than on completion, because a
            // player may keep extending routes on a finished board and that run is still worth
            // resuming until they move on.
            if (_lastEndlessLevelId != null && _lastEndlessLevelId != round.Level.Id)
            {
                _saves?.Delete(_lastEndlessLevelId);
            }

            _lastEndlessLevelId = round.Level.Id;

            _router.Show(GameScreen.Playing);
            _session.Begin(round.Level, _saves);
        }

        private void StartLevel(string levelId)
        {
            _hasRecordedThisClear = false;
            _isEndlessRound = false;

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

            // Visible while playing and while paused, hidden behind the menus. Pausing is looking
            // at the board; opening the level list is leaving it.
            var isBoardOnScreen = screen == GameScreen.Playing || screen == GameScreen.Paused;

            if (_boardView != null)
            {
                _boardView.gameObject.SetActive(isBoardOnScreen);
            }

            // The board fitter rezooms the camera per level, so a menu shown afterwards would
            // inherit that zoom and draw its buttons at the wrong size. Pause keeps the board's
            // framing because the board is still on screen behind it.
            if (!isBoardOnScreen)
            {
                MenuCamera.Frame(_camera);
            }
        }

        private void OnStateChanged(Pathweaver.Core.State.GameState state)
        {
            // Offered while the round is finished and withdrawn if a restart puts the player back
            // below the target, so the control never claims a round is done when it is not.
            if (_nextRoundButton != null)
            {
                _nextRoundButton.gameObject.SetActive(_isEndlessRound && _session.IsComplete);
            }

            if (!_session.IsComplete || _hasRecordedThisClear)
            {
                return;
            }

            // Recorded once per run, not once per state change: clearing the quota does not end the
            // board, so the state keeps changing afterwards.
            _hasRecordedThisClear = true;

            if (_isEndlessRound)
            {
                // Advanced as soon as the quota is met rather than when the player taps onward, so
                // the furthest round reached survives closing the app on a finished board.
                _endlessRun = _endlessRun.Cleared();
                _endlessStore.Save(_endlessRun);
                return;
            }

            _progress = _progress.WithCleared(_session.LevelId);
            _progressStore.Save(_progress);
        }
    }
}
