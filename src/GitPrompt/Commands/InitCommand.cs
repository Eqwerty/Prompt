using GitPrompt.Configuration;
using GitPrompt.Prompting;

namespace GitPrompt.Commands;

internal static class InitCommand
{
    internal static void Run(string shell)
    {
        var error = GetShellError(shell);
        if (error is not null)
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine("Supported shells: bash");
            Console.Error.WriteLine("Usage: eval \"$(gitprompt init bash)\"");

            Environment.Exit(1);
        }

        ConfigInitializer.InitializeDefaultConfig();

        var script = ShellInitializer.GenerateBashInit().ReplaceLineEndings("\n");
        var bytes = Console.OutputEncoding.GetBytes(script);
        using var stdout = Console.OpenStandardOutput();
        stdout.Write(bytes);
    }

    // Exposed for testing.
    internal static string? GetShellError(string shell)
    {
        if (string.Equals(shell, "bash", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return string.IsNullOrEmpty(shell)
            ? "gitprompt: init requires a shell name"
            : $"gitprompt: unsupported shell for 'init': '{shell}'";
    }
}
