namespace DesktopBuddy.Core;

/// <summary>The cat's fur colour, picked in Settings.</summary>
public enum FurColor { BlueGrey, Ginger, Charcoal, Cocoa, Cream, Snow }

/// <summary>The cat's coat pattern, picked in Settings.</summary>
public enum CatPattern { Tabby, Solid, Socks, Patches }

/// <summary>How the cat looks. The default is a blue-grey tabby.</summary>
public readonly record struct CatLook(FurColor Fur, CatPattern Pattern);

/// <summary>
/// The pixel cat, facing right, as 24x20 character grids. Drawn for this project: a round-headed
/// cat with a cream chest, green eyes and a red collar with a gold bell.
/// Coat pixels are marked by the role they play in each pattern, and <see cref="ColorsFor"/> colours them:
/// f = plain fur, d = tabby marking, t = tabby stripe, s = sock, a = patch, b = patch over a tabby marking.
/// Fixed colours: k = outline, l = fur highlight, c = cream, p = pink (ears, nose), g = green eye,
/// w = eye shine, r = collar, y = bell, h = heart, z = sleep mark, q = sweat drop, . = transparent.
/// </summary>
public static class CatPixels
{
    public const int Width = 24;
    public const int Height = 20;

    /// <summary>The tray icon is the cat's head and collar, cut from the sitting frame.</summary>
    public const int IconSize = 16;

    private const uint White = 0xFFFFFFFF;
    private const uint Cream = 0xFFF6F2EA;
    private const uint GingerPatch = 0xFFE8964A;

    /// <summary>Fur, darker marking and highlight colours for each fur choice.</summary>
    private static readonly Dictionary<FurColor, (uint Fur, uint Dark, uint Light)> Furs = new()
    {
        [FurColor.BlueGrey] = (0xFF8E9AB4, 0xFF6A7492, 0xFFB9C3DA),
        [FurColor.Ginger] = (0xFFE8964A, 0xFFC2692B, 0xFFF5BE86),
        [FurColor.Charcoal] = (0xFF55525F, 0xFF3A3844, 0xFF77748A),
        [FurColor.Cocoa] = (0xFF8C6046, 0xFF6B4433, 0xFFAE8168),
        [FurColor.Cream] = (0xFFE6D2AE, 0xFFC4AA80, 0xFFF5E8CF),
        [FurColor.Snow] = (0xFFE9EAF0, 0xFFB3B8CA, 0xFFFFFFFF),
    };

    /// <summary>ARGB colours for each pixel character in the default look; anything else is transparent.</summary>
    public static IReadOnlyDictionary<char, uint> Palette { get; } = ColorsFor(default);

    /// <summary>The palette for a fur colour and pattern.</summary>
    public static Dictionary<char, uint> ColorsFor(CatLook look)
    {
        var (fur, dark, light) = Furs.TryGetValue(look.Fur, out var f) ? f : Furs[FurColor.BlueGrey];
        bool lightFur = look.Fur is FurColor.Cream or FurColor.Snow;
        uint marking = look.Pattern == CatPattern.Tabby ? dark : fur;
        uint sock = look.Pattern != CatPattern.Socks ? fur : look.Fur == FurColor.Snow ? dark : White;
        uint patch = lightFur ? GingerPatch : Cream;

        return new Dictionary<char, uint>
        {
            ['k'] = 0xFF2A2433,
            ['f'] = fur,
            ['d'] = marking,
            ['t'] = marking,
            ['s'] = sock,
            ['a'] = look.Pattern == CatPattern.Patches ? patch : fur,
            ['b'] = look.Pattern == CatPattern.Patches ? patch : marking,
            ['l'] = light,
            ['c'] = 0xFFF4EADD,
            ['p'] = 0xFFF2A0B2,
            ['g'] = 0xFF6CD09A,
            ['w'] = White,
            ['r'] = 0xFFE0564F,
            ['y'] = 0xFFF5C542,
            ['h'] = 0xFFE5484D,
            ['z'] = 0xFF7FB2F0,
            ['q'] = 0xFF8FD0F5,
        };
    }

    private static readonly Dictionary<CatPose, string[]> Frames = new()
    {
        [CatPose.WalkA] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            ".kk.......kfbabfffffk...",
            "kddk.....kfllaaffffffk..",
            "kdk......kflaaaffffffk..",
            "ktk......kfffffffkgwkfk.",
            ".kfk.....kfffffffkggkfk.",
            ".kfk.....kffffffffccccpk",
            "..ktk....kfffffffcccckk.",
            "..kfk..kkffffaaafccck...",
            "..kfdkkbffftfakrrryrk...",
            "...kaabbbftffakrykffk...",
            "...kaabaaftddftffffk....",
            "...kabaaatffftfffffk....",
            "...kfbaaftcccccccffk....",
            "....kkfffkkkkkkkffkk....",
            "....ksk.ksk...ksk.ksk...",
            "....ksk.ksk...ksk.ksk...",
            "....kck.kck...kck.kck...",
        },
        [CatPose.WalkB] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            ".kk.......kfbabfffffk...",
            "kddk.....kfllaaffffffk..",
            "kdk......kflaaaffffffk..",
            "ktk......kfffffffkgwkfk.",
            ".kfk.....kfffffffkggkfk.",
            ".kfk.....kffffffffccccpk",
            "..ktk....kfffffffcccckk.",
            "..kfk..kkffffaaafccck...",
            "..kfdkkbffftfakrrryrk...",
            "...kaabbbftffakrykffk...",
            "...kaabaaftddftffffk....",
            "...kabaaatffftfffffk....",
            "...kfbaaftcccccccffk....",
            "....kkfffkkkkkkkffkk....",
            "...ksk..ksk....ksk.ksk..",
            "..ksk....ksk..ksk...ksk.",
            "..kck....kck..kck...kck.",
        },
        [CatPose.Sit] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            "..........kfbabfffffk...",
            ".........kfllaaffffffk..",
            ".........kflaaaffffffk..",
            ".........kfffffffkgwkfk.",
            ".........kfffffffkggkfk.",
            ".........kffffffffccccpk",
            "..........kffffffcccckk.",
            "..........kaaakrrryrrk..",
            ".........kfbaaftkykccck.",
            "........kftaaatfffkcccck",
            ".......kfbbafftffffkccck",
            "......kfabbbatffffkfccck",
            "......kfabaaatfffksskssk",
            "......kfbaaabffftksskssk",
            ".kk...kftaaatfffkfskssk.",
            "kddkkkkffffffffkcckcck..",
            ".kkdddddkkkkkkkkkkkkkk..",
        },
        [CatPose.SitBlink] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            "..........kfbabfffffk...",
            ".........kfllaaffffffk..",
            ".........kflaaaffffffk..",
            ".........kfffffffkffkfk.",
            ".........kfffffffkkkkfk.",
            ".........kffffffffccccpk",
            "..........kffffffcccckk.",
            "..........kaaakrrryrrk..",
            ".........kfbaaftkykccck.",
            "........kftaaatfffkcccck",
            ".......kfbbafftffffkccck",
            "......kfabbbatffffkfccck",
            "......kfabaaatfffksskssk",
            "......kfbaaabffftksskssk",
            ".kk...kftaaatfffkfskssk.",
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
            "..kkkkkkkkkfdabaffffk...",
            ".ktfbbbfdftllaaafffffk..",
            "ktfaabaaftflfaaafffffk..",
            "ktfaabaaftfffffffkffkfk.",
            "kffabaaatafffffffkkkkfk.",
            "kfffbaafbaafffffffccccpk",
            "kddddfftfaffffkkkcckcck.",
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
            "..kkkkkkkkkfdabaffffk...",
            ".ktfbbbfdftllaaafffffk..",
            "ktfaabaaftflfaaafffffk..",
            "ktfaabaaftfffffffkffkfk.",
            "kffabaaatafffffffkkkkfk.",
            "kfffbaafbaafffffffccccpk",
            "kddddfftfaffffkkkcckcck.",
            ".kkkkkkkkkkkkkkkkkkkkkk.",
        },
        [CatPose.PettedA] = new[]
        {
            "............k....k......",
            "..hh.hh....kpk..kpk.....",
            ".hhhhhhh...kppkkkppk....",
            "..hhhhh...kfbabfffffk...",
            "...hhh...kfllaaffffffk..",
            "....h....kflaaaffffffk..",
            ".........kffffffffkkffk.",
            ".........kfffffffkffkfk.",
            ".........kffffffffccccpk",
            "..........kffffffcccckk.",
            "..........kaaakrrryrrk..",
            ".........kfbaaftkykccck.",
            "........kftaaatfffkcccck",
            ".......kfbbafftffffkccck",
            "......kfabbbatffffkfccck",
            "......kfabaaatfffksskssk",
            "......kfbaaabffftksskssk",
            ".kk...kftaaatfffkfskssk.",
            "kddkkkkffffffffkcckcck..",
            ".kkdddddkkkkkkkkkkkkkk..",
        },
        [CatPose.PettedB] = new[]
        {
            "..hh.hh.....k....k......",
            ".hhhhhhh...kpk..kpk.....",
            "..hhhhh....kppkkkppk....",
            "...hhh....kfbabfffffk...",
            "....h....kfllaaffffffk..",
            ".........kflaaaffffffk..",
            ".........kffffffffkkffk.",
            ".........kfffffffkffkfk.",
            ".........kffffffffccccpk",
            "..........kffffffcccckk.",
            "..........kaaakrrryrrk..",
            ".........kfbaaftkykccck.",
            "........kftaaatfffkcccck",
            ".......kfbbafftffffkccck",
            "......kfabbbatffffkfccck",
            "......kfabaaatfffksskssk",
            "......kfbaaabffftksskssk",
            ".kk...kftaaatfffkfskssk.",
            "kddkkkkffffffffkcckcck..",
            ".kkdddddkkkkkkkkkkkkkk..",
        },
        [CatPose.Dangle] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            "..........kfbabfffffk...",
            ".........kfllaaffffffk..",
            ".........kflaaaffffffk..",
            ".........kfffffffkgwkfk.",
            ".........kfffffffkggkfk.",
            ".........kffffffffccccpk",
            "..........kffffffcccckk.",
            "..........kfffkrrryrrk..",
            "...........kffftffccck..",
            "..........kffabaffccck..",
            "..........kfaabaafccck..",
            "..........kfbbaaatfffk..",
            "..........kfabaaatfffk..",
            "..........kftaaatfffk...",
            ".........kfkkffftsfksk..",
            ".........kfkkckckckkck..",
            ".........kdk.k.k.k..k...",
        },
        [CatPose.HeldBelly] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            "..........kfbabfffffk...",
            ".........kfllaaffffffk..",
            ".........kflaaaffffffk..",
            ".........kfffffffkgwkfk.",
            ".........kfffffffkggkfk.",
            ".........kffffffffccccpk",
            ".........kfffffffcccckk.",
            ".......kkffffaaafccck...",
            "....kkkbffftfakrrryrk...",
            "...kaabbbftffakrykffk...",
            "...kaabaaftddftffffk....",
            "...kabaaatffftfffffk....",
            "..kffbaaftcccccccffk....",
            "..kffkfffkkkkkkkffkk....",
            "..kskskkksk...kskkksk...",
            "..kdksk.ksk...ksk.ksk...",
            "...kkck.kck...kck.kck...",
        },
        [CatPose.Leap] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            "..........kfbabfffffk...",
            ".........kfllaaffffffk..",
            "..k......kflaaaffffffk..",
            ".kdk.....kfffffffkgwkfk.",
            ".kdk.....kfffffffkggkfk.",
            "..kfk....kffffffffccccpk",
            "..kfk....kfffffffcccckk.",
            "...kfk.kkffffaaafccck...",
            "....kfkbafftfakrrryrk...",
            "...kfabbbatffabryfffkkkk",
            "kkkkfabaaatddftfffffsssc",
            "cssffbaaabffftffffffsssc",
            "cssfftaaatcccccffffkkkkk",
            "kkkkkkkkkkkkkkkkkkk.....",
            "........................",
            "........................",
            "........................",
        },
        [CatPose.CrouchA] = new[]
        {
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            "..kkkkkkkkkfdabaffffk...",
            ".ktfbbbfdftllaaafffffk..",
            "ktfaabaaftflfaaafffffk..",
            "ktfaabaaftfffffffkgwkfk.",
            "kffabaaatafffffffkggkfk.",
            "kfffbaafbaafffffffccccpk",
            "kddddfftfaffffkkkcckcck.",
            ".kkkkkkkkkkkkkkkkkkkkkk.",
        },
        [CatPose.CrouchB] = new[]
        {
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "............k....k......",
            "...........kpk..kpk.....",
            "..kkkkkkk..kppkkkppk....",
            ".kffdddfdkkfdabaffffk...",
            "kftfabbffftllaaafffffk..",
            "ktfaabaaftflfaaafffffk..",
            "ktfabbaaftfffffffkgwkfk.",
            "kffabaaadafffffffkggkfk.",
            "kdddbaafbaafffffffccccpk",
            ".kkkkkkkkaffffkkkcckcck.",
            ".kkkkkkkkkkkkkkkkkkkkkk.",
        },
        [CatPose.ScratchA] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            "..........kfbabfffffk...",
            ".........kfllaaffffffk..",
            ".........kflaaaffffffk..",
            ".........kfffffffkffkfk.",
            ".........kfffffffkkkkfk.",
            "......cc.kffffffffccccpk",
            ".....kssk.kffffffcccckk.",
            ".....kssk.kaaakrrryrrk..",
            ".....ksskkfbaaftkykccck.",
            "....kffkkftaaatfffkcccck",
            "....kffkfbbafftffffkccck",
            "....kffkabbbatffffkfccck",
            "....kffkabaaatfffksskssk",
            ".....kkfbaaabffftksskssk",
            ".kk...kftaaatfffkfskssk.",
            "kddkkkkffffffffkcckcck..",
            ".kkdddddkkkkkkkkkkkkkk..",
        },
        [CatPose.ScratchB] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            "..........kfbabfffffk...",
            ".........kfllaaffffffk..",
            ".........kflaaaffffffk..",
            ".........kfffffffkffkfk.",
            ".........kfffffffkkkkfk.",
            ".........kffffffffccccpk",
            "......cc..kffffffcccckk.",
            ".....kssk.kaaakrrryrrk..",
            ".....ksskkfbaaftkykccck.",
            "....kffkkftaaatfffkcccck",
            "....kffkfbbafftffffkccck",
            "....kffkabbbatffffkfccck",
            "....kffkabaaatfffksskssk",
            ".....kkfbaaabffftksskssk",
            ".kk...kftaaatfffkfskssk.",
            "kddkkkkffffffffkcckcck..",
            ".kkdddddkkkkkkkkkkkkkk..",
        },
        [CatPose.Bat] = new[]
        {
            "..........k....k........",
            ".........kpk..kpk.......",
            ".........kppkkkppk......",
            "........kfbabfffffk.....",
            ".......kfllaaffffffk....",
            ".......kflaaaffffffk....",
            ".......kfffffffkgwkfk...",
            ".......kfffffffkggkfk...",
            ".......kffffffffccccpk..",
            "........kffffffcccckk...",
            "........kaaakrrryrrk..k.",
            ".......kfbabffkykccckkck",
            "......kffabafftfkcckssck",
            ".....ktbbatffftffkkfskk.",
            "....ktabbbafftffkkffkk..",
            "....ktababafftfksskk....",
            "....kfbababftffkssk.....",
            "k...kffabafftfkfskk.....",
            "dkkkkffffffffkcckk......",
            "kdddddkkkkkkkkkkkkkk....",
        },
        [CatPose.LookUp] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            "..........kfbabfffffk...",
            ".........kfllaaffffffk..",
            ".........kflaaaffkgwkk..",
            ".........kfffffffkggkfk.",
            ".........kffffffffffffk.",
            ".........kffffffffccccpk",
            "..........kffffffcccckk.",
            "..........kaaakrrryrrk..",
            ".........kfbaaftkykccck.",
            "........kftaaatfffkcccck",
            ".......kfbbafftffffkccck",
            "......kfabbbatffffkfccck",
            "......kfabaaatfffksskssk",
            "......kfbaaabffftksskssk",
            ".kk...kftaaatfffkfskssk.",
            "kddkkkkffffffffkcckcck..",
            ".kkdddddkkkkkkkkkkkkkk..",
        },
        [CatPose.FallA] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            ".kk.......kfbabfffffk...",
            "kddk.....kfllaaffffffk..",
            "kdk......kflaaaffffffk..",
            "kfk......kfffffffkgwkfk.",
            ".kfk.....kfffffffkggkfk.",
            ".kfk.....kffffffffccccpk",
            "..kfk....kfffffffcccckk.",
            "..kfk..kkffffaaafccck...",
            "..kfdkkbffftfakrrryrk...",
            "...kaabbbftffakrykffk...",
            "...kaabaaftddftffffk....",
            "...kabaaatffftfffffk....",
            "...kfbaaftcccccccffk....",
            "....kkffffccccfffkk.....",
            "......ksssskksskssk.....",
            "....kkssksskksskkssk....",
            "...kckkk.kckckk..kkck...",
        },
        [CatPose.FallB] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            ".kk.......kfbabfffffk...",
            "kddk.....kfllaaffffffk..",
            "kdk......kflaaaffffffk..",
            "kfk......kfffffffkgwkfk.",
            ".kfk.....kfffffffkggkfk.",
            ".kfk.....kffffffffccccpk",
            "..kfk....kfffffffcccckk.",
            "..kfk..kkffffaaafccck...",
            "..kfdkkbffftfakrrryrk...",
            "...kaabbbftffakrykffk...",
            "...kaabaaftddftffffk....",
            "...kabaaatffftfffffk....",
            "...kfbaaftcccccccffk....",
            "....kkffffccccfffkkkk...",
            "..kkssssksskkksskssssk..",
            ".kckkkkkssk..ksskkkkkck.",
            "..k....kck....kck....k..",
        },
        [CatPose.Land] = new[]
        {
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            "..kkkkkkkkkfdabaffffk...",
            ".ktfbbbfdftllaaafffffk..",
            "ktfaabaaftflfaaafffffk..",
            "ktfaabaaftfffffffkffkfk.",
            "kffabaaatafffffffkkkkfk.",
            "kfffbaafbaafffffffccccpk",
            "kddddfftfaffffkkkcckcck.",
            ".kkkkkkkkkkkkkkkkkkkkkk.",
        },
        [CatPose.Drowsy] = new[]
        {
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            "..kkkkkkkkkfdabaffffk...",
            ".ktfbbbfdftllaaafffffk..",
            "ktfaabaaftflfaaafffffk..",
            "ktfaabaaftfffffffkkkkfk.",
            "kffabaaatafffffffkggkfk.",
            "kfffbaafbaafffffffccccpk",
            "kddddfftfaffffkkkcckcck.",
            ".kkkkkkkkkkkkkkkkkkkkkk.",
        },
        [CatPose.Stretch] = new[]
        {
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "..k.....................",
            ".kfkk.......k....k......",
            "kfffdkkk...kpk..kpk.....",
            "kffffddfkk.kppkkkppk....",
            "kffffdffdfkfdabaffffk...",
            "kftfbbbffftllaaafffffk..",
            "kddabbaaftflfaaafffffk..",
            ".kkbbbaaftfffffffkffkfk.",
            "...kkaaadafffffffkkkkfk.",
            ".....kkkbaafffffffccccpk",
            "........kkffffkkkcccccc.",
            "..........kkkkkkkkkkkkk.",
        },
        [CatPose.Yawn] = new[]
        {
            "............k....k......",
            "...........kpk..kpk.....",
            "...........kppkkkppk....",
            "..........kfbabfffffk...",
            ".........kfllaaffffffk..",
            ".........kflaaaffffffk..",
            ".........kfffffffkffkfk.",
            ".........kfffffffkkkkfk.",
            ".........kffffffffccckpk",
            "..........kffffffccckpk.",
            "..........kaaakrrryrrk..",
            ".........kfbaaftkykccck.",
            "........kftaaatfffkcccck",
            ".......kfbbafftffffkccck",
            "......kfabbbatffffkfccck",
            "......kfabaaatfffksskssk",
            "......kfbaaabffftksskssk",
            ".kk...kftaaatfffkfskssk.",
            "kddkkkkffffffffkcckcck..",
            ".kkdddddkkkkkkkkkkkkkk..",
        },
    };

    public static IReadOnlyList<string> Rows(CatPose pose) => Frames[pose];

    /// <summary>A sweat drop beside the head, shown when the PC is busy. Only poses with the head in the usual place get one.</summary>
    private static readonly (int X, int Y)[] SweatDrop = { (8, 1), (7, 2), (8, 2), (9, 2), (7, 3), (8, 3), (9, 3), (8, 4) };

    private static readonly HashSet<CatPose> SweatPoses = new()
    {
        CatPose.WalkA, CatPose.WalkB, CatPose.Sit, CatPose.SitBlink, CatPose.LookUp,
    };

    /// <summary>The frame as ARGB pixels, row by row.</summary>
    public static uint[] Argb(CatPose pose, CatLook look = default, bool sweaty = false)
    {
        var colors = ColorsFor(look);
        var pixels = ToArgb(Frames[pose], colors, 0, 0, Width, Height);
        if (sweaty && SweatPoses.Contains(pose))
            foreach (var (x, y) in SweatDrop)
                pixels[y * Width + x] = colors['q'];
        return pixels;
    }

    /// <summary>A 16x16 icon: the head from the sitting frame, centred vertically.</summary>
    public static uint[] IconArgb(CatLook look = default)
    {
        const int left = 8, rows = 12;
        var head = ToArgb(Frames[CatPose.Sit], ColorsFor(look), left, 0, IconSize, rows);
        var icon = new uint[IconSize * IconSize];
        int top = (IconSize - rows) / 2;
        Array.Copy(head, 0, icon, top * IconSize, head.Length);
        return icon;
    }

    private static uint[] ToArgb(string[] rows, Dictionary<char, uint> colors, int left, int top, int width, int height)
    {
        var pixels = new uint[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = colors.TryGetValue(rows[top + y][left + x], out var c) ? c : 0u;
        return pixels;
    }
}
