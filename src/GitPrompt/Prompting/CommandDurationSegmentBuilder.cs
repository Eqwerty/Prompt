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

        var minMs = ConfigReader.Config.CommandDurationMinMs;
        if (minMs is > 0 && ms.Value < minMs)
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

        if (ms < 60_000)
        {
            var tenths = ms / 100;

            return $"{(tenths / 10.0).ToString("0.0", CultureInfo.InvariantCulture)}s";
        }

        if (ms < 3_600_000)
        {
            var m = ms / 60_000;
            var s = ms % 60_000 / 1000;

            return $"{m}m{s}s";
        }

        var hours = ms / 3_600_000;
        var mins = ms % 3_600_000 / 60_000;
        var secs = ms % 60_000 / 1000;

        return $"{hours}h{mins}m{secs}s";
    }
}
