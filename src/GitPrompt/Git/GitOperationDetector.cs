namespace GitPrompt.Git;

internal static class GitOperationDetector
{
    private static readonly string[] RebaseDirectoryNames = ["rebase-merge", "rebase-apply"];

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

        foreach (var rebaseDirectoryName in RebaseDirectoryNames)
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
        if (string.IsNullOrEmpty(gitDirectoryPath) || string.IsNullOrEmpty(headObjectId))
        {
            return matchingRemoteReferences;
        }

        var remoteReferencesDirectoryPath = Path.Combine(gitDirectoryPath, "refs", "remotes");
        if (Directory.Exists(remoteReferencesDirectoryPath))
        {
            try
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
            catch
            {
                // Ignore disappearing or unreadable refs/remotes directory.
            }
        }

        var packedReferencesPath = Path.Combine(gitDirectoryPath, "packed-refs");
        if (File.Exists(packedReferencesPath))
        {
            try
            {
                const string packedRemotePrefix = "refs/remotes/";
                foreach (var line in File.ReadLines(packedReferencesPath))
                {
                    if (string.IsNullOrEmpty(line) || line[0] is '#' or '^')
                    {
                        continue;
                    }

                    var separatorIndex = line.IndexOf(' ');
                    if (separatorIndex <= 0 || separatorIndex + 1 >= line.Length)
                    {
                        continue;
                    }

                    var referenceObjectId = line[..separatorIndex].Trim();
                    var fullReferenceName = line[(separatorIndex + 1)..].Trim();
                    if (!fullReferenceName.StartsWith(packedRemotePrefix, StringComparison.Ordinal) ||
                        !string.Equals(referenceObjectId, headObjectId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var relativeReferencePath = fullReferenceName[packedRemotePrefix.Length..].Replace('\\', '/');
                    AddMatchingRemoteReference(relativeReferencePath);
                }
            }
            catch
            {
                // Ignore unreadable packed refs.
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
