using GitPrompt.Configuration;

namespace GitPrompt.Commands;

internal static class InitCommand
{
    internal static void Run(string shell)
    {
        EnsureValidShell(shell);

        ConfigInitializer.InitializeDefaultConfig();

        var script = GenerateBashInit().ReplaceLineEndings("\n");
        var bytes = Console.OutputEncoding.GetBytes(script);

        using var stdout = Console.OpenStandardOutput();

        stdout.Write(bytes);
    }

    private static void EnsureValidShell(string shell)
    {
        if (string.Equals(shell, "bash", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var error = string.IsNullOrEmpty(shell)
            ? "gitprompt: init requires a shell name"
            : $"gitprompt: unsupported shell for 'init': '{shell}'";

        Console.Error.WriteLine(error);
        Console.Error.WriteLine("Supported shells: bash");

        Environment.Exit(1);
    }

    internal static string GenerateBashInit()
    {
        using var stream = typeof(InitCommand).Assembly.GetManifestResourceStream("bash-init.sh")!;
        using var reader = new StreamReader(stream);

        var template = reader.ReadToEnd();

        var commands = string.Join(" ",
            CommandRegistry.VisibleCommands
                .Select(command => command.Verb)
                .Where(verb => !verb.Contains(' ')));

        return template.Replace("{{GITPROMPT_COMMANDS}}", commands);
    }
}
