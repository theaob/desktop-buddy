namespace DesktopBuddy.Core;

/// <summary>
/// The yarn ball the cat plays with: rolls along a line, slows down, and bounces off the ends.
/// Positions and speeds are in whatever unit the caller uses (the app uses physical pixels).
/// </summary>
public sealed class YarnBall
{
    public const int Size = 10;
    public const int FrameCount = 3;

    // Fraction of speed kept each second; the ball rolls to a stop in a couple of seconds.
    private const double KeptPerSecond = 0.25;
    private const double StopSpeed = 1;

    private const uint Outline = 0xFF6B2638;
    private const uint Yarn = 0xFFE0607E;
    private const uint Strand = 0xFFF5A3B7;

    public double X { get; private set; }

    /// <summary>Units per second; positive rolls right.</summary>
    public double Velocity { get; private set; }

    /// <summary>How far it has rolled in total, which turns the ball.</summary>
    public double Rolled { get; private set; }

    public bool IsRolling => Velocity != 0;

    public void Place(double x)
    {
        X = x;
        Velocity = 0;
    }

    public void Kick(double velocity) => Velocity = velocity;

    /// <summary>Moves the ball, keeping it between <paramref name="min"/> and <paramref name="max"/>.</summary>
    public void Tick(double elapsedSeconds, double min, double max)
    {
        if (Velocity == 0)
        {
            X = Math.Clamp(X, min, Math.Max(min, max));
            return;
        }

        double step = Velocity * elapsedSeconds;
        X += step;
        Rolled += Math.Abs(step);
        if (X < min) { X = min; Velocity = Math.Abs(Velocity); }
        else if (X > max) { X = Math.Max(min, max); Velocity = -Math.Abs(Velocity); }

        Velocity *= Math.Pow(KeptPerSecond, elapsedSeconds);
        if (Math.Abs(Velocity) < StopSpeed)
            Velocity = 0;
    }

    /// <summary>The rolling frame to show, which turns as the ball covers ground.</summary>
    public int Frame(double unitsPerTurnStep) => (int)(Rolled / unitsPerTurnStep) % FrameCount;

    /// <summary>A Size x Size ARGB picture of the ball; each frame turns the strands a little.</summary>
    public static uint[] Argb(int frame)
    {
        var pixels = new uint[Size * Size];
        const double c = (Size - 1) / 2.0, r = Size / 2.0 - 0.4;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                double dx = x - c, dy = y - c, d = Math.Sqrt(dx * dx + dy * dy);
                if (d > r)
                    continue;
                bool edge = d > r - 1;
                bool strand = ((x + 2 * y + frame * 2) % 4 + 4) % 4 == 0;
                pixels[y * Size + x] = edge ? Outline : strand ? Strand : Yarn;
            }
        }
        return pixels;
    }
}
