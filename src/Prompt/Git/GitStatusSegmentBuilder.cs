using System.Diagnostics;
using System.Text;
using static Prompt.Constants.BranchLabelTokens;
using static Prompt.Constants.PromptColors;
using static Prompt.Constants.PromptIcons;

namespace Prompt.Git;

internal static class GitStatusSegmentBuilder
{
    private readonly record struct CountStyle(int Value, string Color, string Icon);

    internal static async Task<string> BuildAsync()
    {
        var statusOutput = await RunGitStatusCommandAsync();
        if (statusOutput is null)
        {
            return string.Empty;
        }

        var gitStatusSnapshot = GitStatusParser.Parse(statusOutput);

        var branchHeadName = gitStatusSnapshot.BranchHeadName;
        var headObjectId = gitStatusSnapshot.HeadObjectId;
        var commitsAhead = gitStatusSnapshot.CommitsAhead;
        var commitsBehind = gitStatusSnapshot.CommitsBehind;
        var stashEntryCount = gitStatusSnapshot.StashEntryCount;
        var upstreamReference = gitStatusSnapshot.UpstreamReference;
        var hasUpstream = gitStatusSnapshot.HasUpstream;
        var hasAheadBehindCounts = gitStatusSnapshot.HasAheadBehindCounts;
        var statusCounts = gitStatusSnapshot.StatusCounts;

        var gitDirectoryPath = FindGitDirectoryPath();
        if (gitDirectoryPath is null)
        {
            return string.Empty;
        }

        if (branchHeadName is "(detached)" || string.IsNullOrEmpty(branchHeadName))
        {
            var rebaseBranchName = ResolveRebaseBranchName(gitDirectoryPath);
            if (!string.IsNullOrEmpty(rebaseBranchName))
            {
                var rebaseBranchDescription = BuildBranchLabel(rebaseBranchName, hasUpstream);

                return BuildDisplay(rebaseBranchDescription, commitsAhead, commitsBehind, stashEntryCount, statusCounts, gitDirectoryPath);
            }

            var shortObjectId = ShortenObjectId(headObjectId);
            if (string.IsNullOrEmpty(shortObjectId))
            {
                return string.Empty;
            }

            var matchingRemoteReferences = FindMatchingRemoteReferences(gitDirectoryPath, headObjectId);
            var detachedBranchDescription = BuildBranchLabel($"{shortObjectId}...");
            if (matchingRemoteReferences.Count is 1)
            {
                detachedBranchDescription = BuildBranchLabel($"{matchingRemoteReferences[0]} {shortObjectId}...");
            }

            return BuildDisplay(detachedBranchDescription, commitsAhead, commitsBehind, stashEntryCount, statusCounts, gitDirectoryPath);
        }

        if (hasUpstream && !hasAheadBehindCounts && !string.IsNullOrEmpty(upstreamReference))
        {
            var (computedAheadCount, computedBehindCount) = await ComputeAheadBehindAgainstUpstreamAsync(gitDirectoryPath, upstreamReference);
            commitsAhead = computedAheadCount;
            commitsBehind = computedBehindCount;
        }
        else if (!hasUpstream)
        {
            commitsAhead = await ComputeLocalAheadCommitCountAsync(gitDirectoryPath);
            commitsBehind = 0;
        }

        var branchDescription = BuildBranchLabel(branchHeadName, hasUpstream);

        return BuildDisplay(branchDescription, commitsAhead, commitsBehind, stashEntryCount, statusCounts, gitDirectoryPath);
    }

    internal static string BuildDisplay(
        string branchDescription,
        int commitsAhead,
        int commitsBehind,
        int stashEntryCount,
        StatusCounts statusCounts,
        string gitDirectoryPath)
    {
        var statusBuilder = new StringBuilder();

        var operationName = ReadGitOperationMarker(gitDirectoryPath);
        branchDescription = AppendOperationToBranchLabel(branchDescription, operationName);

        var noUpstreamPrefix = NoUpstreamBranchMarker + BranchLabelOpen;
        var branchColor = branchDescription.StartsWith(noUpstreamPrefix, StringComparison.Ordinal)
            ? ColorBranchNoUpstream
            : ColorBranch;

        statusBuilder.Append(branchColor).Append(branchDescription).Append(ColorReset);

        if (commitsAhead > 0)
        {
            statusBuilder.Append(' ').Append(ColorAhead).Append(IconAhead).Append(commitsAhead).Append(ColorReset);
        }

        if (commitsBehind > 0)
        {
            statusBuilder.Append(' ').Append(ColorBehind).Append(IconBehind).Append(commitsBehind).Append(ColorReset);
        }

        AppendCountIndicators(
            statusBuilder,
            new CountStyle(statusCounts.StagedAdded, ColorStaged, IconAdded),
            new CountStyle(statusCounts.StagedModified, ColorStaged, IconModified),
            new CountStyle(statusCounts.StagedRenamed, ColorStaged, IconRenamed),
            new CountStyle(statusCounts.StagedDeleted, ColorStaged, IconDeleted),
            new CountStyle(statusCounts.UnstagedAdded, ColorUnstaged, IconAdded),
            new CountStyle(statusCounts.UnstagedModified, ColorUnstaged, IconModified),
            new CountStyle(statusCounts.UnstagedRenamed, ColorUnstaged, IconRenamed),
            new CountStyle(statusCounts.UnstagedDeleted, ColorUnstaged, IconDeleted)
        );

        if (statusCounts.Untracked > 0)
        {
            statusBuilder.Append(' ').Append(ColorUntracked).Append(IconUntracked).Append(statusCounts.Untracked).Append(ColorReset);
        }

        if (statusCounts.Conflicts > 0)
        {
            statusBuilder.Append(' ').Append(ColorState).Append(IconConflicts).Append(statusCounts.Conflicts).Append(ColorReset);
        }

        if (stashEntryCount > 0)
        {
            statusBuilder.Append(' ').Append(ColorStash).Append(IconStash).Append(stashEntryCount).Append(ColorReset);
        }

        return statusBuilder.ToString();
    }

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

    internal static List<string> FindMatchingRemoteReferences(string gitDirectoryPath, string headObjectId)
    {
        var matchingRemoteReferences = new List<string>();
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
                    matchingRemoteReferences.Add(relativeReferencePath);
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
            foreach (var line in EnumerateLines(File.ReadAllText(packedReferencesPath)))
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
                matchingRemoteReferences.Add(relativeReferencePath);
            }
        }

        return matchingRemoteReferences;
    }

    internal static string ShortenObjectId(string objectId)
    {
        if (string.IsNullOrEmpty(objectId))
        {
            return string.Empty;
        }

        return objectId.Length >= 7 ? objectId[..7] : objectId;
    }

    internal static string EscapeCommandLineArgument(string argument)
    {
        if (argument.Length is 0)
        {
            return "\"\"";
        }

        if (!argument.Any(static c => char.IsWhiteSpace(c) || c is '"'))
        {
            return argument;
        }

        return "\"" + argument.Replace("\\", @"\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    private static string BuildBranchLabel(string branchName, bool hasUpstream = true)
    {
        var noUpstreamPrefix = hasUpstream ? string.Empty : NoUpstreamBranchMarker;

        return $"{noUpstreamPrefix}{BranchLabelOpen}{branchName}{BranchLabelClose}";
    }

    private static string AppendOperationToBranchLabel(string branchLabel, string operationName)
    {
        if (string.IsNullOrEmpty(operationName))
        {
            return branchLabel;
        }

        var branchOperationSeparator = "|";
        if (branchLabel.EndsWith(BranchLabelClose, StringComparison.Ordinal))
        {
            return branchLabel[..^BranchLabelClose.Length] + branchOperationSeparator + operationName + BranchLabelClose;
        }

        return branchLabel + branchOperationSeparator + operationName;
    }

    private static void AppendCountIndicators(StringBuilder sb, params CountStyle[] items)
    {
        foreach (var item in items)
        {
            if (item.Value > 0)
            {
                sb.Append(' ').Append(item.Color).Append(item.Icon).Append(item.Value).Append(ColorReset);
            }
        }
    }

    private static Task<string?> RunGitStatusCommandAsync()
    {
        return RunProcessForOutputAsync(
            fileName: "git",
            arguments: "status --porcelain=2 --branch --ahead-behind --show-stash",
            workingDirectory: null,
            requireSuccess: true
        );
    }

    private static string? FindGitDirectoryPath()
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
                    return resolvedGitDirectoryPath;
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

    private static async Task<string?> RunGitCommandInRepositoryAsync(string gitDirectoryPath, params string[] args)
    {
        var repositoryRootPath = Path.GetDirectoryName(gitDirectoryPath);
        if (string.IsNullOrEmpty(repositoryRootPath))
        {
            return null;
        }

        var joinedArguments = string.Join(' ', args.Select(EscapeCommandLineArgument));
        var output = await RunProcessForOutputAsync(
            fileName: "git",
            arguments: joinedArguments,
            workingDirectory: repositoryRootPath,
            requireSuccess: true
        );

        return output?.Trim();
    }

    private static async Task<int> ComputeLocalAheadCommitCountAsync(string gitDirectoryPath)
    {
        var baseReference = await ResolveBaseReferenceAsync(gitDirectoryPath);

        if (string.IsNullOrEmpty(baseReference))
        {
            return 0;
        }

        var forkPointCommit = await RunGitCommandInRepositoryAsync(gitDirectoryPath, "merge-base", "--fork-point", baseReference, "HEAD") ?? string.Empty;
        if (string.IsNullOrEmpty(forkPointCommit))
        {
            forkPointCommit = await RunGitCommandInRepositoryAsync(gitDirectoryPath, "merge-base", baseReference, "HEAD") ?? string.Empty;
        }

        var commitRangeSpec = !string.IsNullOrEmpty(forkPointCommit)
            ? $"{forkPointCommit}..HEAD"
            : $"{baseReference}..HEAD";

        var commitCountOutput = await RunGitCommandInRepositoryAsync(gitDirectoryPath, "rev-list", "--count", commitRangeSpec);

        return int.TryParse(commitCountOutput, out var commitCount) ? commitCount : 0;
    }

    private static async Task<string> ResolveBaseReferenceAsync(string gitDirectoryPath)
    {
        var baseReference = await RunGitCommandInRepositoryAsync(gitDirectoryPath, "symbolic-ref", "--quiet", "--short", "refs/remotes/origin/HEAD");
        if (!string.IsNullOrEmpty(baseReference))
        {
            return baseReference;
        }

        var showRefOutput = await RunGitCommandInRepositoryAsync(gitDirectoryPath, "show-ref");
        if (!string.IsNullOrEmpty(showRefOutput))
        {
            var availableReferences = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in EnumerateLines(showRefOutput))
            {
                var separatorIndex = line.IndexOf(' ');
                if (separatorIndex < 0 || separatorIndex + 1 >= line.Length)
                {
                    continue;
                }

                availableReferences.Add(line[(separatorIndex + 1)..]);
            }

            foreach (var candidateReference in new[] { "origin/main", "origin/master", "main", "master" })
            {
                if (availableReferences.Contains($"refs/remotes/{candidateReference}"))
                {
                    return candidateReference;
                }

                var localCandidateReference = candidateReference.StartsWith("origin/", StringComparison.Ordinal)
                    ? candidateReference["origin/".Length..]
                    : candidateReference;

                if (availableReferences.Contains($"refs/heads/{localCandidateReference}"))
                {
                    return localCandidateReference;
                }
            }
        }

        var upstreamReference = await RunGitCommandInRepositoryAsync(gitDirectoryPath, "rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{u}");

        return string.IsNullOrEmpty(upstreamReference) ? string.Empty : "@{u}";
    }

    private static async Task<(int Ahead, int Behind)> ComputeAheadBehindAgainstUpstreamAsync(string gitDirectoryPath, string upstreamReference)
    {
        if (string.IsNullOrEmpty(upstreamReference))
        {
            return (Ahead: 0, Behind: 0);
        }

        var leftRightCountsOutput = await RunGitCommandInRepositoryAsync(
            gitDirectoryPath,
            "rev-list",
            "--left-right",
            "--count",
            $"{upstreamReference}...HEAD"
        );

        if (string.IsNullOrWhiteSpace(leftRightCountsOutput))
        {
            return (Ahead: 0, Behind: 0);
        }

        var countParts = leftRightCountsOutput.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (countParts.Length < 2)
        {
            return (Ahead: 0, Behind: 0);
        }

        _ = int.TryParse(countParts[0], out var commitsBehind);
        _ = int.TryParse(countParts[1], out var commitsAhead);

        return (commitsAhead, commitsBehind);
    }

    private static IEnumerable<string> EnumerateLines(string text)
    {
        using var reader = new StringReader(text);
        while (reader.ReadLine() is string line)
        {
            yield return line;
        }
    }

    private static async Task<string?> RunProcessForOutputAsync(string fileName, string arguments, string? workingDirectory, bool requireSuccess)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory ?? string.Empty
            };

            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync());

            if (requireSuccess && process.ExitCode is not 0)
            {
                return null;
            }

            return process.ExitCode is 0 ? stdoutTask.Result : string.Empty;
        }
        catch
        {
            return null;
        }
    }
}
