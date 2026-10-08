using System.Text.Json;

namespace DesktopBuddy.Core;

public sealed class BuddySettings
{
    public const double MinWalkSpeed = 0.5;
    public const double MaxWalkSpeed = 3;
    public const int MinNapAfterMinutes = 1;
    public const int MaxNapAfterMinutes = 30;

    /// <summary>Device-independent pixels per animation frame.</summary>
    public double WalkSpeed { get; set; } = 1.5;

    public int NapAfterMinutes { get; set; } = 5;

    public bool Paused { get; set; }

    public BuddySettings Normalized() => new()
    {
        WalkSpeed = double.IsFinite(WalkSpeed) ? Math.Clamp(WalkSpeed, MinWalkSpeed, MaxWalkSpeed) : 1.5,
        NapAfterMinutes = Math.Clamp(NapAfterMinutes, MinNapAfterMinutes, MaxNapAfterMinutes),
        Paused = Paused,
    };
}

public static class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <summary>Reads settings, falling back to defaults when the file is missing or unreadable.</summary>
    public static BuddySettings Load(string path)
    {
        try
        {
            if (!File.Exists(path))
                return new BuddySettings();
            var settings = JsonSerializer.Deserialize<BuddySettings>(File.ReadAllText(path), Options);
            return (settings ?? new BuddySettings()).Normalized();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return new BuddySettings();
        }
    }

    /// <summary>Writes to a temp file first so a crash mid-write can't leave a half-written file.</summary>
    public static void Save(string path, BuddySettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings.Normalized(), Options));
        File.Move(temp, path, overwrite: true);
    }
}
