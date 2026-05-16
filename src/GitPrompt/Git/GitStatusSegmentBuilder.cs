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

        var snapshot = GitStatusParser.Parse(statusOutput);
        var operationName = GitOperationDetector.ReadGitOperationMarker(gitDirectoryPath);

        BranchLabelInfo branchLabel;
        if (snapshot.BranchHeadName is "(detached)" || string.IsNullOrEmpty(snapshot.BranchHeadName))
        {
            branchLabel = ResolveDetachedHeadBranchLabel(gitDirectoryPath, snapshot.HeadObjectId);
            if (string.IsNullOrEmpty(branchLabel.Label))
            {
                return string.Empty;
            }
        }
        else
        {
            var (commitsAhead, commitsBehind) = ResolveAheadBehindCounts(repositoryRootPath, snapshot);
            snapshot = snapshot with { CommitsAhead = commitsAhead, CommitsBehind = commitsBehind };

            var isInOperation = !string.IsNullOrEmpty(operationName);
            var state = snapshot.HasUpstream || isInOperation ? BranchState.Normal : BranchState.NoUpstream;
            branchLabel = GitStatusDisplayFormatter.BuildBranchLabel(snapshot.BranchHeadName, state);
        }

        var segment = ConfigReader.Config.Compact
            ? GitStatusDisplayFormatter.BuildDisplayCompact(branchLabel, snapshot.CommitsAhead, snapshot.CommitsBehind, snapshot.StashEntryCount, snapshot.GitStatusCounts.IsDirty, operationName)
            : GitStatusDisplayFormatter.BuildDisplay(branchLabel, snapshot.CommitsAhead, snapshot.CommitsBehind, snapshot.StashEntryCount, snapshot.GitStatusCounts, operationName);

        GitStatusSharedCache.Set(repositoryRootPath, gitDirectoryPath, segment);
        return segment;
    }

    private static BranchLabelInfo ResolveDetachedHeadBranchLabel(string gitDirectoryPath, string headObjectId)
    {
        var rebaseBranchName = GitOperationDetector.ResolveRebaseBranchName(gitDirectoryPath);
        if (!string.IsNullOrEmpty(rebaseBranchName))
        {
            // During rebase, show as normal — the operation name in the label explains the state.
            return GitStatusDisplayFormatter.BuildBranchLabel(rebaseBranchName, BranchState.Normal);
        }

        var shortObjectId = ShortenCommitHash(headObjectId);
        if (string.IsNullOrEmpty(shortObjectId))
        {
            return default;
        }

        var matchingRemoteReferences = GitOperationDetector.FindMatchingRemoteReferences(gitDirectoryPath, headObjectId);
        var label = matchingRemoteReferences.Count is 1
            ? $"{matchingRemoteReferences[0]} {shortObjectId}..."
            : $"{shortObjectId}...";

        return GitStatusDisplayFormatter.BuildBranchLabel(label, BranchState.Detached);
    }

    private static (int Ahead, int Behind) ResolveAheadBehindCounts(string repositoryRootPath, GitStatusSnapshot snapshot)
    {
        if (snapshot.HasUpstream && !snapshot.HasAheadBehindCounts && !string.IsNullOrEmpty(snapshot.UpstreamReference))
        {
            return GitHistoryCalculator.ComputeAheadBehindAgainstUpstream(repositoryRootPath, snapshot.UpstreamReference);
        }

        if (!snapshot.HasUpstream)
        {
            return (GitHistoryCalculator.ComputeLocalAheadCommitCount(repositoryRootPath), 0);
        }

        return (snapshot.CommitsAhead, snapshot.CommitsBehind);
    }
}
