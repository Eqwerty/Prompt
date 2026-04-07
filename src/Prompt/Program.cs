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

    // Centralized formatting tokens for all git status rendering.
    private const string NoUpstreamBranchMarker = "*";
    private const string BranchLabelOpen = "(";
    private const string BranchLabelClose = ")";
    private const string BranchOperationSeparator = "|";

    private const string IconAhead = "↑";
    private const string IconBehind = "↓";
    private const string IconAdded = "+";
    private const string IconModified = "~";
    private const string IconRenamed = "→";
    private const string IconDeleted = "-";
    private const string IconUntracked = "?";
    private const string IconStash = "@";
    private const string IconConflicts = "!";

    private const string OperationRebase = "REBASE";
    private const string OperationMerge = "MERGE";
    private const string OperationCherryPick = "CHERRY-PICK";
    private const string OperationRevert = "REVERT";
    private const string OperationBisect = "BISECT";

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
            var rebaseBranchName = ResolveRebaseBranchName(gitDirectoryPath);
            if (!string.IsNullOrEmpty(rebaseBranchName))
            {
                var rebaseBranchDescription = BuildBranchLabel(rebaseBranchName, hasUpstream);
                return BuildGitStatusDisplay(rebaseBranchDescription, commitsAhead, commitsBehind, statusCounts, gitDirectoryPath);
            }

            var shortObjectId = ShortenObjectId(headObjectId);
            if (string.IsNullOrEmpty(shortObjectId))
            {
                return string.Empty;
            }

            var matchingRemoteReferences = FindMatchingRemoteReferences(gitDirectoryPath, headObjectId);
            var detachedBranchDescription = BuildBranchLabel($"{shortObjectId}...");
            if (matchingRemoteReferences.Count == 1)
            {
                detachedBranchDescription = BuildBranchLabel($"{matchingRemoteReferences[0]} {shortObjectId}...");
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

        var branchDescription = BuildBranchLabel(branchHeadName, hasUpstream);

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

        var stashEntryCount = ReadStashEntryCount(gitDirectoryPath);
        if (stashEntryCount > 0)
        {
            statusBuilder.Append(' ').Append(ColorStash).Append(IconStash).Append(stashEntryCount).Append(ColorReset);
        }

        if (statusCounts.Conflicts > 0)
        {
            statusBuilder.Append(' ').Append(ColorState).Append(IconConflicts).Append(statusCounts.Conflicts).Append(ColorReset);
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
            return OperationRebase;
        }

        if (File.Exists(Path.Combine(gitDirectoryPath, "MERGE_HEAD")))
        {
            return OperationMerge;
        }

        if (File.Exists(Path.Combine(gitDirectoryPath, "CHERRY_PICK_HEAD")))
        {
            return OperationCherryPick;
        }

        if (File.Exists(Path.Combine(gitDirectoryPath, "REVERT_HEAD")))
        {
            return OperationRevert;
        }

        if (File.Exists(Path.Combine(gitDirectoryPath, "BISECT_LOG")))
        {
            return OperationBisect;
        }

        return string.Empty;
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

        if (branchLabel.EndsWith(BranchLabelClose, StringComparison.Ordinal))
        {
            return branchLabel[..^BranchLabelClose.Length] + BranchOperationSeparator + operationName + BranchLabelClose;
        }

        return branchLabel + BranchOperationSeparator + operationName;
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
