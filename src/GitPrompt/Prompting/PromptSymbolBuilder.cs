using GitPrompt.Configuration;
using GitPrompt.Platform;
using GitPrompt.Constants;

namespace GitPrompt.Prompting;

internal static class PromptSymbolBuilder
{
    internal static string Build(PlatformProvider platformProvider)
    {
        var customSymbol = ConfigReader.Config.Layout!.Symbol;
        if (customSymbol is not null)
        {
            return customSymbol;
        }

        if (platformProvider.IsWindows())
        {
            return PromptSymbols.Windows;
        }

        var isCurrentUnixRootUser = string.Equals(platformProvider.User, "root", StringComparison.Ordinal);

        return isCurrentUnixRootUser ? PromptSymbols.UnixRoot : PromptSymbols.Unix;
    }
}

