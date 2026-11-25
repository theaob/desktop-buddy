using DesktopBuddy.Core;

namespace DesktopBuddy.Core.Tests;

public class PoopTests
{
    private const double Frame = 1.0 / 15;
    private const double Active = 0;

    private static BehaviorEngine NewEngine() => new(new Random(42));

    private static void Run(BehaviorEngine engine, double seconds, double idle = Active)
    {
        for (double t = 0; t < seconds; t += Frame)
            engine.Tick(Frame, idle);
    }

    private static void SitDown(BehaviorEngine engine)
    {
        engine.WalkingAllowed = false;
        for (int i = 0; i < 15 * 60 && engine.State != BuddyState.Sit; i++)
            engine.Tick(Frame, Active);
        Assert.Equal(BuddyState.Sit, engine.State);
    }

    [Fact]
    public void Squats_after_a_while_of_use_and_leaves_a_pile_as_it_finishes()
    {
        var engine = NewEngine();
        engine.AskForScratchesAllowed = false;
        int piles = 0;
        engine.Pooped += () => piles++;

        Run(engine, BehaviorEngine.PoopEveryMinSeconds - 10);
        Assert.Equal(0, piles);

        bool squatted = false;
        for (double t = 0; t < BehaviorEngine.PoopEveryMaxSeconds && !squatted; t += Frame)
        {
            engine.Tick(Frame, Active);
            squatted = engine.State == BuddyState.Squat;
        }
        Assert.True(squatted);
        Assert.True(engine.PoopInSeconds >= BehaviorEngine.PoopEveryMinSeconds);

        Run(engine, BehaviorEngine.SquatSeconds - 0.5);
        Assert.Equal(0, piles);
        Run(engine, 0.5 + Frame);
        Assert.Equal(1, piles);
        Assert.NotEqual(BuddyState.Squat, engine.State);
    }

    [Fact]
    public void Walks_off_after_squatting_when_it_may_walk()
    {
        var engine = NewEngine();
        engine.Poop();
        Run(engine, BehaviorEngine.SquatSeconds + Frame);
        Assert.Equal(BuddyState.Walk, engine.State);

        engine.WalkingAllowed = false;
        SitDown(engine);
        engine.Poop();
        Run(engine, BehaviorEngine.SquatSeconds + Frame);
        Assert.Equal(BuddyState.Sit, engine.State);
    }

    [Fact]
    public void Never_squats_while_the_user_is_away_or_it_is_off()
    {
        var away = NewEngine();
        away.NapAfterSeconds = double.MaxValue;
        var off = NewEngine();
        off.PoopAllowed = false;
        for (double t = 0; t < BehaviorEngine.PoopEveryMaxSeconds * 2; t += Frame)
        {
            away.Tick(Frame, BehaviorEngine.AskOnlyWhileIdleUnderSeconds + 1);
            off.Tick(Frame, Active);
            Assert.NotEqual(BuddyState.Squat, away.State);
            Assert.NotEqual(BuddyState.Squat, off.State);
        }
    }

    [Fact]
    public void With_no_room_for_another_pile_it_asks_for_a_cleanup_instead()
    {
        var engine = NewEngine();
        engine.PoopRoom = false;
        int asks = 0;
        engine.WantsCleanup += () => asks++;

        engine.Poop();

        Assert.Equal(1, asks);
        Assert.NotEqual(BuddyState.Squat, engine.State);
        Assert.True(engine.PoopInSeconds >= BehaviorEngine.PoopEveryMinSeconds);
    }

    [Fact]
    public void Clicking_a_squatting_cat_does_not_interrupt_it()
    {
        var engine = NewEngine();
        engine.Poop();
        engine.Pet();
        engine.Pet();
        engine.Pet();
        Assert.Equal(BuddyState.Squat, engine.State);
    }

    [Fact]
    public void Picked_up_mid_squat_leaves_nothing_behind()
    {
        var engine = NewEngine();
        int piles = 0;
        engine.Pooped += () => piles++;
        engine.Poop();
        engine.BeginDrag();
        Run(engine, BehaviorEngine.SquatSeconds + 1);
        Assert.Equal(0, piles);
    }

    [Fact]
    public void Squat_looks_around_then_shuts_its_eyes()
    {
        Assert.Equal(CatPose.SquatA, CatAnimation.PoseFor(BuddyState.Squat, 0.2));
        Assert.Equal(CatPose.SquatB, CatAnimation.PoseFor(BuddyState.Squat, 2.5));
    }

    [Fact]
    public void Every_cat_has_both_squat_poses()
    {
        foreach (var breed in Enum.GetValues<CatBreed>())
            foreach (var pose in new[] { CatPose.SquatA, CatPose.SquatB })
                Assert.Equal(CatPixels.Height, CatPixels.Rows(breed, pose).Count);
    }

    [Fact]
    public void Birds_leave_a_splat_and_cats_and_dogs_a_pile()
    {
        Assert.Equal(PoopStyle.Splat, PoopPiles.StyleFor(Species.Bird));
        Assert.Equal(PoopStyle.Pile, PoopPiles.StyleFor(Species.Cat));
        Assert.Equal(PoopStyle.Pile, PoopPiles.StyleFor(Species.Dog));

        var piles = new PoopPiles();
        var splat = piles.Drop(10, PoopStyle.Splat)!;
        Assert.Equal(PoopArt.Splat0, PoopPiles.Frame(splat));
        piles.Tick(PoopPiles.StinkFrameSeconds + 0.01);
        Assert.Equal(PoopArt.Splat1, PoopPiles.Frame(splat));

        piles.Clean(splat.Id);
        Assert.Equal(PoopArt.PoofA, PoopPiles.Frame(splat));
    }

    [Fact]
    public void Every_kind_of_buddy_says_something_when_it_wants_or_gets_a_cleanup()
    {
        foreach (var species in Enum.GetValues<Species>())
        {
            Assert.NotEmpty(SpeechLines.For(species).Cleanup);
            Assert.NotEmpty(SpeechLines.For(species).Cleaned);
        }
    }

    [Fact]
    public void Keeps_at_most_three_piles()
    {
        var piles = new PoopPiles();
        for (int i = 0; i < PoopPiles.MaxPiles; i++)
            Assert.NotNull(piles.Drop(i * 100));

        Assert.False(piles.HasRoom);
        Assert.Null(piles.Drop(500));
        Assert.Equal(PoopPiles.MaxPiles, piles.Count);
    }

    [Fact]
    public void A_clicked_pile_puffs_away_then_is_gone()
    {
        var piles = new PoopPiles();
        var pile = piles.Drop(40)!;
        Assert.Equal(PoopArt.Idle0, PoopPiles.Frame(pile));
        piles.Tick(PoopPiles.StinkFrameSeconds + 0.01);
        Assert.Equal(PoopArt.Idle1, PoopPiles.Frame(pile));

        Assert.True(piles.Clean(pile.Id));
        Assert.False(piles.Clean(pile.Id));
        Assert.Equal(0, piles.Count);
        Assert.True(piles.HasRoom);
        Assert.Single(piles.All);  // still puffing
        Assert.Equal(PoopArt.PoofA, PoopPiles.Frame(pile));

        piles.Tick(PoopPiles.PoofSeconds * 0.6);
        Assert.Equal(PoopArt.PoofB, PoopPiles.Frame(pile));
        piles.Tick(PoopPiles.PoofSeconds);
        Assert.Empty(piles.All);
    }

    [Fact]
    public void Every_pile_picture_is_square_and_uses_known_colours()
    {
        for (int frame = 0; frame < PoopArt.FrameCount; frame++)
        {
            var rows = PoopArt.Rows(frame);
            Assert.Equal(PoopArt.Size, rows.Count);
            Assert.All(rows, row =>
            {
                Assert.Equal(PoopArt.Size, row.Length);
                Assert.All(row, c => Assert.True(c == '.' || PoopArt.Palette.ContainsKey(c), $"unknown pixel '{c}'"));
            });
            Assert.Contains(PoopArt.Argb(frame), p => p != 0);
        }
    }
}
