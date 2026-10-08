using System.IO;

namespace DesktopBuddy;

/// <summary>Appends notable events to %LOCALAPPDATA%\DesktopBuddy\buddy.log so problems can be shared.</summary>
internal static class BuddyLog
{
    public static string Folder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopBuddy");

    public static string FilePath { get; } = Path.Combine(Folder, "buddy.log");

    public static void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.AppendAllText(FilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}");
        }
        catch (IOException)
        {
            // Logging must never take the buddy down.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
