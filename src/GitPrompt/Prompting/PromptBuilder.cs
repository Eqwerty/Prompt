using System.Diagnostics;
using GitPrompt.Diagnostics;
using GitPrompt.Git;
using GitPrompt.Platform;

namespace GitPrompt.Prompting;

internal static class PromptBuilder
{
    internal static PromptResult Build(PlatformProvider platformProvider)
    {
        var workingDirectoryPath = platformProvider.WorkingDirectory.Path;

        var totalSw = PromptDiagnostics.IsEnabled ? Stopwatch.StartNew() : null;

        var contextSw = PromptDiagnostics.IsEnabled ? Stopwatch.StartNew() : null;
        var contextSegment = ContextSegmentBuilder.Build(platformProvider);
        contextSw?.Stop();

        var commandDurationSegment = CommandDurationSegmentBuilder.Build(platformProvider);

        var gitSw = PromptDiagnostics.IsEnabled ? Stopwatch.StartNew() : null;
        var gitStatusSegment = GitStatusSegmentBuilder.Build(workingDirectoryPath);
        gitSw?.Stop();

        var promptSymbol = PromptSymbolBuilder.Build(platformProvider);
        totalSw?.Stop();

        return new PromptResult(
            contextSegment,
            commandDurationSegment,
            gitStatusSegment,
            promptSymbol,
            contextSw?.Elapsed ?? TimeSpan.Zero,
            gitSw?.Elapsed ?? TimeSpan.Zero,
            totalSw?.Elapsed ?? TimeSpan.Zero);
    }
}
