using System.Collections.Generic;
using Pathweaver.Core.Rules;
using Pathweaver.Core.State;
using Pathweaver.Game.App;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Which resource a pip row counts.
    /// </summary>
    internal enum TokenKind
    {
        Pivot,
        Skip,
    }

    /// <summary>
    /// Shows how many of a token the player holds.
    /// </summary>
    /// <remarks>
    /// Counted as pips rather than written as a number: at these quantities a count of shapes reads
    /// faster than a digit, and the shapes survive a glance the digit would need a pause for.
    /// <para>
    /// Each block sits directly above the control that spends it, which is what says which currency it
    /// counts: an icon of its own was tried and was both redundant beside that control's glyph and
    /// overlapping it.
    /// </para>
    /// <para>
    /// Without this, tokens are earned and spent invisibly — and a resource the player cannot
    /// see is a resource they will not use, which would quietly undo the whole anti-deadlock
    /// mechanism of PRD section 3.2B.
    /// </para>
    /// </remarks>
    internal sealed class TokenPipsView : MonoBehaviour
    {
        /// <summary>
        /// Pips built, which is as many as any progression can raise a ceiling to.
        /// </summary>
        /// <remarks>
        /// Built once and shown or hidden per state, because the ceiling changes when a relic is
        /// unlocked and rebuilding a mesh on a state change is work for nothing.
        /// </remarks>
        private const int MaximumPips = TokenRules.MaximumCapacity;

        /// <summary>
        /// How large one pip is, in world units.
        /// </summary>
        /// <remarks>
        /// Readable from outside because the help screen draws a row of these next to the control they
        /// count for, and a diagram in a second size would stop looking like the thing it explains.
        /// </remarks>
        internal const float PipRadius = 0.11f;

        /// <summary>Centre-to-centre spacing along a row, in world units.</summary>
        /// <remarks>
        /// A pointy-top pip is <c>radius * sqrt(3)</c> wide — about 0.19 — so this leaves a clear gap
        /// between neighbours while keeping a row of three roughly as wide as the button it is centred on.
        /// A row much wider than its button would stop reading as belonging to it.
        /// </remarks>
        private const float RowSpacing = 0.25f;

        /// <summary>Centre-to-centre spacing between the two rows, in world units.</summary>
        private const float StackSpacing = 0.26f;

        /// <summary>How many pips a row holds.</summary>
        /// <remarks>
        /// Three, so the base allowance of three is one row and the five a full set of relics allows is
        /// two. A single column of five reached far enough up the screen to sit over the board.
        /// </remarks>
        internal const int PipsPerRow = 3;

        /// <summary>
        /// How far above the button the first row sits, in world units.
        /// </summary>
        /// <remarks>
        /// Clear of the button's own top edge — a control of radius 0.34 reaches that far above its
        /// centre — plus a pip's radius and a little air.
        /// </remarks>
        private const float FirstRowOffset = 0.51f;

        [SerializeField]
        private TokenKind _kind = TokenKind.Pivot;

        /// <summary>
        /// Where the column's button sits, which is what the pips are placed relative to.
        /// </summary>
        /// <remarks>
        /// The pips used to have their own anchor further up the screen, outside the tray, where they
        /// obstructed the board on a wide level. They now sit inside the drawer directly above the control
        /// that spends them, which is also where a player looks when deciding whether they can afford to.
        /// </remarks>
        [SerializeField]
        private Vector2 _viewportPosition = new Vector2(0.12f, 0.10f);

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private GameSession _session;

        private readonly List<MeshRenderer> _pips = new List<MeshRenderer>();

        private Camera ResolvedCamera => _camera != null ? _camera : Camera.main;

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

        private void Start()
        {
            Build();

            if (_session?.State != null)
            {
                OnStateChanged(_session.State);
            }
        }

        private void Update()
        {
            var camera = ResolvedCamera;

            var world = camera.ViewportToWorldPoint(
                new Vector3(_viewportPosition.x, _viewportPosition.y, 0f));
            transform.position = new Vector3(world.x, world.y, -0.4f);

            // Deliberately not scaled against the camera. The three board controls are world-sized —
            // their radii were chosen against a board's zoom, not a menu's — and a block that sits above
            // one of them has to move and grow with it. Normalising only the pips made a row placed above
            // a button drift away from it at every zoom but one.
        }

        private void OnStateChanged(GameState state)
        {
            var pool = state == null
                ? TokenPool.Empty
                : _kind == TokenKind.Pivot ? state.PivotTokens : state.SkipTokens;

            var armed = _kind == TokenKind.Pivot && _session != null && _session.IsPivotArmed;

            for (var index = 0; index < _pips.Count; index++)
            {
                // Empty slots stay visible but dim, so the player can see there is something
                // to earn rather than only noticing tokens once they have one.
                var filled = index < pool.Count;

                _pips[index].material.color = filled
                    ? (_kind == TokenKind.Pivot
                        ? (armed ? BoardPalette.TokenArmed : BoardPalette.TokenHeld)
                        : BoardPalette.SkipHeld)
                    : BoardPalette.TokenEmpty;

                // The column is exactly the ceiling, so the count on screen and the count the rules
                // allow are the same number. It used to grow with the hoard instead, which is how six
                // pips came to sit under a game that claimed a maximum of three.
                _pips[index].gameObject.SetActive(index < pool.Capacity);
            }
        }

        private void Build()
        {
            if (_boardView == null)
            {
                return;
            }

            var mesh = HexMeshFactory.CreateHexagon(PipRadius);

            for (var index = 0; index < MaximumPips; index++)
            {
                var pip = new GameObject($"Pip{index}");
                pip.transform.SetParent(transform, worldPositionStays: false);
                pip.transform.localPosition = PipPosition(index);

                pip.AddComponent<MeshFilter>().sharedMesh = mesh;

                var renderer = pip.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _boardView.TileMaterial;
                renderer.material.color = BoardPalette.TokenEmpty;

                _pips.Add(renderer);
            }

        }

        /// <summary>
        /// Where the given pip sits, relative to its button.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Rows of three, stacked upward, each centred on the button. The first version grew each row
        /// inward from the button's centre so it could not run off the screen edge, which left the block
        /// visibly off to one side of the control it belongs to — and made the two columns mirror images
        /// rather than the same thing twice.
        /// </para>
        /// <para>
        /// A centred row of three is about as wide as the button itself, so wherever the button fits the
        /// row does. Each row is centred on its own capacity, so the second row of a full pool of five
        /// sits centred as a pair rather than hanging off the left.
        /// </para>
        /// </remarks>
        internal static Vector3 PipPosition(int index)
        {
            var row = index / PipsPerRow;
            var column = index % PipsPerRow;
            var inRow = Mathf.Min(PipsPerRow, MaximumPips - (row * PipsPerRow));

            return new Vector3(
                (column - ((inRow - 1) * 0.5f)) * RowSpacing,
                FirstRowOffset + (row * StackSpacing),
                0f);
        }

    }
}
