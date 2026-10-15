using DesktopBuddy.Core;

namespace DesktopBuddy.Core.Tests;

public class BreakReminderTests
{
    [Fact]
    public void Reminds_after_the_set_time_at_the_pc()
    {
        var breaks = new BreakReminder { EveryMinutes = 1 };

        Assert.Null(breaks.Tick(59, userIdleSeconds: 0));
        Assert.NotNull(breaks.Tick(2, userIdleSeconds: 0));
        Assert.Equal(0, breaks.WorkedSeconds);  // counting starts over
    }

    [Fact]
    public void Stepping_away_counts_as_a_break()
    {
        var breaks = new BreakReminder { EveryMinutes = 1 };
        breaks.Tick(50, userIdleSeconds: 0);

        breaks.Tick(1, userIdleSeconds: BreakReminder.BreakIdleSeconds);

        Assert.Equal(0, breaks.WorkedSeconds);
        Assert.Null(breaks.Tick(50, userIdleSeconds: 0));
    }

    [Fact]
    public void A_short_pause_neither_counts_nor_resets()
    {
        var breaks = new BreakReminder { EveryMinutes = 1 };
        breaks.Tick(30, userIdleSeconds: 0);

        breaks.Tick(100, userIdleSeconds: BreakReminder.StillWorkingIdleSeconds + 1);

        Assert.Equal(30, breaks.WorkedSeconds);
    }

    [Fact]
    public void Zero_minutes_turns_reminders_off()
    {
        var breaks = new BreakReminder { EveryMinutes = 0 };

        Assert.Null(breaks.Tick(10_000, userIdleSeconds: 0));
    }
}

public class FocusTimerTests
{
    [Fact]
    public void Counts_down_and_finishes_once()
    {
        var focus = new FocusTimer();
        focus.Start(1);
        Assert.Equal("1:00", focus.Display);

        Assert.False(focus.Tick(30.5));
        Assert.Equal("0:30", focus.Display);

        Assert.True(focus.Tick(30));
        Assert.False(focus.IsRunning);
        Assert.False(focus.Tick(1));
    }

    [Fact]
    public void Stopping_cancels_without_finishing()
    {
        var focus = new FocusTimer();
        focus.Start(25);

        focus.Stop();

        Assert.False(focus.IsRunning);
        Assert.False(focus.Tick(25 * 60));
    }
}

public class CpuMonitorTests
{
    [Fact]
    public void Becomes_busy_under_sustained_load_and_calms_down_after()
    {
        var cpu = new CpuMonitor();
        ulong idle = 0, kernel = 0, user = 0;
        void Second(double load)
        {
            ulong busy = (ulong)(1000 * load);
            idle += 1000 - busy;
            kernel += 1000 - busy;  // kernel time includes idle time
            user += busy;
            cpu.Sample(idle, kernel, user);
        }

        Second(0.1);
        for (int i = 0; i < 10; i++) Second(0.95);
        Assert.True(cpu.Busy);
        Assert.True(cpu.Load > 0.8);

        Second(0.7);  // between the thresholds: stays busy
        Assert.True(cpu.Busy);

        for (int i = 0; i < 10; i++) Second(0.1);
        Assert.False(cpu.Busy);
    }

    [Fact]
    public void A_single_spike_is_not_busy()
    {
        var cpu = new CpuMonitor();
        cpu.Sample(0, 0, 0);
        cpu.Sample(0, 1000, 1000);  // one fully busy second

        Assert.False(cpu.Busy);
    }
}

public class YarnBallTests
{
    [Fact]
    public void Rolls_slows_and_stops()
    {
        var ball = new YarnBall();
        ball.Place(100);
        ball.Kick(200);

        ball.Tick(0.5, 0, 10_000);
        Assert.True(ball.X > 100);

        for (int i = 0; i < 200; i++)
            ball.Tick(0.066, 0, 10_000);
        Assert.False(ball.IsRolling);
    }

    [Fact]
    public void Bounces_off_the_ends()
    {
        var ball = new YarnBall();
        ball.Place(95);
        ball.Kick(300);

        ball.Tick(0.1, 0, 100);

        Assert.Equal(100, ball.X);
        Assert.True(ball.Velocity < 0);
    }

    [Fact]
    public void Every_frame_is_a_round_ball()
    {
        for (int f = 0; f < YarnBall.FrameCount; f++)
        {
            var pixels = YarnBall.Argb(f);
            Assert.Equal(YarnBall.Size * YarnBall.Size, pixels.Length);
            Assert.Equal(0u, pixels[0]);  // corners are transparent
            Assert.NotEqual(0u, pixels[YarnBall.Size / 2 * YarnBall.Size + YarnBall.Size / 2]);
        }
    }
}
