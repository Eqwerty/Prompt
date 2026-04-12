using System.Diagnostics;

namespace Prompt.Git;

internal static class GitStatusSegmentBuilder
{
    internal static async Task<string> BuildAsync()
    {
        var repositoryContext = GitRepositoryLocator.FindRepositoryContext();
        if (repositoryContext is null)
        {
            return string.Empty;
        }

        var repositoryRootPath = repositoryContext.Value.WorkingTreePath;
        var gitDirectoryPath = repositoryContext.Value.GitDirectoryPath;

        var statusOutput = await RunGitStatusCommandAsync();
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
                return GitStatusDisplayFormatter.BuildDisplay(rebaseBranchLabel, commitsAhead, commitsBehind, stashEntryCount, statusCounts, gitDirectoryPath);
            }

            var shortObjectId = Utilities.ShortenCommitHash(headObjectId);
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

            return GitStatusDisplayFormatter.BuildDisplay(detachedBranchLabel, commitsAhead, commitsBehind, stashEntryCount, statusCounts, gitDirectoryPath);
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

        return GitStatusDisplayFormatter.BuildDisplay(branchLabel, commitsAhead, commitsBehind, stashEntryCount, statusCounts, gitDirectoryPath);
    }

    private static Task<string?> RunGitStatusCommandAsync()
    {
        return RunProcessForOutputAsync(
            fileName: "git",
            arguments: "status --porcelain=2 --branch --ahead-behind --show-stash",
            workingDirectory: null,
            requireSuccess: true);
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
