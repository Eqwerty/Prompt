namespace GitPrompt.Platform;

internal static class AppPaths
{
    internal static string? GetBinaryPath() => Environment.ProcessPath;

    internal static string GetConfigFilePath()
    {
        return Path.Combine(XdgPaths.GetConfigDirectory(), "config.jsonc");
    }

    internal static string GetAliasesFilePath()
    {
        return Path.Combine(XdgPaths.GetDataDirectory(), "git_aliases.sh");
    }

    internal static string GetCacheDirectory() => XdgPaths.GetCacheDirectory();
}
