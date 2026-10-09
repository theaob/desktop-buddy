namespace DesktopBuddy.Core;

public enum BuddyState { Walk, Sit, Nap, Petted, Dragged, Falling }

/// <summary>
/// Decides what the cat is doing. Pure logic: the caller feeds it elapsed time and the user's
/// idle time each frame, and reports clicks and drags; the window turns the state into pixels.
/// </summary>
public sealed class BehaviorEngine
{
    public const double PetSeconds = 1.6;

    // Any input within this window wakes a napping cat.
    public const double WakeIdleSeconds = 2;

    private readonly Random _random;
    private double _moodTimeLeft;

    public BehaviorEngine(Random? random = null)
    {
        _random = random ?? new Random();
        Enter(BuddyState.Sit);
    }

    public BuddyState State { get; private set; }

    public double TimeInState { get; private set; }

    /// <summary>How long the user must be away from mouse and keyboard before the cat naps.</summary>
    public double NapAfterSeconds { get; set; } = 300;

    /// <summary>False while walking is paused, or when the taskbar is on a side of the screen.</summary>
    public bool WalkingAllowed { get; set; } = true;

    public void Tick(double elapsedSeconds, double userIdleSeconds)
    {
        TimeInState += elapsedSeconds;

        switch (State)
        {
            case BuddyState.Dragged:
            case BuddyState.Falling:
                return;
            case BuddyState.Petted:
                if (TimeInState >= PetSeconds)
                    Enter(BuddyState.Sit);
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

        if (State == BuddyState.Walk && !WalkingAllowed)
        {
            Enter(BuddyState.Sit);
            return;
        }

        _moodTimeLeft -= elapsedSeconds;
        if (_moodTimeLeft > 0)
            return;

        bool walk = State == BuddyState.Sit && WalkingAllowed && _random.NextDouble() < 0.7;
        Enter(walk ? BuddyState.Walk : BuddyState.Sit);
    }

    /// <summary>A click on the cat. Wakes it from a nap too.</summary>
    public void Pet()
    {
        if (State is BuddyState.Dragged or BuddyState.Falling)
            return;
        Enter(BuddyState.Petted);
    }

    public void BeginDrag() => Enter(BuddyState.Dragged);

    public void EndDrag()
    {
        if (State == BuddyState.Dragged)
            Enter(BuddyState.Falling);
    }

    public void Landed()
    {
        if (State == BuddyState.Falling)
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
            _ => 0,
        };
    }

    private double Between(double min, double max) => min + _random.NextDouble() * (max - min);
}
