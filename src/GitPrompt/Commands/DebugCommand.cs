using GitPrompt.Configuration;
using GitPrompt.Diagnostics;
using GitPrompt.Platform;
using GitPrompt.Prompting;

namespace GitPrompt.Commands;

internal static class DebugCommand
{
    internal static void Run()
    {
        PromptDiagnostics.Enable();
        PromptDiagnostics.Reset();

        PromptDiagnostics.RecordConfigLoaded(ConfigReader.LoadResult);

        var platformProvider = PlatformProvider.System;
        var result = PromptBuilder.Build(platformProvider);

        var report = PromptDiagnostics.GetReport(platformProvider.WorkingDirectory.Path, result);

        Console.Write(report);
    }
}
