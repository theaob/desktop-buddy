namespace DesktopBuddy.Core;

/// <summary>
/// The pixel cat, facing right, as 24x20 character grids. Drawn for this project: a round-headed
/// blue-grey cat with a cream chest, green eyes and a red collar with a gold bell.
/// k = outline, f = fur, d = darker fur (stripes, tail tip), l = fur highlight, c = cream,
/// p = pink (ears, nose), g = green eye, w = eye shine, r = collar, y = bell,
/// h = heart, z = sleep mark, . = transparent.
/// </summary>
public static class CatPixels
{
    public const int Width = 24;
    public const int Height = 20;

    /// <summary>The tray icon is the cat's head and collar, cut from the sitting frame.</summary>
    public const int IconSize = 16;

    /// <summary>ARGB colours for each pixel character; anything else is transparent.</summary>
    public static IReadOnlyDictionary<char, uint> Palette { get; } = new Dictionary<char, uint>
    {
        ['k'] = 0xFF2A2433,
        ['f'] = 0xFF8E9AB4,
        ['d'] = 0xFF6A7492,
        ['l'] = 0xFFB9C3DA,
        ['c'] = 0xFFF4EADD,
        ['p'] = 0xFFF2A0B2,
        ['g'] = 0xFF6CD09A,
        ['w'] = 0xFFFFFFFF,
        ['r'] = 0xFFE0564F,
        ['y'] = 0xFFF5C542,
        ['h'] = 0xFFE5484D,
        ['z'] = 0xFF7FB2F0,
    };

    private static readonly Dictionary<CatPose, string[]> Frames = new()
    {
        [CatPose.WalkA] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            ".kk.......kfdfdfffffk...",
            "kddk.....kfllffffffffk..",
            "kdk......kflfffffffffk..",
            "kfk......kfffffffkgwkfk.",
            ".kfk.....kfffffffkggkfk.",
            ".kfk.....kffffffffccccpk",
            "..kfk....kfffffffcccckk.",
            "..kfk..kkffffffffccck...",
            "..kfdkkfffffffkrrryrk...",
            "...kfffddfffffkrykffk...",
            "...kfffffffddffffffk....",
            "...kfffffffffffffffk....",
            "...kffffffcccccccffk....",
            "....kkfffkkkkkkkffkk....",
            "....kfk.kfk...kfk.kfk...",
            "....kfk.kfk...kfk.kfk...",
            "....kck.kck...kck.kck...",
        },
        [CatPose.WalkB] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            ".kk.......kfdfdfffffk...",
            "kddk.....kfllffffffffk..",
            "kdk......kflfffffffffk..",
            "kfk......kfffffffkgwkfk.",
            ".kfk.....kfffffffkggkfk.",
            ".kfk.....kffffffffccccpk",
            "..kfk....kfffffffcccckk.",
            "..kfk..kkffffffffccck...",
            "..kfdkkfffffffkrrryrk...",
            "...kfffddfffffkrykffk...",
            "...kfffffffddffffffk....",
            "...kfffffffffffffffk....",
            "...kffffffcccccccffk....",
            "....kkfffkkkkkkkffkk....",
            "...kfk..kfk....kfk.kfk..",
            "..kfk....kfk..kfk...kfk.",
            "..kck....kck..kck...kck.",
        },
        [CatPose.Sit] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            "..........kfdfdfffffk...",
            ".........kfllffffffffk..",
            ".........kflfffffffffk..",
            ".........kfffffffkgwkfk.",
            ".........kfffffffkggkfk.",
            ".........kffffffffccccpk",
            "..........kffffffcccckk.",
            "..........kfffkrrryrrk..",
            ".........kffffffkykccck.",
            "........kfffffffffkcccck",
            ".......kfddffffffffkccck",
            "......kfffddffffffkfccck",
            "......kffffffffffkffkffk",
            "......kffffffffffkffkffk",
            ".kk...kfffffffffkffkffk.",
            "kddkkkkffffffffkcckcck..",
            ".kkdddddkkkkkkkkkkkkkk..",
        },
        [CatPose.SitBlink] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            "..........kfdfdfffffk...",
            ".........kfllffffffffk..",
            ".........kflfffffffffk..",
            ".........kfffffffkffkfk.",
            ".........kfffffffkkkkfk.",
            ".........kffffffffccccpk",
            "..........kffffffcccckk.",
            "..........kfffkrrryrrk..",
            ".........kffffffkykccck.",
            "........kfffffffffkcccck",
            ".......kfddffffffffkccck",
            "......kfffddffffffkfccck",
            "......kffffffffffkffkffk",
            "......kffffffffffkffkffk",
            ".kk...kfffffffffkffkffk.",
            "kddkkkkffffffffkcckcck..",
            ".kkdddddkkkkkkkkkkkkkk..",
        },
        [CatPose.NapA] = new[]
        {
            "........................",
            "........................",
            "........................",
            "........................",
            "..zzzz..................",
            "....z...................",
            "...z....................",
            "..zzzz..................",
            "........................",
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            "..kkkkkkkkkfdfdfffffk...",
            ".kffddffdffllffffffffk..",
            "kfffffffffflfffffffffk..",
            "kffffffffffffffffkffkfk.",
            "kffffffffffffffffkkkkfk.",
            "kfffffffffffffffffccccpk",
            "kddddfffffffffkkkcckcck.",
            ".kkkkkkkkkkkkkkkkkkkkkk.",
        },
        [CatPose.NapB] = new[]
        {
            "........................",
            "........................",
            "..zzzz..................",
            "....z...................",
            "...z....................",
            "..zzzz..................",
            "........................",
            "........................",
            "........................",
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            "..kkkkkkkkkfdfdfffffk...",
            ".kffddffdffllffffffffk..",
            "kfffffffffflfffffffffk..",
            "kffffffffffffffffkffkfk.",
            "kffffffffffffffffkkkkfk.",
            "kfffffffffffffffffccccpk",
            "kddddfffffffffkkkcckcck.",
            ".kkkkkkkkkkkkkkkkkkkkkk.",
        },
        [CatPose.PettedA] = new[]
        {
            "............k....k......",
            "..hh.hh....kpk..kpk.....",
            ".hhhhhhh...kppkkkppk....",
            "..hhhhh...kfdfdfffffk...",
            "...hhh...kfllffffffffk..",
            "....h....kflfffffffffk..",
            ".........kffffffffkkffk.",
            ".........kfffffffkffkfk.",
            ".........kffffffffccccpk",
            "..........kffffffcccckk.",
            "..........kfffkrrryrrk..",
            ".........kffffffkykccck.",
            "........kfffffffffkcccck",
            ".......kfddffffffffkccck",
            "......kfffddffffffkfccck",
            "......kffffffffffkffkffk",
            "......kffffffffffkffkffk",
            ".kk...kfffffffffkffkffk.",
            "kddkkkkffffffffkcckcck..",
            ".kkdddddkkkkkkkkkkkkkk..",
        },
        [CatPose.PettedB] = new[]
        {
            "..hh.hh.....k....k......",
            ".hhhhhhh...kpk..kpk.....",
            "..hhhhh....kppkkkppk....",
            "...hhh....kfdfdfffffk...",
            "....h....kfllffffffffk..",
            ".........kflfffffffffk..",
            ".........kffffffffkkffk.",
            ".........kfffffffkffkfk.",
            ".........kffffffffccccpk",
            "..........kffffffcccckk.",
            "..........kfffkrrryrrk..",
            ".........kffffffkykccck.",
            "........kfffffffffkcccck",
            ".......kfddffffffffkccck",
            "......kfffddffffffkfccck",
            "......kffffffffffkffkffk",
            "......kffffffffffkffkffk",
            ".kk...kfffffffffkffkffk.",
            "kddkkkkffffffffkcckcck..",
            ".kkdddddkkkkkkkkkkkkkk..",
        },
        [CatPose.Dangle] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            ".kk.......kfdfdfffffk...",
            "kddk.....kfllffffffffk..",
            "kdk......kflfffffffffk..",
            "kfk......kfffffffkgwkfk.",
            ".kfk.....kfffffffkggkfk.",
            ".kfk.....kffffffffccccpk",
            "..kfk....kfffffffcccckk.",
            "..kfk..kkffffffffccck...",
            "..kfdkkfffffffkrrryrk...",
            "...kfffddfffffkrykffk...",
            "...kfffffffddffffffk....",
            "...kfffffffffffffffk....",
            "...kffffffcccccccffk....",
            "....kkfffkkkkkkkffkk....",
            "....kfk.kfk...kfk.kfk...",
            "....kfk.kfk...kfk.kfk...",
            "...kck...kck.kck...kck..",
        },
    };

    public static IReadOnlyList<string> Rows(CatPose pose) => Frames[pose];

    /// <summary>The frame as ARGB pixels, row by row.</summary>
    public static uint[] Argb(CatPose pose) => ToArgb(Frames[pose], 0, 0, Width, Height);

    /// <summary>A 16x16 icon: the head from the sitting frame, centred vertically.</summary>
    public static uint[] IconArgb()
    {
        const int left = 8, rows = 12;
        var head = ToArgb(Frames[CatPose.Sit], left, 0, IconSize, rows);
        var icon = new uint[IconSize * IconSize];
        int top = (IconSize - rows) / 2;
        Array.Copy(head, 0, icon, top * IconSize, head.Length);
        return icon;
    }

    private static uint[] ToArgb(string[] rows, int left, int top, int width, int height)
    {
        var pixels = new uint[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = Palette.TryGetValue(rows[top + y][left + x], out var c) ? c : 0u;
        return pixels;
    }
}
