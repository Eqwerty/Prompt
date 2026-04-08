using System.Text;
using static Prompt.Constants.PromptColors;

namespace Prompt;

internal static class Program
{
    private static async Task<int> Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var platformProvider = PlatformProvider.System;
        var promptContext = PromptContextBuilder.Build(platformProvider);
        var gitStatusSegment = await GitStatusSegmentBuilder.BuildAsync();
        var promptSymbol = GetPromptSymbol(platformProvider);

        if (!string.IsNullOrEmpty(gitStatusSegment))
        {
            Console.Write($"{promptContext} {gitStatusSegment}\n{ColorPrompt}{promptSymbol} {ColorReset}");
            return 0;
        }

        Console.Write($"{promptContext}\n{ColorPrompt}{promptSymbol} {ColorReset}");
        return 0;
    }

    internal static string GetPromptSymbol(PlatformProvider platformProvider)
    {
        return IsCurrentUnixRootUser(platformProvider) ? "#" : "$";
    }

    private static bool IsCurrentUnixRootUser(PlatformProvider platformProvider)
    {
        if (platformProvider.IsWindows())
        {
            return false;
        }

        var userName = platformProvider.User ?? platformProvider.WindowsUserName;
        return string.Equals(userName, "root", StringComparison.Ordinal);
    }
}
