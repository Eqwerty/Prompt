namespace GitPrompt.Commands;

internal static class CommandRegistry
{
    internal static readonly IReadOnlyList<CommandDescriptor> All =
    [
        new(Usage: "gitprompt",             Description: "Print the prompt (used in PROMPT_COMMAND)"),
        new(Usage: "gitprompt init bash",   Description: "Print Bash shell integration script"),
        new(Usage: "gitprompt config",      Description: "Open config.json in $EDITOR"),
        new(Usage: "gitprompt update",      Description: "Update to the latest release"),
        new(Usage: "gitprompt uninstall",   Description: "Remove gitprompt and its config/cache"),
        new(Usage: "gitprompt --help",      Description: "Show this help"),
    ];
}
