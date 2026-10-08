namespace DesktopBuddy.Core;

/// <summary>
/// The pixel cat, facing right, as 16x12 character grids.
/// k = outline, o = fur, w = belly, e = eye, p = nose, h = heart, z = sleep mark, . = transparent.
/// </summary>
public static class CatPixels
{
    public const int Width = 16;
    public const int Height = 12;

    /// <summary>ARGB colours for each pixel character; anything else is transparent.</summary>
    public static IReadOnlyDictionary<char, uint> Palette { get; } = new Dictionary<char, uint>
    {
        ['k'] = 0xFF2B2B2B,
        ['o'] = 0xFFF2A541,
        ['w'] = 0xFFFFF6E8,
        ['e'] = 0xFF111111,
        ['p'] = 0xFFF48FB1,
        ['h'] = 0xFFE5484D,
        ['z'] = 0xFF7FB2F0,
    };

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
    private static readonly string[] LegsTucked = { ".....kk....kk...", "................" };
    private static readonly string[] LegsDangling = { ".....k.k...k.k..", "....k...k.k...k." };
    private static readonly string Empty = new('.', Width);

    private static readonly string[] Heart = { ".h.h.", "hhhhh", ".hhh.", "..h.." };
    private static readonly string[] Zzz = { "zzz", "..z", ".z.", "zzz" };

    private static readonly Dictionary<CatPose, string[]> Frames = new()
    {
        [CatPose.WalkA] = Stack(Body, LegsA),
        [CatPose.WalkB] = Stack(Body, LegsB),
        [CatPose.Sit] = Stack(Body, LegsTucked),
        [CatPose.SitBlink] = Stack(ClosedEyes(Body), LegsTucked),
        // Lying down: body sinks two rows so it rests on the taskbar, legs hidden; the sleep mark pulses.
        [CatPose.NapA] = Overlay(Stack(new[] { Empty, Empty }, ClosedEyes(Body)), Zzz, x: 0, y: 0),
        [CatPose.NapB] = Stack(new[] { Empty, Empty }, ClosedEyes(Body)),
        [CatPose.PettedA] = Overlay(Stack(ClosedEyes(Body), LegsTucked), Heart, x: 4, y: 0),
        [CatPose.PettedB] = Overlay(Stack(ClosedEyes(Body), LegsTucked), Heart, x: 4, y: 1),
        [CatPose.Dangle] = Stack(Body, LegsDangling),
    };

    public static IReadOnlyList<string> Rows(CatPose pose) => Frames[pose];

    /// <summary>The frame as ARGB pixels, row by row.</summary>
    public static uint[] Argb(CatPose pose)
    {
        var rows = Frames[pose];
        var pixels = new uint[Width * Height];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                pixels[y * Width + x] = Palette.TryGetValue(rows[y][x], out var c) ? c : 0u;
        return pixels;
    }

    private static string[] Stack(params string[][] parts) => parts.SelectMany(p => p).ToArray();

    private static string[] ClosedEyes(string[] rows) => rows.Select(r => r.Replace('e', 'o')).ToArray();

    /// <summary>Paints <paramref name="shape"/> onto transparent pixels of <paramref name="rows"/>.</summary>
    private static string[] Overlay(string[] rows, string[] shape, int x, int y)
    {
        var result = rows.Select(r => r.ToCharArray()).ToArray();
        for (int dy = 0; dy < shape.Length; dy++)
            for (int dx = 0; dx < shape[dy].Length; dx++)
            {
                char c = shape[dy][dx];
                if (c != '.' && result[y + dy][x + dx] == '.')
                    result[y + dy][x + dx] = c;
            }
        return result.Select(r => new string(r)).ToArray();
    }
}
