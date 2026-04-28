using GitPrompt.Git;

namespace GitPrompt.Commands;

internal static class CommandRegistry
{
    internal static readonly IReadOnlyList<CommandDescriptor> Commands =
    [
        new(Verb: "--help",
            Usage: "gitprompt --help",
            Description: "Show this help",
            Execute: _ => HelpCommand.PrintHelp()),

        new(Verb: "init",
            Usage: "gitprompt init bash",
            Description: "Print Bash shell integration script",
            Execute: args => InitCommand.Run(args.Length > 1 ? args[1] : string.Empty)),

        new(Verb: "config",
            Usage: "gitprompt config",
            Description: "Open config.jsonc in $EDITOR",
            Execute: _ => ConfigCommand.Run()),

        new(Verb: "config reset",
            Usage: "gitprompt config reset [-y]",
            Description: "Reset config.jsonc to defaults (-y to skip confirmation)",
            Execute: args => ConfigResetCommand.Run(skipConfirmation: args.Contains("-y"))),

        new(Verb: "aliases",
            Usage: "gitprompt aliases",
            Description: "Open git_aliases.sh in $EDITOR (if installed)",
            Execute: _ => AliasesCommand.Run()),

        new(Verb: "update",
            Usage: "gitprompt update",
            Description: "Update to the latest release",
            Execute: _ => UpdateCommand.Run()),

        new(Verb: "update aliases",
            Usage: "gitprompt update aliases",
            Description: "Update git aliases to the latest version",
            Execute: _ => UpdateCommand.RunUpdateAliases()),

        new(Verb: "uninstall",
            Usage: "gitprompt uninstall",
            Description: "Remove gitprompt and its config/cache",
            Execute: _ => UninstallCommand.Run()),

        new(Verb: "debug",
            Usage: "gitprompt debug",
            Description: "Show a diagnostic report for the current directory",
            Execute: _ => DebugCommand.Run()),   

        new(Verb: "--invalidate-status-cache",
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

        if (args.Length > 1 && CommandDescriptorsLookup.TryGetValue($"{args[0]} {args[1]}", out var subCommand))
        {
            subCommand.Execute(args);
            Environment.Exit(0);
        }

        if (!CommandDescriptorsLookup.TryGetValue(args[0], out var command))
        {
            Console.Error.WriteLine($"gitprompt: '{args[0]}' is not a gitprompt command. See gitprompt --help.");
            Environment.Exit(1);
        }

        command.Execute(args);

        Environment.Exit(0);
    }

    internal static readonly IReadOnlyList<CommandDescriptor> VisibleCommands =
        Commands
            .Where(command => !command.IsHidden)
            .ToList();

    private static readonly Dictionary<string, CommandDescriptor> CommandDescriptorsLookup =
        Commands.ToDictionary(command => command.Verb);
}
