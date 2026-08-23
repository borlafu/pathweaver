using System;
using System.Collections.Generic;
using System.Linq;
using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Levels;
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
        private BoardView _boardView;

        [SerializeField]
        private HeldTileView _heldTileView;

        [SerializeField]
        private BoardCameraFitter _cameraFitter;

        private SaveService _saves;
        private LevelDefinition _level;

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

        /// <summary>
        /// Raised when the player turns the held tile.
        /// </summary>
        /// <remarks>
        /// The rotation hint listens for this to stop advertising itself once the player has
        /// clearly found the gesture.
        /// </remarks>
        internal event Action HeldRotated;

        /// <summary>Whether the current run was restored from a save.</summary>
        internal bool WasResumed { get; private set; }

        /// <summary>The level being played.</summary>
        internal string LevelId => _levelId;

        /// <summary>The score that clears this level.</summary>
        internal long TargetScore => _level?.TargetScore ?? 0;

        /// <summary>Whether the level's quota has been met.</summary>
        internal bool IsComplete => State != null && _level != null && _level.IsClearedBy(State.Score);

        /// <summary>
        /// The conduits on the routes that most recently paid out.
        /// </summary>
        /// <remarks>
        /// Kept so the board can show which path just harvested. Without it a completed route
        /// is indistinguishable from a placement that did nothing, which is exactly how the
        /// first device build felt.
        /// </remarks>
        internal IReadOnlyList<HexCoord> LastHarvestedTiles { get; private set; } =
            Array.Empty<HexCoord>();

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
            Begin(_levelId, saves);
        }

        /// <summary>
        /// Starts or resumes a specific level.
        /// </summary>
        /// <remarks>
        /// Which level to play is a decision for the screen the player came from, not a value baked
        /// into the scene. The serialised field remains only as the default for a build that starts
        /// straight into a board.
        /// </remarks>
        internal void Begin(string levelId, SaveService saves)
        {
            Begin(LevelCatalogue.Load(levelId), saves);
        }

        /// <summary>
        /// Starts or resumes a level that is already loaded.
        /// </summary>
        /// <remarks>
        /// The overload Endless Wayfare needs: a generated round has no file to load, and asking the
        /// catalogue for one by identifier would fail. Everything else about playing it is the same,
        /// including the save, which is keyed by the level's own identifier.
        /// </remarks>
        internal void Begin(LevelDefinition level, SaveService saves)
        {
            _level = level ?? throw new ArgumentNullException(nameof(level));
            _levelId = level.Id;
            _saves = saves;

            var resumed = saves?.Load(_levelId);

            // A finished board is not a run. Restoring one would open the level in the state where
            // every control is withdrawn and the only button moves on, so a level already cleared
            // could never be replayed. Saves written by an older build can still hold one, which is
            // why this checks the position rather than trusting that none were written.
            if (resumed != null && _level.IsClearedBy(resumed.Score))
            {
                saves?.Delete(_levelId);
                resumed = null;
            }

            State = resumed ?? _level.CreateGame();
            WasResumed = resumed != null;
            HeldRotation = 0;

            // A token armed on the previous board must not still be armed on this one, or the
            // player's first tap would spend it on a conduit they have not seen yet.
            IsPivotArmed = false;

            _boardView.Build(State);

            // Framing depends on the board's extents, so it happens once the board exists
            // rather than being guessed at design time.
            _cameraFitter?.Fit(State);

            Publish();
        }

        /// <summary>
        /// Abandons the current run and deals a fresh board.
        /// </summary>
        /// <remarks>
        /// The save is deleted rather than left behind, or the next launch would resume the
        /// board the player just walked away from.
        /// </remarks>
        internal void Restart()
        {
            _saves?.Delete(_levelId);

            // Restarts deal from the level already in hand rather than reloading it by identifier.
            // A generated round is not in the catalogue, so looking it up again would fail on
            // exactly the mode where restarting matters most.
            State = _level.CreateGame();
            WasResumed = false;
            HeldRotation = 0;
            IsPivotArmed = false;
            LastHarvestedTiles = Array.Empty<HexCoord>();

            _boardView.Build(State);
            _cameraFitter?.Fit(State);

            Publish();
            SaveNow();
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

            // A finished board is not a run to resume, and its save is deleted rather than written.
            // Without this, opening a level that had already been cleared restored the finished
            // position — where every control is withdrawn and the only button moves on — so a
            // cleared level could never be played again.
            if (IsComplete)
            {
                _saves.Delete(_levelId);
                return;
            }

            _saves.Save(_levelId, State);
        }

        /// <summary>
        /// Discards the tile in hand for the next one, if a skip is available.
        /// </summary>
        /// <returns>Whether the skip happened.</returns>
        internal bool TrySkipHeld()
        {
            if (State == null || !State.SkipTokens.CanSpend)
            {
                return false;
            }

            State = GameEngine.Apply(State, new SkipTile());

            // The new tile arrives unturned, for the same reason a placed one resets it: the
            // player has not looked at this tile yet.
            HeldRotation = 0;

            Publish();
            SaveNow();

            return true;
        }

        /// <summary>
        /// Turns the held tile one step clockwise.
        /// </summary>
        internal void RotateHeld()
        {
            HeldRotation = (HeldRotation + 1) % 6;
            Publish();
            HeldRotated?.Invoke();
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
        /// <summary>
        /// Whether the player has armed a Pivot Token, so the next tap on a conduit spends it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A mode rather than a bare gesture. Tapping a placed conduit was the obvious way to spend
        /// a token, and it is also the obvious way to spend one by accident: the board is the one
        /// thing a player taps constantly, and a token is the scarcest thing they hold.
        /// </para>
        /// <para>
        /// Arming is what makes the mechanic findable at all. Tokens were being earned, counted and
        /// displayed with no way to use them, which is the state the first ten levels shipped in.
        /// </para>
        /// </remarks>
        internal bool IsPivotArmed { get; private set; }

        /// <summary>Raised when the Pivot Token mode is armed or disarmed.</summary>
        internal event Action<bool> PivotArmedChanged;

        /// <summary>Conduits a pivot could act on, which is every conduit while armed.</summary>
        internal ISet<HexCoord> PivotableCells
        {
            get
            {
                var cells = new HashSet<HexCoord>();

                if (!IsPivotArmed || State == null)
                {
                    return cells;
                }

                foreach (var occupied in State.Board.OccupiedCells)
                {
                    cells.Add(occupied.Coordinate);
                }

                return cells;
            }
        }

        /// <summary>
        /// Arms or disarms the Pivot Token mode.
        /// </summary>
        /// <returns>Whether the mode is armed afterwards.</returns>
        internal bool TogglePivotArmed()
        {
            // Nothing to arm without a token to spend or a conduit to spend it on, and an armed
            // mode that can do nothing is a control that appears broken.
            var canPivot = State != null && State.PivotTokens.CanSpend && State.Board.OccupiedCount > 0;

            SetPivotArmed(!IsPivotArmed && canPivot);
            return IsPivotArmed;
        }

        internal void DisarmPivot() => SetPivotArmed(false);

        /// <summary>
        /// Spends a Pivot Token to take a placed conduit off the board.
        /// </summary>
        /// <remarks>
        /// The only thing a Pivot Token does. Turning a placed conduit was the other half of the
        /// mechanic and has been dropped: a conduit was placed connected to something, so turning it
        /// usually just disconnects it, and a player who wants a different shape in that cell wants
        /// the cell back rather than the tile turned.
        /// <para>
        /// The conduit is discarded rather than returned to hand: the token buys back the space, not
        /// the tile.
        /// </para>
        /// </remarks>
        /// <returns>Whether the conduit was removed.</returns>
        internal bool TryPivotRetrieve(HexCoord coordinate)
        {
            if (!CanPivotAt(coordinate))
            {
                return false;
            }

            State = GameEngine.Apply(State, new PivotRetrieve(coordinate));

            SetPivotArmed(false);
            Publish();
            SaveNow();

            return true;
        }

        private bool CanPivotAt(HexCoord coordinate)
            => IsPivotArmed
               && State != null
               && State.PivotTokens.CanSpend
               && State.Board.TryGet(coordinate, out _);

        private void SetPivotArmed(bool armed)
        {
            if (IsPivotArmed == armed)
            {
                return;
            }

            IsPivotArmed = armed;
            PivotArmedChanged?.Invoke(armed);

            // Republished so the board can light the conduits, or stop lighting them.
            if (State != null)
            {
                Publish();
            }
        }

        internal bool TryPlaceAt(HexCoord coordinate)
        {
            if (!CanPlaceAt(coordinate))
            {
                return false;
            }

            var paidBefore = PaidPairs(State);

            State = GameEngine.Apply(State, new PlaceTile(coordinate, HeldRotation));

            // A fresh tile arrives unturned: carrying the previous rotation over would
            // silently orient a tile the player has not looked at yet.
            HeldRotation = 0;

            Publish();

            var harvested = State.CompletedRoutes.Count - paidBefore.Count;
            if (harvested > 0)
            {
                LastHarvestedTiles = TilesOfNewRoutes(paidBefore);
                RoutesHarvested?.Invoke(harvested);
            }

            // Saved per move rather than on a timer. A save is a few hundred bytes, and
            // the alternative is choosing which moves a player is allowed to lose.
            SaveNow();

            return true;
        }

        /// <summary>
        /// The conduits belonging to routes that were not already paid out.
        /// </summary>
        /// <remarks>
        /// Recomputed from the board rather than reported by the engine, because the engine's
        /// job is to score routes, not to describe them for display. Boards hold tens of
        /// cells, so asking again costs nothing worth avoiding.
        /// </remarks>
        private IReadOnlyList<HexCoord> TilesOfNewRoutes(HashSet<(HexCoord Spring, HexCoord Hub)> paidBefore)
        {
            var tiles = new List<HexCoord>();

            foreach (var route in FlowResolver.FindCompletedRoutes(State.Board, State.Endpoints))
            {
                if (paidBefore.Contains((route.Spring.Coordinate, route.Hub.Coordinate)))
                {
                    continue;
                }

                tiles.AddRange(route.Tiles);
            }

            return tiles;
        }

        /// <summary>
        /// The spring and hub pairs already harvested, as plain coordinates.
        /// </summary>
        /// <remarks>
        /// Compared as coordinate pairs rather than as CompletedRoute values, whose constructor
        /// is internal to the simulation. Widening that just so the presentation layer can build
        /// one would loosen the simulation's surface for a display concern.
        /// </remarks>
        private static HashSet<(HexCoord Spring, HexCoord Hub)> PaidPairs(GameState state)
        {
            var pairs = new HashSet<(HexCoord Spring, HexCoord Hub)>();

            foreach (var route in state.CompletedRoutes)
            {
                pairs.Add((route.Spring, route.Hub));
            }

            return pairs;
        }

        private void Publish()
        {
            AvailableCells = State.LegalPlacements
                .Where(placement => placement.Rotation == HeldRotation)
                .Select(placement => placement.Coordinate)
                .ToHashSet();

            // While a token is armed the board shows what the token can act on instead of where the
            // held tile could go. Showing both at once made the board a field of highlights that
            // said nothing about which tap did what.
            _boardView.Refresh(
                State,
                IsPivotArmed ? PivotableCells : AvailableCells,
                pivotArmed: IsPivotArmed);

            if (_heldTileView != null)
            {
                _heldTileView.Show(PendingTile);
            }

            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Deliberately does not start a level. The flow decides when a board appears, so the game
        /// can open on a menu rather than dropping the player straight into a puzzle.
        /// </summary>
        private void Start()
        {
            if (_boardView == null)
            {
                Debug.LogError("[GameSession] No board view assigned.");
            }
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
