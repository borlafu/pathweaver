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

        internal GameState State { get; private set; }

        /// <summary>Clockwise turns applied to the held tile before placing it.</summary>
        internal int HeldRotation { get; private set; }

        /// <summary>The held tile as it would land on the board right now.</summary>
        internal ConduitTile PendingTile => State.HeldTile.RotateClockwise(HeldRotation);

        /// <summary>Cells the held tile could occupy at its current rotation.</summary>
        internal ISet<HexCoord> AvailableCells { get; private set; } = new HashSet<HexCoord>();

        internal void Begin()
        {
            State = LevelCatalogue.Load(_levelId).CreateGame(_seed);
            HeldRotation = 0;

            _boardView.Build(State);
            Publish();
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
    }
}
