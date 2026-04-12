namespace Prompt.Git;

internal static class GitOperationDetector
{
    internal static string ReadGitOperationMarker(string gitDirectoryPath)
    {
        if (string.IsNullOrEmpty(gitDirectoryPath))
        {
            return string.Empty;
        }

        if (Directory.Exists(Path.Combine(gitDirectoryPath, "rebase-merge")) || Directory.Exists(Path.Combine(gitDirectoryPath, "rebase-apply")))
        {
            return "REBASE";
        }

        if (File.Exists(Path.Combine(gitDirectoryPath, "MERGE_HEAD")))
        {
            return "MERGE";
        }

        if (File.Exists(Path.Combine(gitDirectoryPath, "CHERRY_PICK_HEAD")))
        {
            return "CHERRY-PICK";
        }

        if (File.Exists(Path.Combine(gitDirectoryPath, "REVERT_HEAD")))
        {
            return "REVERT";
        }

        if (File.Exists(Path.Combine(gitDirectoryPath, "BISECT_LOG")))
        {
            return "BISECT";
        }

        return string.Empty;
    }

    internal static string ResolveRebaseBranchName(string gitDirectoryPath)
    {
        if (string.IsNullOrEmpty(gitDirectoryPath))
        {
            return string.Empty;
        }

        foreach (var rebaseDirectoryName in new[] { "rebase-merge", "rebase-apply" })
        {
            var headNamePath = Path.Combine(gitDirectoryPath, rebaseDirectoryName, "head-name");
            if (!File.Exists(headNamePath))
            {
                continue;
            }

            try
            {
                var headName = File.ReadAllText(headNamePath).Trim();
                if (string.IsNullOrEmpty(headName))
                {
                    continue;
                }

                const string localHeadPrefix = "refs/heads/";
                if (headName.StartsWith(localHeadPrefix, StringComparison.Ordinal))
                {
                    return headName[localHeadPrefix.Length..];
                }

                const string refsPrefix = "refs/";
                if (headName.StartsWith(refsPrefix, StringComparison.Ordinal))
                {
                    var slashIndex = headName.LastIndexOf('/');
                    if (slashIndex >= 0 && slashIndex + 1 < headName.Length)
                    {
                        return headName[(slashIndex + 1)..];
                    }
                }

                return headName;
            }
            catch
            {
                // Ignore unreadable rebase metadata.
            }
        }

        return string.Empty;
    }

    internal static IReadOnlyList<string> FindMatchingRemoteReferences(string gitDirectoryPath, string headObjectId)
    {
        var matchingRemoteReferences = new List<string>();
        var seenRemoteReferences = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(headObjectId))
        {
            return matchingRemoteReferences;
        }

        var remoteReferencesDirectoryPath = Path.Combine(gitDirectoryPath, "refs", "remotes");
        if (Directory.Exists(remoteReferencesDirectoryPath))
        {
            foreach (var referenceFilePath in Directory.EnumerateFiles(remoteReferencesDirectoryPath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var referenceObjectId = File.ReadAllText(referenceFilePath).Trim();
                    if (!string.Equals(referenceObjectId, headObjectId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var relativeReferencePath = Path.GetRelativePath(remoteReferencesDirectoryPath, referenceFilePath).Replace('\\', '/');
                    AddMatchingRemoteReference(relativeReferencePath);
                }
                catch
                {
                    // Ignore unreadable refs.
                }
            }
        }

        var packedReferencesPath = Path.Combine(gitDirectoryPath, "packed-refs");
        if (File.Exists(packedReferencesPath))
        {
            foreach (var line in Utilities.EnumerateLines(File.ReadAllText(packedReferencesPath)))
            {
                if (string.IsNullOrEmpty(line) || line[0] is '#' or '^')
                {
                    continue;
                }

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length is not 2)
                {
                    continue;
                }

                var referenceObjectId = parts[0];
                var fullReferenceName = parts[1];
                const string prefix = "refs/remotes/";

                if (!fullReferenceName.StartsWith(prefix, StringComparison.Ordinal) ||
                    !string.Equals(referenceObjectId, headObjectId, StringComparison.Ordinal))
                {
                    continue;
                }

                var relativeReferencePath = fullReferenceName[prefix.Length..].Replace('\\', '/');
                AddMatchingRemoteReference(relativeReferencePath);
            }
        }

        return matchingRemoteReferences;

        void AddMatchingRemoteReference(string relativeReferencePath)
        {
            if (seenRemoteReferences.Add(relativeReferencePath))
            {
                matchingRemoteReferences.Add(relativeReferencePath);
            }
        }
    }
}
