using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DesktopBuddy;

/// <summary>A speech bubble that floats above the cat. Click it to dismiss.</summary>
internal sealed class BubbleWindow : Window
{
    private readonly TextBlock _text;
    private IntPtr _hwnd;
    private DateTime _hideAt = DateTime.MaxValue;

    public BubbleWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        Cursor = System.Windows.Input.Cursors.Hand;
        Left = Top = -10000;  // off-screen until the first Follow places it

        var ink = new SolidColorBrush(Color.FromRgb(0x2A, 0x24, 0x33));
        var paper = new SolidColorBrush(Color.FromRgb(0xFF, 0xFD, 0xF7));

        _text = new TextBlock
        {
            Foreground = ink,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 220,
            TextAlignment = TextAlignment.Center,
        };
        var box = new Border
        {
            Background = paper,
            BorderBrush = ink,
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 6, 10, 6),
            Child = _text,
        };
        var tail = new Polygon
        {
            Points = new PointCollection { new Point(0, 0), new Point(12, 0), new Point(6, 7) },
            Fill = paper,
            Stroke = ink,
            StrokeThickness = 1.5,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, -1.5, 0, 0),
        };
        var panel = new StackPanel { Margin = new Thickness(2) };
        panel.Children.Add(box);
        panel.Children.Add(tail);
        Content = panel;

        SourceInitialized += (_, _) =>
        {
            _hwnd = new WindowInteropHelper(this).Handle;
            Native.MakeToolWindow(_hwnd);
        };
        MouseLeftButtonUp += (_, _) => Dismiss();
    }

    /// <summary>The message currently shown, or null when hidden.</summary>
    public string? Message { get; private set; }

    /// <summary>Raised when the user clicks the bubble away.</summary>
    public event Action? Dismissed;

    /// <summary>Shows a message, for a while or until replaced when <paramref name="seconds"/> is null.</summary>
    public void Say(string message, double? seconds)
    {
        Message = message;
        _text.Text = message;
        _hideAt = seconds is { } s ? DateTime.UtcNow.AddSeconds(s) : DateTime.MaxValue;
        if (!IsVisible)
            Show();
    }

    public void Clear()
    {
        Message = null;
        _hideAt = DateTime.MaxValue;
        Hide();
    }

    /// <summary>Keeps the bubble centred above the cat; call every frame. Positions are physical pixels.</summary>
    public void Follow(int catCenterX, int catTop, bool catVisible)
    {
        if (Message is null)
            return;
        if (DateTime.UtcNow >= _hideAt)
        {
            Clear();
            return;
        }
        if (!catVisible)
        {
            if (IsVisible) Hide();
            return;
        }
        if (!IsVisible)
            Show();
        if (_hwnd == IntPtr.Zero || !Native.GetWindowRect(_hwnd, out var me))
            return;

        int x = catCenterX - me.Width / 2, y = catTop - me.Height;
        if (x != me.Left || y != me.Top)
            Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, x, y, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
    }

    private void Dismiss()
    {
        Clear();
        Dismissed?.Invoke();
    }
}
