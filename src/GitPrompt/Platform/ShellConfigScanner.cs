namespace GitPrompt.Platform;

internal static class ShellConfigScanner
{
    internal static readonly string[] SearchFileNames =
    [
        ".bashrc", ".bash_aliases", ".bash_profile", ".bash_login",
        ".profile", ".zshenv", ".zshrc", ".zprofile"
    ];

    internal static string[] GetSearchPaths()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Array.ConvertAll(SearchFileNames, name => Path.Combine(home, name));
    }

    internal static string? FindActiveShellConfig()
    {
        foreach (var path in GetSearchPaths())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            foreach (var line in File.ReadLines(path))
            {
                if (IsGitPromptInitEvalLine(line))
                {
                    return path;
                }
            }
        }

        return null;
    }

    internal static bool IsGitPromptInitEvalLine(string line)
    {
        var trimmed = line.TrimStart();

        return trimmed.StartsWith("eval", StringComparison.OrdinalIgnoreCase)
               && trimmed.Contains("gitprompt", StringComparison.OrdinalIgnoreCase)
               && trimmed.Contains("init", StringComparison.OrdinalIgnoreCase)
               && trimmed.Contains("bash", StringComparison.OrdinalIgnoreCase);
    }
}
