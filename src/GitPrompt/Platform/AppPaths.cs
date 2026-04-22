namespace GitPrompt.Platform;

internal static class AppPaths
{
    internal static string GetConfigFilePath()
    {
        return Path.Combine(XdgPaths.GetConfigDirectory(), "config.json");
    }
}
