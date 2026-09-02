using NUnit.Framework;
using Pathweaver.Game.Presentation;
using Pathweaver.Game.Presentation.Menus;
using Pathweaver.Game.Presentation.Text;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// The picture under each help page: where it sits, and how large it is allowed to be.
    /// </summary>
    /// <remarks>
    /// Whether a figure explains anything needs a person, and <c>TextPreview.CaptureHelp</c> renders each
    /// page for exactly that. What does not need a person is whether it collides with the paragraph above
    /// it or the controls below it — the band it lives in is bounded by two numbers that other files own,
    /// and every collision in this codebase so far has been arithmetic between two such numbers.
    /// </remarks>
    public class HelpFigureTests
    {
        /// <summary>The aspect of the phone the game is tested on: 1080 by 2376.</summary>
        private const float PhoneAspect = 1080f / 2376f;

        /// <summary>Where the help screen's two controls sit, from <c>HelpView</c>.</summary>
        private const float ControlViewportY = 0.09f;

        private const float ControlRadius = 0.4f;

        [Test]
        public void The_figure_sits_below_the_lowest_paragraph()
        {
            // The paragraph is centred on its slot and wraps to as many as four lines, so it grows
            // downward from that slot by half its own height.
            var paragraphBottom =
                HelpView.LastLineViewportY - (2f * LabelMetrics.BodyHeightFraction);

            var figureTop = HelpFigure.ViewportY + (HelpFigure.HeightFraction * 0.5f);

            Assert.That(
                figureTop,
                Is.LessThan(paragraphBottom),
                "The figure would sit on the paragraph above it.");
        }

        [Test]
        public void The_figure_clears_the_controls_below_it()
        {
            var controlTop = ControlViewportY + MenuCamera.ViewportHalfHeight(ControlRadius);
            var figureBottom = HelpFigure.ViewportY - (HelpFigure.HeightFraction * 0.5f);

            Assert.That(
                figureBottom, Is.GreaterThan(controlTop), "The figure would sit on the next button.");
        }

        [Test]
        public void The_figure_stays_within_the_width_of_the_screen()
        {
            Assert.That(HelpFigure.WidthFraction, Is.LessThan(1f));
            Assert.That(HelpFigure.HeightFraction, Is.GreaterThan(0f));
        }

        [Test]
        public void A_figure_is_scaled_down_to_fit_its_band()
        {
            // The widest figure is a spring, four conduits and a hub in a row, which is over five world
            // units across against a band a fraction of a portrait screen wide. Drawn at scale one it
            // would run off both edges.
            var wide = HexMetrics.CellSpacing * (HelpFigure.RouteConduits + 2);

            var scale = HelpFigure.ScaleFor(
                MenuCamera.OrthographicSize, PhoneAspect, wide, HexMetrics.Size * 2f);

            Assert.That(scale, Is.LessThan(1f));
            Assert.That(scale, Is.GreaterThan(0f));
        }

        [Test]
        public void Whichever_dimension_runs_out_first_decides_the_scale()
        {
            // A wide, short figure is limited by the width; a narrow, tall one by the height. Taking only
            // one of the two is how a five-cell row would have run off the screen while measuring as
            // comfortably short.
            var byWidth = HelpFigure.ScaleFor(MenuCamera.OrthographicSize, PhoneAspect, 8f, 0.1f);
            var byHeight = HelpFigure.ScaleFor(MenuCamera.OrthographicSize, PhoneAspect, 0.1f, 8f);

            var worldWidth = MenuCamera.OrthographicSize * 2f * PhoneAspect;
            var worldHeight = MenuCamera.OrthographicSize * 2f;

            Assert.That(
                byWidth,
                Is.EqualTo(worldWidth * HelpFigure.WidthFraction / 8f).Within(0.0001f));

            Assert.That(
                byHeight,
                Is.EqualTo(worldHeight * HelpFigure.HeightFraction / 8f).Within(0.0001f));
        }

        [Test]
        public void A_figure_with_no_geometry_is_not_scaled_to_nothing()
        {
            // Division by a zero extent would give an infinity and a figure that vanished or filled the
            // screen. A figure that measured nothing should simply be drawn at its own size.
            Assert.That(HelpFigure.ScaleFor(MenuCamera.OrthographicSize, PhoneAspect, 0f, 0f),
                Is.EqualTo(1f));
        }

        [Test]
        public void The_figure_is_drawn_behind_the_words_it_illustrates()
        {
            // A picture that covered a paragraph would be worse than no picture. The camera looks along
            // +Z from negative Z, so nearer the viewer is a smaller number.
            Assert.That(HelpFigure.Depth, Is.GreaterThan(HexButton.LabelDepth));
        }

        [Test]
        public void The_route_it_draws_is_long_enough_to_earn_a_token()
        {
            // The page says four conduits or more earns a Pivot Token, and the page before it draws a
            // finished route. Drawing a shorter one would illustrate the rule with a counter-example.
            Assert.That(
                HelpFigure.RouteConduits,
                Is.GreaterThanOrEqualTo(Pathweaver.Core.Rules.TokenRules.PivotThreshold));
        }

        [Test]
        public void There_is_a_figure_for_every_page()
        {
            // Built in Build rather than declared, so this is checked through a real component: a page
            // without a picture would fall back to whichever figure was showing last.
            var camera = new GameObject("camera").AddComponent<Camera>();
            var figure = new GameObject("figure").AddComponent<HelpFigure>();
            var material = new Material(Shader.Find("Sprites/Default"));

            try
            {
                // Every view in this game colours itself with renderer.material, which the Editor reports
                // as a leak outside play mode. That is a statement about edit mode, not about the game, and
                // the objects are destroyed below.
                LogAssert.ignoreFailingMessages = true;

                MenuCamera.Frame(camera);
                figure.Build(camera, material);

                Assert.That(figure.FigureCount, Is.EqualTo(HelpView.PageCount));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;

                Object.DestroyImmediate(figure.gameObject);
                Object.DestroyImmediate(camera.gameObject);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void Turning_past_the_last_page_wraps_rather_than_showing_nothing()
        {
            var camera = new GameObject("camera").AddComponent<Camera>();
            var figure = new GameObject("figure").AddComponent<HelpFigure>();
            var material = new Material(Shader.Find("Sprites/Default"));

            try
            {
                // Every view in this game colours itself with renderer.material, which the Editor reports
                // as a leak outside play mode. That is a statement about edit mode, not about the game, and
                // the objects are destroyed below.
                LogAssert.ignoreFailingMessages = true;

                MenuCamera.Frame(camera);
                figure.Build(camera, material);

                figure.ShowPage(HelpView.PageCount);

                Assert.That(figure.CurrentPage, Is.EqualTo(0));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;

                Object.DestroyImmediate(figure.gameObject);
                Object.DestroyImmediate(camera.gameObject);
                Object.DestroyImmediate(material);
            }
        }
    }
}
