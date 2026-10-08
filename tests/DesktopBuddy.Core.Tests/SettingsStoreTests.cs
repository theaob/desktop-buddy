using DesktopBuddy.Core;

namespace DesktopBuddy.Core.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "DesktopBuddyTests", Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(_dir, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Missing_file_gives_defaults()
    {
        var settings = SettingsStore.Load(SettingsPath);

        Assert.Equal(1.5, settings.WalkSpeed);
        Assert.Equal(5, settings.NapAfterMinutes);
        Assert.False(settings.Paused);
    }

    [Fact]
    public void Saved_settings_load_back()
    {
        SettingsStore.Save(SettingsPath, new BuddySettings { WalkSpeed = 2.25, NapAfterMinutes = 12, Paused = true });

        var loaded = SettingsStore.Load(SettingsPath);

        Assert.Equal(2.25, loaded.WalkSpeed);
        Assert.Equal(12, loaded.NapAfterMinutes);
        Assert.True(loaded.Paused);
        Assert.False(File.Exists(SettingsPath + ".tmp"));
    }

    [Fact]
    public void Corrupt_file_gives_defaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, "{ not json");

        var settings = SettingsStore.Load(SettingsPath);

        Assert.Equal(1.5, settings.WalkSpeed);
    }

    [Fact]
    public void Out_of_range_values_are_clamped()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, """{ "WalkSpeed": 99, "NapAfterMinutes": -4 }""");

        var settings = SettingsStore.Load(SettingsPath);

        Assert.Equal(BuddySettings.MaxWalkSpeed, settings.WalkSpeed);
        Assert.Equal(BuddySettings.MinNapAfterMinutes, settings.NapAfterMinutes);
    }
}
