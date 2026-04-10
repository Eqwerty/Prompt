using Prompt.Platform;
using static Prompt.Constants.PromptSymbols;

namespace Prompt.Prompting;

internal static class PromptSymbolBuilder
{
    internal static string Build(PlatformProvider platformProvider)
    {
        if (platformProvider.IsWindows())
        {
            return Windows;
        }

        var isCurrentUnixRootUser = string.Equals(platformProvider.User, "root", StringComparison.Ordinal);

        return isCurrentUnixRootUser ? UnixRoot : Unix;
    }
}
