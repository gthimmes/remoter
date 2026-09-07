using Remoter.Core.Models;
using Xunit;

namespace Remoter.Core.Tests;

public class TagHueTests
{
    [Fact]
    public void AlwaysInRange()
    {
        foreach (var tag in new[] { "home", "ci", "prod", "win11", "lab", "dmz", "", "  ", "a very long tag name" })
        {
            var index = TagHue.IndexFor(tag);
            Assert.InRange(index, 0, TagHue.Count - 1);
        }
    }

    [Fact]
    public void SameTagAlwaysGetsTheSameHue()
    {
        Assert.Equal(TagHue.IndexFor("production"), TagHue.IndexFor("production"));
    }

    [Fact]
    public void IgnoresCaseAndSurroundingSpace()
    {
        Assert.Equal(TagHue.IndexFor("Home"), TagHue.IndexFor("  home  "));
    }

    [Fact]
    public void NullAndEmptyAreStable()
    {
        Assert.Equal(0, TagHue.IndexFor(null));
        Assert.Equal(0, TagHue.IndexFor(""));
    }

    // The whole point is that a tag keeps its colour between runs, so pin the values. If the hash
    // changes these move, and every user's list repaints; that should be a deliberate decision.
    [Theory]
    [InlineData("home", 2)]
    [InlineData("ci", 1)]
    [InlineData("prod", 0)]
    [InlineData("win11", 3)]
    public void HuesArePinned(string tag, int expected)
    {
        Assert.Equal(expected, TagHue.IndexFor(tag));
    }

    [Fact]
    public void SpreadsAcrossThePalette()
    {
        var tags = new[] { "home", "ci", "prod", "win11", "lab", "dmz", "vpn", "office", "backup", "test" };
        var used = tags.Select(TagHue.IndexFor).Distinct().Count();

        Assert.True(used >= 3, $"expected a spread across the palette, only {used} hues used");
    }
}
