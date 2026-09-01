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
    /// An icon sits at the foot of each column, matching the button beneath it, because two columns of
    /// identical shapes at opposite edges of the screen are distinguishable only by position — and a
    /// player who has not read the help screen has no way to learn which is which.
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

        private const float PipRadius = 0.11f;
        private const float PipSpacing = 0.3f;

        [SerializeField]
        private TokenKind _kind = TokenKind.Pivot;

        [SerializeField]
        private Vector2 _viewportPosition = new Vector2(0.12f, 0.26f);

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private GameSession _session;

        /// <summary>How far below the first pip the column's icon sits, in world units.</summary>
        private const float IconOffset = 0.28f;

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

            // Scaled for the same reason the menu buttons are: a pip's radius and the gap between pips
            // are world units chosen against the menu camera, so on a board zoomed out to fit a large
            // level the column shrank, and on one zoomed in it grew far enough up the screen to sit over
            // the board. Scaling the column scales the spacing with it, because the pips are children
            // placed at multiples of it.
            transform.localScale = Vector3.one * Menus.HexButton.ScaleFor(camera.orthographicSize);
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
                // Stacked upward from the anchor, so a growing count never collides with the
                // controls below it.
                pip.transform.localPosition = new Vector3(0f, index * PipSpacing, 0f);

                pip.AddComponent<MeshFilter>().sharedMesh = mesh;

                var renderer = pip.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _boardView.TileMaterial;
                renderer.material.color = BoardPalette.TokenEmpty;

                _pips.Add(renderer);
            }

            AddIcon(mesh);
        }

        /// <summary>
        /// Puts the matching control's glyph at the foot of the column.
        /// </summary>
        /// <remarks>
        /// The same shapes the buttons below use — a cell with a bar across it for the Pivot Token, a
        /// double chevron for a skip — so the column and the control that spends it read as one thing.
        /// Dimmer than a held pip, because it labels the column rather than counting anything.
        /// </remarks>
        private void AddIcon(Mesh unused)
        {
            var icon = new GameObject("Icon");
            icon.transform.SetParent(transform, worldPositionStays: false);
            icon.transform.localPosition = new Vector3(0f, -IconOffset, 0f);

            if (_kind == TokenKind.Pivot)
            {
                AddPart(icon.transform, "Cell", HexMeshFactory.CreateHexagon(0.1f), BoardPalette.PivotReady);
                AddPart(
                    icon.transform,
                    "Bar",
                    HexMeshFactory.CreateRectangle(0.14f, 0.035f),
                    BoardPalette.TextSecondary,
                    depth: -0.01f);

                return;
            }

            // Two chevrons, matching the button below rather than merely resembling it. One would read as
            // a different mark, which is the opposite of what an icon that labels a column is for.
            var chevron = GlyphMeshFactory.CreateChevron(0.075f, 0.065f, 0.028f);

            AddPart(icon.transform, "ChevronLeft", chevron, BoardPalette.SkipReady, offsetX: -0.035f);
            AddPart(icon.transform, "ChevronRight", chevron, BoardPalette.SkipReady, offsetX: 0.035f, depth: -0.01f);
        }

        private void AddPart(
            Transform parent, string childName, Mesh mesh, Color colour, float depth = 0f, float offsetX = 0f)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(parent, worldPositionStays: false);
            child.transform.localPosition = new Vector3(offsetX, 0f, depth);

            child.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _boardView.TileMaterial;
            renderer.material.color = colour;
        }
    }
}
