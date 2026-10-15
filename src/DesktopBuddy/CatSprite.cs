using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DesktopBuddy.Core;

namespace DesktopBuddy;

/// <summary>Turns the cat's pixel frames into cached WPF bitmaps.</summary>
internal static class CatSprite
{
    private static readonly Dictionary<(CatPose, CatLook, bool, int), BitmapSource> Cache = new();

    public static BitmapSource Frame(CatPose pose, CatLook look, bool sweaty = false, int bubble = 0)
    {
        if (Cache.TryGetValue((pose, look, sweaty, bubble), out var cached))
            return cached;

        // Palette colours are fully opaque or fully transparent, so straight ARGB is already premultiplied.
        var bmp = new WriteableBitmap(CatPixels.Width, CatPixels.Height, 96, 96, PixelFormats.Pbgra32, null);
        bmp.WritePixels(new Int32Rect(0, 0, CatPixels.Width, CatPixels.Height), CatPixels.Argb(pose, look, sweaty, bubble), CatPixels.Width * 4, 0);
        bmp.Freeze();
        Cache[(pose, look, sweaty, bubble)] = bmp;
        return bmp;
    }
}
