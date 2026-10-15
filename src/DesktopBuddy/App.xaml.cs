using System.IO;
using System.Windows;
using DesktopBuddy.Core;

namespace DesktopBuddy;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private bool _ownsSingleInstance;
    private TrayIcon? _tray;
    private SettingsWindow? _settingsWindow;

    internal static string SettingsPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopBuddy", "settings.json");

    internal BuddySettings Settings { get; private set; } = new();

    internal MainWindow? Buddy { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // One cat is plenty: a second launch (say, from the Run key and by hand) just exits.
        _singleInstance = new Mutex(initiallyOwned: true, @"Local\DesktopBuddy.SingleInstance", out _ownsSingleInstance);
        if (!_ownsSingleInstance)
        {
            Shutdown();
            return;
        }

        Settings = SettingsStore.Load(SettingsPath);
        Buddy = new MainWindow(Settings);
        MainWindow = Buddy;
        _tray = new TrayIcon(this);
        Buddy.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        if (_ownsSingleInstance)
            _singleInstance?.ReleaseMutex();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    internal void SaveSettings()
    {
        try
        {
            SettingsStore.Save(SettingsPath, Settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            BuddyLog.Write($"Could not save settings: {ex.Message}");
        }
    }

    /// <summary>Saves a new fur colour or pattern and redraws the tray icon; the cat picks it up on its next frame.</summary>
    internal void ApplyLook()
    {
        SaveSettings();
        _tray?.SetLook(Settings.Look);
    }

    internal void ToggleFocus() => Buddy?.ToggleFocus();

    internal void TogglePaused()
    {
        Settings.Paused = !Settings.Paused;
        SaveSettings();
    }

    internal void ToggleBuddyHidden()
    {
        if (Buddy != null)
            Buddy.UserHidden = !Buddy.UserHidden;
    }

    internal void ShowSettings()
    {
        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow(this);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    internal void Quit()
    {
        BuddyLog.Write("Exited.");
        Buddy?.Close();
    }
}
