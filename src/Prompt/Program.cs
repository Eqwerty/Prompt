using System.Text;
using static Prompt.Constants.PromptColors;

namespace Prompt;

internal static class Program
{
    private static async Task<int> Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var platformProvider = PlatformProvider.System;
        
        var contextSegment = ContextSegmentBuilder.Build(platformProvider);
        var gitStatusSegment = await GitStatusSegmentBuilder.BuildAsync();
        var promptSymbol = GetPromptSymbol(platformProvider);

        var promptLine = string.IsNullOrEmpty(gitStatusSegment)
            ? contextSegment
            : $"{contextSegment} {gitStatusSegment}";

        Console.Write($"{promptLine}\n{ColorPromptSymbol}{promptSymbol} {ColorReset}");
        return 0;
    }

    internal static string GetPromptSymbol(PlatformProvider platformProvider)
    {
        if (platformProvider.IsWindows())
        {
            return ">";
        }

        var isCurrentUnixRootUser = string.Equals(platformProvider.User, "root", StringComparison.Ordinal);
        return isCurrentUnixRootUser ? "#" : "$";
    }
}
