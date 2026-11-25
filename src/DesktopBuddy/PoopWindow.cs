using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DesktopBuddy.Core;

namespace DesktopBuddy;

/// <summary>One pile on the taskbar, in its own little window, so it stays put while the buddy walks off. Click it to clean it up.</summary>
internal sealed class PoopWindow : Window
{
    private static BitmapSource[]? _frames;

    private readonly Image _image;
    private IntPtr _hwnd;
    private int _shownFrame = -1;

    public PoopWindow(int pileId)
    {
        PileId = pileId;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        Left = Top = -10000;

        // Same 2x scale as the cat.
        _image = new Image { Width = PoopArt.Size * 2, Height = PoopArt.Size * 2, Stretch = Stretch.Fill, Cursor = Cursors.Hand, ToolTip = "Clean it up" };
        RenderOptions.SetBitmapScalingMode(_image, BitmapScalingMode.NearestNeighbor);
        Content = _image;

        SourceInitialized += (_, _) =>
        {
            _hwnd = new WindowInteropHelper(this).Handle;
            Native.MakeToolWindow(_hwnd);
        };
        _image.MouseLeftButtonDown += (_, e) =>
        {
            Cleaned?.Invoke(PileId);
            e.Handled = true;
        };
    }

    public int PileId { get; }

    /// <summary>Raised when the user clicks the pile, with its id.</summary>
    public event Action<int>? Cleaned;

    private static BitmapSource Frame(int frame)
    {
        if (_frames is null)
        {
            _frames = new BitmapSource[PoopArt.FrameCount];
            for (int i = 0; i < _frames.Length; i++)
            {
                // Colours are fully opaque or fully transparent, so straight ARGB is already premultiplied.
                var bmp = new WriteableBitmap(PoopArt.Size, PoopArt.Size, 96, 96, PixelFormats.Pbgra32, null);
                bmp.WritePixels(new Int32Rect(0, 0, PoopArt.Size, PoopArt.Size), PoopArt.Argb(i), PoopArt.Size * 4, 0);
                bmp.Freeze();
                _frames[i] = bmp;
            }
        }
        return _frames[frame];
    }

    /// <summary>Puts the pile's bottom-left corner at (x, bottom), in physical pixels.</summary>
    public void Place(int x, int bottom, int frame)
    {
        if (!IsVisible)
            Show();
        if (frame != _shownFrame)
        {
            _image.Source = Frame(frame);
            _image.Cursor = frame >= PoopArt.PoofA ? null : Cursors.Hand;
            _shownFrame = frame;
        }
        if (_hwnd == IntPtr.Zero || !Native.GetWindowRect(_hwnd, out var me))
            return;
        int y = bottom - me.Height;
        if (x != me.Left || y != me.Top)
            Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, x, y, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
    }
}
