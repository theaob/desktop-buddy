using DesktopBuddy.Core;

namespace DesktopBuddy.Core.Tests;

public class BirdTests
{
    private const double Frame = 1.0 / 15;

    private static readonly CatBreed[] Birds =
    {
        CatBreed.Ember, CatBreed.Blueberry, CatBreed.Acorn, CatBreed.Sunny, CatBreed.Kiwi,
        CatBreed.Skye, CatBreed.Inky, CatBreed.Slate, CatBreed.Nugget, CatBreed.Rosehip,
    };

    private static readonly CatPose[] BirdPoses =
    {
        CatPose.PeckA, CatPose.PeckB, CatPose.SingA, CatPose.SingB, CatPose.FlapA, CatPose.FlapB,
    };

    private static readonly BuddyState[] BirdLife = { BuddyState.Peck, BuddyState.Sing, BuddyState.Flap };

    public static TheoryData<CatBreed> AllBirds()
    {
        var data = new TheoryData<CatBreed>();
        foreach (var bird in Birds)
            data.Add(bird);
        return data;
    }

    private static HashSet<BuddyState> StatesSeen(BehaviorEngine engine, double seconds)
    {
        var seen = new HashSet<BuddyState>();
        for (double t = 0; t < seconds; t += Frame)
        {
            engine.Tick(Frame, 0);
            seen.Add(engine.State);
        }
        return seen;
    }

    [Fact]
    public void The_birds_are_birds()
    {
        foreach (var bird in Birds)
            Assert.Equal(Species.Bird, Buddies.SpeciesOf(bird));
        Assert.Equal(Species.Dog, Buddies.SpeciesOf(CatBreed.Mocha));
        Assert.Equal(Species.Cat, Buddies.SpeciesOf(CatBreed.Original));
    }

    [Theory]
    [MemberData(nameof(AllBirds))]
    public void Every_bird_has_its_own_bird_poses(CatBreed bird)
    {
        var look = new CatLook(default, default, bird);
        foreach (var pose in BirdPoses)
            Assert.NotEqual(CatPixels.Argb(CatPose.Sit, look), CatPixels.Argb(pose, look));
        Assert.NotEqual(CatPixels.Argb(CatPose.PeckA, look), CatPixels.Argb(CatPose.PeckB, look));
        Assert.NotEqual(CatPixels.Argb(CatPose.SingA, look), CatPixels.Argb(CatPose.SingB, look));
        Assert.NotEqual(CatPixels.Argb(CatPose.FlapA, look), CatPixels.Argb(CatPose.FlapB, look));
    }

    [Theory]
    [InlineData(CatBreed.Original)]
    [InlineData(CatBreed.BeanRealistic)]
    [InlineData(CatBreed.Mocha)]
    public void A_cat_or_dog_asked_for_a_bird_pose_shows_a_plain_pose_instead(CatBreed breed)
    {
        var look = new CatLook(default, default, breed);
        Assert.Equal(CatPixels.Argb(CatPose.CrouchA, look), CatPixels.Argb(CatPose.PeckA, look));
        Assert.Equal(CatPixels.Argb(CatPose.Yawn, look), CatPixels.Argb(CatPose.SingA, look));
    }

    [Fact]
    public void A_bird_pecks_sings_and_flaps()
    {
        var engine = new BehaviorEngine(new Random(7)) { BirdAllowed = true, AskForScratchesAllowed = false };

        var seen = StatesSeen(engine, 30 * 60);

        Assert.All(BirdLife, state => Assert.Contains(state, seen));
    }

    [Fact]
    public void Cats_and_dogs_never_peck_or_sing()
    {
        var engine = new BehaviorEngine(new Random(7)) { BoneAllowed = true, AskForScratchesAllowed = false };

        var seen = StatesSeen(engine, 30 * 60);

        Assert.All(BirdLife, state => Assert.DoesNotContain(state, seen));
    }

    [Fact]
    public void Switching_away_from_a_bird_stops_it_singing_at_once()
    {
        var engine = new BehaviorEngine(new Random(7)) { BirdAllowed = true, AskForScratchesAllowed = false };
        for (int i = 0; i < 15 * 60 * 30 && engine.State != BuddyState.Sing; i++)
            engine.Tick(Frame, 0);
        Assert.Equal(BuddyState.Sing, engine.State);

        engine.BirdAllowed = false;
        engine.Tick(Frame, 0);

        Assert.Equal(BuddyState.Sit, engine.State);
    }

    [Fact]
    public void Bird_life_has_its_frames()
    {
        Assert.Equal(CatPose.PeckA, CatAnimation.PoseFor(BuddyState.Peck, 0.2));
        Assert.Equal(CatPose.PeckB, CatAnimation.PoseFor(BuddyState.Peck, 0.7));
        Assert.NotEqual(CatAnimation.PoseFor(BuddyState.Sing, 0.1), CatAnimation.PoseFor(BuddyState.Sing, 0.8));
        Assert.NotEqual(CatAnimation.PoseFor(BuddyState.Flap, 0.05), CatAnimation.PoseFor(BuddyState.Flap, 0.22));
    }

    [Fact]
    public void Birds_talk_like_birds()
    {
        var bird = SpeechLines.For(Species.Bird);

        Assert.NotEqual(SpeechLines.For(Species.Cat).Woken, bird.Woken);
        Assert.NotEqual(SpeechLines.For(Species.Dog).Woken, bird.Woken);
        Assert.DoesNotContain(bird.Thanks.Concat(bird.Woken).Concat(bird.Ask), line => line.Contains("Purr") || line.Contains("Wuf"));
        Assert.All(new[] { bird.Woken, bird.GrabbedAwake, bird.Ask, bird.Thanks, bird.Sulk }, Assert.NotEmpty);
    }
}
