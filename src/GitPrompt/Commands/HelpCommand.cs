namespace GitPrompt.Commands;

internal static class HelpCommand
{
    internal static void PrintHelp(TextWriter? output = null)
    {
        output ??= Console.Out;

        var configPath = ConfigCommand.GetConfigFilePath();
        var visibleCommands = CommandRegistry.VisibleCommands;
        var padWidth = visibleCommands.Max(command => command.Usage.Length) + 5;

        output.WriteLine("GitPrompt - fast Git prompt for Bash");
        output.WriteLine();
        output.WriteLine("Usage:");

        foreach (var command in visibleCommands)
        {
            output.WriteLine($"  {command.Usage.PadRight(padWidth)}{command.Description}");
        }

        output.WriteLine();
        output.WriteLine($"Config: {configPath}");
    }
}
