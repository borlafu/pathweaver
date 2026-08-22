using Pathweaver.Core.Determinism;

namespace Pathweaver.Core.Tests.Determinism;

public class SeedSourceTests
{
    [Fact]
    public void The_same_calendar_date_always_yields_the_same_seed()
    {
        // Arrange / Act
        var first = SeedSource.ForDate(2026, 8, 22);
        var second = SeedSource.ForDate(2026, 8, 22);

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void Consecutive_days_yield_different_seeds()
    {
        // Arrange / Act
        var today = SeedSource.ForDate(2026, 8, 22);
        var tomorrow = SeedSource.ForDate(2026, 8, 23);

        // Assert
        Assert.NotEqual(today, tomorrow);
    }

    [Fact]
    public void Transposed_day_and_month_yield_different_seeds()
    {
        // A packed date that merely concatenated its parts would collide here.
        // Arrange / Act
        var januarySecond = SeedSource.ForDate(2026, 1, 2);
        var februaryFirst = SeedSource.ForDate(2026, 2, 1);

        // Assert
        Assert.NotEqual(januarySecond, februaryFirst);
    }

    [Fact]
    public void Adjacent_dates_differ_across_many_bits()
    {
        // A daily puzzle that shifted only slightly from yesterday would feel
        // repetitive, so the seed must scatter rather than increment.
        // Arrange
        var today = SeedSource.ForDate(2026, 8, 22);
        var tomorrow = SeedSource.ForDate(2026, 8, 23);

        // Act
        var differingBits = CountSetBits(today ^ tomorrow);

        // Assert
        Assert.True(
            differingBits >= 16,
            $"Expected a wide spread between adjacent days, saw {differingBits} differing bits.");
    }

    [Theory]
    [InlineData(2000, 1, 1)]
    [InlineData(2026, 2, 29)] // not a leap year
    [InlineData(2026, 13, 1)]
    [InlineData(2026, 0, 1)]
    [InlineData(2026, 8, 0)]
    [InlineData(2026, 8, 32)]
    [InlineData(0, 8, 22)]
    public void Invalid_dates_are_rejected(int year, int month, int day)
    {
        // The one valid row is 2000-01-01, which must not throw; the rest must.
        if (year == 2000)
        {
            SeedSource.ForDate(year, month, day);
            return;
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => SeedSource.ForDate(year, month, day));
    }

    [Fact]
    public void Leap_day_is_accepted_in_a_leap_year()
    {
        // Act / Assert — no throw
        var seed = SeedSource.ForDate(2028, 2, 29);
        Assert.NotEqual(0UL, seed);
    }

    [Fact]
    public void A_years_worth_of_dates_produces_no_collisions()
    {
        // Arrange
        var seeds = new HashSet<ulong>();

        // Act
        for (var month = 1; month <= 12; month++)
        {
            for (var day = 1; day <= DateTime.DaysInMonth(2026, month); day++)
            {
                seeds.Add(SeedSource.ForDate(2026, month, day));
            }
        }

        // Assert
        Assert.Equal(365, seeds.Count);
    }

    [Fact]
    public void A_stream_is_reproducible_from_the_same_seed()
    {
        // Arrange
        var seed = SeedSource.ForDate(2026, 8, 22);

        // Act
        var first = SeedSource.Stream(seed, PathweaverStream.TileBag);
        var second = SeedSource.Stream(seed, PathweaverStream.TileBag);

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void Different_streams_from_one_seed_diverge()
    {
        // This is the property that lets a new subsystem start drawing without
        // shifting the sequence every existing subsystem sees.
        // Arrange
        var seed = SeedSource.ForDate(2026, 8, 22);
        var values = new HashSet<uint>();

        // Act
        foreach (PathweaverStream stream in Enum.GetValues<PathweaverStream>())
        {
            var (_, value) = SeedSource.Stream(seed, stream).NextUInt32();
            values.Add(value);
        }

        // Assert
        Assert.Equal(Enum.GetValues<PathweaverStream>().Length, values.Count);
    }

    [Fact]
    public void An_unknown_stream_is_rejected()
    {
        // Arrange
        var seed = SeedSource.ForDate(2026, 8, 22);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SeedSource.Stream(seed, (PathweaverStream)999));
    }

    private static int CountSetBits(ulong value)
    {
        var count = 0;
        while (value != 0)
        {
            value &= value - 1;
            count++;
        }

        return count;
    }
}
