using System.Text.Json;
using System.Text.Json.Serialization;
using Remoter.Core.Storage;

namespace Remoter.Core.Models;

/// <summary>
/// Everything needed to open a session, minus the password (which lives in Windows Credential Manager).
/// Plain mutable POCO so it serializes cleanly and can be edited in place by the UI.
/// </summary>
public sealed class ConnectionProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Friendly name. Falls back to the host name when empty.</summary>
    public string Name { get; set; } = "";

    public string Host { get; set; } = "";

    public int Port { get; set; } = 3389;

    public string? Username { get; set; }

    public string? Domain { get; set; }

    /// <summary>True when a password for this profile is stored in Credential Manager under <see cref="CredentialTarget"/>.</summary>
    public bool SavePassword { get; set; }

    /// <summary>Folder path this connection lives in, e.g. "Home/Servers". Empty means the top level.</summary>
    public string Folder { get; set; } = "";

    /// <summary>Pinned for one-click access.</summary>
    public bool Favorite { get; set; }

    /// <summary>Free-form labels for filtering.</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Free-form notes.</summary>
    public string Notes { get; set; } = "";

    public DisplaySettings Display { get; set; } = new();

    public LocalResourceSettings LocalResources { get; set; } = new();

    public ExperienceSettings Experience { get; set; } = new();

    public SecuritySettings Security { get; set; } = new();

    public GatewaySettings Gateway { get; set; } = new();

    public ProgramSettings Program { get; set; } = new();

    public DateTimeOffset? LastConnected { get; set; }

    [JsonIgnore]
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Host : Name;

    /// <summary>Heading this connection is shown under: favorites first, then its folder, else "Ungrouped".</summary>
    [JsonIgnore]
    public string GroupKey =>
        Favorite ? "★ Favorites" : string.IsNullOrEmpty(Folder) ? "Ungrouped" : Folder;

    /// <summary>Sort key that orders groups: favorites, then named folders A-Z, then Ungrouped last.</summary>
    [JsonIgnore]
    public string GroupSort =>
        Favorite ? "0" : string.IsNullOrEmpty(Folder) ? "2" : "1" + Folder.ToLowerInvariant();

    [JsonIgnore]
    public string TagList => string.Join(", ", Tags);

    /// <summary>Target name used in Windows Credential Manager. Stable across renames.</summary>
    [JsonIgnore]
    public string CredentialTarget => $"Remoter/{Id:D}";

    /// <summary>"user", "DOMAIN\user" or "" for display purposes.</summary>
    [JsonIgnore]
    public string QualifiedUsername =>
        string.IsNullOrEmpty(Username) ? "" :
        string.IsNullOrEmpty(Domain) ? Username : $"{Domain}\\{Username}";

    public ConnectionProfile Clone()
    {
        var json = JsonSerializer.Serialize(this, JsonDefaults.Options);
        return JsonSerializer.Deserialize<ConnectionProfile>(json, JsonDefaults.Options)!;
    }

    /// <summary>Copies every field except <see cref="Id"/> from <paramref name="source"/>.</summary>
    public void CopyFrom(ConnectionProfile source)
    {
        var clone = source.Clone();
        Name = clone.Name;
        Host = clone.Host;
        Port = clone.Port;
        Username = clone.Username;
        Domain = clone.Domain;
        SavePassword = clone.SavePassword;
        Folder = clone.Folder;
        Favorite = clone.Favorite;
        Tags = clone.Tags;
        Notes = clone.Notes;
        Display = clone.Display;
        LocalResources = clone.LocalResources;
        Experience = clone.Experience;
        Security = clone.Security;
        Gateway = clone.Gateway;
        Program = clone.Program;
        LastConnected = clone.LastConnected;
    }

    /// <summary>Returns a human readable list of problems, empty when the profile can be connected.</summary>
    public IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(Host))
            problems.Add("Computer name or address is required.");
        if (Port is < 1 or > 65535)
            problems.Add("Port must be between 1 and 65535.");
        if (Display.SizeMode != SizeMode.FitToWindow && (Display.Width < 200 || Display.Height < 200))
            problems.Add("Remote desktop size must be at least 200 x 200.");
        if (Display.ColorDepth is not (15 or 16 or 24 or 32))
            problems.Add("Color depth must be 15, 16, 24 or 32 bits.");
        if (Gateway.Usage != GatewayUsage.Never && string.IsNullOrWhiteSpace(Gateway.Hostname))
            problems.Add("A gateway server name is required when a gateway is used.");
        return problems;
    }
}

public sealed class DisplaySettings
{
    public SizeMode SizeMode { get; set; } = SizeMode.FitToWindow;

    /// <summary>Remote width when <see cref="SizeMode"/> is not FitToWindow.</summary>
    public int Width { get; set; } = 1920;

    public int Height { get; set; } = 1080;

    /// <summary>15, 16, 24 or 32.</summary>
    public int ColorDepth { get; set; } = 32;

    /// <summary>Start the session in full screen.</summary>
    public bool FullScreen { get; set; }

    /// <summary>Span the session across all local monitors when in full screen.</summary>
    public bool UseAllMonitors { get; set; }

    /// <summary>Remote DPI scaling percentage (100..500). Null means match the local monitor.</summary>
    public int? DesktopScalePercent { get; set; }

    public bool ShowConnectionBar { get; set; } = true;

    public bool PinConnectionBar { get; set; } = true;
}

public sealed class LocalResourceSettings
{
    public AudioPlaybackMode AudioPlayback { get; set; } = AudioPlaybackMode.OnThisComputer;

    /// <summary>Record from this computer (microphone redirection).</summary>
    public bool AudioCapture { get; set; }

    public KeyboardMode Keyboard { get; set; } = KeyboardMode.OnRemoteComputerInFullScreenOnly;

    public bool Clipboard { get; set; } = true;

    public bool Printers { get; set; } = true;

    public bool SmartCards { get; set; } = true;

    public bool Ports { get; set; }

    /// <summary>Plug and play devices.</summary>
    public bool Devices { get; set; }

    public bool AllDrives { get; set; }

    /// <summary>Drive letters ("C:", "D:") to redirect when <see cref="AllDrives"/> is false.</summary>
    public List<string> Drives { get; set; } = new();

    /// <summary>Also redirect drives plugged in later.</summary>
    public bool DynamicDrives { get; set; }
}

public sealed class ExperienceSettings
{
    public ConnectionSpeed Speed { get; set; } = ConnectionSpeed.AutoDetect;

    public bool DesktopBackground { get; set; } = true;

    public bool FontSmoothing { get; set; } = true;

    public bool DesktopComposition { get; set; } = true;

    public bool ShowWindowContentsWhileDragging { get; set; } = true;

    public bool MenuAndWindowAnimation { get; set; } = true;

    public bool VisualStyles { get; set; } = true;

    public bool PersistentBitmapCaching { get; set; } = true;

    public bool AutoReconnect { get; set; } = true;

    /// <summary>Performance flags as understood by the RDP control (TS_PERF_DISABLE_* / TS_PERF_ENABLE_*).</summary>
    public int ToPerformanceFlags()
    {
        const int DisableWallpaper = 0x01;
        const int DisableFullWindowDrag = 0x02;
        const int DisableMenuAnimations = 0x04;
        const int DisableTheming = 0x08;
        const int EnableFontSmoothing = 0x80;
        const int EnableDesktopComposition = 0x100;

        var flags = 0;
        if (!DesktopBackground) flags |= DisableWallpaper;
        if (!ShowWindowContentsWhileDragging) flags |= DisableFullWindowDrag;
        if (!MenuAndWindowAnimation) flags |= DisableMenuAnimations;
        if (!VisualStyles) flags |= DisableTheming;
        if (FontSmoothing) flags |= EnableFontSmoothing;
        if (DesktopComposition) flags |= EnableDesktopComposition;
        return flags;
    }

    public void FromPerformanceFlags(int flags)
    {
        DesktopBackground = (flags & 0x01) == 0;
        ShowWindowContentsWhileDragging = (flags & 0x02) == 0;
        MenuAndWindowAnimation = (flags & 0x04) == 0;
        VisualStyles = (flags & 0x08) == 0;
        FontSmoothing = (flags & 0x80) != 0;
        DesktopComposition = (flags & 0x100) != 0;
    }
}

public sealed class SecuritySettings
{
    public ServerAuthenticationPolicy ServerAuthentication { get; set; } = ServerAuthenticationPolicy.Warn;

    /// <summary>Network Level Authentication (CredSSP). Leave on unless the server is ancient.</summary>
    public bool NetworkLevelAuthentication { get; set; } = true;

    /// <summary>Restricted Admin mode: your credentials are never sent to the remote host.</summary>
    public bool RestrictedAdmin { get; set; }

    /// <summary>Remote Credential Guard: credentials stay on this machine, Kerberos tickets are redirected.</summary>
    public bool RemoteCredentialGuard { get; set; }

    /// <summary>Connect to the administrative (console) session, like mstsc /admin.</summary>
    public bool AdministrativeSession { get; set; }
}

public sealed class GatewaySettings
{
    public GatewayUsage Usage { get; set; } = GatewayUsage.Never;

    public string? Hostname { get; set; }

    public GatewayCredentialSource CredentialSource { get; set; } = GatewayCredentialSource.AskForPassword;

    /// <summary>Use the session credentials for the gateway too.</summary>
    public bool UseSameCredentials { get; set; } = true;
}

public sealed class ProgramSettings
{
    /// <summary>Program to start on connection instead of the shell.</summary>
    public string? StartProgram { get; set; }

    public string? WorkingDirectory { get; set; }
}
