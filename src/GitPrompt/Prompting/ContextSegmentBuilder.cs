using GitPrompt.Configuration;
using GitPrompt.Platform;
using static GitPrompt.Constants.PromptColors;

namespace GitPrompt.Prompting;

internal static class ContextSegmentBuilder
{
    internal static string Build(PlatformProvider platformProvider)
    {
        var userSegment = ResolveUser(platformProvider);
        var hostSegment = ResolveHost(platformProvider);
        var pathSegment = ResolvePath(platformProvider);

        var response = userSegment;

        if (!string.IsNullOrEmpty(hostSegment))
        {
            response += $" {hostSegment}";
        }

        if (!string.IsNullOrEmpty(pathSegment))
        {
            response += $" {pathSegment}";
        }
        
        return response.TrimStart();
    }

    private static string ResolveUser(PlatformProvider platformProvider)
    {
        if (ConfigReader.Config.Context?.ShowUser is false)
        {
            return string.Empty;
        }

        var user = platformProvider.User;

        if (!string.IsNullOrEmpty(user))
        {
            return $"{ColorUser}{user}{ColorReset}";
        }

        var windowsUserName = platformProvider.WindowsUserName;

        if (!string.IsNullOrEmpty(windowsUserName))
        {
            if (ConfigReader.Config.Context!.ShowDomain ?? false)
            {
                var domain = platformProvider.WindowsUserDomain;

                if (!string.IsNullOrEmpty(domain))
                {
                    return $"{ColorUser}{domain}+{windowsUserName}{ColorReset}";
                }
            }

            return $"{ColorUser}{windowsUserName}{ColorReset}";
        }

        return $"{ColorUser}[user?]{ColorReset}";
    }

    private static string ResolveHost(PlatformProvider platformProvider)
    {
        if (ConfigReader.Config.Context?.ShowHost is false)
        {
            return string.Empty;
        }

        var host = platformProvider.Host;

        if (!string.IsNullOrEmpty(host))
        {
            return $"{ColorHost}{host}{ColorReset}";
        }

        return $"{ColorHost}[host?]{ColorReset}";
    }

    private static string ResolvePath(PlatformProvider platformProvider)
    {
        if (ConfigReader.Config.Context?.ShowPath is false)
        {
            return string.Empty;
        }

        var workingDirectoryPath = platformProvider.WorkingDirectory.Path;
        var isFallbackPath = platformProvider.WorkingDirectory.IsFromFallback;

        if (string.IsNullOrEmpty(workingDirectoryPath))
        {
            return $"{ColorPath}[path?]{ColorReset}";
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
        displayPath = TruncatePath(displayPath, ConfigReader.Config.Context!.MaxPathDepth ?? 0);
        if (isMissingPath)
        {
            displayPath += " [missing]";
        }

        var pathColor = isMissingPath ? ColorMissingPath : ColorPath;

        return $"{pathColor}{displayPath}{ColorReset}";
    }

    internal static string TruncatePath(string displayPath, int maxDepth)
    {
        if (maxDepth <= 0 || string.IsNullOrEmpty(displayPath))
        {
            return displayPath;
        }

        string anchor;
        string[] segments;

        if (displayPath.StartsWith("~/", StringComparison.Ordinal) || displayPath is "~")
        {
            anchor = "~";
            segments = displayPath.Length > 2
                ? displayPath[2..].Split('/')
                : [];
        }
        else if (displayPath.StartsWith('/'))
        {
            anchor = string.Empty;
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
            "~" => $"~/.../{joined}",
            "" => $"/.../{joined}",
            _ => $".../{joined}"
        };
    }
}
