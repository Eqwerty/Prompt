using GitPrompt.Git;

namespace GitPrompt;

internal static class ArgumentProcessor
{
    internal static void HandleArguments(string[] arguments)
    {
        foreach (var argument in arguments)
        {
            if (string.Equals(argument, "--invalidate-status-cache", StringComparison.Ordinal))
            {
                GitStatusSharedCache.Invalidate();
                Environment.Exit(0);
            }
        }
    }
}
