using GitPrompt.Configuration;

namespace GitPrompt.Commands;

internal static class InitCommand
{
    private const string FallbackPlaceholder = "__FALLBACK_PS1__";

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
        Console.Error.WriteLine("Usage: eval \"$(gitprompt init bash)\"");

        Environment.Exit(1);
    }

    private static string GenerateBashInit()
    {
        var fallbackPs1 = OperatingSystem.IsWindows() ? @"\w > " : @"\w \$ ";

        using var stream = typeof(InitCommand).Assembly.GetManifestResourceStream("bash-init.sh")!;
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd().Replace(FallbackPlaceholder, fallbackPs1);
    }
}
