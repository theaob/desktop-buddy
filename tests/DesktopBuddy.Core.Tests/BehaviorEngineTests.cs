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
}
