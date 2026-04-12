namespace Prompt.Git;

internal static class GitRepositoryLocator
{
    internal readonly record struct RepositoryContext(string WorkingTreePath, string GitDirectoryPath);

    internal static RepositoryContext? FindRepositoryContext()
    {
        try
        {
            var current = Directory.GetCurrentDirectory();

            while (true)
            {
                var candidate = Path.Combine(current, ".git");
                var resolvedGitDirectoryPath = ResolveGitDirectoryPath(candidate);
                if (!string.IsNullOrEmpty(resolvedGitDirectoryPath))
                {
                    return new RepositoryContext(current, resolvedGitDirectoryPath);
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
}
