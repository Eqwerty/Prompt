
using FluentAssertions;

namespace Prompt.Tests.Unit;

public sealed class ProgramTests
{
    [Fact]
    public void ParseGitStatusOutput_ParsesAheadBehindAndStatusCounters()
    {
        const string statusOutput = """
# branch.oid 1234567890abcdef1234567890abcdef12345678
# branch.head master
# branch.upstream origin/master
# branch.ab +3 -2
1 A. ignored
1 .M ignored
2 R. ignored
2 .R ignored
1 D. ignored
1 .D ignored
? untracked.txt
u UU ignored
""";

        var gitStatusSnapshot = Program.ParseGitStatusOutput(statusOutput);

        gitStatusSnapshot.BranchHeadName.Should().Be("master");
        gitStatusSnapshot.HeadObjectId.Should().Be("1234567890abcdef1234567890abcdef12345678");
        gitStatusSnapshot.UpstreamReference.Should().Be("origin/master");
        gitStatusSnapshot.HasUpstream.Should().BeTrue();
        gitStatusSnapshot.HasAheadBehindCounts.Should().BeTrue();
        gitStatusSnapshot.CommitsAhead.Should().Be(3);
        gitStatusSnapshot.CommitsBehind.Should().Be(2);

        var statusCounts = gitStatusSnapshot.StatusCounts;
        statusCounts.StagedAdded.Should().Be(1);
        statusCounts.UnstagedModified.Should().Be(1);
        statusCounts.StagedRenamed.Should().Be(1);
        statusCounts.UnstagedRenamed.Should().Be(1);
        statusCounts.StagedDeleted.Should().Be(1);
        statusCounts.UnstagedDeleted.Should().Be(1);
        statusCounts.Untracked.Should().Be(1);
        statusCounts.Conflicts.Should().Be(1);
    }

    [Fact]
    public void BuildGitStatusDisplay_IncludesReadableIndicatorsForCounts()
    {
        using var gitDirectory = new TemporaryDirectory();
        var stashLogDirectoryPath = Path.Combine(gitDirectory.DirectoryPath, "logs", "refs");
        Directory.CreateDirectory(stashLogDirectoryPath);
        File.WriteAllText(Path.Combine(stashLogDirectoryPath, "stash"), "entry-1\nentry-2\n");

        var statusCounts = new Program.StatusCounts
        {
            StagedRenamed = 1,
            UnstagedModified = 1,
            Untracked = 1,
            Conflicts = 1
        };

        var gitStatusDisplay = Program.BuildGitStatusDisplay("(main)", 4, 2, statusCounts, gitDirectory.DirectoryPath);

        gitStatusDisplay.Should().Contain("(main)");
        gitStatusDisplay.Should().Contain("↑4");
        gitStatusDisplay.Should().Contain("↓2");
        gitStatusDisplay.Should().Contain("~1");
        gitStatusDisplay.Should().Contain("→1");
        gitStatusDisplay.Should().Contain("?1");
        gitStatusDisplay.Should().Contain("@2");
        gitStatusDisplay.Should().Contain("!1");
    }

    [Fact]
    public void FindMatchingRemoteReferences_ReturnsLooseAndPackedRemoteRefs()
    {
        using var gitDirectory = new TemporaryDirectory();
        var remoteDirectoryPath = Path.Combine(gitDirectory.DirectoryPath, "refs", "remotes", "origin");
        Directory.CreateDirectory(remoteDirectoryPath);

        File.WriteAllText(Path.Combine(remoteDirectoryPath, "main"), "abcdef1234567890\n");
        File.WriteAllText(
            Path.Combine(gitDirectory.DirectoryPath, "packed-refs"),
            """
# pack-refs with: peeled fully-peeled sorted
abcdef1234567890 refs/remotes/origin/release
1111111111111111 refs/remotes/origin/other
"""
        );

        var matchingRemoteReferences = Program.FindMatchingRemoteReferences(gitDirectory.DirectoryPath, "abcdef1234567890");

        matchingRemoteReferences.Should().Contain("origin/main");
        matchingRemoteReferences.Should().Contain("origin/release");
        matchingRemoteReferences.Should().NotContain("origin/other");
    }

    [Fact]
    public void ReadStashEntryCount_CountsStashLogLines()
    {
        using var gitDirectory = new TemporaryDirectory();
        var stashLogDirectoryPath = Path.Combine(gitDirectory.DirectoryPath, "logs", "refs");
        Directory.CreateDirectory(stashLogDirectoryPath);
        File.WriteAllText(Path.Combine(stashLogDirectoryPath, "stash"), "first\nsecond\nthird\n");

        var stashEntryCount = Program.ReadStashEntryCount(gitDirectory.DirectoryPath);

        stashEntryCount.Should().Be(3);
    }

    [Theory]
    [InlineData("", "\"\"")]
    [InlineData("simple", "simple")]
    [InlineData("two words", "\"two words\"")]
    public void EscapeCommandLineArgument_QuotesOnlyWhenNecessary(string value, string expected)
    {
        var escapedValue = Program.EscapeCommandLineArgument(value);

        escapedValue.Should().Be(expected);
    }

    [Fact]
    public void EscapeCommandLineArgument_EscapesBackslashesAndQuotesInsideQuotedArguments()
    {
        var escapedValue = Program.EscapeCommandLineArgument("C:\\Program Files\\My \"App\"");

        escapedValue.Should().Be("\"C:\\\\Program Files\\\\My \\\"App\\\"\"");
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("123456", "123456")]
    [InlineData("1234567", "1234567")]
    [InlineData("1234567890", "1234567")]
    public void ShortenObjectId_ReturnsExpectedShortForm(string objectId, string expectedShortObjectId)
    {
        var shortObjectId = Program.ShortenObjectId(objectId);

        shortObjectId.Should().Be(expectedShortObjectId);
    }

    [Fact]
    public void ParseGitStatusOutput_TracksAllSupportedStatusCodesAndNoUpstreamState()
    {
        const string statusOutput = """
# branch.oid abcdef1234567890abcdef1234567890abcdef12
# branch.head feature
1 AM file-a
1 MD file-b
2 R. file-c file-c-renamed
2 .R file-d file-d-renamed
1 .A file-e
1 .D file-f
? untracked.txt
u UU conflict.txt
""";

        var gitStatusSnapshot = Program.ParseGitStatusOutput(statusOutput);

        gitStatusSnapshot.HasUpstream.Should().BeFalse();
        gitStatusSnapshot.HasAheadBehindCounts.Should().BeFalse();
        gitStatusSnapshot.CommitsAhead.Should().Be(0);
        gitStatusSnapshot.CommitsBehind.Should().Be(0);

        var statusCounts = gitStatusSnapshot.StatusCounts;
        statusCounts.StagedAdded.Should().Be(1);
        statusCounts.StagedModified.Should().Be(1);
        statusCounts.StagedDeleted.Should().Be(0);
        statusCounts.StagedRenamed.Should().Be(1);
        statusCounts.UnstagedAdded.Should().Be(1);
        statusCounts.UnstagedModified.Should().Be(1);
        statusCounts.UnstagedDeleted.Should().Be(2);
        statusCounts.UnstagedRenamed.Should().Be(1);
        statusCounts.Untracked.Should().Be(1);
        statusCounts.Conflicts.Should().Be(1);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), "Prompt.Tests.Unit", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                foreach (var filePath in Directory.EnumerateFiles(DirectoryPath, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                }

                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
    }
}

