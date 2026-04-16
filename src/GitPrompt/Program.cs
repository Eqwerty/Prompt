using System.Text;
using GitPrompt.Git;
using GitPrompt.Platform;
using GitPrompt.Prompting;
using static GitPrompt.Constants.PromptColors;

Console.OutputEncoding = Encoding.UTF8;

foreach (var argument in args)
{
    if (string.Equals(argument, "--invalidate-status-cache", StringComparison.Ordinal))
    {
        GitStatusSharedCache.Invalidate();
        return 0;
    }
}

var platformProvider = PlatformProvider.System;
var workingDirectoryPath = platformProvider.WorkingDirectory.Path;

var contextSegment = ContextSegmentBuilder.Build(platformProvider);
var gitStatusSegment = await GitStatusSegmentBuilder.BuildAsync(workingDirectoryPath);
var promptSymbol = PromptSymbolBuilder.Build(platformProvider);

var promptLine = string.IsNullOrEmpty(gitStatusSegment)
    ? contextSegment
    : $"{contextSegment} {gitStatusSegment}";

Console.Write($"{promptLine}\n{ColorPromptSymbol}{promptSymbol}{ColorReset} ");

return 0;
