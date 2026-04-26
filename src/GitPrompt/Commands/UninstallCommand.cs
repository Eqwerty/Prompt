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

        CleanShellConfigs();

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
