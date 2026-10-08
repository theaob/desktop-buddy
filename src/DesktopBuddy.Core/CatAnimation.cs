namespace DesktopBuddy.Core;

public enum CatPose { WalkA, WalkB, Sit, SitBlink, NapA, NapB, PettedA, PettedB, Dangle }

public static class CatAnimation
{
    private const double StepsPerSecond = 5;
    private const double BlinkEverySeconds = 4;
    private const double BlinkSeconds = 0.15;
    private const double BreathSeconds = 1.2;
    private const double HeartBobsPerSecond = 4;

    /// <summary>Which frame to show for a state that has lasted <paramref name="secondsInState"/>.</summary>
    public static CatPose PoseFor(BuddyState state, double secondsInState) => state switch
    {
        BuddyState.Walk => Alternate(secondsInState * StepsPerSecond, CatPose.WalkA, CatPose.WalkB),
        BuddyState.Sit => secondsInState % BlinkEverySeconds >= BlinkEverySeconds - BlinkSeconds ? CatPose.SitBlink : CatPose.Sit,
        BuddyState.Nap => Alternate(secondsInState / BreathSeconds, CatPose.NapA, CatPose.NapB),
        BuddyState.Petted => Alternate(secondsInState * HeartBobsPerSecond, CatPose.PettedA, CatPose.PettedB),
        _ => CatPose.Dangle,
    };

    private static CatPose Alternate(double phase, CatPose even, CatPose odd) => (long)phase % 2 == 0 ? even : odd;
}
