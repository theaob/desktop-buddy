using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DesktopBuddy.Core;

namespace DesktopBuddy;

/// <summary>The yarn ball's own little window, so it can roll independently of the cat.</summary>
internal sealed class YarnWindow : Window
{
    private readonly Image _image;
    private readonly BitmapSource[] _frames = new BitmapSource[YarnBall.FrameCount];
    private IntPtr _hwnd;
    private int _shownFrame = -1;

    public YarnWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        Left = Top = -10000;

        for (int i = 0; i < _frames.Length; i++)
        {
            var bmp = new WriteableBitmap(YarnBall.Size, YarnBall.Size, 96, 96, PixelFormats.Pbgra32, null);
            bmp.WritePixels(new Int32Rect(0, 0, YarnBall.Size, YarnBall.Size), YarnBall.Argb(i), YarnBall.Size * 4, 0);
            bmp.Freeze();
            _frames[i] = bmp;
        }

        // Same 2x scale as the cat.
        _image = new Image { Width = YarnBall.Size * 2, Height = YarnBall.Size * 2, Stretch = Stretch.Fill, Cursor = Cursors.Hand, ToolTip = "Flick it!" };
        RenderOptions.SetBitmapScalingMode(_image, BitmapScalingMode.NearestNeighbor);
        Content = _image;

        SourceInitialized += (_, _) =>
        {
            _hwnd = new WindowInteropHelper(this).Handle;
            Native.MakeToolWindow(_hwnd);
        };
        _image.MouseLeftButtonDown += (_, e) =>
        {
            // Flick it away from the side you clicked.
            var p = e.GetPosition(_image);
            Flicked?.Invoke(p.X < _image.Width / 2 ? 1 : -1);
            e.Handled = true;
        };
    }

    /// <summary>Raised when the user clicks the ball, with the direction it should roll.</summary>
    public event Action<int>? Flicked;

    /// <summary>Puts the ball's bottom-left corner at (x, bottom), in physical pixels.</summary>
    public void Place(int x, int bottom, int frame)
    {
        if (!IsVisible)
            Show();
        if (frame != _shownFrame)
        {
            _image.Source = _frames[frame];
            _shownFrame = frame;
        }
        if (_hwnd == IntPtr.Zero || !Native.GetWindowRect(_hwnd, out var me))
            return;
        int y = bottom - me.Height;
        if (x != me.Left || y != me.Top)
            Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, x, y, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
    }

    /// <summary>The ball's width in physical pixels once shown.</summary>
    public int PhysicalWidth => _hwnd != IntPtr.Zero && Native.GetWindowRect(_hwnd, out var me) ? me.Width : 0;
}
