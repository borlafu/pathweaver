using Pathweaver.Core.Atlas;
using Pathweaver.Core.Campaign;
using Pathweaver.Core.Endless;
using Pathweaver.Core.Rules;
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
        /// <summary>
        /// Where the only control a finished board offers sits, as viewport fractions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// In the drawer, in the gap the tile tray leaves behind. It used to be centred on the screen,
        /// which was the wrong reading of "it is the only control left": most routes run through the
        /// middle of the board, so clearing a level hid the route that cleared it, and the payout rising
        /// from the hub spent most of its life behind a green hexagon.
        /// </para>
        /// <para>
        /// The drawer costs nothing to move it to. It is empty by then, it is where every other thing a
        /// thumb touches already lives, and it leaves the whole board visible for the one moment the
        /// player is meant to look at what they built.
        /// </para>
        /// </remarks>
        internal const float NextButtonViewportX = 0.5f;

        internal const float NextButtonViewportY = 0.12f;

        /// <summary>
        /// How large that control is, in world units.
        /// </summary>
        /// <remarks>
        /// Smaller than the 0.85 it was drawn at on the centre of the screen, because the drawer is not
        /// that tall — but it keeps a touch target generous enough that shrinking it costs no reach.
        /// </remarks>
        internal const float NextButtonRadius = 0.55f;

        /// <summary>How far from its centre a tap still counts, as a fraction of the shorter screen edge.</summary>
        internal const float NextButtonTouchFraction = 0.2f;

        /// <summary>
        /// Where a cleared board says what it paid into the World Atlas.
        /// </summary>
        /// <remarks>
        /// Under the score, because it is the score that earned it — the harvest is a share of the base
        /// route score, so the two numbers belong together and one explains the other.
        /// </remarks>
        internal const float EssenceEarnedViewportY = 0.865f;

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
        private AtlasView _atlas;

        [SerializeField]
        private HelpView _help;

        [SerializeField]
        private GameObject _hud;

        /// <summary>
        /// The controls a finished board no longer offers: restart, skip, remove, and the tile tray.
        /// </summary>
        /// <remarks>
        /// Grouped so finishing a board is one call rather than five. The progress bar and the token
        /// pips stay outside the group, because they report rather than act.
        /// </remarks>
        [SerializeField]
        private GameObject _playControls;

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
        private AtlasProgressStore _atlasStore;
        private AtlasProgress _atlasProgress;
        private AtlasMap _atlasMap;
        private HexButton _pauseButton;
        private HexButton _nextButton;
        private Presentation.Text.TextLabel _essenceEarned;
        private bool _hasRecordedThisClear;

        /// <summary>
        /// Where the back button on the help screen goes.
        /// </summary>
        /// <remarks>
        /// Help is reachable from two places and has to return to the one it was opened from. Held here
        /// rather than in <c>HelpView</c> because it is a fact about the route, not about the screen.
        /// </remarks>
        private GameScreen _helpReturnsTo = GameScreen.MainMenu;

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
            _atlasStore = AtlasProgressStore.ForPlayer();
            _atlasProgress = _atlasStore.Load();
            _atlasMap = AtlasCatalogue.Load();

            var material = _boardView.TileMaterial;

            MenuCamera.Frame(_camera);

            _mainMenu.Build(_camera, material);
            _pause.Build(_camera, material);
            _settings.Build(_camera, material);
            _levelSelect.Build(_camera, material, _campaign, _progress);
            _atlas.Build(_camera, material, _atlasMap, _atlasProgress);
            _help.Build(_camera, material);

            // Top right, opposite restart. The two controls that leave a board sit in the two
            // corners furthest from the thumb's resting position, where a mis-tap costs the most.
            _pauseButton = HexButton.Create(
                _hud.transform, "pause", _camera, material,
                new Vector2(0.88f, 0.94f), 0.3f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.1f);

            MenuGlyphs.AddPause(_pauseButton);

            // The only control a finished board offers, in either mode. In the drawer rather than on the
            // middle of the screen, so it does not cover the route it is celebrating. Green rather than
            // blue, because reaching it is the win.
            _nextButton = HexButton.Create(
                _hud.transform, "next", _camera, material,
                new Vector2(NextButtonViewportX, NextButtonViewportY),
                NextButtonRadius,
                BoardPalette.ProgressComplete,
                touchRadiusFraction: NextButtonTouchFraction);
            MenuGlyphs.AddPlay(_nextButton, NextButtonRadius * 0.4f);

            _essenceEarned = Presentation.Text.TextLabel.Create(
                _hud.transform,
                _camera,
                "essence-earned",
                new Vector2(0.5f, EssenceEarnedViewportY),
                Presentation.Text.LabelMetrics.CaptionHeightFraction,
                BoardPalette.AtlasEssence,
                TMPro.TextAlignmentOptions.Center,
                HexButton.LabelDepth);
            _nextButton.gameObject.SetActive(false);

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
                case GameScreen.Atlas:
                    return HandleAtlas(_atlas.ButtonAt(screenPosition));
                case GameScreen.Help:
                    return HandleHelp(_help.ButtonAt(screenPosition));
                case GameScreen.Playing:
                    if (_pauseButton != null && _pauseButton.IsPressed(screenPosition))
                    {
                        _router.Show(GameScreen.Paused);
                        return true;
                    }

                    if (_nextButton != null
                        && _nextButton.gameObject.activeSelf
                        && _nextButton.IsPressed(screenPosition))
                    {
                        MoveOn();
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
                case MainMenuView.AtlasId:
                    ShowAtlas();
                    return true;
                case MainMenuView.HelpId:
                    ShowHelp(GameScreen.MainMenu);
                    return true;
                case MainMenuView.SettingsId:
                    // Opened disarmed, always. An arming left behind from a previous visit would turn
                    // the first tap of this one into a wipe.
                    _settings.SetResetArmed(false);
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
                case PauseView.HelpId:
                    ShowHelp(GameScreen.Paused);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Buys a node, or leaves the atlas.
        /// </summary>
        /// <remarks>
        /// A tap on a node the player cannot afford does nothing rather than explaining itself: the
        /// node already shows its cost in pips and its colour already says whether it is within reach,
        /// so a refusal would be repeating what is on screen.
        /// </remarks>
        private bool HandleAtlas(string button)
        {
            if (button == null)
            {
                return false;
            }

            if (button == AtlasView.BackId)
            {
                // Dropped so that coming back starts from the introduction rather than from whatever the
                // player was reading last time, which by then may be a node they already own.
                _atlas.ClearSelection();
                _router.Show(GameScreen.MainMenu);
                return true;
            }

            // The first tap on a node says what it costs and what it gives; only the second buys it. An
            // unaffordable node is selected too, because saying why is the whole reason this screen came
            // back — it used to answer a tap it could not honour with silence.
            var confirming = _atlas.Select(button);

            if (!confirming || !_atlasMap.CanUnlock(button, _atlasProgress))
            {
                return true;
            }

            _atlasProgress = _atlasProgress.WithUnlocked(button, _atlasMap.Node(button).Cost);
            _atlasStore.Save(_atlasProgress);

            // Rebuilt rather than patched: the node just bought changes its own colour, its links, the
            // essence row, and whatever it unlocked next, and redrawing the screen cannot get that
            // combination wrong.
            _atlas.Build(_camera, _boardView.TileMaterial, _atlasMap, _atlasProgress);
            return true;
        }

        /// <summary>
        /// Answers the device's own back gesture, exactly as the screen's back control would.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Nothing handled it before, so Android did whatever it does with an unconsumed back — which is
        /// not what the on-screen back control does, and on a recent Android also animates the window
        /// shrinking as a preview of leaving the app. A player pressing back on the settings screen
        /// expects the settings screen to close.
        /// </para>
        /// <para>
        /// Every case here is the same route the screen's own control takes, deliberately: two ways to
        /// go back that disagree are worse than one way that is inconvenient. On the board it pauses,
        /// because the board is the one screen with nothing behind it to return to, and pausing is what
        /// the corner control does. On the main menu it leaves the game, which is what back means on a
        /// root screen everywhere else on the device.
        /// </para>
        /// </remarks>
        /// <returns>Whether the game consumed the gesture.</returns>
        internal bool HandleBack()
        {
            switch (_router.Current)
            {
                case GameScreen.Help:
                    return HandleHelp(HelpView.BackId);
                case GameScreen.LevelSelect:
                    return HandleLevelSelect(LevelSelectView.BackId);
                case GameScreen.Settings:
                    return HandleSettings(SettingsView.BackId);
                case GameScreen.Atlas:
                    return HandleAtlas(AtlasView.BackId);
                case GameScreen.Paused:
                    return HandlePause(PauseView.ResumeId);
                case GameScreen.Playing:
                    // A finished board has withdrawn its pause control, so back has nothing to do rather
                    // than quietly pausing a board that is over.
                    if (_session != null && _session.IsComplete)
                    {
                        return true;
                    }

                    _router.Show(GameScreen.Paused);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Turns a page, or goes back to whichever screen asked for help.
        /// </summary>
        /// <remarks>
        /// Returning to where the player came from, rather than always to the main menu, is what makes
        /// help reachable mid-level: a player who paused to read how tokens work should land back on
        /// their board, not outside it.
        /// </remarks>
        private bool HandleHelp(string button)
        {
            if (button == null)
            {
                return false;
            }

            if (button == HelpView.NextId)
            {
                _help.Advance();
                return true;
            }

            if (button == HelpView.BackId)
            {
                if (_helpReturnsTo == GameScreen.Paused)
                {
                    // The board is still framed as it was, so the camera is left alone.
                    _router.Show(GameScreen.Paused);
                    return true;
                }

                _router.Show(GameScreen.MainMenu);
                return true;
            }

            return false;
        }

        private void ShowHelp(GameScreen returnTo)
        {
            _helpReturnsTo = returnTo;
            _help.ShowPage(0);
            _router.Show(GameScreen.Help);
        }

        /// <summary>
        /// Pays Star Essence for a cleared board.
        /// </summary>
        /// <remarks>
        /// Every clear pays, in both modes, because the essence relic and the length curve should pull
        /// the same way: a longer route is worth more points and more essence. A campaign level replayed
        /// pays again — the alternative is a rule a player cannot see, and the campaign is finite while
        /// Endless is not, so there is nothing here worth farming.
        /// </remarks>
        private void AwardEssence(Pathweaver.Core.State.GameState state)
        {
            var bonus = _atlasMap.BonusesFor(_atlasProgress).EssencePerClear;
            var harvested = AtlasEssence.ForClear(state.Score, state.BaseRouteScore, bonus);

            if (harvested <= 0)
            {
                return;
            }

            _atlasProgress = _atlasProgress.WithEssence(harvested);
            _atlasStore.Save(_atlasProgress);

            // Said out loud, next to the score that earned it. It was paid in silence before, so a player
            // had no way to connect a balance on the atlas screen with anything they had done — which is
            // half of why the atlas had to be withheld at all.
            _essenceEarned?.SetText(Pathweaver.Game.Presentation.Text.AtlasWords.Earned(harvested));
        }

        private void ShowAtlas()
        {
            MenuCamera.Frame(_camera);
            _atlas.Build(_camera, _boardView.TileMaterial, _atlasMap, _atlasProgress);
            _router.Show(GameScreen.Atlas);
        }

        /// <summary>
        /// Flips a switch, or takes the two taps that erase everything.
        /// </summary>
        /// <remarks>
        /// Every button other than reset disarms it, including a tap on nothing. Without that the
        /// arming would sit on screen indefinitely, and the tap that finally landed on it would be one
        /// the player had stopped thinking about.
        /// </remarks>
        private bool HandleSettings(string button)
        {
            if (button != SettingsView.ResetId && _settings.IsResetArmed)
            {
                _settings.SetResetArmed(false);
            }

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
                case SettingsView.ResetId:
                    if (!_settings.IsResetArmed)
                    {
                        _settings.SetResetArmed(true);
                        return true;
                    }

                    ResetProgress();
                    return true;
                case SettingsView.BackId:
                    _router.Show(GameScreen.MainMenu);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Erases every record of past play and returns the game to its first-launch state.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The stores are reloaded rather than assigned empty values, so the game reads its own
        /// freshly wiped storage exactly as a first launch does. That also means a file the platform
        /// refused to delete shows up here as progress still present, rather than as a screen that
        /// disagrees with the disk.
        /// </para>
        /// <para>
        /// It ends on the main menu. Staying in settings would leave the player looking at the control
        /// they just used with no sign anything had happened, and the menu is where a first launch
        /// starts.
        /// </para>
        /// </remarks>
        private void ResetProgress()
        {
            ProgressReset.Wipe(_saves, _progressStore, _atlasStore, _endlessStore);

            _progress = _progressStore.Load();
            _atlasProgress = _atlasStore.Load();
            _endlessRun = _endlessStore.Load();

            // The board that was on screen, if any, is no longer anyone's run: its save is gone, and
            // the endless bookkeeping must not delete a save belonging to a round dealt after this.
            _lastEndlessLevelId = null;
            _isEndlessRound = false;
            _hasRecordedThisClear = false;
            _essenceEarned?.SetText(string.Empty);

            _settings.SetResetArmed(false);
            _router.Show(GameScreen.MainMenu);
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
        /// Leaves a finished board for whatever comes next.
        /// </summary>
        /// <remarks>
        /// Endless goes to the next generated round. The campaign goes to the next level that is not
        /// yet cleared, and to the level list once there are none — which is the honest answer at the
        /// end of the biome, rather than offering the last level again and calling it progress.
        /// </remarks>
        private void MoveOn()
        {
            if (_isEndlessRound)
            {
                StartEndlessRound();
                return;
            }

            var next = _campaign.NextLevel(_progress);

            if (next == _session.LevelId)
            {
                ShowLevelSelect();
                return;
            }

            StartLevel(next);
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
            _essenceEarned?.SetText(string.Empty);
            _isEndlessRound = true;

            // A finished board stays playable, so tokens can change after the round was banked.
            // Taking the live counts here means what the player actually holds is what travels.
            if (_isEndlessRound && _session.State != null)
            {
                _endlessRun = _endlessRun.Carrying(
                    _session.State.PivotTokens.Count, _session.State.SkipTokens.Count);
            }

            // Relics reach a generated round the same way carried tokens do, by raising what the round
            // deals. The generator treats both as a floor under its own allowance, and caps the result
            // at the ceilings the same relics have earned — a relic that dealt a fourth token without
            // raising the ceiling would be handing the player something they cannot hold.
            var bonuses = _atlasMap.BonusesFor(_atlasProgress);
            var round = _endlessRun
                .Carrying(
                    _endlessRun.CarriedPivotTokens + bonuses.Tokens,
                    _endlessRun.CarriedSkips + bonuses.Skips)
                .CurrentRound(
                    TokenRules.CapacityWith(bonuses.Tokens), TokenRules.CapacityWith(bonuses.Skips));

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
            _essenceEarned?.SetText(string.Empty);
            _isEndlessRound = false;

            // The screen is shown first so the play interface is active before the level reports
            // its opening state. Starting the level first meant the quota bar was disabled when the
            // only state change it would ever see went past, and it sat empty on a board that was
            // already finished.
            // Carried Pivot Tokens are handed to the level here, with its own allowance as a floor:
            // clearing a level ends the board, so a token earned by the clearing route would be
            // unspendable if it did not travel. Atlas relics are added on top of that, because a
            // permanent upgrade that replaced an allowance would make a generous level worse.
            // Relics raise the ceiling as well as the hand: a fourth token dealt into a pool that holds
            // three would vanish, which is the defect the ceiling was added to fix rather than cause.
            // WithStartingResources trims a hand that overshoots, so a carried count from a board played
            // with more relics unlocked lands on this one rather than throwing.
            var level = LevelCatalogue.Load(levelId);
            var bonuses = _atlasMap.BonusesFor(_atlasProgress);
            var tokens = Mathf.Max(level.StartingTokens, _progress.PivotTokens) + bonuses.Tokens;

            _router.Show(GameScreen.Playing);
            _session.Begin(
                level.WithStartingResources(
                    tokens,
                    level.StartingSkips + bonuses.Skips,
                    TokenRules.CapacityWith(bonuses.Tokens),
                    TokenRules.CapacityWith(bonuses.Skips)),
                _saves);
        }

        private void OnScreenChanged(GameScreen screen)
        {
            if (_hud != null)
            {
                _hud.SetActive(screen == GameScreen.Playing);
            }

            // Named and scored on the way in rather than at build time: the pause screen is built once
            // and the level changes underneath it.
            if (screen == GameScreen.Paused && _pause != null && _session != null)
            {
                _pause.SetLevelName(_session.LevelName);
                _pause.SetScore(_session.State?.Score ?? 0, _session.TargetScore);
                _pause.SetRelics(_atlasMap.BonusesFor(_atlasProgress));
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
            // A finished board stops being a board: every control that plays it is withdrawn and
            // the only one left is the button that moves on. Restarting puts the player back below
            // the target, and everything comes back.
            var isFinished = _session.IsComplete;

            if (_nextButton != null)
            {
                _nextButton.gameObject.SetActive(isFinished);
            }

            if (_playControls != null)
            {
                _playControls.SetActive(!isFinished);
            }

            if (_pauseButton != null)
            {
                _pauseButton.gameObject.SetActive(!isFinished);
            }

            if (!_session.IsComplete || _hasRecordedThisClear)
            {
                return;
            }

            // Recorded once per run, not once per state change: clearing the quota does not end the
            // board, so the state keeps changing afterwards.
            _hasRecordedThisClear = true;

            AwardEssence(state);

            if (_isEndlessRound)
            {
                // Advanced as soon as the quota is met rather than when the player taps onward, so
                // the furthest round reached survives closing the app on a finished board.
                // Whatever is still in hand comes with the player. A Pivot Token is earned by
                // building a long route, and taking it back at a round boundary made the counter
                // look broken as well as being unfair.
                _endlessRun = _endlessRun.Cleared(
                    pivotTokensLeft: state.PivotTokens.Count,
                    skipsLeft: state.SkipTokens.Count);

                _endlessStore.Save(_endlessRun);
                return;
            }

            // Banked at the same moment as the clear, so what the player still holds travels to the
            // next level rather than being lost with the board.
            _progress = _progress
                .WithCleared(_session.LevelId)
                .WithPivotTokens(state.PivotTokens.Count);

            _progressStore.Save(_progress);
        }
    }
}
