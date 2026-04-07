using System.Diagnostics;
using System.Text;

namespace Prompt;

internal static class Program
{
    private const string ColorUser = "\e[0;32m";
    private const string ColorHost = "\e[1;35m";
    private const string ColorPath = "\e[38;5;172m";
    private const string ColorBranch = "\e[1;36m";
    private const string ColorBranchNoUpstream = "\e[1;36m";
    private const string ColorAhead = "\e[1;36m";
    private const string ColorBehind = "\e[1;36m";
    private const string ColorStaged = "\e[0;32m";
    private const string ColorUnstaged = "\e[0;31m";
    private const string ColorUntracked = "\e[0;31m";
    private const string ColorStash = "\e[1;35m";
    private const string ColorState = "\e[1;31m";
    private const string ColorPrompt = "\e[0;37m";
    private const string ColorReset = "\e[0m";

    internal sealed class StatusCounts
    {
        public int StagedAdded;
        public int StagedModified;
        public int StagedDeleted;
        public int StagedRenamed;
        public int UnstagedAdded;
        public int UnstagedModified;
        public int UnstagedDeleted;
        public int UnstagedRenamed;
        public int Untracked;
        public int Conflicts;
    }

    internal sealed class GitStatusSnapshot
    {
        public string BranchHeadName { get; init; } = string.Empty;
        public string HeadObjectId { get; init; } = string.Empty;
        public int CommitsAhead { get; init; }
        public int CommitsBehind { get; init; }
        public string UpstreamReference { get; init; } = string.Empty;
        public bool HasUpstream { get; init; }
        public bool HasAheadBehindCounts { get; init; }
        public StatusCounts StatusCounts { get; init; } = new();
    }

    private readonly record struct CountStyle(int Value, string Color, string Icon);

    private static int Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var promptPrefix = BuildPromptPrefixSegment();
        var gitStatusSegment = BuildGitStatusSegment();

        if (!string.IsNullOrEmpty(gitStatusSegment))
        {
            Console.Write($"{promptPrefix} {gitStatusSegment}\n{ColorPrompt}$ {ColorReset}");
            return 0;
        }

        Console.Write($"{promptPrefix}\n{ColorPrompt}$ {ColorReset}");
        return 0;
    }

    internal static string BuildGitStatusSegment()
    {
        var statusOutput = RunGitStatusCommand();
        if (statusOutput is null)
        {
            return string.Empty;
        }

        var gitStatusSnapshot = ParseGitStatusOutput(statusOutput);
        var branchHeadName = gitStatusSnapshot.BranchHeadName;
        var headObjectId = gitStatusSnapshot.HeadObjectId;
        var commitsAhead = gitStatusSnapshot.CommitsAhead;
        var commitsBehind = gitStatusSnapshot.CommitsBehind;
        var upstreamReference = gitStatusSnapshot.UpstreamReference;
        var hasUpstream = gitStatusSnapshot.HasUpstream;
        var hasAheadBehindCounts = gitStatusSnapshot.HasAheadBehindCounts;
        var statusCounts = gitStatusSnapshot.StatusCounts;

        var gitDirectoryPath = FindGitDirectoryPath();
        if (gitDirectoryPath is null)
        {
            return string.Empty;
        }

        if (branchHeadName == "(detached)" || string.IsNullOrEmpty(branchHeadName))
        {
            var shortObjectId = ShortenObjectId(headObjectId);
            if (string.IsNullOrEmpty(shortObjectId))
            {
                return string.Empty;
            }

            var matchingRemoteReferences = FindMatchingRemoteReferences(gitDirectoryPath, headObjectId);
            var detachedBranchDescription = $"({shortObjectId}...)";
            if (matchingRemoteReferences.Count == 1)
            {
                detachedBranchDescription = $"({matchingRemoteReferences[0]} {shortObjectId}...)";
            }

            return BuildGitStatusDisplay(detachedBranchDescription, commitsAhead, commitsBehind, statusCounts, gitDirectoryPath);
        }

        if (hasUpstream && !hasAheadBehindCounts && !string.IsNullOrEmpty(upstreamReference))
        {
            var (computedAheadCount, computedBehindCount) = ComputeAheadBehindAgainstUpstream(gitDirectoryPath, upstreamReference);
            commitsAhead = computedAheadCount;
            commitsBehind = computedBehindCount;
        }
        else if (!hasUpstream)
        {
            commitsAhead = ComputeLocalAheadCommitCount(gitDirectoryPath);
            commitsBehind = 0;
        }

        var branchDescription = hasUpstream
            ? $"({branchHeadName})"
            : $"{ColorBranchNoUpstream}*({branchHeadName}){ColorReset}";

        return BuildGitStatusDisplay(branchDescription, commitsAhead, commitsBehind, statusCounts, gitDirectoryPath);
    }

    internal static GitStatusSnapshot ParseGitStatusOutput(string statusOutput)
    {
        var branchHeadName = string.Empty;
        var headObjectId = string.Empty;
        var commitsAhead = 0;
        var commitsBehind = 0;
        var upstreamReference = string.Empty;
        var hasUpstream = false;
        var hasAheadBehindCounts = false;
        var statusCounts = new StatusCounts();

        foreach (var line in EnumerateLines(statusOutput))
        {
            if (line.StartsWith("# branch.head ", StringComparison.Ordinal))
            {
                branchHeadName = line[14..];
                continue;
            }

            if (line.StartsWith("# branch.oid ", StringComparison.Ordinal))
            {
                headObjectId = line[13..];
                continue;
            }

            if (line.StartsWith("# branch.ab ", StringComparison.Ordinal))
            {
                var parts = line[12..].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length >= 2)
                {
                    _ = int.TryParse(parts[0].TrimStart('+'), out commitsAhead);
                    _ = int.TryParse(parts[1].TrimStart('-'), out commitsBehind);
                }

                hasUpstream = true;
                hasAheadBehindCounts = true;
                continue;
            }

            if (line.StartsWith("# branch.upstream ", StringComparison.Ordinal))
            {
                upstreamReference = line[18..];
                hasUpstream = true;
                continue;
            }

            if (line.StartsWith("? ", StringComparison.Ordinal))
            {
                statusCounts.Untracked++;
                continue;
            }

            if (line.StartsWith("u ", StringComparison.Ordinal))
            {
                statusCounts.Conflicts++;
                continue;
            }

            if (line.Length >= 4 && (line[0] == '1' || line[0] == '2'))
            {
                var stagedStatusCode = line[2];
                var unstagedStatusCode = line[3];
                AccumulateFileStatus(stagedStatusCode, isStaged: true, statusCounts);
                AccumulateFileStatus(unstagedStatusCode, isStaged: false, statusCounts);
            }
        }

        return new GitStatusSnapshot
        {
            BranchHeadName = branchHeadName,
            HeadObjectId = headObjectId,
            CommitsAhead = commitsAhead,
            CommitsBehind = commitsBehind,
            UpstreamReference = upstreamReference,
            HasUpstream = hasUpstream,
            HasAheadBehindCounts = hasAheadBehindCounts,
            StatusCounts = statusCounts
        };
    }

    internal static void AccumulateFileStatus(char value, bool isStaged, StatusCounts counts)
    {
        switch (value)
        {
            case 'A':
                if (isStaged)
                {
                    counts.StagedAdded++;
                }
                else
                {
                    counts.UnstagedAdded++;
                }

                break;
            case 'M':
                if (isStaged)
                {
                    counts.StagedModified++;
                }
                else
                {
                    counts.UnstagedModified++;
                }

                break;
            case 'D':
                if (isStaged)
                {
                    counts.StagedDeleted++;
                }
                else
                {
                    counts.UnstagedDeleted++;
                }

                break;
            case 'R':
            case 'C':
                if (isStaged)
                {
                    counts.StagedRenamed++;
                }
                else
                {
                    counts.UnstagedRenamed++;
                }

                break;
            case 'U':
                counts.Conflicts++;
                break;
        }
    }

    internal static string BuildGitStatusDisplay(string branchDescription, int commitsAhead, int commitsBehind, StatusCounts statusCounts, string gitDirectoryPath)
    {
        var statusBuilder = new StringBuilder();
        statusBuilder.Append(ColorBranch).Append(branchDescription).Append(ColorReset);

        if (commitsAhead > 0)
        {
            statusBuilder.Append(' ').Append(ColorAhead).Append('↑').Append(commitsAhead).Append(ColorReset);
        }

        if (commitsBehind > 0)
        {
            statusBuilder.Append(' ').Append(ColorBehind).Append('↓').Append(commitsBehind).Append(ColorReset);
        }

        AppendCountIndicators(
            statusBuilder,
            new CountStyle(statusCounts.StagedAdded, ColorStaged, "+"),
            new CountStyle(statusCounts.StagedModified, ColorStaged, "~"),
            new CountStyle(statusCounts.StagedRenamed, ColorStaged, "→"),
            new CountStyle(statusCounts.StagedDeleted, ColorStaged, "-"),
            new CountStyle(statusCounts.UnstagedAdded, ColorUnstaged, "+"),
            new CountStyle(statusCounts.UnstagedModified, ColorUnstaged, "~"),
            new CountStyle(statusCounts.UnstagedRenamed, ColorUnstaged, "→"),
            new CountStyle(statusCounts.UnstagedDeleted, ColorUnstaged, "-")
        );

        if (statusCounts.Untracked > 0)
        {
            statusBuilder.Append(' ').Append(ColorUntracked).Append('?').Append(statusCounts.Untracked).Append(ColorReset);
        }

        var stashEntryCount = ReadStashEntryCount(gitDirectoryPath);
        if (stashEntryCount > 0)
        {
            statusBuilder.Append(' ').Append(ColorStash).Append('@').Append(stashEntryCount).Append(ColorReset);
        }

        if (statusCounts.Conflicts > 0)
        {
            statusBuilder.Append(' ').Append(ColorState).Append('!').Append(statusCounts.Conflicts).Append(ColorReset);
        }

        return statusBuilder.ToString();
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

    private static string BuildPromptPrefixSegment()
    {
        var user = Environment.GetEnvironmentVariable("USER");
        if (string.IsNullOrEmpty(user))
        {
            user = Environment.GetEnvironmentVariable("USERNAME");
        }

        if (string.IsNullOrEmpty(user))
        {
            user = "?";
        }

        var host = Environment.MachineName;
        if (string.IsNullOrEmpty(host))
        {
            host = "?";
        }

        var dotIndex = host.IndexOf('.');
        if (dotIndex > 0)
        {
            host = host[..dotIndex];
        }

        string workingDirectoryPath;
        try
        {
            workingDirectoryPath = Directory.GetCurrentDirectory();
        }
        catch
        {
            workingDirectoryPath = "?";
        }

        if (!string.IsNullOrEmpty(workingDirectoryPath) && workingDirectoryPath != "?")
        {
            try
            {
                var homeDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(homeDirectoryPath))
                {
                    var fullWorkingDirectoryPath = Path.GetFullPath(workingDirectoryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    var fullHomeDirectoryPath = Path.GetFullPath(homeDirectoryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    var pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

                    if (string.Equals(fullWorkingDirectoryPath, fullHomeDirectoryPath, pathComparison))
                    {
                        workingDirectoryPath = "~";
                    }
                    else if (fullWorkingDirectoryPath.StartsWith(fullHomeDirectoryPath + Path.DirectorySeparatorChar, pathComparison))
                    {
                        workingDirectoryPath = "~" + fullWorkingDirectoryPath[fullHomeDirectoryPath.Length..];
                    }
                }
            }
            catch
            {
                // Ignore path normalization failures and keep the raw working directory.
            }

            workingDirectoryPath = workingDirectoryPath.Replace('\\', '/');
        }

        return $"{ColorUser}{user}{ColorReset} {ColorHost}{host}{ColorReset} {ColorPath}{workingDirectoryPath}{ColorReset}";
    }

    private static string? RunGitStatusCommand()
    {
        return RunProcessForOutput(
            fileName: "git",
            arguments: "status --porcelain=2 --branch --ahead-behind",
            workingDirectory: null,
            requireSuccess: true
        );
    }


    private static string? FindGitDirectoryPath()
    {
        string current;
        try
        {
            current = Directory.GetCurrentDirectory();
        }
        catch
        {
            return null;
        }

        while (true)
        {
            var candidate = Path.Combine(current, ".git");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            var parent = Directory.GetParent(current);
            if (parent is null)
            {
                return null;
            }

            current = parent.FullName;
        }
    }

    private static string? RunGitCommandInRepository(string gitDirectoryPath, params string[] args)
    {
        var repositoryRootPath = Path.GetDirectoryName(gitDirectoryPath);
        if (string.IsNullOrEmpty(repositoryRootPath))
        {
            return null;
        }

        var joinedArguments = string.Join(' ', args.Select(EscapeCommandLineArgument));
        var output = RunProcessForOutput(
            fileName: "git",
            arguments: joinedArguments,
            workingDirectory: repositoryRootPath,
            requireSuccess: true
        );

        return output?.Trim();
    }

    private static int ComputeLocalAheadCommitCount(string gitDirectoryPath)
    {
        if (RunGitCommandInRepository(gitDirectoryPath, "rev-parse", "--is-inside-work-tree") is null)
        {
            return 0;
        }

        var currentBranch = RunGitCommandInRepository(gitDirectoryPath, "symbolic-ref", "--quiet", "--short", "HEAD");
        if (string.IsNullOrEmpty(currentBranch))
        {
            return 0;
        }

        var repositoryRootPath = Path.GetDirectoryName(gitDirectoryPath);
        if (string.IsNullOrEmpty(repositoryRootPath))
        {
            return 0;
        }

        var baseReference = RunGitCommandInRepository(gitDirectoryPath, "symbolic-ref", "--quiet", "--short", "refs/remotes/origin/HEAD") ?? string.Empty;

        if (string.IsNullOrEmpty(baseReference))
        {
            var baseReferenceCandidates = new[] { "origin/main", "origin/master", "main", "master" };
            foreach (var candidateReference in baseReferenceCandidates)
            {
                if (RunProcessForOutput("git", $"-C {EscapeCommandLineArgument(repositoryRootPath)} show-ref --verify --quiet refs/remotes/{candidateReference}", null, true) != null)
                {
                    baseReference = candidateReference;
                    break;
                }

                var localCandidateReference = candidateReference.StartsWith("origin/", StringComparison.Ordinal)
                    ? candidateReference[7..]
                    : candidateReference;

                if (RunProcessForOutput("git", $"-C {EscapeCommandLineArgument(repositoryRootPath)} show-ref --verify --quiet refs/heads/{localCandidateReference}", null, true) != null)
                {
                    baseReference = localCandidateReference;
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(baseReference))
        {
            var upstreamReference = RunGitCommandInRepository(gitDirectoryPath, "rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{u}");
            if (!string.IsNullOrEmpty(upstreamReference))
            {
                baseReference = "@{u}";
            }
        }

        if (string.IsNullOrEmpty(baseReference))
        {
            return 0;
        }

        var forkPointCommit = RunGitCommandInRepository(gitDirectoryPath, "merge-base", "--fork-point", baseReference, "HEAD") ?? string.Empty;
        if (string.IsNullOrEmpty(forkPointCommit))
        {
            forkPointCommit = RunGitCommandInRepository(gitDirectoryPath, "merge-base", baseReference, "HEAD") ?? string.Empty;
        }

        var commitRangeSpec = !string.IsNullOrEmpty(forkPointCommit)
            ? $"{forkPointCommit}..HEAD"
            : $"{baseReference}..HEAD";

        var commitCountOutput = RunGitCommandInRepository(gitDirectoryPath, "rev-list", "--count", commitRangeSpec);
        return int.TryParse(commitCountOutput, out var commitCount) ? commitCount : 0;
    }


    private static (int Ahead, int Behind) ComputeAheadBehindAgainstUpstream(string gitDirectoryPath, string upstreamReference)
    {
        if (string.IsNullOrEmpty(upstreamReference))
        {
            return (0, 0);
        }

        var leftRightCountsOutput = RunGitCommandInRepository(
            gitDirectoryPath,
            "rev-list",
            "--left-right",
            "--count",
            $"{upstreamReference}...HEAD"
        );

        if (string.IsNullOrWhiteSpace(leftRightCountsOutput))
        {
            return (0, 0);
        }

        var countParts = leftRightCountsOutput.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        if (countParts.Length < 2)
        {
            return (0, 0);
        }

        _ = int.TryParse(countParts[0], out var commitsBehind);
        _ = int.TryParse(countParts[1], out var commitsAhead);
        return (commitsAhead, commitsBehind);
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
                if (string.IsNullOrEmpty(line) || line[0] == '#' || line[0] == '^')
                {
                    continue;
                }

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length != 2)
                {
                    continue;
                }

                var referenceObjectId = parts[0];
                var fullReferenceName = parts[1];
                const string prefix = "refs/remotes/";

                if (!fullReferenceName.StartsWith(prefix, StringComparison.Ordinal) || !string.Equals(referenceObjectId, headObjectId, StringComparison.Ordinal))
                {
                    continue;
                }

                var relativeReferencePath = fullReferenceName[prefix.Length..].Replace('\\', '/');
                matchingRemoteReferences.Add(relativeReferencePath);
            }
        }

        return matchingRemoteReferences;
    }

    internal static int ReadStashEntryCount(string gitDirectoryPath)
    {
        var stashLog = Path.Combine(gitDirectoryPath, "logs", "refs", "stash");
        if (!File.Exists(stashLog))
        {
            return 0;
        }

        try
        {
            var data = File.ReadAllBytes(stashLog);
            var count = 0;
            foreach (var value in data)
            {
                if (value == (byte)'\n')
                {
                    count++;
                }
            }

            return count;
        }
        catch
        {
            return 0;
        }
    }

    internal static string ShortenObjectId(string objectId)
    {
        if (string.IsNullOrEmpty(objectId))
        {
            return string.Empty;
        }

        return objectId.Length >= 7 ? objectId[..7] : objectId;
    }

    private static IEnumerable<string> EnumerateLines(string text)
    {
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }

    internal static string EscapeCommandLineArgument(string argument)
    {
        if (argument.Length == 0)
        {
            return "\"\"";
        }

        if (!argument.Any(static c => char.IsWhiteSpace(c) || c == '"'))
        {
            return argument;
        }

        return "\"" + argument.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    private static string? RunProcessForOutput(string fileName, string arguments, string? workingDirectory, bool requireSuccess)
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

            // Drain stderr concurrently so a full stderr pipe never blocks stdout reading.
            var stderrTask = process.StandardError.ReadToEndAsync();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            
            // Wait for stderr to complete without blocking
            try
            {
                stderrTask.Wait(TimeSpan.FromSeconds(1));
            }
            catch
            {
                // Ignore stderr timeout
            }

            if (requireSuccess && process.ExitCode != 0)
            {
                return null;
            }

            return process.ExitCode == 0 ? output : string.Empty;
        }
        catch
        {
            return null;
        }
    }
}
