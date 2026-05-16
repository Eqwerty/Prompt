using GitPrompt.Configuration;

namespace GitPrompt.Constants;

internal static class PromptColors
{
    private const string ReadLineStart = "\u0001";
    private const string ReadLineEnd = "\u0002";

    private static string PromptColor(string ansiColor)
    {
        return $"{ReadLineStart}{ansiColor}{ReadLineEnd}";
    }

    private static string ResolveColor(string? configColor, string defaultColor)
    {
        var color = configColor ?? defaultColor;

        try
        {
            return PromptColor(AnsiColorConverter.ToAnsi(color));
        }
        catch (ArgumentException)
        {
            return PromptColor(AnsiColorConverter.ToAnsi(defaultColor));
        }
    }

    internal static string ColorUser => ResolveColor(ConfigReader.Config.Colors.User, AnsiColors.Green);

    internal static string ColorHost => ResolveColor(ConfigReader.Config.Colors.Host, AnsiColors.Magenta);

    internal static string ColorPath => ResolveColor(ConfigReader.Config.Colors.Path, AnsiColors.Orange);

    internal static string ColorCommandDuration => ResolveColor(ConfigReader.Config.Colors.CommandDuration, AnsiColors.Magenta);

    internal static string ColorBranch => ResolveColor(ConfigReader.Config.Colors.Branch, AnsiColors.BoldCyan);

    internal static string ColorBranchNoUpstream => ResolveColor(ConfigReader.Config.Colors.BranchNoUpstream, AnsiColors.BoldCyan);

    internal static string ColorBranchDetached => ResolveColor(ConfigReader.Config.Colors.BranchDetached, AnsiColors.NormalYellow);

    internal static string ColorAhead => ResolveColor(ConfigReader.Config.Colors.Ahead, AnsiColors.BoldCyan);

    internal static string ColorBehind => ResolveColor(ConfigReader.Config.Colors.Behind, AnsiColors.BoldCyan);

    internal static string ColorStaged => ResolveColor(ConfigReader.Config.Colors.Staged, AnsiColors.Green);

    internal static string ColorUnstaged => ResolveColor(ConfigReader.Config.Colors.Unstaged, AnsiColors.Red);

    internal static string ColorUntracked => ResolveColor(ConfigReader.Config.Colors.Untracked, AnsiColors.Red);

    internal static string ColorStash => ResolveColor(ConfigReader.Config.Colors.Stash, AnsiColors.Magenta);

    internal static string ColorConflict => ResolveColor(ConfigReader.Config.Colors.Conflict, AnsiColors.Red);

    internal static string ColorDirty => ResolveColor(ConfigReader.Config.Colors.Dirty, AnsiColors.Orange);

    internal static string ColorClean => ResolveColor(ConfigReader.Config.Colors.Clean, AnsiColors.Green);

    internal static string ColorMissingPath => ResolveColor(ConfigReader.Config.Colors.MissingPath, AnsiColors.Red);

    internal static string ColorTimeout => ResolveColor(ConfigReader.Config.Colors.Timeout, AnsiColors.Yellow);

    internal static string ColorPromptSymbol => ResolveColor(ConfigReader.Config.Colors.PromptSymbol, AnsiColors.White);

    internal static readonly string ColorReset = PromptColor(AnsiColors.Reset);
}
