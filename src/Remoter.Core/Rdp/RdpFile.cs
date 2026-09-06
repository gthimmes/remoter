using System.Globalization;
using System.Text;
using Remoter.Core.Models;

namespace Remoter.Core.Rdp;

/// <summary>
/// Reads and writes the classic mstsc ".rdp" file format ("key:type:value" lines) so existing
/// connection files keep working. Unknown keys are ignored on import and not emitted on export.
/// </summary>
public static class RdpFile
{
    public static ConnectionProfile Parse(string text)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r').Trim();
            if (line.Length == 0)
                continue;
            var first = line.IndexOf(':');
            if (first <= 0)
                continue;
            var second = line.IndexOf(':', first + 1);
            if (second <= first)
                continue;
            var key = line[..first].Trim();
            var value = line[(second + 1)..];
            values[key] = value;
        }

        var p = new ConnectionProfile();

        if (values.TryGetValue("full address", out var address))
        {
            var (host, port) = SplitHostPort(address);
            p.Host = host;
            if (port is not null)
                p.Port = port.Value;
        }
        if (Int(values, "server port") is { } serverPort)
            p.Port = serverPort;
        p.Username = Str(values, "username");
        p.Domain = Str(values, "domain");

        // Display
        if (Int(values, "screen mode id") is { } screenMode)
            p.Display.FullScreen = screenMode == 2;
        var width = Int(values, "desktopwidth");
        var height = Int(values, "desktopheight");
        if (width is > 0 && height is > 0)
        {
            p.Display.Width = width.Value;
            p.Display.Height = height.Value;
            p.Display.SizeMode = Int(values, "smart sizing") == 1 ? SizeMode.SmartSize : SizeMode.Fixed;
        }
        else if (Int(values, "smart sizing") == 1)
        {
            p.Display.SizeMode = SizeMode.SmartSize;
        }
        if (Int(values, "dynamic resolution") == 1)
            p.Display.SizeMode = SizeMode.FitToWindow;
        if (Int(values, "session bpp") is { } bpp)
            p.Display.ColorDepth = bpp;
        p.Display.UseAllMonitors = Int(values, "use multimon") == 1;
        if (Int(values, "desktopscalefactor") is { } scale && scale >= 100)
            p.Display.DesktopScalePercent = scale;
        if (Int(values, "displayconnectionbar") is { } bar)
            p.Display.ShowConnectionBar = bar == 1;
        if (Int(values, "pinconnectionbar") is { } pin)
            p.Display.PinConnectionBar = pin == 1;

        // Local resources
        if (Int(values, "audiomode") is { } audio && Enum.IsDefined(typeof(AudioPlaybackMode), audio))
            p.LocalResources.AudioPlayback = (AudioPlaybackMode)audio;
        p.LocalResources.AudioCapture = Int(values, "audiocapturemode") == 1;
        if (Int(values, "keyboardhook") is { } hook && Enum.IsDefined(typeof(KeyboardMode), hook))
            p.LocalResources.Keyboard = (KeyboardMode)hook;
        if (Int(values, "redirectclipboard") is { } clip) p.LocalResources.Clipboard = clip == 1;
        if (Int(values, "redirectprinters") is { } prn) p.LocalResources.Printers = prn == 1;
        if (Int(values, "redirectsmartcards") is { } sc) p.LocalResources.SmartCards = sc == 1;
        if (Int(values, "redirectcomports") is { } com) p.LocalResources.Ports = com == 1;
        if (Int(values, "devicestoredirect") is { } dev) p.LocalResources.Devices = dev == 1;
        else if (Str(values, "devicestoredirect") is { Length: > 0 } devStr) p.LocalResources.Devices = devStr == "*";
        var drives = Str(values, "drivestoredirect");
        if (!string.IsNullOrWhiteSpace(drives))
        {
            if (drives.Trim() == "*")
            {
                p.LocalResources.AllDrives = true;
            }
            else
            {
                foreach (var d in drives.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (d.Equals("DynamicDrives", StringComparison.OrdinalIgnoreCase))
                        p.LocalResources.DynamicDrives = true;
                    else
                        p.LocalResources.Drives.Add(d.ToUpperInvariant());
                }
            }
        }
        else if (Int(values, "redirectdrives") == 1)
        {
            p.LocalResources.AllDrives = true;
        }

        // Experience
        if (Int(values, "connection type") is { } speed && Enum.IsDefined(typeof(ConnectionSpeed), speed))
            p.Experience.Speed = (ConnectionSpeed)speed;
        if (Int(values, "networkautodetect") == 1 || Int(values, "bandwidthautodetect") == 1)
            p.Experience.Speed = ConnectionSpeed.AutoDetect;
        if (Int(values, "disable wallpaper") is { } wp) p.Experience.DesktopBackground = wp == 0;
        if (Int(values, "allow font smoothing") is { } fs) p.Experience.FontSmoothing = fs == 1;
        if (Int(values, "allow desktop composition") is { } dc) p.Experience.DesktopComposition = dc == 1;
        if (Int(values, "disable full window drag") is { } fwd) p.Experience.ShowWindowContentsWhileDragging = fwd == 0;
        if (Int(values, "disable menu anims") is { } ma) p.Experience.MenuAndWindowAnimation = ma == 0;
        if (Int(values, "disable themes") is { } th) p.Experience.VisualStyles = th == 0;
        if (Int(values, "bitmapcachepersistenable") is { } bc) p.Experience.PersistentBitmapCaching = bc == 1;
        if (Int(values, "autoreconnection enabled") is { } ar) p.Experience.AutoReconnect = ar == 1;

        // Security
        if (Int(values, "authentication level") is { } auth && Enum.IsDefined(typeof(ServerAuthenticationPolicy), auth))
            p.Security.ServerAuthentication = (ServerAuthenticationPolicy)auth;
        if (Int(values, "enablecredsspsupport") is { } nla) p.Security.NetworkLevelAuthentication = nla == 1;
        p.Security.RestrictedAdmin = Int(values, "restricted admin") == 1 || Int(values, "restrictedlogon") == 1;
        p.Security.RemoteCredentialGuard = Int(values, "remotecredentialguard") == 1 || Int(values, "redirected authentication") == 1;
        p.Security.AdministrativeSession = Int(values, "administrative session") == 1;

        // Gateway
        if (Int(values, "gatewayusagemethod") is { } gw)
            p.Gateway.Usage = gw switch { 1 => GatewayUsage.Always, 2 => GatewayUsage.AutoDetect, _ => GatewayUsage.Never };
        p.Gateway.Hostname = Str(values, "gatewayhostname");
        if (Int(values, "gatewaycredentialssource") is { } gcs && Enum.IsDefined(typeof(GatewayCredentialSource), gcs))
            p.Gateway.CredentialSource = (GatewayCredentialSource)gcs;
        if (Int(values, "promptcredentialonce") is { } once) p.Gateway.UseSameCredentials = once == 1;

        // Program
        p.Program.StartProgram = Str(values, "alternate shell");
        p.Program.WorkingDirectory = Str(values, "shell working directory");

        p.Name = p.Host;
        return p;
    }

    public static string Write(ConnectionProfile p)
    {
        var sb = new StringBuilder();
        void S(string key, string? value) { if (!string.IsNullOrEmpty(value)) sb.Append(key).Append(":s:").Append(value).Append("\r\n"); }
        void I(string key, int value) => sb.Append(key).Append(":i:").Append(value.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
        void B(string key, bool value) => I(key, value ? 1 : 0);

        S("full address", p.Port == 3389 ? p.Host : $"{p.Host}:{p.Port}");
        I("server port", p.Port);
        S("username", p.Username);
        S("domain", p.Domain);

        I("screen mode id", p.Display.FullScreen ? 2 : 1);
        I("desktopwidth", p.Display.Width);
        I("desktopheight", p.Display.Height);
        B("smart sizing", p.Display.SizeMode == SizeMode.SmartSize);
        B("dynamic resolution", p.Display.SizeMode == SizeMode.FitToWindow);
        I("session bpp", p.Display.ColorDepth);
        B("use multimon", p.Display.UseAllMonitors);
        if (p.Display.DesktopScalePercent is { } scale)
            I("desktopscalefactor", scale);
        B("displayconnectionbar", p.Display.ShowConnectionBar);
        B("pinconnectionbar", p.Display.PinConnectionBar);

        I("audiomode", (int)p.LocalResources.AudioPlayback);
        B("audiocapturemode", p.LocalResources.AudioCapture);
        I("keyboardhook", (int)p.LocalResources.Keyboard);
        B("redirectclipboard", p.LocalResources.Clipboard);
        B("redirectprinters", p.LocalResources.Printers);
        B("redirectsmartcards", p.LocalResources.SmartCards);
        B("redirectcomports", p.LocalResources.Ports);
        S("devicestoredirect", p.LocalResources.Devices ? "*" : null);
        if (p.LocalResources.AllDrives)
        {
            S("drivestoredirect", "*");
        }
        else
        {
            var parts = new List<string>(p.LocalResources.Drives.Select(d => d.EndsWith(':') ? d : d + ":"));
            if (p.LocalResources.DynamicDrives)
                parts.Add("DynamicDrives");
            S("drivestoredirect", string.Join(";", parts));
        }

        I("connection type", (int)p.Experience.Speed);
        B("networkautodetect", p.Experience.Speed == ConnectionSpeed.AutoDetect);
        B("bandwidthautodetect", p.Experience.Speed == ConnectionSpeed.AutoDetect);
        B("disable wallpaper", !p.Experience.DesktopBackground);
        B("allow font smoothing", p.Experience.FontSmoothing);
        B("allow desktop composition", p.Experience.DesktopComposition);
        B("disable full window drag", !p.Experience.ShowWindowContentsWhileDragging);
        B("disable menu anims", !p.Experience.MenuAndWindowAnimation);
        B("disable themes", !p.Experience.VisualStyles);
        B("bitmapcachepersistenable", p.Experience.PersistentBitmapCaching);
        B("autoreconnection enabled", p.Experience.AutoReconnect);

        I("authentication level", (int)p.Security.ServerAuthentication);
        B("enablecredsspsupport", p.Security.NetworkLevelAuthentication);
        B("negotiate security layer", true);
        B("prompt for credentials", false);
        B("restricted admin", p.Security.RestrictedAdmin);
        B("remotecredentialguard", p.Security.RemoteCredentialGuard);
        B("administrative session", p.Security.AdministrativeSession);

        I("gatewayusagemethod", (int)p.Gateway.Usage);
        S("gatewayhostname", p.Gateway.Hostname);
        I("gatewaycredentialssource", (int)p.Gateway.CredentialSource);
        I("gatewayprofileusagemethod", p.Gateway.Usage == GatewayUsage.Never ? 0 : 1);
        B("promptcredentialonce", p.Gateway.UseSameCredentials);

        S("alternate shell", p.Program.StartProgram);
        S("shell working directory", p.Program.WorkingDirectory);

        return sb.ToString();
    }

    public static (string Host, int? Port) SplitHostPort(string address)
    {
        address = address.Trim();
        if (address.StartsWith('['))
        {
            // [ipv6]:port
            var close = address.IndexOf(']');
            if (close > 0)
            {
                var host = address[1..close];
                var rest = address[(close + 1)..];
                if (rest.StartsWith(':') && int.TryParse(rest[1..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var p6))
                    return (host, p6);
                return (host, null);
            }
        }

        var colons = address.Count(c => c == ':');
        if (colons == 1)
        {
            var idx = address.LastIndexOf(':');
            if (int.TryParse(address[(idx + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
                return (address[..idx], port);
        }
        return (address, null);
    }

    private static string? Str(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var v) && v.Length > 0 ? v : null;

    private static int? Int(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var v) && int.TryParse(v.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : null;
}
