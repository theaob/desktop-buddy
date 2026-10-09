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

/// <summary>Reads the taskbar on a given monitor using documented shell APIs only.</summary>
internal static class TaskbarTracker
{
    // An auto-hide taskbar leaves a thin strip on screen; anything thinner than this counts as hidden.
    private const int MinVisibleThickness = 6;

    /// <returns>The taskbar on <paramref name="monitor"/>, or null when that monitor has none.</returns>
    public static TaskbarInfo? Query(IntPtr monitor)
    {
        var pos = new Native.APPBARDATA { cbSize = Marshal.SizeOf<Native.APPBARDATA>() };
        if (Native.SHAppBarMessage(Native.ABM_GETTASKBARPOS, ref pos) == UIntPtr.Zero)
            return null;

        var state = new Native.APPBARDATA { cbSize = Marshal.SizeOf<Native.APPBARDATA>() };
        bool autoHide = ((uint)Native.SHAppBarMessage(Native.ABM_GETSTATE, ref state) & Native.ABS_AUTOHIDE) != 0;

        var docked = pos.rc;
        var primaryMonitor = Native.MonitorFromRect(ref docked, Native.MONITOR_DEFAULTTOPRIMARY);
        if (monitor == primaryMonitor || monitor == IntPtr.Zero)
        {
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

            var bounds = Native.MonitorBounds(primaryMonitor) ?? pos.rc;
            return Build(actual, pos.rc, bounds, edge, autoHide);
        }

        // Other monitors get a Shell_SecondaryTrayWnd when "show taskbar on all displays" is on.
        // The shell reports no docked rectangle for these, so their window rect stands in for it.
        var monitorBounds = Native.MonitorBounds(monitor);
        if (monitorBounds is not { } m)
            return null;
        for (var w = Native.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Shell_SecondaryTrayWnd", null);
             w != IntPtr.Zero;
             w = Native.FindWindowEx(IntPtr.Zero, w, "Shell_SecondaryTrayWnd", null))
        {
            if (!Native.GetWindowRect(w, out var rect) || Native.MonitorFromRect(ref rect, Native.MONITOR_DEFAULTTONEAREST) != monitor)
                continue;
            return Build(rect, rect, m, EdgeOf(rect, m), autoHide);
        }
        return null;
    }

    private static TaskbarInfo Build(Native.RECT actual, Native.RECT docked, Native.RECT monitor, TaskbarEdge edge, bool autoHide)
    {
        int visibleThickness = edge switch
        {
            TaskbarEdge.Bottom => monitor.Bottom - actual.Top,
            TaskbarEdge.Top => actual.Bottom - monitor.Top,
            TaskbarEdge.Left => actual.Right - monitor.Left,
            _ => monitor.Right - actual.Left,
        };
        return new TaskbarInfo(actual, docked, monitor, edge, autoHide, visibleThickness >= MinVisibleThickness);
    }

    /// <summary>A wide bar sits on the top or bottom edge, a tall one on the left or right, whichever side it is nearer.</summary>
    internal static TaskbarEdge EdgeOf(Native.RECT bar, Native.RECT monitor)
    {
        if (bar.Width >= bar.Height)
            return bar.Top + bar.Bottom > monitor.Top + monitor.Bottom ? TaskbarEdge.Bottom : TaskbarEdge.Top;
        return bar.Left + bar.Right > monitor.Left + monitor.Right ? TaskbarEdge.Right : TaskbarEdge.Left;
    }
}
