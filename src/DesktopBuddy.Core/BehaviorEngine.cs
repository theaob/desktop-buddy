namespace DesktopBuddy.Core;

public enum BuddyState
{
    Walk, Sit, Nap, Petted, Dragged, Falling,
    Scratch,    // sits and scratches with a back leg
    Look,       // looks toward a window that just did something
    Follow,     // walks along to stay under the mouse
    Stalk,      // crouches and wiggles, about to pounce on the mouse
    Pounce,     // in the air, leaping at the mouse
    Play,       // chasing the yarn ball
    Celebrate,  // a focus session just ended
}

/// <summary>Where the user grabbed the cat, which decides how it hangs while carried.</summary>
public enum DragGrip { Scruff, Belly }

/// <summary>
/// Decides what the cat is doing. Pure logic: the caller feeds it elapsed time, the user's idle
/// time and where the mouse is each frame, and reports clicks, drags and window events; the
/// window turns the state into pixels and movement.
/// </summary>
public sealed class BehaviorEngine
{
    public const double PetSeconds = 1.6;
    public const double ScratchSeconds = 2.4;
    public const double LookSeconds = 2;
    public const double StalkSeconds = 1.4;
    public const double CelebrateSeconds = 3;

    // Any input within this window wakes a napping cat.
    public const double WakeIdleSeconds = 2;

    // A nearby mouse gets pounced on about once every few seconds, then the cat rests from hunting.
    public const double HuntChancePerSecond = 0.35;
    public const double HuntCooldownSeconds = 40;

    // Window events closer together than this don't turn its head again.
    public const double LookCooldownSeconds = 6;

    private readonly Random _random;
    private double _moodTimeLeft;
    private double _huntCooldown;
    private double _lookCooldown;

    public BehaviorEngine(Random? random = null)
    {
        _random = random ?? new Random();
        Enter(BuddyState.Sit);
    }

    public BuddyState State { get; private set; }

    public double TimeInState { get; private set; }

    public DragGrip Grip { get; private set; }

    /// <summary>How long the user must be away from mouse and keyboard before the cat naps.</summary>
    public double NapAfterSeconds { get; set; } = 300;

    /// <summary>False while walking is paused, or when the taskbar is on a side of the screen.</summary>
    public bool WalkingAllowed { get; set; } = true;

    /// <summary>Following and pouncing on the mouse.</summary>
    public bool MouseGamesAllowed { get; set; } = true;

    public bool PlayAllowed { get; set; } = true;

    /// <summary>The mouse is on the cat's monitor, so following it makes sense.</summary>
    public bool CursorOnMonitor { get; set; }

    /// <summary>The mouse is just above the cat, close enough to pounce on.</summary>
    public bool CursorNear { get; set; }

    /// <summary>States the cat picks for itself, which napping, hunting and pausing may interrupt.</summary>
    private bool IsIdleLife => State is BuddyState.Sit or BuddyState.Walk or BuddyState.Scratch
        or BuddyState.Look or BuddyState.Follow or BuddyState.Play;

    private bool IsMoving => State is BuddyState.Walk or BuddyState.Follow or BuddyState.Play;

    public void Tick(double elapsedSeconds, double userIdleSeconds)
    {
        TimeInState += elapsedSeconds;
        _huntCooldown = Math.Max(0, _huntCooldown - elapsedSeconds);
        _lookCooldown = Math.Max(0, _lookCooldown - elapsedSeconds);

        switch (State)
        {
            case BuddyState.Dragged:
            case BuddyState.Falling:
            case BuddyState.Pounce:
                return;
            case BuddyState.Petted:
                if (TimeInState >= PetSeconds)
                    Enter(BuddyState.Sit);
                return;
            case BuddyState.Celebrate:
                if (TimeInState >= CelebrateSeconds)
                    Enter(BuddyState.Sit);
                return;
            case BuddyState.Stalk:
                if (!MouseGamesAllowed || !WalkingAllowed)
                    Enter(BuddyState.Sit);
                else if (TimeInState >= StalkSeconds)
                    Enter(BuddyState.Pounce);
                return;
            case BuddyState.Nap:
                if (userIdleSeconds < WakeIdleSeconds)
                    Enter(BuddyState.Sit);
                return;
        }

        if (userIdleSeconds >= NapAfterSeconds)
        {
            Enter(BuddyState.Nap);
            return;
        }

        if ((IsMoving && !WalkingAllowed)
            || (State == BuddyState.Follow && (!MouseGamesAllowed || !CursorOnMonitor))
            || (State == BuddyState.Play && !PlayAllowed))
        {
            Enter(BuddyState.Sit);
            return;
        }

        if (CanHunt && _random.NextDouble() < HuntChancePerSecond * elapsedSeconds)
        {
            Enter(BuddyState.Stalk);
            return;
        }

        bool done = State switch
        {
            BuddyState.Scratch => TimeInState >= ScratchSeconds,
            BuddyState.Look => TimeInState >= LookSeconds,
            _ => (_moodTimeLeft -= elapsedSeconds) <= 0,
        };
        if (done)
            Enter(State == BuddyState.Sit ? PickActivity() : BuddyState.Sit);
    }

    private bool CanHunt => CursorNear && MouseGamesAllowed && WalkingAllowed && _huntCooldown <= 0
        && State is BuddyState.Sit or BuddyState.Walk or BuddyState.Follow or BuddyState.Look;

    /// <summary>What a sitting cat does next.</summary>
    private BuddyState PickActivity()
    {
        double r = _random.NextDouble();
        if (!WalkingAllowed)
            return r < 0.2 ? BuddyState.Scratch : BuddyState.Sit;
        if (r < 0.45) return BuddyState.Walk;
        if (r < 0.57) return BuddyState.Scratch;
        if (r < 0.67 && PlayAllowed) return BuddyState.Play;
        if (r < 0.77 && MouseGamesAllowed && CursorOnMonitor) return BuddyState.Follow;
        return BuddyState.Sit;
    }

    /// <summary>A click on the cat. Wakes it from a nap too.</summary>
    public void Pet()
    {
        if (State is BuddyState.Dragged or BuddyState.Falling or BuddyState.Pounce)
            return;
        Enter(BuddyState.Petted);
    }

    /// <summary>
    /// A window opened, moved or flashed on the cat's monitor.
    /// Returns true when the cat turns to look, so the caller can face it that way.
    /// </summary>
    public bool Notice()
    {
        if (_lookCooldown > 0 || State is not (BuddyState.Sit or BuddyState.Walk or BuddyState.Scratch or BuddyState.Follow))
            return false;
        _lookCooldown = LookCooldownSeconds;
        Enter(BuddyState.Look);
        return true;
    }

    /// <summary>A focus session ended.</summary>
    public void Celebrate()
    {
        if (State is BuddyState.Dragged or BuddyState.Falling or BuddyState.Pounce)
            return;
        Enter(BuddyState.Celebrate);
    }

    public void BeginDrag(DragGrip grip = DragGrip.Scruff)
    {
        Grip = grip;
        Enter(BuddyState.Dragged);
    }

    public void EndDrag()
    {
        if (State == BuddyState.Dragged)
            Enter(BuddyState.Falling);
    }

    /// <summary>Back on its feet after a drop or a pounce.</summary>
    public void Landed()
    {
        if (State == BuddyState.Pounce)
            _huntCooldown = HuntCooldownSeconds;
        if (State is BuddyState.Falling or BuddyState.Pounce)
            Enter(BuddyState.Sit);
    }

    private void Enter(BuddyState state)
    {
        State = state;
        TimeInState = 0;
        _moodTimeLeft = state switch
        {
            BuddyState.Walk => Between(4, 12),
            BuddyState.Sit => Between(3, 8),
            BuddyState.Follow => Between(6, 12),
            BuddyState.Play => Between(10, 20),
            _ => 0,
        };
    }

    private double Between(double min, double max) => min + _random.NextDouble() * (max - min);
}
