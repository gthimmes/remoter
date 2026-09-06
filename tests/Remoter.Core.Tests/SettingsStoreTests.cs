using Remoter.Core.Storage;
using Xunit;

namespace Remoter.Core.Tests;

public class SettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "RemoterTests", Guid.NewGuid().ToString("N"));

    private string Path_ => Path.Combine(_dir, "settings.json");

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void DefaultsToFollowingWindows()
    {
        Assert.Equal(AppTheme.System, new AppSettings().Theme);
        Assert.Equal(AppTheme.System, new SettingsStore(Path_).Load().Theme);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var store = new SettingsStore(Path_);
        store.Save(new AppSettings { Theme = AppTheme.Light });

        Assert.Equal(AppTheme.Light, store.Load().Theme);
    }

    [Fact]
    public void WritesTheThemeByName_SoTheFileStaysReadable()
    {
        new SettingsStore(Path_).Save(new AppSettings { Theme = AppTheme.Dark });

        Assert.Contains("\"Dark\"", File.ReadAllText(Path_));
    }

    [Fact]
    public void CorruptFile_FallsBackToDefaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path_, "{ this is not json");

        Assert.Equal(AppTheme.System, new SettingsStore(Path_).Load().Theme);
    }

    [Fact]
    public void EmptyFile_FallsBackToDefaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path_, "null");

        Assert.Equal(AppTheme.System, new SettingsStore(Path_).Load().Theme);
    }
}
