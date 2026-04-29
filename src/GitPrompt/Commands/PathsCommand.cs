using GitPrompt.Constants;
using GitPrompt.Diagnostics;
using GitPrompt.Platform;

namespace GitPrompt.Commands;

internal static class PathsCommand
{
    private static readonly string[] Labels = ["binary", "config", "aliases", "cache dir", "shell config"];
    private static readonly int LabelWidth = Labels.Max(l => l.Length);

    internal static void Run(TextWriter? output = null)
    {
        output ??= Console.Out;

        var report = BuildReport(
            AppPaths.GetBinaryPath(),
            AppPaths.GetConfigFilePath(),
            AppPaths.GetAliasesFilePath(),
            AppPaths.GetCacheDirectory(),
            ShellConfigScanner.FindActiveShellConfig());

        output.Write(report);
    }

    internal static string BuildReport(
        string? binaryPath,
        string configPath,
        string aliasesPath,
        string cacheDirPath,
        string? shellConfigPath)
    {
        var lines = new List<string?>
        {
            BuildRow("binary", binaryPath),
            BuildRow("config", configPath),
            BuildRow("aliases", aliasesPath),
            BuildRow("cache dir", cacheDirPath),
            BuildRow("shell config", shellConfigPath),
        };

        return BoxRenderer.Render("GitPrompt paths", lines, AnsiColors.LightGray);
    }

    private static string BuildRow(string label, string? path)
    {
        string displayPath;
        if (path is null)
        {
            displayPath = "(none found)";
        }
        else
        {
            var exists = File.Exists(path) || Directory.Exists(path);
            var normalizedPath = path.Replace('\\', '/');
            displayPath = exists ? normalizedPath : $"{normalizedPath} (not found)";
        }

        return $"  {label.PadRight(LabelWidth)}  {displayPath}";
    }
}
