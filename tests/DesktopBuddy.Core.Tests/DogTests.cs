using DesktopBuddy.Core;

namespace DesktopBuddy.Core.Tests;

public class DogTests
{
    private const double Frame = 1.0 / 15;

    private static readonly CatBreed[] Dogs =
    {
        CatBreed.Mocha, CatBreed.Juniper, CatBreed.Domino, CatBreed.Pip, CatBreed.Toast,
        CatBreed.Clover, CatBreed.Waffles, CatBreed.Hazel, CatBreed.Peaches,
    };

    private static readonly CatPose[] BonePoses =
    {
        CatPose.CarryA, CatPose.CarryB, CatPose.ChewA, CatPose.ChewB, CatPose.Toss, CatPose.Catch, CatPose.PlayBow,
    };

    private static readonly BuddyState[] BonePlay = { BuddyState.PlayBow, BuddyState.Carry, BuddyState.Chew, BuddyState.BoneToss };

    public static TheoryData<CatBreed> AllDogs()
    {
        var data = new TheoryData<CatBreed>();
        foreach (var dog in Dogs)
            data.Add(dog);
        return data;
    }

    /// <summary>Every state the engine goes through in this many seconds of the user being at the PC.</summary>
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
    public void The_dogs_are_dogs_and_everyone_else_is_a_cat()
    {
        foreach (var breed in Enum.GetValues<CatBreed>())
            Assert.Equal(Dogs.Contains(breed) ? Species.Dog : Species.Cat, Buddies.SpeciesOf(breed));
    }

    [Theory]
    [MemberData(nameof(AllDogs))]
    public void Every_dog_has_its_own_bone_poses(CatBreed dog)
    {
        var look = new CatLook(default, default, dog);
        foreach (var pose in BonePoses)
            Assert.NotEqual(CatPixels.Argb(CatPose.Sit, look), CatPixels.Argb(pose, look));
        Assert.NotEqual(CatPixels.Argb(CatPose.WalkA, look), CatPixels.Argb(CatPose.CarryA, look));
        Assert.NotEqual(CatPixels.Argb(CatPose.CarryA, look), CatPixels.Argb(CatPose.CarryB, look));
        Assert.NotEqual(CatPixels.Argb(CatPose.ChewA, look), CatPixels.Argb(CatPose.ChewB, look));
    }

    [Theory]
    [InlineData(CatBreed.Original)]
    [InlineData(CatBreed.BeanRealistic)]
    [InlineData(CatBreed.SproutCartoon)]
    public void A_cat_asked_for_a_bone_pose_shows_the_plain_pose_instead(CatBreed cat)
    {
        var look = new CatLook(default, default, cat);
        Assert.Equal(CatPixels.Argb(CatPose.WalkA, look), CatPixels.Argb(CatPose.CarryA, look));
        Assert.Equal(CatPixels.Argb(CatPose.Stretch, look), CatPixels.Argb(CatPose.PlayBow, look));
    }

    [Fact]
    public void A_dog_plays_with_its_bone_in_every_way()
    {
        var engine = new BehaviorEngine(new Random(7)) { BoneAllowed = true, AskForScratchesAllowed = false };

        var seen = StatesSeen(engine, 30 * 60);

        Assert.All(BonePlay, state => Assert.Contains(state, seen));
    }

    [Fact]
    public void Without_a_bone_there_is_no_bone_play()
    {
        var engine = new BehaviorEngine(new Random(7)) { AskForScratchesAllowed = false };

        var seen = StatesSeen(engine, 30 * 60);

        Assert.All(BonePlay, state => Assert.DoesNotContain(state, seen));
    }

    [Fact]
    public void A_dog_that_may_not_walk_still_chews_and_tosses_but_never_trots_off()
    {
        var engine = new BehaviorEngine(new Random(7)) { BoneAllowed = true, WalkingAllowed = false, AskForScratchesAllowed = false };

        var seen = StatesSeen(engine, 30 * 60);

        Assert.Contains(BuddyState.Chew, seen);
        Assert.Contains(BuddyState.BoneToss, seen);
        Assert.DoesNotContain(BuddyState.Carry, seen);
        Assert.DoesNotContain(BuddyState.PlayBow, seen);
    }

    [Fact]
    public void Turning_bone_play_off_puts_the_bone_down_at_once()
    {
        var engine = new BehaviorEngine(new Random(7)) { BoneAllowed = true, AskForScratchesAllowed = false };
        for (int i = 0; i < 15 * 60 * 30 && engine.State != BuddyState.Chew; i++)
            engine.Tick(Frame, 0);
        Assert.Equal(BuddyState.Chew, engine.State);

        engine.BoneAllowed = false;
        engine.Tick(Frame, 0);

        Assert.Equal(BuddyState.Sit, engine.State);
    }

    [Fact]
    public void A_play_bow_nobody_answers_ends_with_the_dog_trotting_off_with_its_bone()
    {
        var engine = new BehaviorEngine(new Random(7)) { BoneAllowed = true, AskForScratchesAllowed = false };
        for (int i = 0; i < 15 * 60 * 30 && engine.State != BuddyState.PlayBow; i++)
            engine.Tick(Frame, 0);
        Assert.Equal(BuddyState.PlayBow, engine.State);

        for (double t = 0; t < BehaviorEngine.PlayBowSeconds + 0.2; t += Frame)
            engine.Tick(Frame, 0);

        Assert.Equal(BuddyState.Carry, engine.State);
    }

    [Fact]
    public void Bone_play_has_its_frames()
    {
        Assert.Contains(CatAnimation.PoseFor(BuddyState.Carry, 0.1), new[] { CatPose.CarryA, CatPose.CarryB });
        Assert.Equal(CatPose.Catch, CatAnimation.PoseFor(BuddyState.Carry, 0.1, moving: false));
        Assert.Contains(CatAnimation.PoseFor(BuddyState.Chew, 0.1), new[] { CatPose.ChewA, CatPose.ChewB });
        Assert.NotEqual(CatAnimation.PoseFor(BuddyState.Chew, 0.1), CatAnimation.PoseFor(BuddyState.Chew, 0.5));
        Assert.Equal(CatPose.Toss, CatAnimation.PoseFor(BuddyState.BoneToss, 0.2));
        Assert.Equal(CatPose.Catch, CatAnimation.PoseFor(BuddyState.BoneToss, 0.9));
        Assert.Equal(CatPose.PlayBow, CatAnimation.PoseFor(BuddyState.PlayBow, 0.5));
    }

    [Fact]
    public void Dogs_and_cats_talk_differently()
    {
        var cat = SpeechLines.For(Species.Cat);
        var dog = SpeechLines.For(Species.Dog);

        Assert.NotEqual(cat.Woken, dog.Woken);
        Assert.NotEqual(cat.Thanks, dog.Thanks);
        Assert.DoesNotContain(dog.Thanks.Concat(dog.Woken).Concat(dog.Ask), line => line.Contains("Purr") || line.Contains("Mrr"));
        foreach (var lines in new[] { cat, dog })
            Assert.All(new[] { lines.Woken, lines.GrabbedAwake, lines.Ask, lines.Thanks, lines.Sulk }, Assert.NotEmpty);
    }
}
