namespace DesktopBuddy.Core;

public enum CatPose
{
    WalkA, WalkB, Sit, SitBlink, NapA, NapB, PettedA, PettedB, Dangle,
    HeldBelly, Leap, CrouchA, CrouchB, ScratchA, ScratchB, Bat, LookUp,
    FallA, FallB, Land, Drowsy, Stretch, Yawn,
}

public static class CatAnimation
{
    private const double StepsPerSecond = 5;
    private const double BlinkEverySeconds = 4;
    private const double BlinkSeconds = 0.15;
    private const double BreathSeconds = 1.2;
    private const double HeartBobsPerSecond = 4;
    private const double ScratchesPerSecond = 8;
    private const double WigglesPerSecond = 6;
    private const double FlailsPerSecond = 7;

    // Waking up: drowsy until the first mark, stretching until the second, then a yawn.
    private const double DrowsyUntil = 0.9;
    private const double StretchUntil = 2.0;

    /// <summary>Which frame to show for a state that has lasted <paramref name="secondsInState"/>.</summary>
    /// <param name="moving">For following and playing: false while the cat stands still (under the mouse, or batting the ball).</param>
    /// <param name="grip">How a carried cat hangs.</param>
    public static CatPose PoseFor(BuddyState state, double secondsInState, bool moving = true, DragGrip grip = DragGrip.Scruff) => state switch
    {
        BuddyState.Walk => Steps(secondsInState),
        BuddyState.Sit => Sitting(secondsInState),
        BuddyState.Nap => Alternate(secondsInState / BreathSeconds, CatPose.NapA, CatPose.NapB),
        BuddyState.Petted or BuddyState.Celebrate => Alternate(secondsInState * HeartBobsPerSecond, CatPose.PettedA, CatPose.PettedB),
        BuddyState.Scratch => Alternate(secondsInState * ScratchesPerSecond, CatPose.ScratchA, CatPose.ScratchB),
        BuddyState.Look => CatPose.LookUp,
        BuddyState.Follow => moving ? Steps(secondsInState) : Sitting(secondsInState),
        BuddyState.Play => moving ? Steps(secondsInState) : CatPose.Bat,
        BuddyState.Stalk => Alternate(secondsInState * WigglesPerSecond, CatPose.CrouchA, CatPose.CrouchB),
        BuddyState.Pounce => CatPose.Leap,
        BuddyState.Falling => Alternate(secondsInState * FlailsPerSecond, CatPose.FallA, CatPose.FallB),
        BuddyState.Landing => CatPose.Land,
        BuddyState.WakeUp => secondsInState < DrowsyUntil ? CatPose.Drowsy
            : secondsInState < StretchUntil ? CatPose.Stretch
            : CatPose.Yawn,
        _ => grip == DragGrip.Belly ? CatPose.HeldBelly : CatPose.Dangle,
    };

    private static CatPose Steps(double t) => Alternate(t * StepsPerSecond, CatPose.WalkA, CatPose.WalkB);

    private static CatPose Sitting(double t) =>
        t % BlinkEverySeconds >= BlinkEverySeconds - BlinkSeconds ? CatPose.SitBlink : CatPose.Sit;

    private static CatPose Alternate(double phase, CatPose even, CatPose odd) => (long)phase % 2 == 0 ? even : odd;
}
