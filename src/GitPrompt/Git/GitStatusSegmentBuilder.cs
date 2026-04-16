using static GitPrompt.Git.Utilities;

namespace GitPrompt.Git;

internal static class GitStatusSegmentBuilder
{
    internal static async Task<string> BuildAsync(string workingDirectoryPath)
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

        var statusOutput = await RunGitCommandAsync(
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

        if (branchHeadName is "(detached)" || string.IsNullOrEmpty(branchHeadName))
        {
            var rebaseBranchName = GitOperationDetector.ResolveRebaseBranchName(gitDirectoryPath);
            if (!string.IsNullOrEmpty(rebaseBranchName))
            {
                var rebaseBranchLabel = GitStatusDisplayFormatter.BuildBranchLabel(rebaseBranchName, hasUpstream);
                return CacheAndReturn(GitStatusDisplayFormatter.BuildDisplay(rebaseBranchLabel,
                    commitsAhead,
                    commitsBehind,
                    stashEntryCount,
                    statusCounts,
                    gitDirectoryPath));
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
                gitDirectoryPath));
        }

        if (hasUpstream && !hasAheadBehindCounts && !string.IsNullOrEmpty(upstreamReference))
        {
            var (computedAheadCount, computedBehindCount) =
                await GitHistoryCalculator.ComputeAheadBehindAgainstUpstreamAsync(repositoryRootPath, upstreamReference);

            commitsAhead = computedAheadCount;
            commitsBehind = computedBehindCount;
        }
        else if (!hasUpstream)
        {
            commitsAhead = await GitHistoryCalculator.ComputeLocalAheadCommitCountAsync(repositoryRootPath);
            commitsBehind = 0;
        }

        var branchLabel = GitStatusDisplayFormatter.BuildBranchLabel(branchHeadName, hasUpstream);

        return CacheAndReturn(GitStatusDisplayFormatter.BuildDisplay(branchLabel,
            commitsAhead,
            commitsBehind,
            stashEntryCount,
            statusCounts,
            gitDirectoryPath));

        string CacheAndReturn(string segment)
        {
            GitStatusSharedCache.Set(repositoryRootPath, gitDirectoryPath, segment);

            return segment;
        }
    }
}
