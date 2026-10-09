namespace DesktopBuddy.Core;

/// <summary>A pomodoro-style countdown the cat holds up while you work.</summary>
public sealed class FocusTimer
{
    public bool IsRunning { get; private set; }

    public double RemainingSeconds { get; private set; }

    public void Start(int minutes)
    {
        RemainingSeconds = Math.Max(1, minutes) * 60;
        IsRunning = true;
    }

    public void Stop()
    {
        IsRunning = false;
        RemainingSeconds = 0;
    }

    /// <returns>True once, on the tick the session finishes.</returns>
    public bool Tick(double elapsedSeconds)
    {
        if (!IsRunning)
            return false;
        RemainingSeconds -= elapsedSeconds;
        if (RemainingSeconds > 0)
            return false;
        Stop();
        return true;
    }

    /// <summary>Time left as m:ss, rounded up so it never shows 0:00 while running.</summary>
    public string Display
    {
        get
        {
            int total = (int)Math.Ceiling(Math.Max(0, RemainingSeconds));
            return $"{total / 60}:{total % 60:00}";
        }
    }
}
