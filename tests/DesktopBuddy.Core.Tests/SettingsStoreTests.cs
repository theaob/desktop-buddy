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

    [Fact]
    public void Fur_and_pattern_are_saved_by_name()
    {
        SettingsStore.Save(SettingsPath, new BuddySettings { Fur = FurColor.Ginger, Pattern = CatPattern.Socks });

        Assert.Contains("\"Ginger\"", File.ReadAllText(SettingsPath));
        Assert.Equal(new CatLook(FurColor.Ginger, CatPattern.Socks), SettingsStore.Load(SettingsPath).Look);
    }

    [Fact]
    public void Unknown_fur_or_pattern_falls_back_to_the_default_look()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, """{ "Fur": 42, "Pattern": 7 }""");

        Assert.Equal(default(CatLook), SettingsStore.Load(SettingsPath).Look);
    }

    [Fact]
    public void Home_spot_loads_back_and_is_empty_by_default()
    {
        Assert.Null(SettingsStore.Load(SettingsPath).HomeX);

        SettingsStore.Save(SettingsPath, new BuddySettings { HomeX = -1500, HomeY = 900 });
        var loaded = SettingsStore.Load(SettingsPath);

        Assert.Equal(-1500, loaded.HomeX);
        Assert.Equal(900, loaded.HomeY);
    }

    [Fact]
    public void Phase_two_settings_default_on_and_are_kept_in_range()
    {
        var defaults = SettingsStore.Load(SettingsPath);
        Assert.Equal(50, defaults.BreakEveryMinutes);
        Assert.Equal(25, defaults.FocusMinutes);
        Assert.True(defaults.ChaseMouse && defaults.WatchWindows && defaults.PlayWithYarn && defaults.ReactToCpu);

        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, """{ "BreakEveryMinutes": 999, "FocusMinutes": 1, "ChaseMouse": false }""");
        var loaded = SettingsStore.Load(SettingsPath);

        Assert.Equal(BuddySettings.MaxBreakEveryMinutes, loaded.BreakEveryMinutes);
        Assert.Equal(BuddySettings.MinFocusMinutes, loaded.FocusMinutes);
        Assert.False(loaded.ChaseMouse);
        Assert.True(loaded.WatchWindows);
    }
}
