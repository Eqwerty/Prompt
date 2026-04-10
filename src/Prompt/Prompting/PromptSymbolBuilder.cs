using Prompt.Platform;
using Prompt.Constants;

namespace Prompt.Prompting;

internal static class PromptSymbolBuilder
{
    internal static string Build(PlatformProvider platformProvider)
    {
        if (platformProvider.IsWindows())
        {
            return PromptSymbols.Windows;
        }

        var isCurrentUnixRootUser = string.Equals(platformProvider.User, "root", StringComparison.Ordinal);

        return isCurrentUnixRootUser ? PromptSymbols.UnixRoot : PromptSymbols.Unix;
    }
}
