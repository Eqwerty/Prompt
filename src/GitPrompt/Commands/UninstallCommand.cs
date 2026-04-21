using System.Diagnostics;
using GitPrompt.Platform;

namespace GitPrompt.Commands;

internal static class UninstallCommand
{
    private static readonly string[] ShellConfigFiles =
    [
        ".bashrc", ".bash_aliases", ".bash_profile", ".bash_login", ".profile",
        ".zshenv", ".zshrc", ".zprofile"
    ];

    internal static void Run()
    {
        var binaryPath = Environment.ProcessPath;
        var configDir = XdgPaths.GetConfigDirectory();
        var cacheDir = XdgPaths.GetCacheDirectory();

        WarnAboutShellConfigReferences();

        if (Directory.Exists(configDir))
        {
            Directory.Delete(configDir, recursive: true);
        }

        if (Directory.Exists(cacheDir))
        {
            Directory.Delete(cacheDir, recursive: true);
        }

        if (binaryPath is not null)
        {
            DeleteBinary(binaryPath);
        }

        Console.WriteLine("Uninstalled.");
    }

    private static void WarnAboutShellConfigReferences()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var matches = new List<string>();

        foreach (var fileName in ShellConfigFiles)
        {
            var path = Path.Combine(home, fileName);
            if (!File.Exists(path))
            {
                continue;
            }

            var lines = File.ReadAllLines(path);
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("gitprompt", StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add($"{path}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        if (matches.Count is 0)
        {
            return;
        }

        Console.Error.WriteLine("warn: Found gitprompt references — remove from your shell config manually:");
        foreach (var match in matches)
        {
            Console.Error.WriteLine($"  {match}");
        }

        Console.Error.WriteLine();
    }

    private static void DeleteBinary(string binaryPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.Delete(binaryPath);
            return;
        }

        // On Windows, the running .exe is locked. Spawn a hidden cmd.exe that waits
        // 1 second for the lock to release, then force-deletes the binary.
        var psi = new ProcessStartInfo("cmd.exe")
        {
            Arguments = $"/c timeout /T 1 /NOBREAK > nul & del /f /q \"{binaryPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process.Start(psi);
    }
}
