using static Prompt.Constants.PromptColors;

namespace Prompt;

internal static class ContextSegmentBuilder
{
    internal static string Build(PlatformProvider platformProvider)
    {
        var resolvedUser = ResolveUser(platformProvider);
        var resolvedHost = ResolveHost(platformProvider);
        var resolvedPath = ResolveWorkingDirectoryPath(platformProvider);

        return $"{ColorUser}{resolvedUser}{ColorReset} {ColorHost}{resolvedHost}{ColorReset} {ColorPath}{resolvedPath}{ColorReset}";
    }

    private static string ResolveUser(PlatformProvider platformProvider)
    {
        var user = platformProvider.User;
        if (!string.IsNullOrEmpty(user))
        {
            return user;
        }

        var windowsUserName = platformProvider.WindowsUserName;
        if (!string.IsNullOrEmpty(windowsUserName))
        {
            return windowsUserName;
        }

        return "?";
    }

    private static string ResolveHost(PlatformProvider platformProvider)
    {
        var host = platformProvider.Host;
        if (string.IsNullOrEmpty(host))
        {
            return "?";
        }

        var dotIndex = host.IndexOf('.');
        return dotIndex > 0 ? host[..dotIndex] : host;
    }

    private static string ResolveWorkingDirectoryPath(PlatformProvider platformProvider)
    {
        var workingDirectoryPath = platformProvider.WorkingDirectoryPath;
        var resolvedPath = string.IsNullOrEmpty(workingDirectoryPath) ? "?" : workingDirectoryPath;

        if (resolvedPath is "?")
        {
            return resolvedPath;
        }

        try
        {
            var homeDirectoryPath = platformProvider.HomeDirectoryPath;
            if (!string.IsNullOrEmpty(homeDirectoryPath))
            {
                var fullWorkingDirectoryPath = Path.GetFullPath(resolvedPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                var fullHomeDirectoryPath = Path.GetFullPath(homeDirectoryPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                var pathComparison = platformProvider.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

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
