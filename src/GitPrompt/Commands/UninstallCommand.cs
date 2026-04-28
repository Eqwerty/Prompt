using System.Diagnostics;
using GitPrompt.Platform;

namespace GitPrompt.Commands;

internal static class UninstallCommand
{
    private static readonly string[] ShellConfigFiles =
    [
        ".bashrc", ".bash_aliases", ".bash_profile", ".bash_login", ".profile", ".zshenv", ".zshrc", ".zprofile"
    ];

    internal static void Run()
    {
        var binaryPath = Environment.ProcessPath;
        var configDir = XdgPaths.GetConfigDirectory();
        var cacheDir = XdgPaths.GetCacheDirectory();
        var dataDir = XdgPaths.GetDataDirectory();

        if (IsCwdInsideAnyDirectory(configDir, cacheDir, dataDir))
        {
            return;
        }

        CleanShellConfigs();

        TryDeleteDirectory(configDir);
        TryDeleteDirectory(cacheDir);
        TryDeleteDirectory(dataDir);

        if (binaryPath is not null)
        {
            DeleteBinary(binaryPath);
        }

        Console.WriteLine("Uninstalled.");
        Console.WriteLine("Restart your terminal to apply changes.");
    }

    private static bool IsCwdInsideAnyDirectory(params string[] directories)
    {
        var cwd = Directory.GetCurrentDirectory();

        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            var normalizedCwd = Path.GetFullPath(cwd).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedDir = Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            if (normalizedCwd.Equals(normalizedDir, comparison)
                || normalizedCwd.StartsWith(normalizedDir + Path.DirectorySeparatorChar, comparison))
            {
                Console.Error.WriteLine($"error: Navigate out of {dir} before uninstalling.");
                return true;
            }
        }

        return false;
    }

    private static void TryDeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"warn: Could not delete {path} — it may be your current directory. Navigate away and try again.");
        }
    }

    private static void CleanShellConfigs()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var remainingRefs = new List<string>();

        foreach (var fileName in ShellConfigFiles)
        {
            var path = Path.Combine(home, fileName);
            if (!File.Exists(path))
            {
                continue;
            }

            var lines = File.ReadAllLines(path);
            var filtered = lines.Where(line => !IsGitPromptInitEvalLine(line)).ToArray();

            if (filtered.Length != lines.Length)
            {
                File.WriteAllLines(path, filtered);
                Console.WriteLine($"Removed gitprompt init from {path}");
            }

            for (var i = 0; i < filtered.Length; i++)
            {
                if (filtered[i].Contains("gitprompt", StringComparison.OrdinalIgnoreCase))
                {
                    remainingRefs.Add($"{path}:{i + 1}: {filtered[i].Trim()}");
                }
            }
        }

        if (remainingRefs.Count is 0)
        {
            return;
        }

        Console.Error.WriteLine("warn: Found gitprompt references — remove from your shell config manually:");
        foreach (var match in remainingRefs)
        {
            Console.Error.WriteLine($"  {match}");
        }

        Console.Error.WriteLine();
    }

    private static bool IsGitPromptInitEvalLine(string line)
    {
        var trimmed = line.TrimStart();

        return trimmed.StartsWith("eval", StringComparison.OrdinalIgnoreCase)
               && trimmed.Contains("gitprompt", StringComparison.OrdinalIgnoreCase)
               && trimmed.Contains("init", StringComparison.OrdinalIgnoreCase)
               && trimmed.Contains("bash", StringComparison.OrdinalIgnoreCase);
    }

    private static void DeleteBinary(string binaryPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.Delete(binaryPath);
            return;
        }

        // Windows blocks deletion of a running .exe but allows renaming it.
        // Rename first so the binary immediately disappears from its known path:
        // this lets the shell prompt fall back to the original PS1 on the next
        // render without requiring a terminal restart.
        // Then spawn a hidden cmd.exe to delete the renamed file once the process exits.
        var renamedPath = binaryPath + ".old";
        string pathToDelete;
        try
        {
            File.Move(binaryPath, renamedPath, overwrite: true);
            pathToDelete = renamedPath;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            pathToDelete = binaryPath;
        }

        var psi = new ProcessStartInfo("cmd.exe")
        {
            Arguments = $"/c timeout /T 3 /NOBREAK > nul & del /f /q \"{pathToDelete}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process.Start(psi);
    }
}
