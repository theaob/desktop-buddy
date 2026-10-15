namespace DesktopBuddy.Core;

/// <summary>
/// Counts time spent at the PC and says when a break is due. Stepping away for a while counts
/// as a break and starts the count again.
/// </summary>
public sealed class BreakReminder
{
    /// <summary>Idle shorter than this still counts as working (reading, thinking).</summary>
    public const double StillWorkingIdleSeconds = 60;

    /// <summary>Away at least this long counts as a break.</summary>
    public const double BreakIdleSeconds = 5 * 60;

    private static readonly string[] Messages =
    {
        "Time for a break! Stretch your legs.",
        "Break time! Rest your eyes for a minute.",
        "You've been busy. Grab some water?",
        "Up you get! A short walk helps.",
    };

    private int _nextMessage;

    /// <summary>Minutes of work between reminders; 0 turns reminders off.</summary>
    public int EveryMinutes { get; set; } = 50;

    public double WorkedSeconds { get; private set; }

    /// <returns>A reminder to show, or null when none is due.</returns>
    public string? Tick(double elapsedSeconds, double userIdleSeconds)
    {
        if (userIdleSeconds >= BreakIdleSeconds || EveryMinutes <= 0)
        {
            WorkedSeconds = 0;
            return null;
        }
        if (userIdleSeconds < StillWorkingIdleSeconds)
            WorkedSeconds += elapsedSeconds;

        if (WorkedSeconds < EveryMinutes * 60)
            return null;

        WorkedSeconds = 0;
        return Messages[_nextMessage++ % Messages.Length];
    }
}
