using NUnit.Framework;
using Pathweaver.Game.Presentation.Menus;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// The main menu's row of secondary buttons, and the one fact that decides how many there are.
    /// </summary>
    /// <remarks>
    /// Layout arithmetic only. Whether a hexagon looks tappable needs a person, but whether the row
    /// stays centred when a button leaves it does not — and that is the part a hidden atlas changes.
    /// </remarks>
    public class MainMenuViewTests
    {
        [Test]
        public void The_atlas_is_withheld()
        {
            // The reason lives on MainMenuView.IsAtlasVisible: the atlas works but explains nothing,
            // and it cannot explain itself without a font. This test exists so re-enabling it is a
            // deliberate act with a failing test attached, rather than a stray edit.
            Assert.That(MainMenuView.IsAtlasVisible, Is.False);
        }

        [Test]
        public void Hiding_the_atlas_leaves_four_secondary_buttons()
        {
            Assert.That(MainMenuView.SecondaryCount, Is.EqualTo(4));
        }

        [Test]
        public void Four_buttons_land_where_they_were_placed_by_hand()
        {
            // The row was authored as 0.155, 0.385, 0.615, 0.845 before it was computed. If the
            // arithmetic disagrees, the spacing changed for everyone rather than only for the row
            // that lost a button.
            Assert.That(MainMenuView.SecondaryX(0, 4), Is.EqualTo(0.155f).Within(0.0001f));
            Assert.That(MainMenuView.SecondaryX(1, 4), Is.EqualTo(0.385f).Within(0.0001f));
            Assert.That(MainMenuView.SecondaryX(2, 4), Is.EqualTo(0.615f).Within(0.0001f));
            Assert.That(MainMenuView.SecondaryX(3, 4), Is.EqualTo(0.845f).Within(0.0001f));
        }

        [Test]
        public void Three_buttons_stay_centred()
        {
            // Not the current count, but the arithmetic has to survive one leaving as well as one
            // arriving — the atlas returns as a fifth, and could be withheld again.
            Assert.That(MainMenuView.SecondaryX(0, 3), Is.EqualTo(0.27f).Within(0.0001f));
            Assert.That(MainMenuView.SecondaryX(1, 3), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(MainMenuView.SecondaryX(2, 3), Is.EqualTo(0.73f).Within(0.0001f));
        }

        [Test]
        public void The_row_is_symmetrical_at_every_count()
        {
            for (var count = 1; count <= 5; count++)
            {
                var first = MainMenuView.SecondaryX(0, count);
                var last = MainMenuView.SecondaryX(count - 1, count);

                Assert.That(
                    (first + last) * 0.5f,
                    Is.EqualTo(0.5f).Within(0.0001f),
                    $"A row of {count} is not centred.");
            }
        }

        [Test]
        public void A_fifth_button_closes_the_row_up_rather_than_running_off_the_screen()
        {
            // The atlas returns as a fifth. At a fixed spacing the outermost two would sit at 0.04 and
            // 0.96, half off a phone; the row narrows instead.
            for (var index = 0; index < 5; index++)
            {
                Assert.That(MainMenuView.SecondaryX(index, 5), Is.InRange(0.1f, 0.9f));
            }

            Assert.That(MainMenuView.SecondaryX(0, 5), Is.EqualTo(0.155f).Within(0.0001f));
            Assert.That(MainMenuView.SecondaryX(4, 5), Is.EqualTo(0.845f).Within(0.0001f));
        }

        [Test]
        public void Every_button_stays_on_screen()
        {
            // A viewport fraction outside 0 to 1 is off the edge of the phone. Radius is not included
            // because the buttons are small and the margin at three or four is generous; what matters
            // is that a future fifth button fails here rather than on a device.
            for (var index = 0; index < MainMenuView.SecondaryCount; index++)
            {
                Assert.That(
                    MainMenuView.SecondaryX(index, MainMenuView.SecondaryCount),
                    Is.InRange(0.05f, 0.95f));
            }
        }
    }
}
