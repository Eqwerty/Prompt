using GitPrompt.Commands;
using GitPrompt.Git;
using GitPrompt.Prompting;

namespace GitPrompt;

internal static class ArgumentProcessor
{
    internal static void HandleArguments(string[] arguments)
    {
        for (var i = 0; i < arguments.Length; i++)
        {
            var argument = arguments[i];

            if (string.Equals(argument, "--help", StringComparison.Ordinal) ||
                string.Equals(argument, "-h", StringComparison.Ordinal) ||
                string.Equals(argument, "help", StringComparison.Ordinal))
            {
                HelpCommand.PrintHelp();
                Environment.Exit(0);
            }

            if (string.Equals(argument, "--invalidate-status-cache", StringComparison.Ordinal))
            {
                GitStatusSharedCache.Invalidate();
                Environment.Exit(0);
            }

            if (string.Equals(argument, "init", StringComparison.Ordinal))
            {
                var shell = i + 1 < arguments.Length ? arguments[i + 1] : string.Empty;
                ShellInitializer.Initialize(shell);
                Environment.Exit(0);
            }

            if (string.Equals(argument, "config", StringComparison.Ordinal))
            {
                ConfigCommand.Run();
                Environment.Exit(0);
            }
        }
    }
}
