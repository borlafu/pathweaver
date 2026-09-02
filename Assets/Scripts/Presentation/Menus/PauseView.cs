using UnityEngine;

namespace Pathweaver.Game.Presentation.Menus
{
    /// <summary>
    /// Paused: carry on, start the level again, or go back to the list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The board stays visible around it, but not behind it. A player who pauses mid-puzzle is usually
    /// still looking at the board, so blanking the screen would make pausing feel like leaving — and
    /// leaving the board directly behind full-size controls made both hard to read. A panel settles it,
    /// the same way the restart question already does, and for the same reason: the material the board is
    /// drawn with is opaque, so there is no dimming to be had, only covering or not covering.
    /// </para>
    /// <para>
    /// It is also where the level's name goes. Every level file carries one and there was nowhere to
    /// show it; the board itself is the wrong place, because a line of text over the top rows would
    /// cover the puzzle permanently to answer a question asked once. Pausing is when a player asks
    /// which level they are on.
    /// </para>
    /// </remarks>
    internal sealed class PauseView : MonoBehaviour
    {
        internal const string ResumeId = "resume";
        internal const string RestartId = "restart";
        internal const string MenuId = "menu";
        internal const string HelpId = "help";

        /// <summary>
        /// Where the help control sits, below the row that leaves or restarts.
        /// </summary>
        /// <remarks>
        /// Reachable from here as well as from the main menu, because a player who is stuck is stuck
        /// mid-level. Making them abandon the board to read how it works is the one moment help is
        /// least affordable.
        /// </remarks>
        internal const float HelpViewportY = 0.22f;

        /// <summary>
        /// Where the level's name sits, clear of the resume button below it.
        /// </summary>
        internal const float TitleViewportY = 0.75f;

        /// <summary>Where the score sits, just under the level's name.</summary>
        internal const float ScoreViewportY = 0.685f;

        /// <summary>
        /// Where the line naming the World Atlas relics in force sits.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Here rather than on the board. A fourth skip pip appearing in the drawer read as the game being
        /// inconsistent rather than as something the player had bought, and the moment they want that
        /// explained is the moment they stop to ask how it is going — which is what pausing is. On the
        /// board it would be a permanent line of text over the puzzle to answer a question asked once.
        /// </para>
        /// <para>
        /// Above the level's name rather than under the score, which is where it went first: the block
        /// below the score is solid controls from 0.674 downward, so a line at 0.64 was drawn inside the
        /// resume button. The top of the panel was the only free space, and the panel grew to hold it.
        /// </para>
        /// </remarks>
        internal const float RelicsViewportY = 0.8f;

        /// <summary>
        /// How much of the width that line may use before wrapping.
        /// </summary>
        /// <remarks>
        /// Narrower than the screen, because the panel behind it is narrower than the screen: a line that
        /// ran to the edges would hang off the thing it is written on. A full set of relics wraps to two
        /// lines at this width, which is what the panel's height allows for.
        /// </remarks>
        internal const float RelicsWrapWidthFraction = 0.7f;

        /// <summary>
        /// Where the panel is centred, as a viewport fraction.
        /// </summary>
        /// <remarks>
        /// Between the topmost thing on the screen and the bottommost, so the panel covers the whole block
        /// rather than being centred on the screen and missing one end. The topmost is the relics line
        /// rather than the title, since the relics line arrived above it.
        /// </remarks>
        internal static float PanelViewportY => (RelicsViewportY + HelpViewportY) * 0.5f;

        /// <summary>How large the panel behind the controls is, in world units.</summary>
        /// <remarks>
        /// <para>
        /// Sized so that at the menu camera it covers about 0.10 to 0.90 of the width and 0.15 to 0.82 of
        /// the height — which is everything on this screen, from above the level's name down to below the
        /// help control, with board still visible around all four sides.
        /// </para>
        /// <para>
        /// The first attempt was 2.6 tall and cut the title and the score off above its own top edge. The
        /// numbers are in world units and the things they have to contain are in viewport fractions, which
        /// is the conversion this codebase keeps getting wrong; <c>MenuCamera.ViewportHalfHeight</c> is
        /// what makes it checkable.
        /// </para>
        /// <para>
        /// It grew from 4.22 when the relics line arrived above the title, because a panel that stops short
        /// of the topmost thing written on it is the same bug as the first attempt.
        /// </para>
        /// </remarks>
        internal const float PanelWidth = 2.33f;

        internal const float PanelHeight = 4.4f;

        private HexButton _resume;
        private HexButton _restart;
        private HexButton _menu;
        private HexButton _help;
        private Text.TextLabel _title;
        private Text.TextLabel _score;
        private Text.TextLabel _relics;
        private Text.TextLabel _helpMark;
        private Transform _panel;
        private MeshRenderer _panelRenderer;
        private Camera _panelCamera;

        internal void Build(Camera camera, Material material)
        {
            _panel = BuildPanel(camera, material);

            _title = Text.TextLabel.Create(
                transform,
                camera,
                "level-name",
                new Vector2(0.5f, TitleViewportY),
                Text.LabelMetrics.HeadingHeightFraction,
                BoardPalette.TextPrimary,
                TMPro.TextAlignmentOptions.Center,
                HexButton.LabelDepth);

            _score = Text.TextLabel.Create(
                transform,
                camera,
                "score",
                new Vector2(0.5f, ScoreViewportY),
                Text.LabelMetrics.BodyHeightFraction,
                BoardPalette.TextSecondary,
                TMPro.TextAlignmentOptions.Center,
                HexButton.LabelDepth);

            _relics = Text.TextLabel.Create(
                transform,
                camera,
                "relics",
                new Vector2(0.5f, RelicsViewportY),
                Text.LabelMetrics.CaptionHeightFraction,
                BoardPalette.AtlasEssence,
                TMPro.TextAlignmentOptions.Center,
                HexButton.LabelDepth);

            _relics.SetWrapWidth(RelicsWrapWidthFraction);

            _resume = HexButton.Create(
                transform, ResumeId, camera, material,
                new Vector2(0.5f, 0.58f), 0.6f, BoardPalette.MenuPrimary, touchRadiusFraction: 0.16f);
            MenuGlyphs.AddPlay(_resume, 0.24f);

            _restart = HexButton.Create(
                transform, RestartId, camera, material,
                new Vector2(0.33f, 0.38f), 0.45f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.13f);
            MenuGlyphs.AddRestartArrow(_restart);

            _menu = HexButton.Create(
                transform, MenuId, camera, material,
                new Vector2(0.67f, 0.38f), 0.45f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.13f);
            MenuGlyphs.AddLevelStack(_menu, pipRadius: 0.06f, spacing: 0.14f);

            _help = HexButton.Create(
                transform, HelpId, camera, material,
                new Vector2(0.5f, HelpViewportY), 0.36f, BoardPalette.MenuSecondary,
                touchRadiusFraction: 0.11f);

            _helpMark = Text.TextLabel.Create(
                transform,
                camera,
                HelpId,
                new Vector2(0.5f, HelpViewportY),
                Text.LabelMetrics.BodyHeightFraction,
                BoardPalette.MenuGlyph,
                TMPro.TextAlignmentOptions.Center,
                HexButton.LabelDepth);

            _helpMark.SetText("?");
        }

        /// <summary>
        /// The panel the controls are read against.
        /// </summary>
        /// <remarks>
        /// Behind every control and in front of the board. Sized in world units and scaled against the
        /// camera like the controls on it, so the whole screen keeps its proportions at any board zoom.
        /// </remarks>
        private Transform BuildPanel(Camera camera, Material material)
        {
            var panel = new GameObject("Panel");
            panel.transform.SetParent(transform, worldPositionStays: false);

            panel.AddComponent<MeshFilter>().sharedMesh =
                HexMeshFactory.CreateRectangle(PanelWidth, PanelHeight);

            var renderer = panel.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.material.color = BoardPalette.DialogPanel;

            _panelRenderer = renderer;
            _panelCamera = camera;

            return panel.transform;
        }

        private void Update()
        {
            if (_panel == null || _panelCamera == null)
            {
                return;
            }

            // Centred on the block of controls rather than on the screen, and behind the deepest of them.
            var world = _panelCamera.ViewportToWorldPoint(new Vector3(0.5f, PanelViewportY, 0f));

            _panel.position = new Vector3(world.x, world.y, HexButton.FaceDepth + 0.2f);
            _panel.localScale =
                Vector3.one * HexButton.ScaleFor(_panelCamera.orthographicSize);
        }

        /// <summary>
        /// Names the level being paused, and says how the run is going.
        /// </summary>
        /// <remarks>
        /// Set when the screen opens rather than at build time, because the screen is built once and
        /// the level changes underneath it.
        /// </remarks>
        internal void SetLevelName(string name)
        {
            _title?.SetText(name ?? string.Empty);
        }

        /// <summary>
        /// Repeats the score, because pausing is when a player asks how it is going.
        /// </summary>
        /// <remarks>
        /// The same wording and grouping as the number under the progress bar. Two different renderings of
        /// one figure would read as two figures.
        /// </remarks>
        /// <summary>
        /// Names the World Atlas relics in force, or writes nothing when there are none.
        /// </summary>
        /// <remarks>
        /// Nothing rather than "no relics": a player who has bought none has probably not met the atlas,
        /// and a line about a screen they have never opened explains less than an empty space does.
        /// </remarks>
        internal void SetRelics(Pathweaver.Core.Atlas.AtlasBonuses bonuses)
        {
            _relics?.SetText(Text.AtlasWords.Relics(bonuses));
        }

        internal void SetScore(long score, long target)
        {
            _score?.SetText(
                $"{score.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)} / "
                + target.ToString("N0", System.Globalization.CultureInfo.InvariantCulture));

            _score?.SetColour(
                score >= target ? BoardPalette.ProgressComplete : BoardPalette.TextSecondary);
        }

        internal string ButtonAt(Vector2 screenPosition)
        {
            if (_resume != null && _resume.IsPressed(screenPosition))
            {
                return ResumeId;
            }

            if (_restart != null && _restart.IsPressed(screenPosition))
            {
                return RestartId;
            }

            if (_menu != null && _menu.IsPressed(screenPosition))
            {
                return MenuId;
            }

            return _help != null && _help.IsPressed(screenPosition) ? HelpId : null;
        }
    }
}
