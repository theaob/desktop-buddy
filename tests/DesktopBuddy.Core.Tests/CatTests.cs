using DesktopBuddy.Core;

namespace DesktopBuddy.Core.Tests;

public class CatTests
{
    public static TheoryData<CatPose> AllPoses()
    {
        var data = new TheoryData<CatPose>();
        foreach (var pose in Enum.GetValues<CatPose>())
            data.Add(pose);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllPoses))]
    public void Every_frame_has_the_sprite_size_and_uses_known_colours(CatPose pose)
    {
        var rows = CatPixels.Rows(pose);

        Assert.Equal(CatPixels.Height, rows.Count);
        Assert.All(rows, row =>
        {
            Assert.Equal(CatPixels.Width, row.Length);
            Assert.All(row, c => Assert.True(c == '.' || CatPixels.Palette.ContainsKey(c), $"unknown pixel '{c}'"));
        });
        Assert.Equal(CatPixels.Width * CatPixels.Height, CatPixels.Argb(pose).Length);
    }

    [Fact]
    public void Tray_icon_is_16_by_16_and_not_empty()
    {
        var icon = CatPixels.IconArgb();

        Assert.Equal(CatPixels.IconSize * CatPixels.IconSize, icon.Length);
        Assert.Contains(icon, pixel => pixel != 0);
    }

    [Fact]
    public void Every_fur_and_pattern_colours_every_pixel_character()
    {
        foreach (var fur in Enum.GetValues<FurColor>())
            foreach (var pattern in Enum.GetValues<CatPattern>())
                Assert.Equal(CatPixels.Palette.Keys.Order(), CatPixels.ColorsFor(new CatLook(fur, pattern)).Keys.Order());
    }

    [Fact]
    public void Each_fur_colour_looks_different()
    {
        var sits = Enum.GetValues<FurColor>().Select(fur => CatPixels.Argb(CatPose.Sit, new CatLook(fur, CatPattern.Tabby))).ToList();

        for (int i = 0; i < sits.Count; i++)
            for (int j = i + 1; j < sits.Count; j++)
                Assert.NotEqual(sits[i], sits[j]);
    }

    [Theory]
    [InlineData(CatPattern.Solid)]
    [InlineData(CatPattern.Socks)]
    [InlineData(CatPattern.Patches)]
    public void Each_pattern_differs_from_tabby(CatPattern pattern)
    {
        foreach (var fur in Enum.GetValues<FurColor>())
            Assert.NotEqual(
                CatPixels.Argb(CatPose.WalkA, new CatLook(fur, CatPattern.Tabby)),
                CatPixels.Argb(CatPose.WalkA, new CatLook(fur, pattern)));
    }

    [Fact]
    public void Solid_cat_has_no_stripes()
    {
        var colors = CatPixels.ColorsFor(new CatLook(FurColor.Ginger, CatPattern.Solid));

        Assert.Equal(colors['f'], colors['d']);
        Assert.Equal(colors['f'], colors['t']);
    }

    [Fact]
    public void Walking_alternates_steps()
    {
        var first = CatAnimation.PoseFor(BuddyState.Walk, 0);
        var second = CatAnimation.PoseFor(BuddyState.Walk, 0.25);

        Assert.Equal(CatPose.WalkA, first);
        Assert.Equal(CatPose.WalkB, second);
    }

    [Fact]
    public void Sitting_cat_blinks_briefly()
    {
        Assert.Equal(CatPose.Sit, CatAnimation.PoseFor(BuddyState.Sit, 1));
        Assert.Equal(CatPose.SitBlink, CatAnimation.PoseFor(BuddyState.Sit, 3.9));
    }

    [Fact]
    public void Held_cat_dangles_and_falling_cat_flails()
    {
        Assert.Equal(CatPose.Dangle, CatAnimation.PoseFor(BuddyState.Dragged, 0.5));
        Assert.Equal(CatPose.FallA, CatAnimation.PoseFor(BuddyState.Falling, 0));
        Assert.Equal(CatPose.FallB, CatAnimation.PoseFor(BuddyState.Falling, 0.2));
        Assert.Equal(CatPose.Land, CatAnimation.PoseFor(BuddyState.Landing, 0.1));
    }

    [Fact]
    public void Waking_cat_blinks_then_stretches_then_yawns()
    {
        Assert.Equal(CatPose.Drowsy, CatAnimation.PoseFor(BuddyState.WakeUp, 0.3));
        Assert.Equal(CatPose.Stretch, CatAnimation.PoseFor(BuddyState.WakeUp, 1.5));
        Assert.Equal(CatPose.Yawn, CatAnimation.PoseFor(BuddyState.WakeUp, 2.5));
    }

    [Fact]
    public void A_cat_clicked_awake_jumps_glares_then_yawns()
    {
        Assert.Equal(CatPose.Startle, CatAnimation.PoseFor(BuddyState.Woken, 0.2));
        Assert.Equal(CatPose.Drowsy, CatAnimation.PoseFor(BuddyState.Woken, 1.5));
        Assert.Equal(CatPose.Yawn, CatAnimation.PoseFor(BuddyState.Woken, 2.6));
        Assert.Equal(CatPixels.BubblePop, CatAnimation.SnotBubble(BuddyState.Woken, 0.1));
        Assert.Equal(0, CatAnimation.SnotBubble(BuddyState.Woken, 1));
    }

    [Fact]
    public void New_states_pick_their_poses()
    {
        Assert.Contains(CatAnimation.PoseFor(BuddyState.Scratch, 0.1), new[] { CatPose.ScratchA, CatPose.ScratchB });
        Assert.Contains(CatAnimation.PoseFor(BuddyState.Stalk, 0.1), new[] { CatPose.CrouchA, CatPose.CrouchB });
        Assert.Equal(CatPose.Leap, CatAnimation.PoseFor(BuddyState.Pounce, 0.1));
        Assert.Equal(CatPose.LookUp, CatAnimation.PoseFor(BuddyState.Look, 0.1));
        Assert.Equal(CatPose.Bat, CatAnimation.PoseFor(BuddyState.Play, 0.1, moving: false));
        Assert.Equal(CatPose.WalkA, CatAnimation.PoseFor(BuddyState.Follow, 0, moving: true));
        Assert.Equal(CatPose.Sit, CatAnimation.PoseFor(BuddyState.Follow, 0, moving: false));
    }

    [Fact]
    public void Belly_hold_has_its_own_pose()
    {
        Assert.Equal(CatPose.HeldBelly, CatAnimation.PoseFor(BuddyState.Dragged, 0, grip: DragGrip.Belly));
    }

    [Fact]
    public void Sweat_drop_only_on_poses_with_room_for_it()
    {
        Assert.NotEqual(CatPixels.Argb(CatPose.WalkA), CatPixels.Argb(CatPose.WalkA, sweaty: true));
        Assert.Equal(CatPixels.Argb(CatPose.NapA), CatPixels.Argb(CatPose.NapA, sweaty: true));
    }

    [Fact]
    public void Sweat_drop_only_covers_empty_pixels()
    {
        foreach (var pose in new[] { CatPose.WalkA, CatPose.WalkB, CatPose.Sit, CatPose.SitBlink, CatPose.LookUp, CatPose.SitTail, CatPose.RunA, CatPose.RunB, CatPose.RunC, CatPose.RunD })
        {
            var dry = CatPixels.Argb(pose);
            var wet = CatPixels.Argb(pose, sweaty: true);
            for (int i = 0; i < dry.Length; i++)
                if (dry[i] != wet[i])
                    Assert.Equal(0u, dry[i]);
        }
    }

    [Fact]
    public void Snot_bubble_swells_and_shrinks_while_asleep_and_pops_on_waking()
    {
        var sizes = Enumerable.Range(0, 16).Select(i => CatAnimation.SnotBubble(BuddyState.Nap, i * 0.45)).ToList();
        Assert.Contains(3, sizes);
        Assert.Contains(0, sizes);
        Assert.Contains(3, Enumerable.Range(0, 16).Select(i => CatAnimation.SnotBubble(BuddyState.Doze, i * 0.45)));

        Assert.Equal(CatPixels.BubblePop, CatAnimation.SnotBubble(BuddyState.WakeUp, 0.1));
        Assert.Equal(0, CatAnimation.SnotBubble(BuddyState.WakeUp, 1));
        Assert.Equal(0, CatAnimation.SnotBubble(BuddyState.Walk, 1));
    }

    [Theory]
    [InlineData(CatPose.SitBlink)]
    [InlineData(CatPose.NapA)]
    [InlineData(CatPose.NapB)]
    [InlineData(CatPose.Drowsy)]
    [InlineData(CatPose.Curl)]
    public void Snot_bubble_sits_in_empty_space_beside_the_nose(CatPose pose)
    {
        var plain = CatPixels.Argb(pose);
        foreach (int size in new[] { 1, 2, 3, CatPixels.BubblePop })
        {
            var bubbly = CatPixels.Argb(pose, bubble: size);
            Assert.NotEqual(plain, bubbly);
            for (int i = 0; i < plain.Length; i++)
                if (plain[i] != bubbly[i])
                    Assert.Equal(0u, plain[i]);
        }
    }

    [Fact]
    public void Open_eyed_poses_never_get_a_snot_bubble()
    {
        Assert.Equal(CatPixels.Argb(CatPose.WalkA), CatPixels.Argb(CatPose.WalkA, bubble: 3));
    }

    [Fact]
    public void Sitting_cat_swishes_its_tail_now_and_then()
    {
        var poses = Enumerable.Range(0, 40).Select(i => CatAnimation.PoseFor(BuddyState.Sit, i * 0.1)).ToList();
        Assert.Contains(CatPose.SitTail, poses);
        Assert.Contains(CatPose.Sit, poses);
    }

    [Fact]
    public void Grooming_loafing_and_rolling_over_have_their_own_poses()
    {
        Assert.Contains(CatAnimation.PoseFor(BuddyState.Groom, 0.1), new[] { CatPose.GroomA, CatPose.GroomB });
        Assert.Contains(CatAnimation.PoseFor(BuddyState.Groom, 0.5), new[] { CatPose.GroomA, CatPose.GroomB });
        Assert.Equal(CatPose.Loaf, CatAnimation.PoseFor(BuddyState.Loaf, 1));
        Assert.Equal(CatPose.LoafBlink, CatAnimation.PoseFor(BuddyState.Loaf, 4.8));
        Assert.Contains(CatAnimation.PoseFor(BuddyState.BellyUp, 0.1), new[] { CatPose.BellyA, CatPose.BellyB });
        Assert.Equal(CatPose.Startle, CatAnimation.PoseFor(BuddyState.Startle, 0.5));
    }

    [Fact]
    public void Asking_for_scratches_wiggles_on_its_back_and_sulking_loafs()
    {
        Assert.Contains(CatAnimation.PoseFor(BuddyState.AskBelly, 0.1), new[] { CatPose.BellyA, CatPose.BellyB });
        Assert.NotEqual(CatAnimation.PoseFor(BuddyState.AskBelly, 0.1), CatAnimation.PoseFor(BuddyState.AskBelly, 0.8));
        Assert.Equal(CatPose.Loaf, CatAnimation.PoseFor(BuddyState.Sulk, 4.8));  // no contented slow-blink
    }

    [Fact]
    public void A_long_nap_curls_up()
    {
        Assert.Contains(CatAnimation.PoseFor(BuddyState.Nap, 10), new[] { CatPose.NapA, CatPose.NapB });
        Assert.Equal(CatPose.Curl, CatAnimation.PoseFor(BuddyState.Nap, CatAnimation.CurlAfterSeconds + 1));
        Assert.Contains(3, Enumerable.Range(0, 16).Select(i => CatAnimation.SnotBubble(BuddyState.Nap, CatAnimation.CurlAfterSeconds + i * 0.45)));
    }

    [Fact]
    public void A_hurrying_cat_gallops()
    {
        var stride = Enumerable.Range(0, 4).Select(i => CatAnimation.PoseFor(BuddyState.Walk, (i + 0.5) / 12, running: true));
        Assert.Equal(new[] { CatPose.RunA, CatPose.RunB, CatPose.RunC, CatPose.RunD }, stride);
        Assert.Equal(CatPose.RunA, CatAnimation.PoseFor(BuddyState.Walk, 4.5 / 12, running: true));
        Assert.Contains(CatAnimation.PoseFor(BuddyState.Play, 0.1, moving: true, running: true), new[] { CatPose.RunA, CatPose.RunB, CatPose.RunC, CatPose.RunD });
        Assert.Equal(CatPose.Bat, CatAnimation.PoseFor(BuddyState.Play, 0.1, moving: false, running: true));
    }
}
