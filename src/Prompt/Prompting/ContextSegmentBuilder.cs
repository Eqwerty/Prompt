using Prompt.Platform;
using static Prompt.Constants.PromptColors;

namespace Prompt.Prompting;

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
        if (!string.IsNullOrEmpty(host))
        {
            return host;
        }

        return "?";
    }

    private static string ResolveWorkingDirectoryPath(PlatformProvider platformProvider)
    {
        var workingDirectoryPath = platformProvider.WorkingDirectoryPath;

        if (string.IsNullOrEmpty(workingDirectoryPath))
        {
            return "?";
        }

        try
        {
            var homeDirectoryPath = platformProvider.HomeDirectoryPath;
            if (!string.IsNullOrEmpty(homeDirectoryPath))
            {
                var fullWorkingDirectoryPath = Path.GetFullPath(workingDirectoryPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                var fullHomeDirectoryPath = Path.GetFullPath(homeDirectoryPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                var pathComparison = platformProvider.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

                if (string.Equals(fullWorkingDirectoryPath, fullHomeDirectoryPath, pathComparison))
                {
                    workingDirectoryPath = "~";
                }
                else if (fullWorkingDirectoryPath.StartsWith(fullHomeDirectoryPath + Path.DirectorySeparatorChar, pathComparison))
                {
                    workingDirectoryPath = "~" + fullWorkingDirectoryPath[fullHomeDirectoryPath.Length..];
                }
            }
        }
        catch
        {
            // Keep the raw path if normalization fails.
        }

        return workingDirectoryPath.Replace('\\', '/');
    }
}
