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
    WakeUp,     // slowly waking from a nap: blinks, stretches, yawns
    Landing,    // squashed for a moment after a drop or a pounce
    Doze,       // a short snooze sitting up, snot bubble and all
    Groom,      // licks a paw and wipes its face
    Loaf,       // paws tucked under, content
    BellyUp,    // rolls over to show its belly after a lot of petting
    Startle,    // fur on end for a moment when a window flashes for attention
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
    public const double WakeUpSeconds = 2.8;
    public const double LandingSeconds = 0.4;
    public const double GroomSeconds = 3.2;
    public const double BellyUpSeconds = 3.2;
    public const double StartleSeconds = 1.2;

    // This many clicks within the window rolls the cat onto its back.
    public const int BellyUpPets = 3;
    public const double BellyUpWithinSeconds = 4;

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
    private double _clock;
    private readonly Queue<double> _petTimes = new();

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
        or BuddyState.Look or BuddyState.Follow or BuddyState.Play or BuddyState.Doze
        or BuddyState.Groom or BuddyState.Loaf;

    private bool IsMoving => State is BuddyState.Walk or BuddyState.Follow or BuddyState.Play;

    public void Tick(double elapsedSeconds, double userIdleSeconds)
    {
        TimeInState += elapsedSeconds;
        _clock += elapsedSeconds;
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
            case BuddyState.WakeUp:
                if (TimeInState >= WakeUpSeconds)
                    Enter(BuddyState.Sit);
                return;
            case BuddyState.Landing:
                if (TimeInState >= LandingSeconds)
                    Enter(BuddyState.Sit);
                return;
            case BuddyState.BellyUp:
                if (TimeInState >= BellyUpSeconds)
                    Enter(BuddyState.Sit);
                return;
            case BuddyState.Startle:
                if (TimeInState >= StartleSeconds)
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
                    Enter(BuddyState.WakeUp);
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
            BuddyState.Groom => TimeInState >= GroomSeconds,
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
            return r < 0.15 ? BuddyState.Scratch : r < 0.3 ? BuddyState.Groom : r < 0.42 ? BuddyState.Doze
                : r < 0.55 ? BuddyState.Loaf : BuddyState.Sit;
        if (r < 0.36) return BuddyState.Walk;
        if (r < 0.44) return BuddyState.Scratch;
        if (r < 0.52) return BuddyState.Groom;
        if (r < 0.59) return BuddyState.Doze;
        if (r < 0.66) return BuddyState.Loaf;
        if (r < 0.74 && PlayAllowed) return BuddyState.Play;
        if (r < 0.82 && MouseGamesAllowed && CursorOnMonitor) return BuddyState.Follow;
        return BuddyState.Sit;
    }

    /// <summary>A click on the cat. Wakes it from a nap too; a few in a row and it rolls over for belly rubs.</summary>
    public void Pet()
    {
        if (State is BuddyState.Dragged or BuddyState.Falling or BuddyState.Pounce)
            return;
        _petTimes.Enqueue(_clock);
        while (_petTimes.Count > 0 && _clock - _petTimes.Peek() > BellyUpWithinSeconds)
            _petTimes.Dequeue();
        if (State == BuddyState.BellyUp)
            return;
        if (_petTimes.Count >= BellyUpPets && State != BuddyState.Nap)
        {
            _petTimes.Clear();
            Enter(BuddyState.BellyUp);
            return;
        }
        Enter(BuddyState.Petted);
    }

    /// <summary>
    /// A window opened, moved or flashed on the cat's monitor. A flash asking for attention
    /// (<paramref name="urgent"/>) startles it; anything else makes it look.
    /// Returns true when the cat reacts, so the caller can face it that way.
    /// </summary>
    public bool Notice(bool urgent = false)
    {
        if (_lookCooldown > 0 || State is not (BuddyState.Sit or BuddyState.Walk or BuddyState.Scratch or BuddyState.Follow
            or BuddyState.Doze or BuddyState.Groom or BuddyState.Loaf))
            return false;
        _lookCooldown = LookCooldownSeconds;
        Enter(urgent ? BuddyState.Startle : BuddyState.Look);
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
            Enter(BuddyState.Landing);
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
            BuddyState.Doze => Between(8, 20),
            BuddyState.Loaf => Between(10, 25),
            _ => 0,
        };
    }

    private double Between(double min, double max) => min + _random.NextDouble() * (max - min);
}
