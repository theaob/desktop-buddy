using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DesktopBuddy;

/// <summary>Live readout of what the buddy sees, with a copy button for sharing test results.</summary>
internal sealed class DiagnosticsWindow : Window
{
    private readonly TextBox _text;

    public DiagnosticsWindow()
    {
        Title = "Desktop Buddy diagnostics";
        Width = 620;
        Height = 330;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        _text = new TextBox
        {
            IsReadOnly = true,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(8),
        };

        var copy = new Button { Content = "Copy to clipboard", Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(4, 0, 4, 8) };
        copy.Click += (_, _) =>
        {
            try { Clipboard.SetText(_text.Text); }
            catch (COMException) { /* clipboard busy; the user can retry */ }
        };

        var openLog = new Button { Content = "Open log folder", Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(4, 0, 8, 8) };
        openLog.Click += (_, _) =>
        {
            Directory.CreateDirectory(BuddyLog.Folder);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{BuddyLog.Folder}\"") { UseShellExecute = true });
        };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(copy);
        buttons.Children.Add(openLog);

        var root = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);
        root.Children.Add(_text);
        Content = root;
    }

    public void Refresh(string text)
    {
        if (_text.Text != text)
            _text.Text = text;
    }
}
