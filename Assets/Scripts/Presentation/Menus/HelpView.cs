using System.Collections.Generic;
using System.Globalization;
using Pathweaver.Game.Presentation.Text;
using TMPro;
using UnityEngine;

namespace Pathweaver.Game.Presentation.Menus
{
    /// <summary>
    /// What the game expects of the player, in words, a page at a time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Until there was a font, every rule in this game had to be discovered by trial: that a tile may
    /// only join something of its own kind, that the pip column counts two different things, that the
    /// button beneath it arms a removal rather than performing one, that a spring radiates and a hub
    /// converges. Testers reported none of these as confusing, which is worse than reporting them —
    /// they simply played worse.
    /// </para>
    /// <para>
    /// Paged rather than scrolled. A scroll is a drag, and every other gesture on a menu screen here
    /// is a tap; mixing them is how a mis-swipe becomes a level launch, which is the same reason
    /// <c>LevelSelectView</c> grows its grid instead of scrolling it.
    /// </para>
    /// <para>
    /// Two controls, so neither is ambiguous: one advances and wraps, one leaves. A single button that
    /// meant "back a page, or out if you are on the first" would mean two things.
    /// </para>
    /// </remarks>
    internal sealed class HelpView : MonoBehaviour
    {
        internal const string NextId = "next";
        internal const string BackId = "back";

        internal const float HeadingViewportY = 0.82f;

        /// <summary>Where the first body line sits.</summary>
        internal const float FirstLineViewportY = 0.70f;

        /// <summary>
        /// The gap between body lines, in viewport fractions.
        /// </summary>
        /// <remarks>
        /// Generous, because each line wraps to two or three of its own. A tighter gap would let a
        /// three-line paragraph run into the paragraph beneath it, and the wrap depends on the
        /// player's screen width rather than on anything checkable here.
        /// </remarks>
        internal const float LineSpacing = 0.14f;

        /// <summary>How much of the screen width a body line may use before wrapping.</summary>
        internal const float WrapWidthFraction = 0.84f;

        /// <summary>
        /// How much of the screen width a heading may use before wrapping.
        /// </summary>
        /// <remarks>
        /// The headings had no wrap box at all, and "Longer routes pay more  3/4" ran off both edges of a
        /// portrait phone with its first and last characters cut in half. Heading text is about one and a
        /// half times body size, so it fits far fewer characters to a line than the paragraph beneath it —
        /// which is not obvious from either number, and is why this is a box rather than a shorter title.
        /// Wrapping to two lines still clears the first paragraph slot.
        /// </remarks>
        internal const float HeadingWrapWidthFraction = 0.9f;

        /// <summary>
        /// The longest a heading may be, in characters, counting the page number appended to it.
        /// </summary>
        /// <remarks>
        /// Heading text is about one and a half times body size, so where a paragraph fits roughly 34
        /// characters to a line a heading fits about 20 — and two lines is all the room there is above the
        /// first paragraph. The <c>1/4</c> counts, which is five characters nothing in the heading text
        /// itself accounts for.
        /// </remarks>
        internal const int LongestHeading = 40;

        /// <summary>
        /// The longest a paragraph may be, in characters.
        /// </summary>
        /// <remarks>
        /// Body text at the wrap width fits roughly 34 characters to a line on a 1080-wide phone, and
        /// four wrapped lines is 0.11 of screen height against a <see cref="LineSpacing"/> of 0.14. So
        /// this is the point at which a paragraph starts touching the one below it. A test holds the
        /// pages to it, because the wrap depends on the player's screen and cannot be seen from here —
        /// the first draft of the last page ran to five lines and climbed over its own heading.
        /// </remarks>
        internal const int LongestParagraph = 136;

        /// <summary>
        /// How many paragraph slots the page with the most paragraphs uses.
        /// </summary>
        /// <remarks>
        /// The layout below the text depends on it: <see cref="HelpFigure"/> sits in the band between the
        /// last slot and the two controls at the bottom, and that band closes as paragraphs are added.
        /// </remarks>
        internal static int LongestPageLines
        {
            get
            {
                var longest = 0;
                foreach (var page in Pages)
                {
                    longest = Mathf.Max(longest, page.Lines.Length);
                }

                return longest;
            }
        }

        /// <summary>Where the lowest paragraph slot in use sits.</summary>
        internal static float LastLineViewportY
            => FirstLineViewportY - ((LongestPageLines - 1) * LineSpacing);

        /// <summary>
        /// The pages, in the order a player meets what they describe.
        /// </summary>
        /// <remarks>
        /// Springs and hubs first because they are what a board is made of; tokens last because they
        /// are what rescues a board that has gone wrong, which is not the first thing to learn.
        /// </remarks>
        private static readonly Page[] Pages =
        {
            new Page(
                "Springs and hubs",
                new[]
                {
                    "Every resource starts at a spring and has to reach a hub of the same kind.",
                    "A spring's ring grows outward from its centre. A hub's collapses inward. "
                    + "That is how to tell them apart without relying on colour.",
                }),
            new Page(
                "Laying a conduit",
                new[]
                {
                    "Drag a tile from the tray onto the board. Tap it in the tray to turn it first.",
                    "A tile can only join a conduit or an endpoint of its own kind, and only where "
                    + "their edges face each other.",
                }),
            new Page(
                "Longer routes pay more",
                new[]
                {
                    "When a route joins a spring to a hub it pays out, and every extra conduit in it "
                    + "multiplies what it pays.",
                    "A long route also fills the board. If no tile you hold can be placed anywhere, "
                    + "the run is over.",
                }),
            new Page(
                "Tokens and skips",
                new[]
                {
                    "The left column counts Pivot Tokens. The button under it arms one — tap a "
                    + "conduit to remove it. The space comes back; the tile does not.",
                    "The right column counts skips. The button under it throws away the tile you are "
                    + "holding and deals another.",
                    "Finishing a route of four conduits or more earns a Pivot Token.",
                }),
        };

        private readonly List<TextLabel> _lines = new List<TextLabel>();

        private HexButton _next;
        private HexButton _back;
        private TextLabel _heading;
        private HelpFigure _figure;
        private int _page;

        /// <summary>How many pages there are.</summary>
        internal static int PageCount => Pages.Length;

        /// <summary>Which page is showing, from zero.</summary>
        internal int CurrentPage => _page;

        /// <summary>Every paragraph on every page, for the tests that hold them to a length.</summary>
        internal static IEnumerable<string> AllParagraphs
        {
            get
            {
                foreach (var page in Pages)
                {
                    foreach (var line in page.Lines)
                    {
                        yield return line;
                    }
                }
            }
        }

        /// <summary>
        /// The heading a page is drawn with, page number and all.
        /// </summary>
        /// <remarks>
        /// The page number is in the heading rather than drawn as pips, because a heading is being read
        /// anyway and a second row of shapes would be one more thing to decode. It is built here rather
        /// than inline so a test can measure the string that actually gets rendered — the counter is five
        /// characters the heading text itself does not account for, and it is what pushed the longest
        /// heading off both edges of the screen.
        /// </remarks>
        internal static string HeadingFor(int page)
        {
            var index = ((page % PageCount) + PageCount) % PageCount;

            return $"{Pages[index].Heading}  {(index + 1).ToString(CultureInfo.InvariantCulture)}/"
                   + PageCount.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Every page's heading.</summary>
        internal static IEnumerable<string> AllHeadings
        {
            get
            {
                foreach (var page in Pages)
                {
                    yield return page.Heading;
                }
            }
        }

        internal void Build(Camera camera, Material material)
        {
            _heading = TextLabel.Create(
                transform,
                camera,
                "heading",
                new Vector2(0.5f, HeadingViewportY),
                LabelMetrics.HeadingHeightFraction,
                BoardPalette.TextPrimary,
                TextAlignmentOptions.Center,
                HexButton.LabelDepth);

            _heading.SetWrapWidth(HeadingWrapWidthFraction);

            // One label per line of the longest page, reused rather than rebuilt: a page turn should
            // not create and destroy meshes, and the count is known at compile time.
            for (var index = 0; index < LongestPageLines; index++)
            {
                var line = TextLabel.Create(
                    transform,
                    camera,
                    $"line{index}",
                    new Vector2(0.5f, FirstLineViewportY - (index * LineSpacing)),
                    LabelMetrics.BodyHeightFraction,
                    BoardPalette.TextSecondary,
                    // Centred on its slot rather than hanging from the top of its rect: a wrapped
                    // paragraph then grows evenly about its anchor instead of climbing over whatever
                    // is above it, which is how the last page's first paragraph came to sit on top of
                    // the heading.
                    TextAlignmentOptions.Center,
                    HexButton.LabelDepth);

                line.SetWrapWidth(WrapWidthFraction);
                _lines.Add(line);
            }

            _next = HexButton.Create(
                transform, NextId, camera, material,
                new Vector2(0.86f, 0.09f), 0.4f, BoardPalette.MenuPrimary, touchRadiusFraction: 0.12f);
            MenuGlyphs.AddPlay(_next, 0.16f);

            _back = HexButton.Create(
                transform, BackId, camera, material,
                new Vector2(0.14f, 0.09f), 0.4f, BoardPalette.MenuSecondary, touchRadiusFraction: 0.12f);
            MenuGlyphs.AddBack(_back);

            // A picture per page, drawn from the board's own cells. Built after the controls so that if
            // the two ever overlap it is visible in the capture rather than hidden behind a button.
            _figure = new GameObject("Figure").AddComponent<HelpFigure>();
            _figure.transform.SetParent(transform, worldPositionStays: false);
            _figure.Build(camera, material);

            ShowPage(0);
        }

        /// <summary>
        /// Shows the given page, wrapping past the last.
        /// </summary>
        internal void ShowPage(int page)
        {
            _page = ((page % PageCount) + PageCount) % PageCount;

            var current = Pages[_page];

            _heading?.SetText(HeadingFor(_page));

            for (var index = 0; index < _lines.Count; index++)
            {
                _lines[index].SetText(
                    index < current.Lines.Length ? current.Lines[index] : string.Empty);
            }

            _figure?.ShowPage(_page);
        }

        /// <summary>Advances a page, wrapping back to the first.</summary>
        internal void Advance()
        {
            ShowPage(_page + 1);
        }

        internal string ButtonAt(Vector2 screenPosition)
        {
            if (_next != null && _next.IsPressed(screenPosition))
            {
                return NextId;
            }

            return _back != null && _back.IsPressed(screenPosition) ? BackId : null;
        }

        private readonly struct Page
        {
            internal Page(string heading, string[] lines)
            {
                Heading = heading;
                Lines = lines;
            }

            internal string Heading { get; }

            internal string[] Lines { get; }
        }
    }
}
