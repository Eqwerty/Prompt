namespace GitPrompt.Commands;

internal static class HelpCommand
{
    internal static void PrintHelp()
    {
        var configPath = ConfigCommand.GetConfigFilePath();
        var padWidth = CommandRegistry.All.Max(commandDescriptor => commandDescriptor.Usage.Length) + 5;

        Console.WriteLine("GitPrompt - fast Git prompt for Bash");
        Console.WriteLine();
        Console.WriteLine("Usage:");

        foreach (var command in CommandRegistry.All)
        {
            Console.WriteLine($"  {command.Usage.PadRight(padWidth)}{command.Description}");
        }

        Console.WriteLine();
        Console.WriteLine($"Config: {configPath}");
    }
}
