using Pathweaver.Core.State;
using Pathweaver.Game.Presentation;
using UnityEngine;

namespace Pathweaver.Game.App
{
    /// <summary>
    /// Loads a level and draws its opening position.
    /// </summary>
    /// <remarks>
    /// A stepping stone, not the real game shell. It exists so the rendering can be
    /// looked at before input, sessions, or menus exist. GameSession replaces it in
    /// #23; until then this is the only thing that turns a level file into pixels.
    /// </remarks>
    internal sealed class BoardDemo : MonoBehaviour
    {
        [SerializeField]
        private string _levelId = "biome1-01";

        [SerializeField]
        private ulong _seed = 42UL;

        [SerializeField]
        private BoardView _boardView;

        internal GameState State { get; private set; }

        private void Start()
        {
            Show();
        }

        internal void Show()
        {
            if (_boardView == null)
            {
                Debug.LogError("[BoardDemo] No board view assigned.");
                return;
            }

            State = LevelCatalogue.Load(_levelId).CreateGame(_seed);
            _boardView.Build(State);

            Debug.Log(
                $"[BoardDemo] {_levelId} drawn: {State.Board.Coordinates.Count} cells, " +
                $"held {State.HeldTile}, {State.LegalPlacements.Count} legal placements");
        }
    }
}
