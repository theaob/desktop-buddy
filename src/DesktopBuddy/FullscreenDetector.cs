namespace DesktopBuddy;

/// <summary>Decides when the buddy should get out of the way (games, videos, presentations).</summary>
internal static class FullscreenDetector
{
    private static readonly HashSet<string> ShellClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd",
    };

    /// <returns>Why the buddy should hide, or null when it can stay.</returns>
    public static string? HideReason(IntPtr ownWindow)
    {
        if (Native.SHQueryUserNotificationState(out int state) == 0)
        {
            switch (state)
            {
                case Native.QUNS_RUNNING_D3D_FULL_SCREEN: return "fullscreen game (Direct3D)";
                case Native.QUNS_PRESENTATION_MODE: return "presentation mode";
                case Native.QUNS_BUSY: return "fullscreen app (busy)";
            }
        }

        // Borderless fullscreen windows (browser video, many games) don't always set the
        // notification state, so also check whether the foreground window covers its monitor.
        var fg = Native.GetForegroundWindow();
        if (fg == IntPtr.Zero || fg == ownWindow || fg == Native.GetShellWindow() || fg == Native.GetDesktopWindow())
            return null;
        if (ShellClasses.Contains(Native.ClassName(fg)))
            return null;
        if (!Native.GetWindowRect(fg, out var rect))
            return null;

        var monitor = Native.MonitorBounds(Native.MonitorFromWindow(fg, Native.MONITOR_DEFAULTTONEAREST));
        if (monitor is not { } m)
            return null;

        bool coversMonitor = rect.Left <= m.Left && rect.Top <= m.Top && rect.Right >= m.Right && rect.Bottom >= m.Bottom;
        return coversMonitor ? "fullscreen window" : null;
    }
}
