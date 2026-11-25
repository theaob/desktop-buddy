namespace DesktopBuddy.Core;

/// <summary>What a buddy leaves behind: cats and dogs a little brown pile, birds a white splat.</summary>
public enum PoopStyle { Pile, Splat }

/// <summary>
/// The little piles a buddy leaves on the taskbar now and then, until the user clicks them away.
/// Nothing here is cat-specific: any buddy with squat poses can leave one. Positions are in whatever
/// unit the caller uses (the app uses physical pixels for the pile's left edge).
/// </summary>
public sealed class PoopPiles
{
    /// <summary>The buddy won't leave more than this many lying around; it asks for a cleanup instead.</summary>
    public const int MaxPiles = 3;

    /// <summary>How long the puff of dust lasts after a click.</summary>
    public const double PoofSeconds = 0.5;

    /// <summary>The stink lines waver back and forth this often.</summary>
    public const double StinkFrameSeconds = 0.5;

    private readonly List<PoopPile> _piles = new();
    private int _nextId;

    /// <summary>Every pile, including ones still puffing away after a cleanup.</summary>
    public IReadOnlyList<PoopPile> All => _piles;

    /// <summary>Piles still waiting to be cleaned up.</summary>
    public int Count => _piles.Count(p => !p.Cleaned);

    public bool HasRoom => Count < MaxPiles;

    /// <summary>Leaves a pile at <paramref name="x"/>. Returns null when there are already <see cref="MaxPiles"/>.</summary>
    public PoopPile? Drop(double x, PoopStyle style = PoopStyle.Pile)
    {
        if (!HasRoom)
            return null;
        var pile = new PoopPile(++_nextId, x, style);
        _piles.Add(pile);
        return pile;
    }

    /// <summary>The user clicked a pile: it puffs away. Returns false if it was already gone or going.</summary>
    public bool Clean(int id)
    {
        var pile = _piles.Find(p => p.Id == id);
        if (pile is null || pile.Cleaned)
            return false;
        pile.Cleaned = true;
        pile.Age = 0;
        return true;
    }

    public void Tick(double elapsedSeconds)
    {
        foreach (var pile in _piles)
            pile.Age += elapsedSeconds;
        _piles.RemoveAll(p => p.Cleaned && p.Age >= PoofSeconds);
    }

    /// <summary>Which picture of <see cref="PoopArt"/> to show for a pile right now.</summary>
    public static int Frame(PoopPile pile) => pile.Cleaned
        ? (pile.Age < PoofSeconds / 2 ? PoopArt.PoofA : PoopArt.PoofB)
        : pile.Style == PoopStyle.Splat
            ? ((int)(pile.Age / StinkFrameSeconds) % 2 == 0 ? PoopArt.Splat0 : PoopArt.Splat1)
            : ((int)(pile.Age / StinkFrameSeconds) % 2 == 0 ? PoopArt.Idle0 : PoopArt.Idle1);

    /// <summary>What each kind of buddy leaves behind.</summary>
    public static PoopStyle StyleFor(Species species) => species == Species.Bird ? PoopStyle.Splat : PoopStyle.Pile;
}

public sealed class PoopPile
{
    internal PoopPile(int id, double x, PoopStyle style)
    {
        Id = id;
        X = x;
        Style = style;
    }

    /// <summary>A pile or a splat; it keeps its look even if the buddy is swapped for another kind.</summary>
    public PoopStyle Style { get; }

    public int Id { get; }

    /// <summary>Left edge of the pile.</summary>
    public double X { get; }

    /// <summary>Seconds since it was left, or since it was cleaned once <see cref="Cleaned"/>.</summary>
    public double Age { get; internal set; }

    /// <summary>Clicked away; it's puffing out of sight.</summary>
    public bool Cleaned { get; internal set; }
}

/// <summary>
/// The pile's pictures as 12x12 character grids: a little three-tier swirl with wavy stink lines,
/// a bird's white splat with a little glint, and a puff of dust with sparkles for when either is cleaned up.
/// Drawn for this project. d = splat's dark middle, g = splat's outline, o = outline, b = brown, m = darker groove, h = highlight, w = stink line, c/e = dust puff and its edge, y = sparkle.
/// </summary>
public static class PoopArt
{
    public const int Size = 12;
    public const int Idle0 = 0, Idle1 = 1, PoofA = 2, PoofB = 3, Splat0 = 4, Splat1 = 5;
    public const int FrameCount = 6;

    public static IReadOnlyDictionary<char, uint> Palette { get; } = new Dictionary<char, uint>
    {
        ['o'] = 0xFF4A2C21,
        ['b'] = 0xFF8B5A3C,
        ['m'] = 0xFF6E4430,
        ['h'] = 0xFFB5825C,
        ['w'] = 0xFFA9BE93,
        ['c'] = 0xFFF3F2F6,
        ['e'] = 0xFFC7CBD8,
        ['y'] = 0xFFF5D76E,
        ['d'] = 0xFF5E5B68,
        ['g'] = 0xFF8C91A1,
    };

    private static readonly string[][] Frames =
    {
        // Lying there, stink lines one way
        new[]
        {
            "....w...w...",
            "...w...w....",
            "...w...w....",
            "....w...w...",
            "............",
            "......oo....",
            ".....ohbo...",
            "....ommmmo..",
            "....ohbbbbo.",
            "...ommmmmmo.",
            "..ohbbbbbbbo",
            "..oooooooooo",
        },
        // ...and the other way
        new[]
        {
            "...w...w....",
            "....w...w...",
            "....w...w...",
            "...w...w....",
            "............",
            "......oo....",
            ".....ohbo...",
            "....ommmmo..",
            "....ohbbbbo.",
            "...ommmmmmo.",
            "..ohbbbbbbbo",
            "..oooooooooo",
        },
        // Cleaned up: a puff of dust and sparkles
        new[]
        {
            "............",
            "..y......y..",
            "............",
            ".....y......",
            "...e.ee.e...",
            "..ecceccce..",
            ".ecccccccce.",
            "..eccccccce.",
            "...eeccee...",
            "............",
            "............",
            "............",
        },
        // ...then just the last sparkles
        new[]
        {
            "............",
            ".y........y.",
            "............",
            "......y.....",
            "............",
            "..y.....y...",
            "............",
            "............",
            "....e..e....",
            "............",
            "............",
            "............",
        },
        // A bird's splat, with a couple of specks beside it...
        new[]
        {
            "............",
            "............",
            "............",
            "............",
            "............",
            "............",
            "............",
            "......g.....",
            ".g...gcg..g.",
            "....gccdcg..",
            "..ggcdddccg.",
            ".gccccdccccg",
        },
        // ...that glint now and then
        new[]
        {
            "............",
            "............",
            "............",
            "............",
            "............",
            "............",
            "............",
            "......g.....",
            ".y...gcg....",
            "....gccdcg..",
            "..ggcdddccg.",
            ".gccccdccccg",
        },
    };

    public static IReadOnlyList<string> Rows(int frame) => Frames[frame];

    /// <summary>A Size x Size ARGB picture, row by row; colours are fully opaque or fully transparent.</summary>
    public static uint[] Argb(int frame)
    {
        var rows = Frames[frame];
        var pixels = new uint[Size * Size];
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                pixels[y * Size + x] = Palette.TryGetValue(rows[y][x], out var c) ? c : 0u;
        return pixels;
    }
}
