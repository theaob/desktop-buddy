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

    [Theory]
    [InlineData(BuddyState.Dragged)]
    [InlineData(BuddyState.Falling)]
    public void Held_or_falling_cat_dangles(BuddyState state)
    {
        Assert.Equal(CatPose.Dangle, CatAnimation.PoseFor(state, 0.5));
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
        Assert.Equal(CatPose.HeldBelly, CatAnimation.PoseFor(BuddyState.Falling, 0, grip: DragGrip.Belly));
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
        foreach (var pose in new[] { CatPose.WalkA, CatPose.WalkB, CatPose.Sit, CatPose.SitBlink, CatPose.LookUp })
        {
            var dry = CatPixels.Argb(pose);
            var wet = CatPixels.Argb(pose, sweaty: true);
            for (int i = 0; i < dry.Length; i++)
                if (dry[i] != wet[i])
                    Assert.Equal(0u, dry[i]);
        }
    }
}
