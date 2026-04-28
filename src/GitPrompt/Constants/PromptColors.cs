namespace GitPrompt.Constants;

internal static class PromptColors
{
    private const string ReadLineStart = "\u0001";
    private const string ReadLineEnd = "\u0002";

    private static string PromptColor(string ansiColor) => $"{ReadLineStart}{ansiColor}{ReadLineEnd}";

    internal static readonly string ColorUser = PromptColor(AnsiColors.Green);
    internal static readonly string ColorHost = PromptColor(AnsiColors.Magenta);
    internal static readonly string ColorPath = PromptColor(AnsiColors.Orange);
    internal static readonly string ColorCommandDuration = PromptColor(AnsiColors.Magenta);
    internal static readonly string ColorBranch = PromptColor(AnsiColors.Blue);
    internal static readonly string ColorBranchNoUpstream = PromptColor(AnsiColors.Blue);
    internal static readonly string ColorAhead = PromptColor(AnsiColors.Blue);
    internal static readonly string ColorBehind = PromptColor(AnsiColors.Blue);
    internal static readonly string ColorStaged = PromptColor(AnsiColors.Green);
    internal static readonly string ColorUnstaged = PromptColor(AnsiColors.Red);
    internal static readonly string ColorUntracked = PromptColor(AnsiColors.Red);
    internal static readonly string ColorStash = PromptColor(AnsiColors.Magenta);
    internal static readonly string ColorConflict = PromptColor(AnsiColors.BoldRed);
    internal static readonly string ColorMissingPath = PromptColor(AnsiColors.BoldRed);
    internal static readonly string ColorTimeout = PromptColor(AnsiColors.Yellow);
    internal static readonly string ColorPromptSymbol = PromptColor(AnsiColors.LightGray);
    internal static readonly string ColorReset = PromptColor(AnsiColors.Reset);
}
