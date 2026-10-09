using System.Windows.Interop;

namespace DesktopBuddy;

/// <summary>
/// Tells the cat when another app's window opens, finishes moving, or flashes for attention.
/// Uses the shell hook and a WinEvent hook, both documented notification APIs; neither watches
/// keyboard or mouse input.
/// </summary>
internal sealed class AppWatcher : IDisposable
{
    private readonly IntPtr _hwnd;
    private readonly HwndSource? _source;
    private readonly int _shellHookMessage;
    private readonly Native.WinEventProc _onWinEvent;  // kept in a field so the GC can't collect it while Windows still calls it
    private readonly IntPtr _winEventHook;
    private readonly uint _ownProcess = (uint)Environment.ProcessId;

    /// <summary>Raised with the window that did something.</summary>
    public event Action<IntPtr>? WindowActivity;

    public AppWatcher(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _shellHookMessage = Native.RegisterWindowMessage("SHELLHOOK");
        _source = HwndSource.FromHwnd(hwnd);
        _source?.AddHook(OnMessage);
        if (!Native.RegisterShellHookWindow(hwnd))
            BuddyLog.Write("Could not register for shell window events.");

        _onWinEvent = OnWinEvent;
        _winEventHook = Native.SetWinEventHook(Native.EVENT_SYSTEM_MOVESIZEEND, Native.EVENT_SYSTEM_MOVESIZEEND, IntPtr.Zero,
            _onWinEvent, 0, 0, Native.WINEVENT_OUTOFCONTEXT | Native.WINEVENT_SKIPOWNPROCESS);
    }

    public void Dispose()
    {
        Native.DeregisterShellHookWindow(_hwnd);
        _source?.RemoveHook(OnMessage);
        if (_winEventHook != IntPtr.Zero)
            Native.UnhookWinEvent(_winEventHook);
    }

    private IntPtr OnMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == _shellHookMessage && (int)wParam is Native.HSHELL_WINDOWCREATED or Native.HSHELL_FLASH)
            Report(lParam);
        return IntPtr.Zero;
    }

    private void OnWinEvent(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint thread, uint time)
    {
        if (idObject == 0)  // OBJID_WINDOW: the window itself, not a part of it
            Report(hwnd);
    }

    private void Report(IntPtr window)
    {
        if (window == IntPtr.Zero || !Native.IsWindowVisible(window))
            return;
        Native.GetWindowThreadProcessId(window, out uint process);
        if (process == _ownProcess)
            return;
        try
        {
            WindowActivity?.Invoke(window);
        }
        catch (Exception e)
        {
            // This runs inside a callback from Windows, where an escaping exception would end the app.
            BuddyLog.Write($"Window activity handler failed: {e.Message}");
        }
    }
}
