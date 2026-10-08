using System.Runtime.InteropServices;

namespace DesktopBuddy;

internal enum TaskbarEdge { Left, Top, Right, Bottom }

/// <param name="Bounds">Where the taskbar actually is right now (physical pixels).</param>
/// <param name="DockedBounds">Where the taskbar sits when shown, as reported by the shell.</param>
/// <param name="IsVisible">False while an auto-hide taskbar is tucked off-screen.</param>
internal sealed record TaskbarInfo(
    Native.RECT Bounds,
    Native.RECT DockedBounds,
    Native.RECT Monitor,
    TaskbarEdge Edge,
    bool AutoHide,
    bool IsVisible);

/// <summary>Reads the primary taskbar's position using documented shell APIs only.</summary>
internal static class TaskbarTracker
{
    // An auto-hide taskbar leaves a thin strip on screen; anything thinner than this counts as hidden.
    private const int MinVisibleThickness = 6;

    public static TaskbarInfo? Query()
    {
        var pos = new Native.APPBARDATA { cbSize = Marshal.SizeOf<Native.APPBARDATA>() };
        if (Native.SHAppBarMessage(Native.ABM_GETTASKBARPOS, ref pos) == UIntPtr.Zero)
            return null;

        var state = new Native.APPBARDATA { cbSize = Marshal.SizeOf<Native.APPBARDATA>() };
        bool autoHide = ((uint)Native.SHAppBarMessage(Native.ABM_GETSTATE, ref state) & Native.ABS_AUTOHIDE) != 0;

        var edge = pos.uEdge switch
        {
            Native.ABE_LEFT => TaskbarEdge.Left,
            Native.ABE_TOP => TaskbarEdge.Top,
            Native.ABE_RIGHT => TaskbarEdge.Right,
            _ => TaskbarEdge.Bottom,
        };

        // ABM_GETTASKBARPOS reports the docked rectangle even while an auto-hide taskbar is
        // tucked away, so read the taskbar window itself for where it is right now.
        var actual = pos.rc;
        var tray = Native.FindWindow("Shell_TrayWnd", null);
        if (tray != IntPtr.Zero && Native.GetWindowRect(tray, out var trayRect))
            actual = trayRect;

        var docked = pos.rc;
        var monitor = Native.MonitorBounds(Native.MonitorFromRect(ref docked, Native.MONITOR_DEFAULTTOPRIMARY))
                      ?? pos.rc;

        int visibleThickness = edge switch
        {
            TaskbarEdge.Bottom => monitor.Bottom - actual.Top,
            TaskbarEdge.Top => actual.Bottom - monitor.Top,
            TaskbarEdge.Left => actual.Right - monitor.Left,
            _ => monitor.Right - actual.Left,
        };

        return new TaskbarInfo(actual, pos.rc, monitor, edge, autoHide, visibleThickness >= MinVisibleThickness);
    }
}
