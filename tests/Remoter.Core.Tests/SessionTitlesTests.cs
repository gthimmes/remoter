using Remoter.Core.Sessions;
using Xunit;

namespace Remoter.Core.Tests;

public class SessionTitlesTests
{
    [Fact]
    public void Unique_KeepsTheNameWhenNothingElseUsesIt()
    {
        Assert.Equal("littleguy", SessionTitles.Unique("littleguy", []));
        Assert.Equal("littleguy", SessionTitles.Unique("littleguy", ["bigguy"]));
    }

    [Fact]
    public void Unique_NumbersRepeatsFromTwo()
    {
        Assert.Equal("littleguy (2)", SessionTitles.Unique("littleguy", ["littleguy"]));
        Assert.Equal("littleguy (3)", SessionTitles.Unique("littleguy", ["littleguy", "littleguy (2)"]));
    }

    [Fact]
    public void Unique_FillsAGapLeftByAClosedTab()
    {
        Assert.Equal("littleguy (2)", SessionTitles.Unique("littleguy", ["littleguy", "littleguy (3)"]));
    }

    [Fact]
    public void Unique_IgnoresCaseAndSurroundingSpace()
    {
        Assert.Equal("LittleGuy (2)", SessionTitles.Unique("  LittleGuy  ", ["littleguy"]));
    }

    [Fact]
    public void Unique_FallsBackWhenTheNameIsEmpty()
    {
        Assert.Equal("Session", SessionTitles.Unique("   ", []));
        Assert.Equal("Session (2)", SessionTitles.Unique("", ["Session"]));
    }

    [Theory]
    [InlineData(0, null, "Remoter sessions")]
    [InlineData(1, "littleguy", "littleguy - Remoter")]
    [InlineData(3, "littleguy", "littleguy - Remoter (3 sessions)")]
    public void WindowTitle_NamesTheActiveSessionAndCountsTheRest(int count, string? active, string expected)
    {
        Assert.Equal(expected, SessionTitles.WindowTitle(active, count));
    }
}
