using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopBuddy.Core;

public sealed class BuddySettings
{
    public const double MinWalkSpeed = 0.5;
    public const double MaxWalkSpeed = 3;
    public const int MinNapAfterMinutes = 1;
    public const int MaxNapAfterMinutes = 30;
    public const int MaxBreakEveryMinutes = 120;
    public const int MinFocusMinutes = 5;
    public const int MaxFocusMinutes = 90;

    /// <summary>Device-independent pixels per animation frame.</summary>
    public double WalkSpeed { get; set; } = 1.5;

    public int NapAfterMinutes { get; set; } = 5;

    public bool Paused { get; set; }

    /// <summary>Minutes of work between break reminders; 0 turns them off.</summary>
    public int BreakEveryMinutes { get; set; } = 50;

    public int FocusMinutes { get; set; } = 25;

    /// <summary>Sometimes follow the mouse along the taskbar and pounce on it.</summary>
    public bool ChaseMouse { get; set; } = true;

    /// <summary>Turn to look at windows that open, move or flash.</summary>
    public bool WatchWindows { get; set; } = true;

    public bool PlayWithYarn { get; set; } = true;

    /// <summary>Hurry and sweat when the PC is busy.</summary>
    public bool ReactToCpu { get; set; } = true;

    public FurColor Fur { get; set; }

    public CatPattern Pattern { get; set; }

    /// <summary>A point on the monitor the cat was last dropped on (physical pixels), so it comes back there.</summary>
    public int? HomeX { get; set; }

    public int? HomeY { get; set; }

    [JsonIgnore]
    public CatLook Look => new(Fur, Pattern);

    public BuddySettings Normalized() => new()
    {
        WalkSpeed = double.IsFinite(WalkSpeed) ? Math.Clamp(WalkSpeed, MinWalkSpeed, MaxWalkSpeed) : 1.5,
        NapAfterMinutes = Math.Clamp(NapAfterMinutes, MinNapAfterMinutes, MaxNapAfterMinutes),
        Paused = Paused,
        BreakEveryMinutes = Math.Clamp(BreakEveryMinutes, 0, MaxBreakEveryMinutes),
        FocusMinutes = Math.Clamp(FocusMinutes, MinFocusMinutes, MaxFocusMinutes),
        ChaseMouse = ChaseMouse,
        WatchWindows = WatchWindows,
        PlayWithYarn = PlayWithYarn,
        ReactToCpu = ReactToCpu,
        Fur = Enum.IsDefined(Fur) ? Fur : default,
        Pattern = Enum.IsDefined(Pattern) ? Pattern : default,
        HomeX = HomeX,
        HomeY = HomeY,
    };
}

public static class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

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
