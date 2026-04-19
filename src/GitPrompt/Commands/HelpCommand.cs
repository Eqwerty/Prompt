namespace GitPrompt.Commands;

internal static class HelpCommand
{
    internal static void PrintHelp()
    {
        var configPath = OperatingSystem.IsWindows()
            ? @"%APPDATA%\gitprompt\config.json"
            : "~/.config/gitprompt/config.json";

        Console.WriteLine($"""
            gitprompt - fast Git prompt for Bash

            Usage:
              gitprompt               Print the prompt (used in PROMPT_COMMAND)
              gitprompt init bash     Print Bash shell integration script
              gitprompt config        Open config.json in $EDITOR
              gitprompt update        Update to the latest release
              gitprompt uninstall     Remove gitprompt and its config/cache
              gitprompt --help        Show this help

            Config: {configPath}
            """);
    }
}
