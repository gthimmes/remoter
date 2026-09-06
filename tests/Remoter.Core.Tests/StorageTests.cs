using Remoter.Core.Models;
using Remoter.Core.Security;
using Remoter.Core.Sessions;
using Remoter.Core.Storage;
using Xunit;

namespace Remoter.Core.Tests;

public class StorageTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "RemoterTests", Guid.NewGuid().ToString("N"));

    private string StorePath => Path.Combine(_dir, "connections.json");

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void ProfileStore_SaveThenLoad_RoundTrips()
    {
        var store = new ProfileStore(StorePath);
        var profile = new ConnectionProfile { Name = "Home PC", Host = "home", Username = "glenn" };

        store.Save(new[] { profile });
        var loaded = store.Load();

        var one = Assert.Single(loaded);
        Assert.Equal(profile.Id, one.Id);
        Assert.Equal("Home PC", one.Name);
        Assert.Equal("home", one.Host);
    }

    [Fact]
    public void ProfileStore_MissingFile_ReturnsEmpty()
    {
        Assert.Empty(new ProfileStore(StorePath).Load());
    }

    [Fact]
    public void ProfileStore_CorruptFile_IsMovedAsideAndReturnsEmpty()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(StorePath, "{ not valid json ");

        var loaded = new ProfileStore(StorePath).Load(out var warning);

        Assert.Empty(loaded);
        Assert.NotNull(warning);
        Assert.False(File.Exists(StorePath));
        Assert.Single(Directory.GetFiles(_dir, "*.corrupt-*"));
    }

    [Fact]
    public void ConnectionManager_Add_SavesPasswordWhenRequested()
    {
        var creds = new InMemoryCredentialStore();
        var manager = new ConnectionManager(new ProfileStore(StorePath), creds);
        var profile = new ConnectionProfile { Host = "h", Username = "u", SavePassword = true };

        manager.Add(profile, "secret");

        Assert.Equal("secret", manager.GetPassword(profile));
        Assert.Single(manager.Profiles);
    }

    [Fact]
    public void ConnectionManager_ClearingSavePassword_DeletesCredential()
    {
        var creds = new InMemoryCredentialStore();
        var manager = new ConnectionManager(new ProfileStore(StorePath), creds);
        var profile = new ConnectionProfile { Host = "h", Username = "u", SavePassword = true };
        manager.Add(profile, "secret");

        var edit = profile.Clone();
        edit.SavePassword = false;
        manager.Update(edit, null);

        Assert.Null(creds.Read(profile.CredentialTarget));
    }

    [Fact]
    public void ConnectionManager_Remove_DeletesProfileAndCredential()
    {
        var creds = new InMemoryCredentialStore();
        var manager = new ConnectionManager(new ProfileStore(StorePath), creds);
        var profile = new ConnectionProfile { Host = "h", SavePassword = true };
        manager.Add(profile, "secret");

        manager.Remove(profile);

        Assert.Empty(manager.Profiles);
        Assert.Null(creds.Read(profile.CredentialTarget));
    }
}

public class DisconnectReasonTests
{
    [Fact]
    public void Describe_KnownExtendedReason_UsesFriendlyText()
    {
        var info = DisconnectReasons.Describe(reason: 3, extendedReason: 5, controlDescription: null);
        Assert.Contains("Another user", info.Message);
        Assert.True(info.IsError);
    }

    [Fact]
    public void Describe_UserLogoff_IsNotError()
    {
        var info = DisconnectReasons.Describe(reason: 2, extendedReason: 12, controlDescription: null);
        Assert.False(info.IsError);
        Assert.True(DisconnectReasons.IsUserInitiated(2, 12));
    }

    [Fact]
    public void Describe_UnknownReason_FallsBackToControlDescription()
    {
        var info = DisconnectReasons.Describe(reason: 99999, extendedReason: 0, controlDescription: "Custom control text");
        Assert.Equal("Custom control text", info.Message);
        Assert.True(info.IsError);
    }

    [Fact]
    public void Describe_DnsFailure_IsError()
    {
        var info = DisconnectReasons.Describe(reason: 260, extendedReason: 0, controlDescription: null);
        Assert.True(info.IsError);
        Assert.Contains("could not be resolved", info.Message);
    }
}
