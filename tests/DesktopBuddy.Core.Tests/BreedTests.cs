using DesktopBuddy.Core;

namespace DesktopBuddy.Core.Tests;

public class BreedTests
{
    private static IEnumerable<CatBreed> Designed => Enum.GetValues<CatBreed>().Where(b => b != CatBreed.Original);

    public static TheoryData<CatBreed> DesignedBreeds()
    {
        var data = new TheoryData<CatBreed>();
        foreach (var breed in Designed)
            data.Add(breed);
        return data;
    }

    [Theory]
    [MemberData(nameof(DesignedBreeds))]
    public void Every_pose_is_drawn_at_the_sprite_size_in_known_colours(CatBreed breed)
    {
        var colors = CatPixels.BreedColors(breed);
        foreach (var pose in Enum.GetValues<CatPose>())
        {
            var rows = CatPixels.Rows(breed, pose);
            Assert.Equal(CatPixels.Height, rows.Count);
            Assert.All(rows, row =>
            {
                Assert.Equal(CatPixels.Width, row.Length);
                Assert.All(row, c => Assert.True(c == '.' || colors.ContainsKey(c), $"{breed} {pose}: unknown pixel '{c}'"));
            });
            Assert.Contains(CatPixels.Argb(pose, new CatLook(default, default, breed)), pixel => pixel != 0);
        }
    }

    [Theory]
    [MemberData(nameof(DesignedBreeds))]
    public void Sweat_and_snot_bubbles_stay_inside_the_frame(CatBreed breed)
    {
        var look = new CatLook(default, default, breed);
        foreach (var pose in Enum.GetValues<CatPose>())
            foreach (var bubble in new[] { 0, 1, 2, 3, CatPixels.BubblePop })
                Assert.Equal(CatPixels.Width * CatPixels.Height, CatPixels.Argb(pose, look, sweaty: true, bubble).Length);
    }

    [Theory]
    [MemberData(nameof(DesignedBreeds))]
    public void A_busy_pc_makes_a_walking_cat_sweat(CatBreed breed)
    {
        var look = new CatLook(default, default, breed);
        Assert.NotEqual(CatPixels.Argb(CatPose.WalkA, look), CatPixels.Argb(CatPose.WalkA, look, sweaty: true));
    }

    [Fact]
    public void The_sweat_drop_stays_put_while_the_cat_gallops()
    {
        foreach (var breed in Enum.GetValues<CatBreed>())
        {
            var look = new CatLook(default, default, breed);
            var drops = new[] { CatPose.RunA, CatPose.RunB, CatPose.RunC, CatPose.RunD }.Select(pose =>
            {
                var dry = CatPixels.Argb(pose, look);
                var wet = CatPixels.Argb(pose, look, sweaty: true);
                return Enumerable.Range(0, dry.Length).Where(i => dry[i] != wet[i]).ToArray();
            }).ToList();

            Assert.NotEmpty(drops[0]);
            Assert.All(drops, drop => Assert.Equal(drops[0], drop));
        }
    }

    [Theory]
    [MemberData(nameof(DesignedBreeds))]
    public void A_napping_cat_blows_a_bubble(CatBreed breed)
    {
        var look = new CatLook(default, default, breed);
        Assert.NotEqual(CatPixels.Argb(CatPose.NapA, look), CatPixels.Argb(CatPose.NapA, look, bubble: 3));
    }

    [Theory]
    [MemberData(nameof(DesignedBreeds))]
    public void Tray_icon_shows_the_head(CatBreed breed)
    {
        var icon = CatPixels.IconArgb(new CatLook(default, default, breed));

        Assert.Equal(CatPixels.IconSize * CatPixels.IconSize, icon.Length);
        Assert.True(icon.Count(pixel => pixel != 0) > 40);
    }

    [Fact]
    public void Every_cat_looks_different()
    {
        var sits = Enum.GetValues<CatBreed>().Select(b => CatPixels.Argb(CatPose.Sit, new CatLook(default, default, b))).ToList();

        for (int i = 0; i < sits.Count; i++)
            for (int j = i + 1; j < sits.Count; j++)
                Assert.NotEqual(sits[i], sits[j]);
    }

    [Fact]
    public void Fur_and_pattern_do_not_change_a_designed_cat()
    {
        Assert.Equal(
            CatPixels.Argb(CatPose.Sit, new CatLook(FurColor.BlueGrey, CatPattern.Tabby, CatBreed.PebbleRealistic)),
            CatPixels.Argb(CatPose.Sit, new CatLook(FurColor.Ginger, CatPattern.Patches, CatBreed.PebbleRealistic)));
    }
}
