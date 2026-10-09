using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DesktopBuddy.Core;

namespace DesktopBuddy;

/// <summary>Small settings dialog. Every change applies and saves immediately.</summary>
internal sealed class SettingsWindow : Window
{
    public SettingsWindow(App app)
    {
        Title = "Desktop Buddy settings";
        Width = 380;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var settings = app.Settings;
        var gap = new Thickness(0, 0, 0, 12);

        var startup = new CheckBox { Content = "Start with Windows", IsChecked = StartupRegistration.IsEnabled(), Margin = gap };
        startup.Click += (_, _) =>
        {
            if (!StartupRegistration.Set(startup.IsChecked == true))
                startup.IsChecked = StartupRegistration.IsEnabled();
        };

        var pause = new CheckBox { Content = "Pause walking", IsChecked = settings.Paused, Margin = gap };
        pause.Click += (_, _) =>
        {
            settings.Paused = pause.IsChecked == true;
            app.SaveSettings();
        };
        Activated += (_, _) => pause.IsChecked = settings.Paused;  // may have changed from the tray

        var speedLabel = new TextBlock();
        var speed = new Slider
        {
            Minimum = BuddySettings.MinWalkSpeed,
            Maximum = BuddySettings.MaxWalkSpeed,
            Value = settings.WalkSpeed,
            TickFrequency = 0.25,
            IsSnapToTickEnabled = true,
            Margin = gap,
        };
        void ShowSpeed() => speedLabel.Text = $"Walking speed: {speed.Value:0.##}";
        speed.ValueChanged += (_, _) =>
        {
            settings.WalkSpeed = speed.Value;
            ShowSpeed();
            app.SaveSettings();
        };
        ShowSpeed();

        var napLabel = new TextBlock();
        var nap = new Slider
        {
            Minimum = BuddySettings.MinNapAfterMinutes,
            Maximum = BuddySettings.MaxNapAfterMinutes,
            Value = settings.NapAfterMinutes,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            Margin = gap,
        };
        void ShowNap() => napLabel.Text = $"Nap after you've been away for {nap.Value:0} min";
        nap.ValueChanged += (_, _) =>
        {
            settings.NapAfterMinutes = (int)nap.Value;
            ShowNap();
            app.SaveSettings();
        };
        ShowNap();

        var preview = new Image
        {
            Width = CatPixels.Width * 3,
            Height = CatPixels.Height * 3,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = gap,
        };
        RenderOptions.SetBitmapScalingMode(preview, BitmapScalingMode.NearestNeighbor);
        void ShowPreview() => preview.Source = CatSprite.Frame(CatPose.Sit, settings.Look);
        ShowPreview();

        var fur = Choice(FurNames, settings.Fur, gap, value =>
        {
            settings.Fur = value;
            ShowPreview();
            app.ApplyLook();
        });
        var pattern = Choice(PatternNames, settings.Pattern, gap, value =>
        {
            settings.Pattern = value;
            ShowPreview();
            app.ApplyLook();
        });

        var close = new Button { Content = "Close", IsCancel = true, IsDefault = true, Padding = new Thickness(16, 4, 16, 4), HorizontalAlignment = HorizontalAlignment.Right };
        close.Click += (_, _) => Close();

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(startup);
        panel.Children.Add(pause);
        panel.Children.Add(speedLabel);
        panel.Children.Add(speed);
        panel.Children.Add(napLabel);
        panel.Children.Add(nap);
        panel.Children.Add(new TextBlock { Text = "Fur colour" });
        panel.Children.Add(fur);
        panel.Children.Add(new TextBlock { Text = "Pattern" });
        panel.Children.Add(pattern);
        panel.Children.Add(preview);
        panel.Children.Add(close);
        Content = panel;
    }

    private static readonly Dictionary<FurColor, string> FurNames = new()
    {
        [FurColor.BlueGrey] = "Blue-grey",
        [FurColor.Ginger] = "Ginger",
        [FurColor.Charcoal] = "Charcoal",
        [FurColor.Cocoa] = "Cocoa",
        [FurColor.Cream] = "Cream",
        [FurColor.Snow] = "Snow",
    };

    private static readonly Dictionary<CatPattern, string> PatternNames = new()
    {
        [CatPattern.Tabby] = "Tabby stripes",
        [CatPattern.Solid] = "Solid",
        [CatPattern.Socks] = "White socks",
        [CatPattern.Patches] = "Patches",
    };

    /// <summary>A drop-down of named choices that reports the picked value.</summary>
    private static ComboBox Choice<T>(Dictionary<T, string> names, T current, Thickness margin, Action<T> picked) where T : notnull
    {
        var box = new ComboBox
        {
            ItemsSource = names,
            DisplayMemberPath = "Value",
            SelectedValuePath = "Key",
            SelectedValue = current,
            Margin = margin,
        };
        box.SelectionChanged += (_, _) =>
        {
            if (box.SelectedValue is T value)
                picked(value);
        };
        return box;
    }
}
