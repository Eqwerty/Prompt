using System.Diagnostics;

namespace Prompt.Git;

internal static class Utilities
{
    internal static IEnumerable<string> EnumerateLines(string text)
    {
        using var reader = new StringReader(text);
        while (reader.ReadLine() is string line)
        {
            yield return line;
        }
    }

    internal static string EscapeCommandLineArgument(string argument)
    {
        if (argument.Length is 0)
        {
            return "\"\"";
        }

        if (!argument.Any(static c => char.IsWhiteSpace(c) || c is '"'))
        {
            return argument;
        }

        return "\"" + argument.Replace("\\", @"\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    internal static string ShortenCommitHash(string objectId)
    {
        if (string.IsNullOrEmpty(objectId))
        {
            return string.Empty;
        }

        return objectId.Length >= 7 ? objectId[..7] : objectId;
    }

    internal static async Task<string?> RunProcessForOutputAsync(string fileName, string arguments, string? workingDirectory, bool requireSuccess)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory ?? string.Empty
            };

            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync());

            if (requireSuccess && process.ExitCode is not 0)
            {
                return null;
            }

            return process.ExitCode is 0 ? stdoutTask.Result : string.Empty;
        }
        catch
        {
            return null;
        }
    }
}
