using System.IO;

namespace BlitzText.Windows.Services;

public sealed class StartupShortcutService
{
    private const string ShortcutName = "BlitzText.lnk";
    private const string StartupArgument = "--startup";

    private readonly string startupShortcutPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
        ShortcutName);

    public bool IsEnabled => File.Exists(startupShortcutPath);

    public void SetEnabled(bool enabled, string executablePath)
    {
        if (!enabled)
        {
            if (File.Exists(startupShortcutPath))
            {
                File.Delete(startupShortcutPath);
            }

            return;
        }

        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host is unavailable.");
        dynamic shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Could not create a Windows shortcut.");
        dynamic shortcut = shell.CreateShortcut(startupShortcutPath);
        shortcut.TargetPath = executablePath;
        shortcut.WorkingDirectory = Path.GetDirectoryName(executablePath) ?? "";
        shortcut.Arguments = StartupArgument;
        shortcut.IconLocation = executablePath;
        shortcut.Save();
    }

    public static bool IsStartupLaunch(string[] args) =>
        args.Any(argument => string.Equals(argument, StartupArgument, StringComparison.OrdinalIgnoreCase));
}
