using System;
using Pathweaver.Core.Tiles;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// The artwork and colours the board is drawn with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An asset rather than code, so artwork can be dropped in without touching the game.
    /// Every sprite field is optional: where one is absent the board falls back to the
    /// generated geometry it uses today, so a half-finished set of art is playable and an
    /// empty theme looks exactly like the placeholder build.
    /// </para>
    /// <para>
    /// That matters more than it sounds. Art arrives piecemeal, and the alternative — a
    /// switch from "all procedural" to "all sprites" — means nothing can be seen in the game
    /// until every last piece exists.
    /// </para>
    /// <para>
    /// Sizes and formats are specified in docs/art/tile-design-guide.md.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Pathweaver/Board Theme", fileName = "BoardTheme")]
    internal sealed class BoardTheme : ScriptableObject
    {
        /// <summary>
        /// Artwork and colour for one resource.
        /// </summary>
        [Serializable]
        internal sealed class ResourceArt
        {
            [SerializeField]
            internal ResourceKind kind;

            [SerializeField]
            [Tooltip("The conduit painted across a tile. Leave empty to use generated spokes.")]
            internal Sprite arm;

            [SerializeField]
            [Tooltip("The shape identifying this resource. Leave empty to use the generated motif.")]
            internal Sprite motif;

            [SerializeField]
            [Tooltip("Overrides the placeholder colour when set to anything but clear.")]
            internal Color colour = Color.clear;
        }

        [Header("Cells")]
        [SerializeField]
        [Tooltip("The hexagon behind everything. Leave empty to use the generated hexagon.")]
        private Sprite _cellBackground;

        [SerializeField]
        [Tooltip("Drawn on a cell the held tile may legally occupy.")]
        private Sprite _legalCellOverlay;

        [Header("Endpoints")]
        [SerializeField]
        private Sprite _spring;

        [SerializeField]
        private Sprite _hub;

        [Header("Resources")]
        [SerializeField]
        private ResourceArt[] _resources = Array.Empty<ResourceArt>();

        internal Sprite CellBackground => _cellBackground;

        internal Sprite LegalCellOverlay => _legalCellOverlay;

        internal Sprite Spring => _spring;

        internal Sprite Hub => _hub;

        /// <summary>
        /// The artwork for a resource, or null when none has been supplied.
        /// </summary>
        internal ResourceArt For(ResourceKind kind)
        {
            foreach (var art in _resources)
            {
                if (art != null && art.kind == kind)
                {
                    return art;
                }
            }

            return null;
        }

        /// <summary>
        /// The colour for a resource: the theme's if it sets one, otherwise the placeholder.
        /// </summary>
        /// <remarks>
        /// A cleared colour counts as unset. That keeps "I have not decided yet" distinct from
        /// "I have chosen black", which a default-constructed Color cannot express.
        /// </remarks>
        internal Color ColourFor(ResourceKind kind)
        {
            var art = For(kind);

            return art != null && art.colour != Color.clear
                ? art.colour
                : BoardPalette.ForKind(kind);
        }

        internal Sprite MotifFor(ResourceKind kind) => For(kind)?.motif;

        internal Sprite ArmFor(ResourceKind kind) => For(kind)?.arm;
    }
}
