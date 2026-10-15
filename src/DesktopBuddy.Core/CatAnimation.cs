namespace DesktopBuddy.Core;

public enum CatPose
{
    WalkA, WalkB, Sit, SitBlink, NapA, NapB, PettedA, PettedB, Dangle,
    HeldBelly, Leap, CrouchA, CrouchB, ScratchA, ScratchB, Bat, LookUp,
    FallA, FallB, Land, Drowsy, Stretch, Yawn,
    SitTail, GroomA, GroomB, Loaf, LoafBlink, BellyA, BellyB, Startle, Curl, RunA, RunB,
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
    private const double StridesPerSecond = 8;
    private const double GroomStrokesPerSecond = 3;
    private const double RollsPerSecond = 3;
    private const double BegWigglesPerSecond = 1.5;

    // A long nap ends up curled into a ball.
    public const double CurlAfterSeconds = 60;

    // A sitting cat swishes its tail now and then; a loafing one slow-blinks.
    private const double SwishEverySeconds = 4;
    private const double SwishFrom = 1.6;
    private const double SwishUntil = 2.2;
    private const double SwishesPerSecond = 5;
    private const double SlowBlinkEverySeconds = 5;
    private const double SlowBlinkSeconds = 0.5;

    // Waking up: drowsy until the first mark, stretching until the second, then a yawn.
    private const double DrowsyUntil = 0.9;
    private const double StretchUntil = 2.0;

    // Clicked awake: fur on end until the first mark, a glare until the second, then a yawn.
    private const double WokenStartleUntil = 0.8;
    private const double WokenGlareUntil = 2.2;

    // The snot bubble swells and shrinks with each slow breath, and pops as the cat wakes.
    private static readonly int[] BubbleCycle = { 0, 1, 2, 3, 3, 2, 1, 0 };
    private const double BubbleStepSeconds = 0.45;
    private const double PopSeconds = 0.3;

    /// <summary>Snot bubble size for this moment: 0 none, 1 to 3 growing, or <see cref="CatPixels.BubblePop"/> as it bursts.</summary>
    public static int SnotBubble(BuddyState state, double secondsInState) => state switch
    {
        BuddyState.Nap or BuddyState.Doze => BubbleCycle[(int)(secondsInState / BubbleStepSeconds) % BubbleCycle.Length],
        BuddyState.WakeUp or BuddyState.Woken when secondsInState < PopSeconds => CatPixels.BubblePop,
        _ => 0,
    };

    /// <summary>Which frame to show for a state that has lasted <paramref name="secondsInState"/>.</summary>
    /// <param name="moving">For following and playing: false while the cat stands still (under the mouse, or batting the ball).</param>
    /// <param name="grip">How a carried cat hangs.</param>
    /// <param name="running">The cat is in a hurry (a busy PC, or chasing the yarn), so it gallops instead of walking.</param>
    public static CatPose PoseFor(BuddyState state, double secondsInState, bool moving = true, DragGrip grip = DragGrip.Scruff,
        bool running = false) => state switch
    {
        BuddyState.Walk => running ? Gallop(secondsInState) : Steps(secondsInState),
        BuddyState.Sit => Sitting(secondsInState),
        BuddyState.Nap when secondsInState >= CurlAfterSeconds => CatPose.Curl,
        BuddyState.Nap => Alternate(secondsInState / BreathSeconds, CatPose.NapA, CatPose.NapB),
        BuddyState.Petted or BuddyState.Celebrate => Alternate(secondsInState * HeartBobsPerSecond, CatPose.PettedA, CatPose.PettedB),
        BuddyState.Scratch => Alternate(secondsInState * ScratchesPerSecond, CatPose.ScratchA, CatPose.ScratchB),
        BuddyState.Look => CatPose.LookUp,
        BuddyState.Doze => CatPose.SitBlink,
        BuddyState.Follow => moving ? Steps(secondsInState) : Sitting(secondsInState),
        BuddyState.Play => !moving ? CatPose.Bat : running ? Gallop(secondsInState) : Steps(secondsInState),
        BuddyState.Groom => Alternate(secondsInState * GroomStrokesPerSecond, CatPose.GroomA, CatPose.GroomB),
        BuddyState.Loaf => secondsInState % SlowBlinkEverySeconds >= SlowBlinkEverySeconds - SlowBlinkSeconds ? CatPose.LoafBlink : CatPose.Loaf,
        BuddyState.BellyUp => Alternate(secondsInState * RollsPerSecond, CatPose.BellyA, CatPose.BellyB),
        BuddyState.Startle => CatPose.Startle,
        BuddyState.AskBelly => Alternate(secondsInState * BegWigglesPerSecond, CatPose.BellyA, CatPose.BellyB),
        BuddyState.Sulk => CatPose.Loaf,
        BuddyState.Stalk => Alternate(secondsInState * WigglesPerSecond, CatPose.CrouchA, CatPose.CrouchB),
        BuddyState.Pounce => CatPose.Leap,
        BuddyState.Falling => Alternate(secondsInState * FlailsPerSecond, CatPose.FallA, CatPose.FallB),
        BuddyState.Landing => CatPose.Land,
        BuddyState.WakeUp => secondsInState < DrowsyUntil ? CatPose.Drowsy
            : secondsInState < StretchUntil ? CatPose.Stretch
            : CatPose.Yawn,
        BuddyState.Woken => secondsInState < WokenStartleUntil ? CatPose.Startle
            : secondsInState < WokenGlareUntil ? CatPose.Drowsy
            : CatPose.Yawn,
        _ => grip == DragGrip.Belly ? CatPose.HeldBelly : CatPose.Dangle,
    };

    private static CatPose Steps(double t) => Alternate(t * StepsPerSecond, CatPose.WalkA, CatPose.WalkB);

    private static CatPose Gallop(double t) => Alternate(t * StridesPerSecond, CatPose.RunA, CatPose.RunB);

    private static CatPose Sitting(double t)
    {
        if (t % BlinkEverySeconds >= BlinkEverySeconds - BlinkSeconds)
            return CatPose.SitBlink;
        double swish = t % SwishEverySeconds;
        return swish >= SwishFrom && swish < SwishUntil ? Alternate(t * SwishesPerSecond, CatPose.SitTail, CatPose.Sit) : CatPose.Sit;
    }

    private static CatPose Alternate(double phase, CatPose even, CatPose odd) => (long)phase % 2 == 0 ? even : odd;
}
