using System.Text.Json.Serialization;

namespace Remoter.Core.Models;

/// <summary>
/// A host the user has connected to successfully. Kept as a short, most-recent-first list so the
/// top box can offer quick re-picks, separate from the saved connection profiles.
/// </summary>
public sealed class RecentConnection
{
    public string Host { get; set; } = "";

    public int Port { get; set; } = 3389;

    public string? Username { get; set; }

    public string? Domain { get; set; }

    public DateTimeOffset LastConnected { get; set; } = DateTimeOffset.Now;

    /// <summary>What the user typed / would type to reach this host, e.g. "desktop" or "desktop:3390".</summary>
    [JsonIgnore]
    public string Address => Port == 3389 ? Host : $"{Host}:{Port}";

    /// <summary>Same key used to dedupe recents: host + port, case-insensitive.</summary>
    [JsonIgnore]
    public string Key => $"{Host.ToLowerInvariant()}:{Port}";
}
