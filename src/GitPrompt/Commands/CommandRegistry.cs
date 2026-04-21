using System.Diagnostics.CodeAnalysis;
using GitPrompt.Git;

namespace GitPrompt.Commands;

internal static class CommandRegistry
{
    internal static readonly IReadOnlyList<CommandDescriptor> Commands =
    [
        new(Verbs: ["--help", "-h", "help"],
            Usage: "gitprompt --help",
            Description: "Show this help",
            Execute: _ => HelpCommand.PrintHelp()),

        new(Verbs: ["init"],
            Usage: "gitprompt init bash",
            Description: "Print Bash shell integration script",
            Execute: args => InitCommand.Run(args.Length > 1 ? args[1] : string.Empty)),

        new(Verbs: ["config"],
            Usage: "gitprompt config",
            Description: "Open config.json in $EDITOR",
            Execute: _ => ConfigCommand.Run()),

        new(Verbs: ["update"],
            Usage: "gitprompt update",
            Description: "Update to the latest release",
            Execute: _ => UpdateCommand.Run()),

        new(Verbs: ["uninstall"],
            Usage: "gitprompt uninstall",
            Description: "Remove gitprompt and its config/cache",
            Execute: _ => UninstallCommand.Run()),

        new(Verbs: ["--invalidate-status-cache"],
            Usage: "gitprompt --invalidate-status-cache",
            Description: "Invalidate the shared Git status cache",
            Execute: _ => GitStatusSharedCache.Invalidate(),
            IsHidden: true)
    ];

    internal static void Dispatch(string[] args)
    {
        if (args.Length is 0)
        {
            return;
        }

        if (!TryGetCommandByVerb(args[0], out var command))
        {
            Console.Error.WriteLine($"gitprompt: '{args[0]}' is not a gitprompt command. See gitprompt --help.");
            Environment.Exit(1);
        }

        command.Execute(args);

        Environment.Exit(0);
    }

    internal static bool TryGetCommandByVerb(string verb, [NotNullWhen(returnValue: true)] out CommandDescriptor? command)
    {
        return CommandDescriptorsLookup.TryGetValue(verb, out command);
    }

    internal static readonly IReadOnlyList<CommandDescriptor> VisibleCommands =
        Commands
            .Where(command => !command.IsHidden)
            .ToList();

    private static readonly Dictionary<string, CommandDescriptor> CommandDescriptorsLookup =
        Commands
            .SelectMany(command => command.Verbs, (command, verb) => (verb, command))
            .ToDictionary(tuple => tuple.verb, tuple => tuple.command);
}
