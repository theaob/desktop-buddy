using DesktopBuddy.Core;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace DesktopBuddy;

/// <summary>The notification-area icon: show or hide the cat, pause it, settings, and exit.</summary>
internal sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Drawing.Icon _icon;
    private readonly IntPtr _iconHandle;

    public TrayIcon(App app)
    {
        var show = new Forms.ToolStripMenuItem("Hide buddy", null, (_, _) => app.ToggleBuddyHidden());
        var pause = new Forms.ToolStripMenuItem("Pause walking", null, (_, _) => app.TogglePaused());
        var startup = new Forms.ToolStripMenuItem("Start with Windows", null, (_, _) => StartupRegistration.Set(!StartupRegistration.IsEnabled()));

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(show);
        menu.Items.Add(pause);
        menu.Items.Add(startup);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("Settings...", null, (_, _) => app.ShowSettings()));
        menu.Items.Add(new Forms.ToolStripMenuItem("Diagnostics...", null, (_, _) => app.Buddy?.ShowDiagnostics()));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("Exit", null, (_, _) => app.Quit()));
        menu.Opening += (_, _) =>
        {
            show.Text = app.Buddy?.UserHidden == true ? "Show buddy" : "Hide buddy";
            pause.Checked = app.Settings.Paused;
            startup.Checked = StartupRegistration.IsEnabled();
        };

        (_icon, _iconHandle) = CreateIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _icon,
            Text = "Desktop Buddy",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => app.ShowSettings();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon.Dispose();
        Native.DestroyIcon(_iconHandle);
    }

    /// <summary>Draws the sitting cat into a 16x16 icon.</summary>
    private static (Drawing.Icon Icon, IntPtr Handle) CreateIcon()
    {
        var pixels = CatPixels.Argb(CatPose.Sit);
        int top = (16 - CatPixels.Height) / 2;
        using var bmp = new Drawing.Bitmap(16, 16);
        for (int y = 0; y < CatPixels.Height; y++)
            for (int x = 0; x < CatPixels.Width; x++)
                bmp.SetPixel(x, y + top, Drawing.Color.FromArgb(unchecked((int)pixels[y * CatPixels.Width + x])));

        IntPtr handle = bmp.GetHicon();
        return (Drawing.Icon.FromHandle(handle), handle);
    }
}
