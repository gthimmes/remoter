using Remoter.Core.Models;
using Xunit;

namespace Remoter.Core.Tests;

public class ConnectionQueryTests
{
    private static ConnectionProfile Sample() => new()
    {
        Name = "Home Server",
        Host = "192.168.1.10",
        Username = "glenn",
        Domain = "HOME",
        Folder = "Home/Servers",
        Notes = "Runs the SQL database",
        Tags = { "prod", "sql" },
    };

    [Theory]
    [InlineData("", true)]
    [InlineData("home", true)]
    [InlineData("SERVER", true)]
    [InlineData("192.168", true)]
    [InlineData("home\\glenn", true)]
    [InlineData("sql", true)]         // matches tag and notes
    [InlineData("home sql", true)]    // ANDed terms, both present
    [InlineData("home azure", false)] // second term absent
    [InlineData("nope", false)]
    public void Matches_Cases(string query, bool expected)
    {
        Assert.Equal(expected, ConnectionQuery.Matches(Sample(), query));
    }

    [Theory]
    [InlineData("Home/Servers", "Home/Servers")]
    [InlineData("/Home//Servers/", "Home/Servers")]
    [InlineData("Home\\Servers", "Home/Servers")]
    [InlineData("  Home / Servers ", "Home/Servers")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void NormalizeFolder_Cases(string? input, string expected)
    {
        Assert.Equal(expected, ConnectionQuery.NormalizeFolder(input));
    }

    [Fact]
    public void AllFolders_IncludesParents_Sorted()
    {
        var profiles = new[]
        {
            new ConnectionProfile { Folder = "Home/Servers" },
            new ConnectionProfile { Folder = "Home/Desktops" },
            new ConnectionProfile { Folder = "Work" },
            new ConnectionProfile { Folder = "" },
        };

        var folders = ConnectionQuery.AllFolders(profiles);

        Assert.Contains("Home", folders);
        Assert.Contains("Home/Servers", folders);
        Assert.Contains("Home/Desktops", folders);
        Assert.Contains("Work", folders);
        Assert.DoesNotContain("", folders);
    }
}
