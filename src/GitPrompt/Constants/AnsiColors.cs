namespace GitPrompt.Constants;

internal static class AnsiColors
{
    private const string Escape = "\e";

    internal const string Green = $"{Escape}[0;32m";
    internal const string BoldMagenta = $"{Escape}[1;35m";
    internal const string Orange = $"{Escape}[38;5;172m";
    internal const string BoldCyan = $"{Escape}[1;36m";
    internal const string Red = $"{Escape}[0;31m";
    internal const string BoldRed = $"{Escape}[1;31m";
    internal const string LightGray = $"{Escape}[0;37m";
    internal const string Reset = $"{Escape}[0m";
}
