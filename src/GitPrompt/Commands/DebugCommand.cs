using System.Diagnostics;
using GitPrompt.Diagnostics;
using GitPrompt.Git;
using GitPrompt.Platform;
using GitPrompt.Prompting;

namespace GitPrompt.Commands;

internal static class DebugCommand
{
    internal static async Task RunAsync()
    {
        PromptDiagnostics.Enable();
        PromptDiagnostics.Reset();

        var platformProvider = PlatformProvider.System;
        var workingDirectoryPath = platformProvider.WorkingDirectory.Path;

        var totalSw = Stopwatch.StartNew();

        var contextSw = Stopwatch.StartNew();
        _ = ContextSegmentBuilder.Build(platformProvider);
        contextSw.Stop();

        var gitSw = Stopwatch.StartNew();
        var gitStatusSegment = await GitStatusSegmentBuilder.BuildAsync(workingDirectoryPath);
        gitSw.Stop();

        totalSw.Stop();

        var report = PromptDiagnostics.GetReport(
            directory: workingDirectoryPath,
            gitStatusSegment: gitStatusSegment,
            contextElapsed: contextSw.Elapsed,
            gitElapsed: gitSw.Elapsed,
            totalElapsed: totalSw.Elapsed);

        Console.Write(report);
    }
}
