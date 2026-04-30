using GitPrompt.Configuration;

namespace GitPrompt.Constants;

internal static class PromptColors
{
    private const string ReadLineStart = "\u0001";
    private const string ReadLineEnd = "\u0002";

    private static string PromptColor(string ansiColor) => $"{ReadLineStart}{ansiColor}{ReadLineEnd}";

    private static string HexColor(string? configHex, string defaultHex)
    {
        var hex = configHex ?? defaultHex;
        try
        {
            return PromptColor(AnsiColorConverter.ToAnsi(hex));
        }
        catch (ArgumentException)
        {
            return PromptColor(AnsiColorConverter.ToAnsi(defaultHex));
        }
    }

    internal static string ColorUser            => HexColor(ConfigReader.Config.Colors.User,            AnsiColors.Green);
    internal static string ColorHost            => HexColor(ConfigReader.Config.Colors.Host,            AnsiColors.Magenta);
    internal static string ColorPath            => HexColor(ConfigReader.Config.Colors.Path,            AnsiColors.Orange);
    internal static string ColorCommandDuration => HexColor(ConfigReader.Config.Colors.CommandDuration, AnsiColors.Magenta);
    internal static string ColorBranch          => HexColor(ConfigReader.Config.Colors.Branch,          AnsiColors.Blue);
    internal static string ColorBranchNoUpstream => HexColor(ConfigReader.Config.Colors.BranchNoUpstream, AnsiColors.Blue);
    internal static string ColorAhead           => HexColor(ConfigReader.Config.Colors.Ahead,           AnsiColors.Blue);
    internal static string ColorBehind          => HexColor(ConfigReader.Config.Colors.Behind,          AnsiColors.Blue);
    internal static string ColorStaged          => HexColor(ConfigReader.Config.Colors.Staged,          AnsiColors.Green);
    internal static string ColorUnstaged        => HexColor(ConfigReader.Config.Colors.Unstaged,        AnsiColors.Red);
    internal static string ColorUntracked       => HexColor(ConfigReader.Config.Colors.Untracked,       AnsiColors.Red);
    internal static string ColorStash           => HexColor(ConfigReader.Config.Colors.Stash,           AnsiColors.Magenta);
    internal static string ColorConflict        => HexColor(ConfigReader.Config.Colors.Conflict,        AnsiColors.BoldRed);
    internal static string ColorDirty           => HexColor(ConfigReader.Config.Colors.Dirty,           AnsiColors.Orange);
    internal static string ColorClean           => HexColor(ConfigReader.Config.Colors.Clean,           AnsiColors.Green);
    internal static string ColorMissingPath     => HexColor(ConfigReader.Config.Colors.MissingPath,     AnsiColors.BoldRed);
    internal static string ColorTimeout         => HexColor(ConfigReader.Config.Colors.Timeout,         AnsiColors.Yellow);
    internal static string ColorPromptSymbol    => HexColor(ConfigReader.Config.Colors.PromptSymbol,    AnsiColors.LightGray);
    internal static readonly string ColorReset   = PromptColor(AnsiColors.Reset);
}
