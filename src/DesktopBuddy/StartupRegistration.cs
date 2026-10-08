using System.IO;
using System.Security;
using Microsoft.Win32;

namespace DesktopBuddy;

/// <summary>"Start with Windows" via the current user's Run key; no admin rights needed.</summary>
internal static class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DesktopBuddy";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) is string;
    }

    /// <returns>False when the setting could not be changed (for example, blocked by policy).</returns>
    public static bool Set(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled)
                key.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
            else
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            BuddyLog.Write($"Start with Windows {(enabled ? "on" : "off")}.");
            return true;
        }
        catch (Exception e) when (e is UnauthorizedAccessException or SecurityException or IOException)
        {
            BuddyLog.Write($"Could not change Start with Windows: {e.Message}");
            return false;
        }
    }
}
