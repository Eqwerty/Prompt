using GitPrompt.Configuration;
using static GitPrompt.Constants.PromptColors;
using static GitPrompt.Git.Utilities;

namespace GitPrompt.Git;

internal static class GitStatusSegmentBuilder
{
    private static readonly string TimeoutSegment = $"{ColorTimeout}[timeout]{ColorReset}";

    internal static string Build(string workingDirectoryPath)
    {
        try
        {
            return BuildCore(workingDirectoryPath);
        }
        catch (GitCommandTimeoutException)
        {
            return TimeoutSegment;
        }
    }

    private static string BuildCore(string workingDirectoryPath)
    {
        var repositoryContext = GitRepositoryLocator.FindRepositoryContext(workingDirectoryPath);
        if (repositoryContext is null)
        {
            return string.Empty;
        }

        var repositoryRootPath = repositoryContext.Value.WorkingTreePath;
        var gitDirectoryPath = repositoryContext.Value.GitDirectoryPath;

        if (GitStatusSharedCache.TryGet(repositoryRootPath, gitDirectoryPath, out var cachedSegment))
        {
            return cachedSegment;
        }

        var isCompactMode = ConfigReader.Config.Compact;

        var statusOutput = RunGitCommand(
            repositoryRootPath,
            "status",
            "--porcelain=2",
            "--branch",
            "--ahead-behind",
            "--show-stash");

        if (statusOutput is null)
        {
            return string.Empty;
        }

        if (isCompactMode)
        {
            return BuildCoreCompact(repositoryRootPath, gitDirectoryPath, statusOutput);
        }

        return BuildCoreVerbose(repositoryRootPath, gitDirectoryPath, statusOutput);
    }

    private static string BuildCoreCompact(string repositoryRootPath, string gitDirectoryPath, string statusOutput)
    {
        var snapshot = GitStatusParser.ParseCompact(statusOutput);

        var branchHeadName = snapshot.BranchHeadName;
        var headObjectId = snapshot.HeadObjectId;
        var commitsAhead = snapshot.CommitsAhead;
        var commitsBehind = snapshot.CommitsBehind;
        var stashEntryCount = snapshot.StashEntryCount;
        var upstreamReference = snapshot.UpstreamReference;
        var hasUpstream = snapshot.HasUpstream;
        var hasAheadBehindCounts = snapshot.HasAheadBehindCounts;
        var isDirty = snapshot.IsDirty;

        var operationName = GitOperationDetector.ReadGitOperationMarker(gitDirectoryPath);

        if (branchHeadName is "(detached)" || string.IsNullOrEmpty(branchHeadName))
        {
            var rebaseBranchName = GitOperationDetector.ResolveRebaseBranchName(gitDirectoryPath);
            if (!string.IsNullOrEmpty(rebaseBranchName))
            {
                var rebaseBranchLabel = GitStatusDisplayFormatter.BuildBranchLabel(rebaseBranchName);
                
                return CacheAndReturn(GitStatusDisplayFormatter.BuildDisplayCompact(rebaseBranchLabel,
                    commitsAhead,
                    commitsBehind,
                    stashEntryCount,
                    isDirty,
                    operationName));
            }

            var shortObjectId = ShortenCommitHash(headObjectId);
            if (string.IsNullOrEmpty(shortObjectId))
            {
                return string.Empty;
            }

            var matchingRemoteReferences = GitOperationDetector.FindMatchingRemoteReferences(gitDirectoryPath, headObjectId);
            var detachedBranchLabel = GitStatusDisplayFormatter.BuildBranchLabel($"{shortObjectId}...");
            if (matchingRemoteReferences.Count is 1)
            {
                detachedBranchLabel = GitStatusDisplayFormatter.BuildBranchLabel($"{matchingRemoteReferences[0]} {shortObjectId}...");
            }

            return CacheAndReturn(GitStatusDisplayFormatter.BuildDisplayCompact(detachedBranchLabel,
                commitsAhead,
                commitsBehind,
                stashEntryCount,
                isDirty,
                operationName));
        }

        if (hasUpstream && !hasAheadBehindCounts && !string.IsNullOrEmpty(upstreamReference))
        {
            var (computedAheadCount, computedBehindCount) =
                GitHistoryCalculator.ComputeAheadBehindAgainstUpstream(repositoryRootPath, upstreamReference);

            commitsAhead = computedAheadCount;
            commitsBehind = computedBehindCount;
        }
        else if (!hasUpstream)
        {
            commitsAhead = GitHistoryCalculator.ComputeLocalAheadCommitCount(repositoryRootPath);
            commitsBehind = 0;
        }

        var isInOperation = !string.IsNullOrEmpty(operationName);
        var branchLabel = GitStatusDisplayFormatter.BuildBranchLabel(branchHeadName, hasUpstream || isInOperation);

        return CacheAndReturn(GitStatusDisplayFormatter.BuildDisplayCompact(branchLabel,
            commitsAhead,
            commitsBehind,
            stashEntryCount,
            isDirty,
            operationName));

        string CacheAndReturn(string segment)
        {
            GitStatusSharedCache.Set(repositoryRootPath, gitDirectoryPath, segment);
            
            return segment;
        }
    }

    private static string BuildCoreVerbose(string repositoryRootPath, string gitDirectoryPath, string statusOutput)
    {
        var snapshot = GitStatusParser.Parse(statusOutput);

        var branchHeadName = snapshot.BranchHeadName;
        var headObjectId = snapshot.HeadObjectId;
        var commitsAhead = snapshot.CommitsAhead;
        var commitsBehind = snapshot.CommitsBehind;
        var stashEntryCount = snapshot.StashEntryCount;
        var upstreamReference = snapshot.UpstreamReference;
        var hasUpstream = snapshot.HasUpstream;
        var hasAheadBehindCounts = snapshot.HasAheadBehindCounts;
        var statusCounts = snapshot.GitStatusCounts;

        var operationName = GitOperationDetector.ReadGitOperationMarker(gitDirectoryPath);

        if (branchHeadName is "(detached)" || string.IsNullOrEmpty(branchHeadName))
        {
            var rebaseBranchName = GitOperationDetector.ResolveRebaseBranchName(gitDirectoryPath);
            if (!string.IsNullOrEmpty(rebaseBranchName))
            {
                var rebaseBranchLabel = GitStatusDisplayFormatter.BuildBranchLabel(rebaseBranchName);
                
                return CacheAndReturn(GitStatusDisplayFormatter.BuildDisplay(rebaseBranchLabel,
                    commitsAhead,
                    commitsBehind,
                    stashEntryCount,
                    statusCounts,
                    operationName));
            }

            var shortObjectId = ShortenCommitHash(headObjectId);
            if (string.IsNullOrEmpty(shortObjectId))
            {
                return string.Empty;
            }

            var matchingRemoteReferences = GitOperationDetector.FindMatchingRemoteReferences(gitDirectoryPath, headObjectId);
            var detachedBranchLabel = GitStatusDisplayFormatter.BuildBranchLabel($"{shortObjectId}...");
            if (matchingRemoteReferences.Count is 1)
            {
                detachedBranchLabel = GitStatusDisplayFormatter.BuildBranchLabel($"{matchingRemoteReferences[0]} {shortObjectId}...");
            }

            return CacheAndReturn(GitStatusDisplayFormatter.BuildDisplay(detachedBranchLabel,
                commitsAhead,
                commitsBehind,
                stashEntryCount,
                statusCounts,
                operationName));
        }

        if (hasUpstream && !hasAheadBehindCounts && !string.IsNullOrEmpty(upstreamReference))
        {
            var (computedAheadCount, computedBehindCount) =
                GitHistoryCalculator.ComputeAheadBehindAgainstUpstream(repositoryRootPath, upstreamReference);

            commitsAhead = computedAheadCount;
            commitsBehind = computedBehindCount;
        }
        else if (!hasUpstream)
        {
            commitsAhead = GitHistoryCalculator.ComputeLocalAheadCommitCount(repositoryRootPath);
            commitsBehind = 0;
        }

        var isInOperation = !string.IsNullOrEmpty(operationName);
        var branchLabel = GitStatusDisplayFormatter.BuildBranchLabel(branchHeadName, hasUpstream || isInOperation);

        return CacheAndReturn(GitStatusDisplayFormatter.BuildDisplay(branchLabel,
            commitsAhead,
            commitsBehind,
            stashEntryCount,
            statusCounts,
            operationName));

        string CacheAndReturn(string segment)
        {
            GitStatusSharedCache.Set(repositoryRootPath, gitDirectoryPath, segment);

            return segment;
        }
    }
}
