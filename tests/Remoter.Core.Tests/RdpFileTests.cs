using Remoter.Core.Models;
using Remoter.Core.Rdp;
using Xunit;

namespace Remoter.Core.Tests;

public class RdpFileTests
{
    [Fact]
    public void Parse_ReadsAddressUsernameAndDisplay()
    {
        const string text = """
            full address:s:desktop.home:3390
            username:s:glenn
            domain:s:HOME
            screen mode id:i:2
            desktopwidth:i:2560
            desktopheight:i:1440
            session bpp:i:24
            audiomode:i:1
            redirectclipboard:i:0
            drivestoredirect:s:C:;DynamicDrives
            authentication level:i:2
            gatewayusagemethod:i:1
            gatewayhostname:s:gw.example.com
            """;

        var p = RdpFile.Parse(text);

        Assert.Equal("desktop.home", p.Host);
        Assert.Equal(3390, p.Port);
        Assert.Equal("glenn", p.Username);
        Assert.Equal("HOME", p.Domain);
        Assert.True(p.Display.FullScreen);
        Assert.Equal(2560, p.Display.Width);
        Assert.Equal(1440, p.Display.Height);
        Assert.Equal(24, p.Display.ColorDepth);
        Assert.Equal(AudioPlaybackMode.OnRemoteComputer, p.LocalResources.AudioPlayback);
        Assert.False(p.LocalResources.Clipboard);
        Assert.Contains("C:", p.LocalResources.Drives);
        Assert.True(p.LocalResources.DynamicDrives);
        Assert.Equal(ServerAuthenticationPolicy.Warn, p.Security.ServerAuthentication);
        Assert.Equal(GatewayUsage.Always, p.Gateway.Usage);
        Assert.Equal("gw.example.com", p.Gateway.Hostname);
    }

    [Theory]
    [InlineData(0, ServerAuthenticationPolicy.AlwaysConnect)]
    [InlineData(1, ServerAuthenticationPolicy.DoNotConnect)]
    [InlineData(2, ServerAuthenticationPolicy.Warn)]
    public void Parse_AuthenticationLevel_MatchesRdpSemantics(int level, ServerAuthenticationPolicy expected)
    {
        // .rdp "authentication level": 0 connect without warning, 1 do not connect, 2 warn.
        var p = RdpFile.Parse($"full address:s:host\r\nauthentication level:i:{level}");
        Assert.Equal(expected, p.Security.ServerAuthentication);
    }

    [Fact]
    public void Parse_AllDrivesWildcard()
    {
        var p = RdpFile.Parse("full address:s:host\r\ndrivestoredirect:s:*");
        Assert.True(p.LocalResources.AllDrives);
        Assert.Empty(p.LocalResources.Drives);
    }

    [Fact]
    public void WriteThenParse_RoundTripsKeySettings()
    {
        var original = new ConnectionProfile
        {
            Host = "server1",
            Port = 3389,
            Username = "admin",
            Domain = "CORP",
            Display = { SizeMode = SizeMode.Fixed, Width = 1600, Height = 900, ColorDepth = 32, FullScreen = true },
            LocalResources = { Clipboard = false, Printers = false, AllDrives = true },
            Experience = { DesktopBackground = false, Speed = ConnectionSpeed.Lan },
            Security = { ServerAuthentication = ServerAuthenticationPolicy.AlwaysConnect, NetworkLevelAuthentication = false },
        };

        var round = RdpFile.Parse(RdpFile.Write(original));

        Assert.Equal(original.Host, round.Host);
        Assert.Equal(original.Port, round.Port);
        Assert.Equal(original.Username, round.Username);
        Assert.Equal(original.Domain, round.Domain);
        Assert.Equal(original.Display.Width, round.Display.Width);
        Assert.Equal(original.Display.Height, round.Display.Height);
        Assert.Equal(original.Display.ColorDepth, round.Display.ColorDepth);
        Assert.True(round.Display.FullScreen);
        Assert.False(round.LocalResources.Clipboard);
        Assert.False(round.LocalResources.Printers);
        Assert.True(round.LocalResources.AllDrives);
        Assert.False(round.Experience.DesktopBackground);
        Assert.False(round.Security.NetworkLevelAuthentication);
        Assert.Equal(ServerAuthenticationPolicy.AlwaysConnect, round.Security.ServerAuthentication);
    }

    [Theory]
    [InlineData("host", "host", null)]
    [InlineData("host:3390", "host", 3390)]
    [InlineData("192.168.1.5:3391", "192.168.1.5", 3391)]
    [InlineData("[fe80::1]:3392", "fe80::1", 3392)]
    [InlineData("[fe80::1]", "fe80::1", null)]
    public void SplitHostPort_Cases(string input, string host, int? port)
    {
        var (h, p) = RdpFile.SplitHostPort(input);
        Assert.Equal(host, h);
        Assert.Equal(port, p);
    }
}
