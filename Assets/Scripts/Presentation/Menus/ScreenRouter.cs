using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathweaver.Game.Presentation.Menus
{
    /// <summary>Which part of the game is on screen.</summary>
    internal enum GameScreen
    {
        MainMenu,
        LevelSelect,
        Playing,
        Paused,
        Settings,
    }

    /// <summary>
    /// Shows one screen at a time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One Unity scene, with screens as objects switched on and off, rather than a scene each.
    /// Loading a scene costs time the 1.5 second cold-boot budget cannot spare, and the board has
    /// to survive being paused anyway — so it must stay loaded, which makes separate scenes a cost
    /// with no benefit.
    /// </para>
    /// <para>
    /// The board is visible while paused on purpose: a player pausing mid-puzzle is usually
    /// looking at it, not trying to leave.
    /// </para>
    /// </remarks>
    internal sealed class ScreenRouter : MonoBehaviour
    {
        /// <remarks>
        /// Serialised fields rather than a dictionary populated at edit time. Unity does not
        /// serialise dictionaries, so a map built while assembling the scene is empty by the time
        /// the game runs — which showed up as every screen drawing at once, since hiding the others
        /// was a loop over nothing.
        /// </remarks>
        [SerializeField]
        private GameObject _mainMenu;

        [SerializeField]
        private GameObject _levelSelect;

        [SerializeField]
        private GameObject _paused;

        [SerializeField]
        private GameObject _settings;

        private readonly Dictionary<GameScreen, GameObject> _screens = new Dictionary<GameScreen, GameObject>();

        /// <summary>Raised after the screen changes.</summary>
        internal event Action<GameScreen> ScreenChanged;

        internal GameScreen Current { get; private set; } = GameScreen.MainMenu;

        /// <summary>Whether the board should accept play input.</summary>
        internal bool IsPlaying => Current == GameScreen.Playing;

        private void Awake()
        {
            _screens[GameScreen.MainMenu] = _mainMenu;
            _screens[GameScreen.LevelSelect] = _levelSelect;
            _screens[GameScreen.Paused] = _paused;
            _screens[GameScreen.Settings] = _settings;

            // Everything off until something is shown, so a screen left enabled in the scene cannot
            // appear over whatever the game opens on.
            foreach (var root in _screens.Values)
            {
                if (root != null)
                {
                    root.SetActive(false);
                }
            }
        }

        internal void Show(GameScreen screen)
        {
            Current = screen;

            foreach (var pair in _screens)
            {
                if (pair.Value != null)
                {
                    pair.Value.SetActive(pair.Key == screen);
                }
            }

            ScreenChanged?.Invoke(screen);
        }
    }
}
