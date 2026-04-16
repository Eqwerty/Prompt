using System.Text;
using GitPrompt;
using GitPrompt.Git;
using GitPrompt.Platform;
using GitPrompt.Prompting;
using static GitPrompt.Constants.PromptColors;

Console.OutputEncoding = Encoding.UTF8;

ArgumentProcessor.HandleArguments(args);

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
