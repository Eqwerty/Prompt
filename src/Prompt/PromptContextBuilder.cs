using static Prompt.Constants.PromptColors;

namespace Prompt;

internal static class PromptContextBuilder
{
    internal static string Build(PlatformProvider platform)
    {
        var resolvedUser = ResolveUser(platform);
        var resolvedHost = ResolveHost(platform);
        var resolvedPath = ResolveWorkingDirectoryPath(platform);

        return $"{ColorUser}{resolvedUser}{ColorReset} {ColorHost}{resolvedHost}{ColorReset} {ColorPath}{resolvedPath}{ColorReset}";
    }

    private static string ResolveUser(PlatformProvider platform)
    {
        var user = platform.User;
        if (!string.IsNullOrEmpty(user))
        {
            return user;
        }

        var windowsUserName = platform.WindowsUserName;
        if (!string.IsNullOrEmpty(windowsUserName))
        {
            return windowsUserName;
        }

        return "?";
    }

    private static string ResolveHost(PlatformProvider platform)
    {
        var host = platform.Host;
        if (string.IsNullOrEmpty(host))
        {
            return "?";
        }

        var dotIndex = host.IndexOf('.');
        return dotIndex > 0 ? host[..dotIndex] : host;
    }

    private static string ResolveWorkingDirectoryPath(PlatformProvider platform)
    {
        var workingDirectoryPath = platform.WorkingDirectoryPath;
        var resolvedPath = string.IsNullOrEmpty(workingDirectoryPath) ? "?" : workingDirectoryPath;

        if (resolvedPath is "?")
        {
            return resolvedPath;
        }

        try
        {
            var homeDirectoryPath = platform.HomeDirectoryPath;
            if (!string.IsNullOrEmpty(homeDirectoryPath))
            {
                var fullWorkingDirectoryPath = Path.GetFullPath(resolvedPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                var fullHomeDirectoryPath = Path.GetFullPath(homeDirectoryPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                var pathComparison = platform.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

                if (string.Equals(fullWorkingDirectoryPath, fullHomeDirectoryPath, pathComparison))
                {
                    resolvedPath = "~";
                }
                else if (fullWorkingDirectoryPath.StartsWith(fullHomeDirectoryPath + Path.DirectorySeparatorChar, pathComparison))
                {
                    resolvedPath = "~" + fullWorkingDirectoryPath[fullHomeDirectoryPath.Length..];
                }
            }
        }
        catch
        {
            // Keep the raw path if normalization fails.
        }

        return resolvedPath.Replace('\\', '/');
    }
}
