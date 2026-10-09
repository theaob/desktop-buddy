using DesktopBuddy.Core;

namespace DesktopBuddy.Core.Tests;

public class BehaviorEngineTests
{
    private const double Frame = 1.0 / 15;
    private const double Active = 0;  // user idle seconds while they're using the PC

    private static BehaviorEngine NewEngine() => new(new Random(42));

    private static void Run(BehaviorEngine engine, double seconds, double idle = Active)
    {
        for (double t = 0; t < seconds; t += Frame)
            engine.Tick(Frame, idle);
    }

    [Fact]
    public void Starts_sitting()
    {
        Assert.Equal(BuddyState.Sit, NewEngine().State);
    }

    [Fact]
    public void Walks_sometimes_when_allowed()
    {
        var engine = NewEngine();
        bool walked = false;
        for (int i = 0; i < 15 * 120 && !walked; i++)
        {
            engine.Tick(Frame, Active);
            walked = engine.State == BuddyState.Walk;
        }
        Assert.True(walked);
    }

    [Fact]
    public void Never_walks_when_walking_is_not_allowed()
    {
        var engine = NewEngine();
        engine.WalkingAllowed = false;
        for (int i = 0; i < 15 * 120; i++)
        {
            engine.Tick(Frame, Active);
            Assert.NotEqual(BuddyState.Walk, engine.State);
        }
    }

    [Fact]
    public void Stops_walking_as_soon_as_walking_is_paused()
    {
        var engine = NewEngine();
        while (engine.State != BuddyState.Walk)
            engine.Tick(Frame, Active);

        engine.WalkingAllowed = false;
        engine.Tick(Frame, Active);

        Assert.Equal(BuddyState.Sit, engine.State);
    }

    [Fact]
    public void Petting_shows_a_reaction_then_sits()
    {
        var engine = NewEngine();
        engine.Pet();
        Assert.Equal(BuddyState.Petted, engine.State);

        Run(engine, BehaviorEngine.PetSeconds + Frame);

        Assert.Equal(BuddyState.Sit, engine.State);
    }

    [Fact]
    public void Naps_when_the_user_is_away_and_wakes_on_input()
    {
        var engine = NewEngine();
        engine.NapAfterSeconds = 60;

        engine.Tick(Frame, userIdleSeconds: 61);
        Assert.Equal(BuddyState.Nap, engine.State);

        engine.Tick(Frame, userIdleSeconds: 120);
        Assert.Equal(BuddyState.Nap, engine.State);

        engine.Tick(Frame, userIdleSeconds: 0.5);
        Assert.Equal(BuddyState.Sit, engine.State);
    }

    [Fact]
    public void Petting_wakes_a_napping_cat()
    {
        var engine = NewEngine();
        engine.NapAfterSeconds = 60;
        engine.Tick(Frame, userIdleSeconds: 61);

        engine.Pet();

        Assert.Equal(BuddyState.Petted, engine.State);
    }

    [Fact]
    public void Drag_then_drop_falls_then_sits_on_landing()
    {
        var engine = NewEngine();

        engine.BeginDrag();
        Run(engine, 30, idle: 1000);  // ticks and idle time don't interrupt a drag
        Assert.Equal(BuddyState.Dragged, engine.State);

        engine.EndDrag();
        Run(engine, 5);
        Assert.Equal(BuddyState.Falling, engine.State);

        engine.Landed();
        Assert.Equal(BuddyState.Sit, engine.State);
    }

    [Fact]
    public void Petting_is_ignored_while_dragged()
    {
        var engine = NewEngine();
        engine.BeginDrag();

        engine.Pet();

        Assert.Equal(BuddyState.Dragged, engine.State);
    }

    [Fact]
    public void Landing_or_dropping_out_of_order_does_nothing()
    {
        var engine = NewEngine();

        engine.EndDrag();
        engine.Landed();

        Assert.Equal(BuddyState.Sit, engine.State);
    }

    private static bool RunUntil(BehaviorEngine engine, BuddyState state, double seconds = 600)
    {
        for (double t = 0; t < seconds; t += Frame)
        {
            engine.Tick(Frame, Active);
            if (engine.State == state)
                return true;
        }
        return false;
    }

    [Fact]
    public void Scratches_sometimes()
    {
        Assert.True(RunUntil(NewEngine(), BuddyState.Scratch));
    }

    [Fact]
    public void Plays_with_yarn_sometimes_but_not_when_play_is_off()
    {
        Assert.True(RunUntil(NewEngine(), BuddyState.Play));

        var engine = NewEngine();
        engine.PlayAllowed = false;
        Assert.False(RunUntil(engine, BuddyState.Play));
    }

    [Fact]
    public void Follows_the_mouse_only_while_it_is_on_the_cats_monitor()
    {
        var away = NewEngine();
        away.CursorOnMonitor = false;
        Assert.False(RunUntil(away, BuddyState.Follow));

        var engine = NewEngine();
        engine.CursorOnMonitor = true;
        Assert.True(RunUntil(engine, BuddyState.Follow));

        engine.CursorOnMonitor = false;
        engine.Tick(Frame, Active);
        Assert.Equal(BuddyState.Sit, engine.State);
    }

    [Fact]
    public void Stalks_a_nearby_mouse_then_pounces_then_rests_from_hunting()
    {
        var engine = NewEngine();
        engine.CursorNear = true;

        Assert.True(RunUntil(engine, BuddyState.Stalk, 120));
        Run(engine, BehaviorEngine.StalkSeconds + Frame);
        Assert.Equal(BuddyState.Pounce, engine.State);

        Run(engine, 5);  // stays in the air until the window says it landed
        Assert.Equal(BuddyState.Pounce, engine.State);

        engine.Landed();
        Assert.Equal(BuddyState.Sit, engine.State);
        Assert.False(RunUntil(engine, BuddyState.Stalk, BehaviorEngine.HuntCooldownSeconds - 1));
    }

    [Fact]
    public void Never_hunts_when_mouse_games_are_off()
    {
        var engine = NewEngine();
        engine.CursorNear = true;
        engine.MouseGamesAllowed = false;

        Assert.False(RunUntil(engine, BuddyState.Stalk));
    }

    [Fact]
    public void Looks_at_window_activity_then_carries_on()
    {
        var engine = NewEngine();

        Assert.True(engine.Notice());
        Assert.Equal(BuddyState.Look, engine.State);

        Assert.False(engine.Notice());  // too soon to turn again
        Run(engine, BehaviorEngine.LookSeconds + Frame);
        Assert.Equal(BuddyState.Sit, engine.State);
    }

    [Fact]
    public void Ignores_window_activity_while_napping_or_carried()
    {
        var engine = NewEngine();
        engine.NapAfterSeconds = 60;
        engine.Tick(Frame, userIdleSeconds: 61);
        Assert.False(engine.Notice());

        engine.BeginDrag();
        Assert.False(engine.Notice());
    }

    [Fact]
    public void Celebrates_then_sits()
    {
        var engine = NewEngine();
        engine.Celebrate();
        Assert.Equal(BuddyState.Celebrate, engine.State);

        Run(engine, BehaviorEngine.CelebrateSeconds + Frame);
        Assert.Equal(BuddyState.Sit, engine.State);
    }

    [Theory]
    [InlineData(DragGrip.Scruff)]
    [InlineData(DragGrip.Belly)]
    public void Remembers_how_it_was_picked_up(DragGrip grip)
    {
        var engine = NewEngine();
        engine.BeginDrag(grip);

        Assert.Equal(grip, engine.Grip);
    }
}
