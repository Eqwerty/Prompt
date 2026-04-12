namespace Prompt.Git;

internal static class GitHistoryCalculator
{
    internal static async Task<int> ComputeLocalAheadCommitCountAsync(string repositoryRootPath)
    {
        var baseReference = await ResolveBaseReferenceAsync(repositoryRootPath);

        if (string.IsNullOrEmpty(baseReference))
        {
            return 0;
        }

        var forkPointCommit = await RunGitCommandInRepositoryAsync(repositoryRootPath, "merge-base", "--fork-point", baseReference, "HEAD") ?? string.Empty;
        if (string.IsNullOrEmpty(forkPointCommit))
        {
            forkPointCommit = await RunGitCommandInRepositoryAsync(repositoryRootPath, "merge-base", baseReference, "HEAD") ?? string.Empty;
        }

        var commitRangeSpec = !string.IsNullOrEmpty(forkPointCommit)
            ? $"{forkPointCommit}..HEAD"
            : $"{baseReference}..HEAD";

        var commitCountOutput = await RunGitCommandInRepositoryAsync(repositoryRootPath, "rev-list", "--count", commitRangeSpec);

        return int.TryParse(commitCountOutput, out var commitCount) ? commitCount : 0;
    }

    internal static async Task<(int Ahead, int Behind)> ComputeAheadBehindAgainstUpstreamAsync(string repositoryRootPath, string upstreamReference)
    {
        if (string.IsNullOrEmpty(upstreamReference))
        {
            return (Ahead: 0, Behind: 0);
        }

        var leftRightCountsOutput = await RunGitCommandInRepositoryAsync(
            repositoryRootPath,
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

    internal static string EscapeCommandLineArgument(string argument) => Utilities.EscapeCommandLineArgument(argument);

    private static async Task<string> ResolveBaseReferenceAsync(string repositoryRootPath)
    {
        var baseReference = await RunGitCommandInRepositoryAsync(repositoryRootPath, "symbolic-ref", "--quiet", "--short", "refs/remotes/origin/HEAD");
        if (!string.IsNullOrEmpty(baseReference))
        {
            return baseReference;
        }

        var showRefOutput = await RunGitCommandInRepositoryAsync(repositoryRootPath, "show-ref");
        if (!string.IsNullOrEmpty(showRefOutput))
        {
            var availableReferences = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in Utilities.EnumerateLines(showRefOutput))
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

        var upstreamReference = await RunGitCommandInRepositoryAsync(repositoryRootPath, "rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{u}");

        return string.IsNullOrEmpty(upstreamReference) ? string.Empty : "@{u}";
    }

    private static async Task<string?> RunGitCommandInRepositoryAsync(string repositoryRootPath, params string[] args)
    {
        if (string.IsNullOrEmpty(repositoryRootPath))
        {
            return null;
        }

        var joinedArguments = string.Join(' ', args.Select(Utilities.EscapeCommandLineArgument));
        var output = await Utilities.RunProcessForOutputAsync(
            fileName: "git",
            arguments: joinedArguments,
            workingDirectory: repositoryRootPath,
            requireSuccess: true
        );

        return output?.Trim();
    }
}
