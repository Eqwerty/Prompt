using System.Globalization;
using GitPrompt.Configuration;
using GitPrompt.Platform;
using static GitPrompt.Constants.PromptColors;

namespace GitPrompt.Prompting;

internal static class CommandDurationSegmentBuilder
{
    internal static string Build(PlatformProvider platformProvider)
    {
        if (!ConfigReader.Config.ShowCommandDuration)
        {
            return string.Empty;
        }

        var ms = platformProvider.LastCommandDurationMs;
        if (ms is null)
        {
            return string.Empty;
        }

        return $"{ColorCommandDuration}{FormatDuration(ms.Value)}{ColorReset}";
    }

    internal static string FormatDuration(long ms)
    {
        if (ms < 1000)
        {
            return $"{ms.ToString(CultureInfo.InvariantCulture)}ms";
        }

        var seconds = ms / 1000.0;

        if (seconds < 60)
        {
            return $"{seconds:##,###.00}s";
        }

        var minutes = seconds / 60.0;

        return $"{minutes:##,###.00}m";
    }
}
