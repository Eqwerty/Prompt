using System.Collections.Concurrent;
using static Prompt.Git.Utilities;

namespace Prompt.Git;

internal static class GitRepositoryLocator
{
    internal readonly record struct RepositoryContext(string WorkingTreePath, string GitDirectoryPath);

    private static readonly ConcurrentDictionary<string, RepositoryContext> RepositoryContextCache = new(FileSystemPathComparer);

    internal static RepositoryContext? FindRepositoryContext()
    {
        try
        {
            return FindRepositoryContext(Directory.GetCurrentDirectory());
        }
        catch
        {
            return null;
        }
    }

    internal static RepositoryContext? FindRepositoryContext(string startDirectoryPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(startDirectoryPath) || !Directory.Exists(startDirectoryPath))
            {
                return null;
            }

            var current = Path.GetFullPath(startDirectoryPath);
            var scannedPaths = new List<string>();

            while (true)
            {
                scannedPaths.Add(current);

                if (TryGetValidCachedRepositoryContext(current, out var cachedContext))
                {
                    CacheRepositoryContext(scannedPaths, cachedContext);

                    return cachedContext;
                }

                if (TryGetValidSharedCachedRepositoryContext(current, out var sharedCachedContext))
                {
                    CacheRepositoryContext(scannedPaths, sharedCachedContext);

                    return sharedCachedContext;
                }

                var candidate = Path.Combine(current, ".git");
                var resolvedGitDirectoryPath = ResolveGitDirectoryPath(candidate);
                if (!string.IsNullOrEmpty(resolvedGitDirectoryPath))
                {
                    var repositoryContext = new RepositoryContext(current, resolvedGitDirectoryPath);
                    CacheRepositoryContext(scannedPaths, repositoryContext);

                    return repositoryContext;
                }

                var parent = Directory.GetParent(current);
                if (parent is null)
                {
                    return null;
                }

                current = parent.FullName;
            }
        }
        catch
        {
            return null;
        }
    }

    internal static string? ResolveGitDirectoryPath(string dotGitPath)
    {
        if (Directory.Exists(dotGitPath))
        {
            return dotGitPath;
        }

        if (!File.Exists(dotGitPath))
        {
            return null;
        }

        try
        {
            var referenceLine = File.ReadLines(dotGitPath).FirstOrDefault()?.Trim();
            if (string.IsNullOrEmpty(referenceLine))
            {
                return null;
            }

            const string gitDirectoryPrefix = "gitdir:";
            if (!referenceLine.StartsWith(gitDirectoryPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var gitDirectoryValue = referenceLine[gitDirectoryPrefix.Length..].Trim().Trim('"');
            if (string.IsNullOrEmpty(gitDirectoryValue))
            {
                return null;
            }

            var dotGitParentPath = Path.GetDirectoryName(dotGitPath);
            if (string.IsNullOrEmpty(dotGitParentPath))
            {
                return null;
            }

            var resolvedPath = Path.IsPathRooted(gitDirectoryValue)
                ? gitDirectoryValue
                : Path.GetFullPath(Path.Combine(dotGitParentPath, gitDirectoryValue));

            return Directory.Exists(resolvedPath) ? resolvedPath : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryGetValidCachedRepositoryContext(string path, out RepositoryContext repositoryContext)
    {
        if (RepositoryContextCache.TryGetValue(path, out repositoryContext))
        {
            if (IsRepositoryContextValid(path, repositoryContext))
            {
                return true;
            }

            RepositoryContextCache.TryRemove(path, out _);
        }

        return false;
    }

    private static bool TryGetValidSharedCachedRepositoryContext(string path, out RepositoryContext repositoryContext)
    {
        if (GitRepositorySharedCache.TryGet(path, out repositoryContext) && IsRepositoryContextValid(path, repositoryContext))
        {
            return true;
        }

        return false;
    }

    private static bool IsRepositoryContextValid(string startDirectoryPath, RepositoryContext repositoryContext)
    {
        if (!Directory.Exists(repositoryContext.WorkingTreePath) || !Directory.Exists(repositoryContext.GitDirectoryPath))
        {
            return false;
        }

        if (!IsPathInWorkingTree(startDirectoryPath, repositoryContext.WorkingTreePath))
        {
            return false;
        }

        var dotGitPath = Path.Combine(repositoryContext.WorkingTreePath, ".git");
        var resolvedGitDirectoryPath = ResolveGitDirectoryPath(dotGitPath);

        return !string.IsNullOrEmpty(resolvedGitDirectoryPath)
               && FileSystemPathComparer.Equals(Path.GetFullPath(resolvedGitDirectoryPath), Path.GetFullPath(repositoryContext.GitDirectoryPath));
    }

    private static bool IsPathInWorkingTree(string path, string workingTreePath)
    {
        var normalizedPath = TrimEndingDirectorySeparator(Path.GetFullPath(path));
        var normalizedWorkingTreePath = TrimEndingDirectorySeparator(Path.GetFullPath(workingTreePath));

        if (FileSystemPathComparer.Equals(normalizedPath, normalizedWorkingTreePath))
        {
            return true;
        }

        var expectedPrefix = normalizedWorkingTreePath + Path.DirectorySeparatorChar;

        return normalizedPath.StartsWith(expectedPrefix, FileSystemPathComparison);
    }

    private static string TrimEndingDirectorySeparator(string path)
    {
        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static void CacheRepositoryContext(IEnumerable<string> paths, RepositoryContext repositoryContext)
    {
        var normalizedPaths = new List<string>();
        foreach (var path in paths)
        {
            var normalizedPath = Path.GetFullPath(path);
            RepositoryContextCache[normalizedPath] = repositoryContext;
            normalizedPaths.Add(normalizedPath);
        }

        GitRepositorySharedCache.Set(normalizedPaths, repositoryContext);
    }
}
