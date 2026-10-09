using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using DesktopBuddy.Core;

namespace DesktopBuddy;

/// <summary>
/// The cat: a transparent, topmost window that stands on the taskbar. The behaviour engine decides
/// what it does; this window draws it and moves it. Placement uses SetWindowPos in physical pixels,
/// so DPI scaling can't skew it.
/// </summary>
public partial class MainWindow : Window
{
    private const int FrameMs = 66;                 // ~15 fps keeps CPU low
    private const int EnvironmentEveryNFrames = 4;  // re-read taskbar and fullscreen state ~4x a second
    private const int DragThresholdPx = 4;          // smaller movements count as a click
    private const double GravityDip = 1.2;          // DIPs per frame, added to the fall speed each frame

    private readonly BuddySettings _settings;
    private readonly BehaviorEngine _engine = new();
    private readonly DispatcherTimer _timer = new(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(FrameMs) };
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    private IntPtr _hwnd;
    private int _frame;
    private double _lastTickSeconds;
    private IntPtr _monitor;  // the monitor the cat lives on
    private TaskbarInfo? _taskbar;
    private string? _hideReason;
    private double _walkOffset = double.NaN;  // physical px from the left end of the walk zone
    private int _direction = 1;
    private string _lastLoggedState = "";
    private DiagnosticsWindow? _diagnostics;

    private bool _pressed;
    private Native.POINT _pressPoint;
    private Native.POINT _grabOffset;  // cursor position inside the window when the press began
    private double _fallY;
    private double _fallSpeed;

    public MainWindow(BuddySettings settings)
    {
        _settings = settings;
        InitializeComponent();
        CatImage.Source = CatSprite.Frame(CatPose.Sit, settings.Look);
        SourceInitialized += OnSourceInitialized;
        _timer.Tick += OnTick;
    }

    /// <summary>Hidden from the tray menu; separate from hiding for fullscreen apps.</summary>
    internal bool UserHidden { get; set; }

    internal string Diagnostics { get; private set; } = "";

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;

        // Tool window keeps it out of Alt+Tab; no-activate means clicking the cat never steals focus.
        int ex = Native.GetWindowLong(_hwnd, Native.GWL_EXSTYLE);
        ex = (ex | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE) & ~Native.WS_EX_APPWINDOW;
        Native.SetWindowLong(_hwnd, Native.GWL_EXSTYLE, ex);

        _monitor = HomeMonitor();
        BuddyLog.Write($"Started. Windows {Environment.OSVersion.Version}, monitors: {Native.GetSystemMetrics(Native.SM_CMONITORS)}");
        RefreshEnvironment();
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        double now = _clock.Elapsed.TotalSeconds;
        double elapsed = Math.Min(now - _lastTickSeconds, 0.5);  // a long stall shouldn't fast-forward the cat
        _lastTickSeconds = now;

        if (_frame++ % EnvironmentEveryNFrames == 0)
            RefreshEnvironment();

        bool held = _engine.State == BuddyState.Dragged;
        if ((UserHidden || _hideReason != null) && !held)
        {
            if (Visibility == Visibility.Visible)
                Visibility = Visibility.Hidden;
            return;
        }
        if (Visibility != Visibility.Visible)
            Visibility = Visibility.Visible;

        _engine.NapAfterSeconds = _settings.NapAfterMinutes * 60;
        _engine.WalkingAllowed = !_settings.Paused && IsHorizontalTaskbar;
        _engine.Tick(elapsed, Native.UserIdleSeconds());

        CatImage.Source = CatSprite.Frame(CatAnimation.PoseFor(_engine.State, _engine.TimeInState), _settings.Look);
        Flip.ScaleX = _direction;
        Move();
    }

    private bool IsHorizontalTaskbar => _taskbar is null || _taskbar.Edge is TaskbarEdge.Bottom or TaskbarEdge.Top;

    private void Move()
    {
        if (_engine.State == BuddyState.Dragged)
            return;  // follows the mouse instead
        if (!Native.GetWindowRect(_hwnd, out var me))
            return;

        double scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        var zone = WalkZone(me.Width, me.Height);
        int x = zone.Left, y = zone.Y;

        if (zone.Horizontal)
        {
            int span = Math.Max(0, zone.Right - zone.Left - me.Width);
            if (double.IsNaN(_walkOffset))
                _walkOffset = span * 0.5;

            if (_engine.State == BuddyState.Walk)
            {
                _walkOffset += _direction * _settings.WalkSpeed * scale;
                if (_walkOffset <= 0) { _walkOffset = 0; _direction = 1; }
                else if (_walkOffset >= span) { _walkOffset = span; _direction = -1; }
            }

            _walkOffset = Math.Clamp(_walkOffset, 0, span);
            x = zone.Left + (int)_walkOffset;
        }

        if (_engine.State == BuddyState.Falling)
        {
            _fallSpeed += GravityDip * scale;
            _fallY += _fallSpeed;
            // Dropped below its spot (say, onto the taskbar)? It hops back up, which also counts as landing.
            if (_fallY >= y || !zone.Horizontal)
                _engine.Landed();
            else
                y = (int)_fallY;
        }

        if (x != me.Left || y != me.Top)
            Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, x, y, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
    }

    /// <summary>Where the cat may stand, in physical pixels, given its window size.</summary>
    private (bool Horizontal, int Left, int Right, int Y) WalkZone(int width, int height)
    {
        if (_taskbar is not { } tb)
        {
            // No taskbar on this monitor: stand on its bottom edge.
            var m = Native.MonitorBounds(_monitor) ?? default;
            return (true, m.Left, m.Right, m.Bottom - height);
        }

        var mon = tb.Monitor;
        return tb.Edge switch
        {
            // Stand on top of the taskbar; while an auto-hide taskbar is tucked away, stand on the screen edge.
            TaskbarEdge.Bottom => (true, tb.DockedBounds.Left, tb.DockedBounds.Right, (tb.IsVisible ? tb.Bounds.Top : mon.Bottom) - height),
            // Hang just under a top taskbar.
            TaskbarEdge.Top => (true, tb.DockedBounds.Left, tb.DockedBounds.Right, tb.IsVisible ? tb.Bounds.Bottom : mon.Top),
            // Side taskbars: sit beside it near the bottom. Walking there is out of scope for v1.
            TaskbarEdge.Left => (false, tb.IsVisible ? tb.Bounds.Right : mon.Left, 0, mon.Bottom - height),
            _ => (false, (tb.IsVisible ? tb.Bounds.Left : mon.Right) - width, 0, mon.Bottom - height),
        };
    }

    // ---- Click to pet, drag to move ----

    private void OnCatMouseDown(object sender, MouseButtonEventArgs e)
    {
        Native.GetCursorPos(out _pressPoint);
        Native.GetWindowRect(_hwnd, out var me);
        _grabOffset = new Native.POINT { X = _pressPoint.X - me.Left, Y = _pressPoint.Y - me.Top };
        _pressed = true;
        CatImage.CaptureMouse();
        e.Handled = true;
    }

    private void OnCatMouseMove(object sender, MouseEventArgs e)
    {
        if (!_pressed)
            return;

        Native.GetCursorPos(out var p);
        if (_engine.State != BuddyState.Dragged)
        {
            if (Math.Abs(p.X - _pressPoint.X) < DragThresholdPx && Math.Abs(p.Y - _pressPoint.Y) < DragThresholdPx)
                return;
            _engine.BeginDrag();
        }

        Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, p.X - _grabOffset.X, p.Y - _grabOffset.Y, 0, 0,
            Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
    }

    private void OnCatMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_pressed)
            return;
        _pressed = false;
        CatImage.ReleaseMouseCapture();

        if (_engine.State == BuddyState.Dragged)
            DropCat();
        else
            _engine.Pet();
        e.Handled = true;
    }

    private void OnCatLostCapture(object sender, MouseEventArgs e)
    {
        // Capture can be taken away mid-drag (Alt+Tab, a UAC prompt); drop the cat where it is.
        if (!_pressed)
            return;
        _pressed = false;
        if (_engine.State == BuddyState.Dragged)
            DropCat();
    }

    private void DropCat()
    {
        _engine.EndDrag();
        Native.GetWindowRect(_hwnd, out var me);

        // The cat now lives on whichever monitor it was dropped on, and comes back there next time.
        _monitor = Native.MonitorFromWindow(_hwnd, Native.MONITOR_DEFAULTTONEAREST);
        _taskbar = TaskbarTracker.Query(_monitor);
        _settings.HomeX = (me.Left + me.Right) / 2;
        _settings.HomeY = (me.Top + me.Bottom) / 2;
        CurrentApp.SaveSettings();

        var zone = WalkZone(me.Width, me.Height);
        _walkOffset = me.Left - zone.Left;  // walk on from wherever it was dropped
        _fallY = me.Top;
        _fallSpeed = 0;
    }

    // ---- Environment and diagnostics ----

    private void RefreshEnvironment()
    {
        if (Native.MonitorBounds(_monitor) is null)
            _monitor = HomeMonitor();  // its monitor was unplugged
        _taskbar = TaskbarTracker.Query(_monitor);
        _hideReason = FullscreenDetector.HideReason(_hwnd, _monitor);

        var dpi = VisualTreeHelper.GetDpi(this);
        Native.GetWindowRect(_hwnd, out var me);

        string state = _taskbar is { } tb
            ? $"taskbar edge={tb.Edge} autoHide={tb.AutoHide} visible={tb.IsVisible} rect={tb.Bounds} docked={tb.DockedBounds} monitor={tb.Monitor}"
            : "taskbar not found";
        state += $" | dpiScale={dpi.DpiScaleX:0.00} | hide={_hideReason ?? "no"}";

        if (state != _lastLoggedState)
        {
            BuddyLog.Write(state);
            _lastLoggedState = state;
        }

        Diagnostics = string.Join(Environment.NewLine,
            $"Buddy state:     {_engine.State}{(UserHidden ? " (hidden by you)" : "")}",
            $"You idle for:    {Native.UserIdleSeconds():0} s (naps after {_settings.NapAfterMinutes} min)",
            $"Taskbar edge:    {(_taskbar?.Edge.ToString() ?? "not found")}",
            $"Auto-hide:       {YesNo(_taskbar?.AutoHide)}",
            $"Taskbar showing: {YesNo(_taskbar?.IsVisible)}",
            $"Taskbar now:     {_taskbar?.Bounds}",
            $"Taskbar docked:  {_taskbar?.DockedBounds}",
            $"Cat's monitor:   {Native.MonitorBounds(_monitor)}",
            $"Taskbar monitor: {_taskbar?.Monitor}",
            $"Buddy window:    {me}",
            $"DPI scale:       {dpi.DpiScaleX:0.00} ({dpi.PixelsPerInchX:0} dpi)",
            $"Monitors:        {Native.GetSystemMetrics(Native.SM_CMONITORS)}",
            $"Hiding because:  {_hideReason ?? "nothing"}",
            $"Windows:         {Environment.OSVersion.Version}",
            $"Log file:        {BuddyLog.FilePath}");

        _diagnostics?.Refresh(Diagnostics);
    }

    /// <summary>The monitor the cat was last dropped on, or the primary one if that's gone.</summary>
    private IntPtr HomeMonitor()
    {
        // With no saved spot, (0, 0) is always on the primary monitor.
        var home = new Native.POINT { X = _settings.HomeX ?? 0, Y = _settings.HomeY ?? 0 };
        return Native.MonitorFromPoint(home, Native.MONITOR_DEFAULTTOPRIMARY);
    }

    private static string YesNo(bool? value) => value switch { true => "yes", false => "no", null => "?" };

    internal void ShowDiagnostics()
    {
        if (_diagnostics is null)
        {
            _diagnostics = new DiagnosticsWindow();
            _diagnostics.Closed += (_, _) => _diagnostics = null;
        }
        _diagnostics.Refresh(Diagnostics);
        _diagnostics.Show();
        _diagnostics.Activate();
    }

    // ---- Right-click menu ----

    private static App CurrentApp => (App)Application.Current;

    private void OnMenuOpened(object sender, RoutedEventArgs e) => PauseItem.IsChecked = _settings.Paused;

    private void OnSettings(object sender, RoutedEventArgs e) => CurrentApp.ShowSettings();

    private void OnPause(object sender, RoutedEventArgs e)
    {
        _settings.Paused = PauseItem.IsChecked;
        CurrentApp.SaveSettings();
    }

    private void OnHide(object sender, RoutedEventArgs e) => UserHidden = true;

    private void OnDiagnostics(object sender, RoutedEventArgs e) => ShowDiagnostics();

    private void OnExit(object sender, RoutedEventArgs e) => CurrentApp.Quit();
}
