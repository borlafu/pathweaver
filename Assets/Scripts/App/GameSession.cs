using System;
using System.Collections.Generic;
using System.Linq;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Rules;
using Pathweaver.Core.State;
using Pathweaver.Core.Tiles;
using Pathweaver.Game.Presentation;
using UnityEngine;

namespace Pathweaver.Game.App
{
    /// <summary>
    /// Owns the game state and is the only thing that advances it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every rule lives in the simulation; this class translates intent into a command
    /// and hands the new state to the views. Input asks the session to do something,
    /// the session asks the simulation whether it is allowed, and the answer is
    /// authoritative.
    /// </para>
    /// <para>
    /// The rotation a player has dialled up before committing lives here rather than in
    /// the simulation, because an uncommitted turn is not part of the game — it is a
    /// pending intent, and putting it in the state would mean saving and replaying it.
    /// </para>
    /// </remarks>
    internal sealed class GameSession : MonoBehaviour
    {
        [SerializeField]
        private string _levelId = "biome1-01";

        [SerializeField]
        private ulong _seed = 42UL;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private HeldTileView _heldTileView;

        [SerializeField]
        private BoardCameraFitter _cameraFitter;

        private SaveService _saves;

        /// <summary>Raised whenever the state changes, including at the start.</summary>
        internal event Action<GameState> StateChanged;

        /// <summary>
        /// Raised when placing a tile completes routes, with how many.
        /// </summary>
        /// <remarks>
        /// A separate signal because feedback needs the moment, not the total. Watching
        /// the score would mean every listener re-deriving "did something just pay out",
        /// and each would get it subtly differently.
        /// </remarks>
        internal event Action<int> RoutesHarvested;

        /// <summary>Raised after a tile is placed, whatever it did or did not complete.</summary>
        internal event Action TilePlaced;

        /// <summary>Whether the current run was restored from a save.</summary>
        internal bool WasResumed { get; private set; }

        internal GameState State { get; private set; }

        /// <summary>Clockwise turns applied to the held tile before placing it.</summary>
        internal int HeldRotation { get; private set; }

        /// <summary>The held tile as it would land on the board right now.</summary>
        internal ConduitTile PendingTile => State.HeldTile.RotateClockwise(HeldRotation);

        /// <summary>Cells the held tile could occupy at its current rotation.</summary>
        internal ISet<HexCoord> AvailableCells { get; private set; } = new HashSet<HexCoord>();

        internal void Begin()
        {
            Begin(SaveService.ForPlayer());
        }

        /// <summary>
        /// Starts or resumes a run, using the given store.
        /// </summary>
        /// <remarks>
        /// Resuming is the default rather than an option: PRD section 2.1 describes three
        /// to six minute transit sessions, so being dropped back exactly where the player
        /// left off is the normal case, not a recovery path.
        /// </remarks>
        internal void Begin(SaveService saves)
        {
            _saves = saves;

            var level = LevelCatalogue.Load(_levelId);
            var resumed = saves?.Load(_levelId);

            State = resumed ?? level.CreateGame(_seed);
            WasResumed = resumed != null;
            HeldRotation = 0;

            _boardView.Build(State);

            // Framing depends on the board's extents, so it happens once the board exists
            // rather than being guessed at design time.
            _cameraFitter?.Fit(State);

            Publish();
        }

        /// <summary>
        /// Writes the run out, if there is one.
        /// </summary>
        internal void SaveNow()
        {
            if (State == null || _saves == null)
            {
                return;
            }

            _saves.Save(_levelId, State);
        }

        /// <summary>
        /// Turns the held tile one step clockwise.
        /// </summary>
        internal void RotateHeld()
        {
            HeldRotation = (HeldRotation + 1) % 6;
            Publish();
        }

        /// <summary>
        /// Whether the held tile may be placed on a cell as currently turned.
        /// </summary>
        internal bool CanPlaceAt(HexCoord coordinate)
            => State != null
               && State.Board.Contains(coordinate)
               && AvailableCells.Contains(coordinate);

        /// <summary>
        /// Places the held tile, if the rules allow it.
        /// </summary>
        /// <returns>Whether the move happened.</returns>
        internal bool TryPlaceAt(HexCoord coordinate)
        {
            if (!CanPlaceAt(coordinate))
            {
                return false;
            }

            var harvestedBefore = State.CompletedRoutes.Count;

            State = GameEngine.Apply(State, new PlaceTile(coordinate, HeldRotation));

            // A fresh tile arrives unturned: carrying the previous rotation over would
            // silently orient a tile the player has not looked at yet.
            HeldRotation = 0;

            Publish();

            TilePlaced?.Invoke();

            var harvested = State.CompletedRoutes.Count - harvestedBefore;
            if (harvested > 0)
            {
                RoutesHarvested?.Invoke(harvested);
            }

            // Saved per move rather than on a timer. A save is a few hundred bytes, and
            // the alternative is choosing which moves a player is allowed to lose.
            SaveNow();

            return true;
        }

        private void Publish()
        {
            AvailableCells = State.LegalPlacements
                .Where(placement => placement.Rotation == HeldRotation)
                .Select(placement => placement.Coordinate)
                .ToHashSet();

            _boardView.Refresh(State, AvailableCells);

            if (_heldTileView != null)
            {
                _heldTileView.Show(PendingTile);
            }

            StateChanged?.Invoke(State);
        }

        private void Start()
        {
            if (_boardView == null)
            {
                Debug.LogError("[GameSession] No board view assigned.");
                return;
            }

            Begin();
        }

        /// <summary>
        /// Android can kill a backgrounded process without warning, and OnApplicationQuit
        /// is not guaranteed to arrive. Pausing is the last reliable moment to write.
        /// </summary>
        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                SaveNow();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SaveNow();
            }
        }

        private void OnApplicationQuit()
        {
            SaveNow();
        }
    }
}
