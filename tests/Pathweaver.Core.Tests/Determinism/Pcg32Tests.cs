using Pathweaver.Core.Determinism;

namespace Pathweaver.Core.Tests.Determinism;

public class Pcg32Tests
{
    // Published output of the reference pcg32 implementation seeded with
    // state 42, sequence 54. Testing against an external reference rather than
    // our own first run is the point: it catches a transcription error that a
    // self-generated golden file would happily bless.
    private static readonly uint[] ReferenceSequence =
    {
        0xa15c02b7,
        0x7b47f409,
        0xba1d3330,
        0x83d2f293,
        0xbfa4784b,
        0xcbed606e,
    };

    [Fact]
    public void Matches_the_reference_implementation_for_seed_42_sequence_54()
    {
        // Arrange
        var generator = Pcg32.Seed(42UL, 54UL);

        // Act / Assert
        foreach (var expected in ReferenceSequence)
        {
            uint actual;
            (generator, actual) = generator.NextUInt32();
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void Same_seed_produces_the_same_sequence()
    {
        // Arrange
        var first = Pcg32.Seed(1234UL, 1UL);
        var second = Pcg32.Seed(1234UL, 1UL);

        // Act / Assert
        for (var i = 0; i < 50; i++)
        {
            uint a, b;
            (first, a) = first.NextUInt32();
            (second, b) = second.NextUInt32();
            Assert.Equal(a, b);
        }
    }

    [Fact]
    public void Different_sequences_diverge_from_the_same_state()
    {
        // Arrange
        var streamOne = Pcg32.Seed(7UL, 1UL);
        var streamTwo = Pcg32.Seed(7UL, 2UL);

        // Act
        var (_, firstValue) = streamOne.NextUInt32();
        var (_, secondValue) = streamTwo.NextUInt32();

        // Assert
        Assert.NotEqual(firstValue, secondValue);
    }

    [Fact]
    public void Advancing_returns_a_new_generator_and_leaves_the_original_untouched()
    {
        // Arrange
        var original = Pcg32.Seed(99UL, 3UL);

        // Act
        var (advanced, firstValue) = original.NextUInt32();
        var (_, replayedValue) = original.NextUInt32();
        var (_, nextValue) = advanced.NextUInt32();

        // Assert
        Assert.Equal(firstValue, replayedValue);
        Assert.NotEqual(firstValue, nextValue);
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(6u)]
    [InlineData(52u)]
    [InlineData(1000u)]
    public void Bounded_draws_stay_below_the_exclusive_bound(uint bound)
    {
        // Arrange
        var generator = Pcg32.Seed(2024UL, 11UL);

        // Act / Assert
        for (var i = 0; i < 500; i++)
        {
            uint value;
            (generator, value) = generator.NextUInt32(bound);
            Assert.InRange(value, 0u, bound - 1);
        }
    }

    [Fact]
    public void A_bound_of_one_always_yields_zero()
    {
        // Arrange
        var generator = Pcg32.Seed(5UL, 5UL);

        // Act
        uint value;
        (generator, value) = generator.NextUInt32(1u);

        // Assert
        Assert.Equal(0u, value);
    }

    [Fact]
    public void A_bound_of_zero_is_rejected()
    {
        // Arrange
        var generator = Pcg32.Seed(5UL, 5UL);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => generator.NextUInt32(0u));
    }

    [Fact]
    public void Bounded_draws_are_reproducible_from_the_same_seed()
    {
        // Arrange
        var first = Pcg32.Seed(31337UL, 9UL);
        var second = Pcg32.Seed(31337UL, 9UL);

        // Act / Assert
        for (var i = 0; i < 100; i++)
        {
            uint a, b;
            (first, a) = first.NextUInt32(6u);
            (second, b) = second.NextUInt32(6u);
            Assert.Equal(a, b);
        }
    }

    [Fact]
    public void Bounded_draws_cover_the_whole_range()
    {
        // Arrange
        var generator = Pcg32.Seed(77UL, 4UL);
        var seen = new HashSet<uint>();

        // Act
        for (var i = 0; i < 500; i++)
        {
            uint value;
            (generator, value) = generator.NextUInt32(6u);
            seen.Add(value);
        }

        // Assert — a fair six-sided draw over 500 rolls hits every face
        Assert.Equal(6, seen.Count);
    }

    [Fact]
    public void Generators_with_equal_state_are_equal()
    {
        // Arrange
        var first = Pcg32.Seed(8UL, 8UL);
        var second = Pcg32.Seed(8UL, 8UL);

        // Act / Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
