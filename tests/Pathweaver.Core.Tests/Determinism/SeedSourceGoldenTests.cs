using Pathweaver.Core.Determinism;

namespace Pathweaver.Core.Tests.Determinism;

/// <summary>
/// Regression anchors for the date-to-seed pipeline.
/// </summary>
/// <remarks>
/// These values were produced by an independent reimplementation of SplitMix64
/// and PCG32 in another language, so they check the C# rather than merely
/// recording it. Unlike the PCG reference vectors in
/// <see cref="Pcg32Tests"/>, they are not an external standard — they exist to
/// make an accidental change to seeding fail loudly.
/// <para>
/// A deliberate change to the algorithm means every player's Daily Expedition
/// history changes. If one of these fails, decide whether that is intended
/// before updating the constant.
/// </para>
/// </remarks>
public class SeedSourceGoldenTests
{
    [Theory]
    [InlineData(2026, 8, 22, 3416675652221899442UL)]
    [InlineData(2026, 1, 1, 15110480651355526817UL)]
    [InlineData(2028, 2, 29, 17030735235978330075UL)]
    public void Seeds_for_known_dates_are_stable(int year, int month, int day, ulong expected)
    {
        // Act / Assert
        Assert.Equal(expected, SeedSource.ForDate(year, month, day));
    }

    [Theory]
    [InlineData(PathweaverStream.GridLayout, 0xbaf944deu)]
    [InlineData(PathweaverStream.TileBag, 0x0708cce9u)]
    [InlineData(PathweaverStream.Objectives, 0x017a7c6eu)]
    [InlineData(PathweaverStream.Environment, 0xfd13001eu)]
    public void First_draw_per_stream_is_stable_for_2026_08_22(PathweaverStream stream, uint expected)
    {
        // Arrange
        var seed = SeedSource.ForDate(2026, 8, 22);

        // Act
        var (_, value) = SeedSource.Stream(seed, stream).NextUInt32();

        // Assert
        Assert.Equal(expected, value);
    }
}
