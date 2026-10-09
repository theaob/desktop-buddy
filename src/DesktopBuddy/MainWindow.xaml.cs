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
    private const int CpuEveryNFrames = 15;         // sample CPU load about once a second

    // Hunting: the mouse counts as close when it's this near, in DIPs, and above the cat.
    private const double HuntReachDip = 130;
    private const double HuntHeightDip = 140;
    private const double MaxLeapDip = 110;

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
    private double _leapSpeedX;  // sideways physical px per frame during a pounce

    private readonly BreakReminder _breaks = new();
    private readonly FocusTimer _focus = new();
    private readonly CpuMonitor _cpu = new();
    private readonly YarnBall _yarn = new();
    private readonly Random _random = new();
    private BubbleWindow? _bubble;
    private YarnWindow? _yarnWindow;
    private AppWatcher? _watcher;
    private BuddyState _lastState;
    private bool _moving;            // walked this frame; standing still shows a different pose
    private double _lastKickSeconds;
    private bool _focusSignHidden;   // the user clicked the focus countdown away
    private Native.POINT _cursor;

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
        Native.MakeToolWindow(_hwnd);

        _monitor = HomeMonitor();

        _bubble = new BubbleWindow();
        _bubble.Dismissed += () => _focusSignHidden = _focus.IsRunning;
        _yarnWindow = new YarnWindow();
        _yarnWindow.Flicked += direction =>
            _yarn.Kick(direction * _random.Next(300, 500) * VisualTreeHelper.GetDpi(this).DpiScaleX);
        _watcher = new AppWatcher(_hwnd);
        _watcher.WindowActivity += OnWindowActivity;
        Closed += (_, _) =>
        {
            _watcher?.Dispose();
            _bubble?.Close();
            _yarnWindow?.Close();
        };

        BuddyLog.Write($"Started. Windows {Environment.OSVersion.Version}, monitors: {Native.GetSystemMetrics(Native.SM_CMONITORS)}");
        RefreshEnvironment();
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        double now = _clock.Elapsed.TotalSeconds;
        double elapsed = Math.Min(now - _lastTickSeconds, 0.5);  // a long stall shouldn't fast-forward the cat
        _lastTickSeconds = now;

        if (_frame % EnvironmentEveryNFrames == 0)
            RefreshEnvironment();
        if (_frame % CpuEveryNFrames == 0 && Native.GetSystemTimes(out ulong idle, out ulong kernel, out ulong user))
            _cpu.Sample(idle, kernel, user);
        _frame++;

        double userIdle = Native.UserIdleSeconds();
        TickReminders(elapsed, userIdle);

        bool held = _engine.State == BuddyState.Dragged;
        if ((UserHidden || _hideReason != null) && !held)
        {
            if (Visibility == Visibility.Visible)
                Visibility = Visibility.Hidden;
            _yarnWindow?.Hide();
            _bubble?.Follow(0, 0, catVisible: false);
            return;
        }
        if (Visibility != Visibility.Visible)
            Visibility = Visibility.Visible;

        Native.GetCursorPos(out _cursor);
        Native.GetWindowRect(_hwnd, out var me);
        double scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        bool cursorOnMonitor = Native.MonitorBounds(_monitor) is { } m
            && _cursor.X >= m.Left && _cursor.X < m.Right && _cursor.Y >= m.Top && _cursor.Y < m.Bottom;
        double cursorAbove = (me.Top - _cursor.Y) / scale;
        double cursorAside = (_cursor.X - (me.Left + me.Width / 2)) / scale;

        _engine.NapAfterSeconds = _settings.NapAfterMinutes * 60;
        _engine.WalkingAllowed = !_settings.Paused && IsHorizontalTaskbar;
        _engine.MouseGamesAllowed = _settings.ChaseMouse && !_focus.IsRunning;
        _engine.PlayAllowed = _settings.PlayWithYarn && !_focus.IsRunning;
        _engine.CursorOnMonitor = cursorOnMonitor;
        _engine.CursorNear = cursorOnMonitor && Math.Abs(cursorAside) < HuntReachDip && cursorAbove > 4 && cursorAbove < HuntHeightDip;
        _engine.Tick(elapsed, userIdle);

        if (_engine.State != _lastState)
        {
            OnStateChanged(_lastState, _engine.State);
            _lastState = _engine.State;
        }

        Move(elapsed);

        bool sweaty = _settings.ReactToCpu && _cpu.Busy;
        var pose = CatAnimation.PoseFor(_engine.State, _engine.TimeInState, _moving, _engine.Grip);
        CatImage.Source = CatSprite.Frame(pose, _settings.Look, sweaty);
        Flip.ScaleX = _direction;

        if (Native.GetWindowRect(_hwnd, out me))
            _bubble?.Follow((me.Left + me.Right) / 2, me.Top, catVisible: true);
    }

    /// <summary>Break reminders and the focus countdown, which keep counting while the cat is hidden.</summary>
    private void TickReminders(double elapsed, double userIdle)
    {
        if (_bubble is null)
            return;

        _breaks.EveryMinutes = _settings.BreakEveryMinutes;
        string? reminder = _breaks.Tick(elapsed, userIdle);
        if (reminder != null && !_focus.IsRunning)
        {
            BuddyLog.Write("Break reminder shown.");
            _bubble.Say(reminder, 30);
        }

        if (_focus.Tick(elapsed))
        {
            BuddyLog.Write("Focus session finished.");
            _engine.Celebrate();
            _bubble.Say("Focus session done. Nice work!", 10);
        }
        else if (_focus.IsRunning && !_focusSignHidden)
        {
            string sign = $"Focus {_focus.Display}";
            if (_bubble.Message != sign)
                _bubble.Say(sign, null);
        }
    }

    private void OnStateChanged(BuddyState from, BuddyState to)
    {
        if (from == BuddyState.Play)
            _yarnWindow?.Hide();

        if (!Native.GetWindowRect(_hwnd, out var me))
            return;
        double scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        int catCenter = (me.Left + me.Right) / 2;

        switch (to)
        {
            case BuddyState.Stalk:
                _direction = _cursor.X >= catCenter ? 1 : -1;  // eyes on the prey
                break;

            case BuddyState.Pounce:
            {
                // Leap so the cat's middle reaches the mouse's height, landing about where the mouse is.
                double gravity = GravityDip * scale;
                double rise = Math.Clamp(me.Top + me.Height / 2.0 - _cursor.Y, 16 * scale, MaxLeapDip * scale);
                double launch = Math.Sqrt(2 * gravity * rise);
                double frames = 2 * launch / gravity;
                _fallY = me.Top;
                _fallSpeed = -launch;
                _leapSpeedX = Math.Clamp((_cursor.X - catCenter) / frames, -6 * scale, 6 * scale);
                _direction = _cursor.X >= catCenter ? 1 : -1;
                break;
            }

            case BuddyState.Play:
            {
                // The ball turns up a little way off and rolls toward the cat.
                var zone = WalkZone(me.Width, me.Height);
                int ballWidth = (int)(YarnBall.Size * 2 * scale);
                double max = Math.Max(0, zone.Right - zone.Left - ballWidth);
                double side = _random.Next(2) == 0 ? -1 : 1;
                double catOffset = me.Left - zone.Left + me.Width / 2.0;
                double start = Math.Clamp(catOffset + side * _random.Next(120, 260) * scale, 0, max);
                _yarn.Place(start);
                _yarn.Kick(Math.Sign(catOffset - start) * 120 * scale);
                _lastKickSeconds = 0;
                break;
            }
        }
    }

    private void OnWindowActivity(IntPtr window)
    {
        if (!_settings.WatchWindows || UserHidden || _hideReason != null)
            return;
        if (Native.MonitorFromWindow(window, Native.MONITOR_DEFAULTTONEAREST) != _monitor)
            return;
        if (!Native.GetWindowRect(window, out var rect) || !Native.GetWindowRect(_hwnd, out var me))
            return;
        if (_engine.Notice())
            _direction = (rect.Left + rect.Right) / 2 >= (me.Left + me.Right) / 2 ? 1 : -1;
    }

    private bool IsHorizontalTaskbar => _taskbar is null || _taskbar.Edge is TaskbarEdge.Bottom or TaskbarEdge.Top;

    private void Move(double elapsed)
    {
        _moving = false;
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

            // A busy PC makes the cat hurry.
            double speed = _settings.WalkSpeed * scale * (_settings.ReactToCpu && _cpu.Busy ? 2 : 1);
            switch (_engine.State)
            {
                case BuddyState.Walk:
                    _walkOffset += _direction * speed;
                    if (_walkOffset <= 0) { _walkOffset = 0; _direction = 1; }
                    else if (_walkOffset >= span) { _walkOffset = span; _direction = -1; }
                    _moving = true;
                    break;
                case BuddyState.Follow:
                    StepToward(_cursor.X - zone.Left - me.Width / 2.0, speed * 1.3, 8 * scale);
                    break;
                case BuddyState.Play:
                    PlayWithYarn(elapsed, zone.Left, zone.Right, zone.Y + me.Height, me.Width, speed * 1.5, scale);
                    break;
                case BuddyState.Pounce:
                    _walkOffset += _leapSpeedX;
                    break;
            }

            _walkOffset = Math.Clamp(_walkOffset, 0, span);
            x = zone.Left + (int)_walkOffset;
        }

        if (_engine.State is BuddyState.Falling or BuddyState.Pounce)
        {
            _fallSpeed += GravityDip * scale;
            _fallY += _fallSpeed;
            // Dropped below its spot (say, onto the taskbar)? It hops back up, which also counts as landing.
            if (_fallY >= y || !zone.Horizontal)
                _engine.Landed();
            else
                y = (int)_fallY;
        }
        else if (_engine.State == BuddyState.Celebrate)
        {
            y -= (int)(Math.Abs(Math.Sin(_engine.TimeInState * Math.PI * 2.5)) * 10 * scale);  // happy hops
        }

        if (x != me.Left || y != me.Top)
            Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, x, y, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
    }

    /// <summary>Walks toward a spot in the walk zone, stopping once within <paramref name="close"/>.</summary>
    private void StepToward(double target, double speed, double close)
    {
        double gap = target - _walkOffset;
        if (Math.Abs(gap) <= close)
            return;
        _direction = gap > 0 ? 1 : -1;
        _walkOffset += _direction * Math.Min(speed, Math.Abs(gap));
        _moving = true;
    }

    /// <summary>Chases the ball and bats it along when it's within reach. Positions are physical pixels.</summary>
    private void PlayWithYarn(double elapsed, int left, int right, int bottom, int catWidth, double speed, double scale)
    {
        if (_yarnWindow is null)
            return;
        int ballWidth = (int)(YarnBall.Size * 2 * scale);
        _yarn.Tick(elapsed, 0, Math.Max(0, right - left - ballWidth));
        _yarnWindow.Place(left + (int)_yarn.X, bottom, _yarn.Frame(4 * scale));

        double gap = _yarn.X + ballWidth / 2.0 - (_walkOffset + catWidth / 2.0);
        double reach = catWidth * 0.45;
        if (Math.Abs(gap) > reach)
        {
            StepToward(_walkOffset + gap - Math.Sign(gap) * reach * 0.8, speed, 0);
            return;
        }

        // In reach: paw at it (the standing pose is a bat), then send it rolling.
        if (gap != 0)
            _direction = gap > 0 ? 1 : -1;
        if (Math.Abs(_yarn.Velocity) < 60 * scale && _engine.TimeInState - _lastKickSeconds > 0.4)
        {
            _yarn.Kick(_direction * _random.Next(150, 380) * scale);
            _lastKickSeconds = _engine.TimeInState;
        }
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
            // Grabbed near the head it hangs by the scruff; lower down you're holding it under the belly.
            _engine.BeginDrag(_grabOffset.Y < ActualPixelHeight() * 0.45 ? DragGrip.Scruff : DragGrip.Belly);
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

    private int ActualPixelHeight() => Native.GetWindowRect(_hwnd, out var me) ? me.Height : 1;

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
            $"CPU load:        {_cpu.Load:P0}{(_cpu.Busy ? " (busy)" : "")}",
            $"Focus timer:     {(_focus.IsRunning ? _focus.Display + " left" : "off")}",
            $"Since a break:   {_breaks.WorkedSeconds / 60:0} min of use (reminds every {_settings.BreakEveryMinutes} min)",
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

    private void OnMenuOpened(object sender, RoutedEventArgs e)
    {
        PauseItem.IsChecked = _settings.Paused;
        FocusItem.Header = FocusMenuText;
    }

    internal string FocusMenuText => _focus.IsRunning ? "Stop focus timer" : $"Start focus timer ({_settings.FocusMinutes} min)";

    internal void ToggleFocus()
    {
        if (_focus.IsRunning)
        {
            _focus.Stop();
            _bubble?.Clear();
            BuddyLog.Write("Focus session stopped.");
            return;
        }
        _focus.Start(_settings.FocusMinutes);
        _focusSignHidden = false;
        BuddyLog.Write($"Focus session started ({_settings.FocusMinutes} min).");
    }

    private void OnFocus(object sender, RoutedEventArgs e) => ToggleFocus();

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
