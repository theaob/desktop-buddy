using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DesktopBuddy;

internal enum CatPose { WalkA, WalkB, Sit }

/// <summary>
/// Placeholder pixel cat for the spike, facing right. Real sprite sheets replace this in phase 1.
/// k = outline, o = fur, w = belly, e = eye, p = nose, . = transparent.
/// </summary>
internal static class CatSprite
{
    public const int PixelWidth = 16;
    public const int PixelHeight = 12;

    private static readonly string[] Body =
    {
        "..........k..k..",
        ".........kokkok.",
        "..k......kooook.",
        ".kok.....koeoek.",
        ".kok.....kooopk.",
        "..kok.kkkkooook.",
        "...kkoooooooook.",
        "....koooooooook.",
        "....koooowwwook.",
        "....kkkkkkkkkkk.",
    };

    private static readonly string[] LegsA = { ".....k.k...k.k..", ".....k.k...k.k.." };
    private static readonly string[] LegsB = { "....k.k.....k.k.", "....k.k.....k.k." };
    private static readonly string[] LegsSit = { ".....kk....kk...", "................" };

    private static readonly Dictionary<char, uint> Palette = new()
    {
        ['k'] = 0xFF2B2B2B,
        ['o'] = 0xFFF2A541,
        ['w'] = 0xFFFFF6E8,
        ['e'] = 0xFF111111,
        ['p'] = 0xFFF48FB1,
    };

    private static readonly Dictionary<CatPose, BitmapSource> Cache = new();

    public static BitmapSource Frame(CatPose pose)
    {
        if (Cache.TryGetValue(pose, out var cached))
            return cached;

        var legs = pose switch
        {
            CatPose.WalkA => LegsA,
            CatPose.WalkB => LegsB,
            _ => LegsSit,
        };
        var rows = Body.Concat(legs).ToArray();

        var pixels = new uint[PixelWidth * PixelHeight];
        for (int y = 0; y < PixelHeight; y++)
            for (int x = 0; x < PixelWidth; x++)
                pixels[y * PixelWidth + x] = Palette.TryGetValue(rows[y][x], out var c) ? c : 0u;

        var bmp = new WriteableBitmap(PixelWidth, PixelHeight, 96, 96, PixelFormats.Pbgra32, null);
        bmp.WritePixels(new Int32Rect(0, 0, PixelWidth, PixelHeight), pixels, PixelWidth * 4, 0);
        bmp.Freeze();
        Cache[pose] = bmp;
        return bmp;
    }
}
