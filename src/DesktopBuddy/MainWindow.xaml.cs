using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace DesktopBuddy;

/// <summary>
/// Phase 0 spike: a transparent, topmost window that stands on the taskbar and walks along it.
/// All placement is done in physical pixels with SetWindowPos, so DPI scaling can't skew it.
/// </summary>
public partial class MainWindow : Window
{
    private const int FrameMs = 66;                 // ~15 fps keeps CPU low
    private const int EnvironmentEveryNFrames = 4;  // re-read taskbar and fullscreen state ~4x a second
    private const double WalkSpeedDip = 1.5;        // DIPs per frame, scaled to pixels by the monitor DPI

    private readonly DispatcherTimer _timer = new(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(FrameMs) };
    private readonly Random _random = new();

    private IntPtr _hwnd;
    private int _frame;
    private TaskbarInfo? _taskbar;
    private string? _hideReason;
    private double _walkOffset = double.NaN;  // physical px from the left end of the walk zone
    private int _direction = 1;
    private bool _sitting;
    private DateTime _nextMoodChange = DateTime.Now;
    private string _lastLoggedState = "";
    private DiagnosticsWindow? _diagnostics;

    public MainWindow()
    {
        InitializeComponent();
        CatImage.Source = CatSprite.Frame(CatPose.Sit);
        SourceInitialized += OnSourceInitialized;
        _timer.Tick += OnTick;
    }

    internal string Diagnostics { get; private set; } = "";

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;

        // Tool window keeps it out of Alt+Tab; no-activate means clicking the cat never steals focus.
        int ex = Native.GetWindowLong(_hwnd, Native.GWL_EXSTYLE);
        ex = (ex | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE) & ~Native.WS_EX_APPWINDOW;
        Native.SetWindowLong(_hwnd, Native.GWL_EXSTYLE, ex);

        SpikeLog.Write($"Started. Windows {Environment.OSVersion.Version}, monitors: {Native.GetSystemMetrics(Native.SM_CMONITORS)}");
        RefreshEnvironment();
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_frame++ % EnvironmentEveryNFrames == 0)
            RefreshEnvironment();

        if (_hideReason != null)
        {
            if (Visibility == Visibility.Visible)
                Visibility = Visibility.Hidden;
            return;
        }
        if (Visibility != Visibility.Visible)
            Visibility = Visibility.Visible;

        bool walking = UpdateMood();
        Place(walking);
    }

    /// <returns>True when the cat should be walking this frame.</returns>
    private bool UpdateMood()
    {
        if (DateTime.Now >= _nextMoodChange)
        {
            _sitting = _random.NextDouble() < 0.3;
            _nextMoodChange = DateTime.Now.AddSeconds(_sitting ? _random.Next(3, 8) : _random.Next(4, 12));
        }

        bool horizontal = _taskbar is null || _taskbar.Edge is TaskbarEdge.Bottom or TaskbarEdge.Top;
        bool walking = horizontal && !_sitting && !PauseItem.IsChecked;

        var pose = walking ? ((_frame / 3) % 2 == 0 ? CatPose.WalkA : CatPose.WalkB) : CatPose.Sit;
        CatImage.Source = CatSprite.Frame(pose);
        Flip.ScaleX = _direction;
        return walking;
    }

    private void Place(bool walking)
    {
        if (!Native.GetWindowRect(_hwnd, out var me))
            return;

        var zone = WalkZone(me.Width, me.Height);
        int x = zone.Left, y = zone.Y;

        if (zone.Horizontal)
        {
            int span = Math.Max(0, zone.Right - zone.Left - me.Width);
            if (double.IsNaN(_walkOffset))
                _walkOffset = span * 0.5;

            if (walking)
            {
                double scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
                _walkOffset += _direction * WalkSpeedDip * scale;
                if (_walkOffset <= 0) { _walkOffset = 0; _direction = 1; }
                else if (_walkOffset >= span) { _walkOffset = span; _direction = -1; }
            }

            _walkOffset = Math.Clamp(_walkOffset, 0, span);
            x = zone.Left + (int)_walkOffset;
        }

        if (x != me.Left || y != me.Top)
            Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, x, y, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
    }

    /// <summary>Where the cat may stand, in physical pixels, given its window size.</summary>
    private (bool Horizontal, int Left, int Right, int Y) WalkZone(int width, int height)
    {
        if (_taskbar is not { } tb)
        {
            // No taskbar reported: stand on the bottom edge of the primary screen.
            var m = Native.MonitorBounds(Native.MonitorFromWindow(_hwnd, Native.MONITOR_DEFAULTTOPRIMARY)) ?? default;
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

    private void RefreshEnvironment()
    {
        _taskbar = TaskbarTracker.Query();
        _hideReason = FullscreenDetector.HideReason(_hwnd);

        var dpi = VisualTreeHelper.GetDpi(this);
        Native.GetWindowRect(_hwnd, out var me);

        string state = _taskbar is { } tb
            ? $"taskbar edge={tb.Edge} autoHide={tb.AutoHide} visible={tb.IsVisible} rect={tb.Bounds} docked={tb.DockedBounds} monitor={tb.Monitor}"
            : "taskbar not found";
        state += $" | dpiScale={dpi.DpiScaleX:0.00} | hide={_hideReason ?? "no"}";

        if (state != _lastLoggedState)
        {
            SpikeLog.Write(state);
            _lastLoggedState = state;
        }

        Diagnostics = string.Join(Environment.NewLine,
            $"Taskbar edge:    {(_taskbar?.Edge.ToString() ?? "not found")}",
            $"Auto-hide:       {YesNo(_taskbar?.AutoHide)}",
            $"Taskbar showing: {YesNo(_taskbar?.IsVisible)}",
            $"Taskbar now:     {_taskbar?.Bounds}",
            $"Taskbar docked:  {_taskbar?.DockedBounds}",
            $"Monitor:         {_taskbar?.Monitor}",
            $"Buddy window:    {me}",
            $"DPI scale:       {dpi.DpiScaleX:0.00} ({dpi.PixelsPerInchX:0} dpi)",
            $"Monitors:        {Native.GetSystemMetrics(Native.SM_CMONITORS)}",
            $"Hiding because:  {_hideReason ?? "nothing"}",
            $"Windows:         {Environment.OSVersion.Version}",
            $"Log file:        {SpikeLog.FilePath}");

        _diagnostics?.Refresh(Diagnostics);
    }

    private static string YesNo(bool? value) => value switch { true => "yes", false => "no", null => "?" };

    private void OnDiagnostics(object sender, RoutedEventArgs e)
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

    private void OnExit(object sender, RoutedEventArgs e)
    {
        SpikeLog.Write("Exited.");
        Close();
    }
}
