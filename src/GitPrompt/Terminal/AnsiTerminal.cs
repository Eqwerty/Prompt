namespace GitPrompt.Terminal;

internal static class AnsiTerminal
{
    private static readonly bool ColorsEnabled = DetectColors();

    internal static string Reset => ColorsEnabled ? "\e[0m" : string.Empty;

    internal static string Green => ColorsEnabled ? "\e[32m" : string.Empty;

    internal static string Yellow => ColorsEnabled ? "\e[33m" : string.Empty;

    internal static string Red => ColorsEnabled ? "\e[31m" : string.Empty;

    internal static string HideCursor => ColorsEnabled ? "\e[?25l" : string.Empty;

    internal static string ShowCursor => ColorsEnabled ? "\e[?25h" : string.Empty;

    private static bool DetectColors()
    {
        if (Console.IsOutputRedirected)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR")))
        {
            return false;
        }

        if (Environment.GetEnvironmentVariable("TERM") is "dumb")
        {
            return false;
        }

        return true;
    }
}
