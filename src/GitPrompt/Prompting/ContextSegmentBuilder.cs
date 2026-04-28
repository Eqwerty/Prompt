using GitPrompt.Configuration;
using GitPrompt.Platform;
using static GitPrompt.Constants.PromptColors;

namespace GitPrompt.Prompting;

internal static class ContextSegmentBuilder
{
    internal static string Build(PlatformProvider platformProvider)
    {
        var config = ConfigReader.Config;

        var (resolvedPath, isMissingPath) = ResolveWorkingDirectoryPath(platformProvider);
        var pathColor = isMissingPath ? ColorMissingPath : ColorPath;
        var pathSegment = $"{pathColor}{resolvedPath}{ColorReset}";

        var showUser = config.ShowUser;
        var showHost = config.ShowHost;

        if (showUser && showHost)
        {
            return $"{ColorUser}{ResolveUser(platformProvider)}{ColorReset} {ColorHost}{ResolveHost(platformProvider)}{ColorReset} {pathSegment}";
        }

        if (showUser)
        {
            return $"{ColorUser}{ResolveUser(platformProvider)}{ColorReset} {pathSegment}";
        }

        if (showHost)
        {
            return $"{ColorHost}{ResolveHost(platformProvider)}{ColorReset} {pathSegment}";
        }

        return pathSegment;
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

        return "[user?]";
    }

    private static string ResolveHost(PlatformProvider platformProvider)
    {
        var host = platformProvider.Host;

        if (!string.IsNullOrEmpty(host))
        {
            return host;
        }

        return "[host?]";
    }

    private static (string DisplayPath, bool IsMissingPath) ResolveWorkingDirectoryPath(PlatformProvider platformProvider)
    {
        var workingDirectoryPath = platformProvider.WorkingDirectory.Path;
        var isFallbackPath = platformProvider.WorkingDirectory.IsFromFallback;

        if (string.IsNullOrEmpty(workingDirectoryPath))
        {
            return (DisplayPath: "[path?]", IsMissingPath: false);
        }

        var isMissingPath = isFallbackPath && !Directory.Exists(workingDirectoryPath);

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

        var displayPath = workingDirectoryPath.Replace('\\', '/');
        displayPath = TruncatePath(displayPath, ConfigReader.Config.MaxPathDepth);
        if (isMissingPath)
        {
            displayPath += " [missing]";
        }

        return (displayPath, isMissingPath);
    }

    internal static string TruncatePath(string displayPath, int maxDepth)
    {
        if (maxDepth <= 0 || string.IsNullOrEmpty(displayPath))
        {
            return displayPath;
        }

        // Determine anchor ("~", "", or "") and content segments.
        string anchor;
        string[] segments;

        if (displayPath.StartsWith("~/", StringComparison.Ordinal) || displayPath == "~")
        {
            anchor = "~";
            segments = displayPath.Length > 2
                ? displayPath[2..].Split('/')
                : [];
        }
        else if (displayPath.StartsWith('/'))
        {
            anchor = string.Empty; // represents the leading "/"
            var withoutLeadingSlash = displayPath[1..];
            segments = withoutLeadingSlash.Length > 0
                ? withoutLeadingSlash.Split('/')
                : [];
        }
        else
        {
            anchor = null!;
            segments = displayPath.Split('/');
        }

        if (segments.Length <= maxDepth)
        {
            return displayPath;
        }

        var kept = segments[^maxDepth..];
        var joined = string.Join("/", kept);

        return anchor switch
        {
            "~" => $"~/\u2026/{joined}",
            "" => $"/\u2026/{joined}",
            _ => $"\u2026/{joined}"
        };
    }
}
