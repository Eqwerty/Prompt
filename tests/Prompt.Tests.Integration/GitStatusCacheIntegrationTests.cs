using FluentAssertions;
using Prompt.Constants;
using Prompt.Git;

namespace Prompt.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class GitStatusCacheIntegrationTests
{
    [Fact]
    public async Task BuildGitStatusSegment_WhenCalledTwiceWithoutStateChange_ReturnsSameSegment()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        using var cacheDir = new TestHelpers.TemporaryDirectory();
        using var cacheOverride = new TestHelpers.GitStatusCacheOverride(cacheDir.DirectoryPath);

        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "readme.txt"), "hello\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add readme.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"initial commit\"");

        // Act – first call populates the cache
        var firstResult = await GitStatusSegmentBuilder.BuildAsync(repositoryPath);

        // Act – second call should return the cached segment (state unchanged)
        var secondResult = await GitStatusSegmentBuilder.BuildAsync(repositoryPath);

        // Assert
        firstResult.Should().NotBeEmpty();
        secondResult.Should().Be(firstResult);

        var cacheFiles = Directory.GetFiles(cacheDir.DirectoryPath, "*.cache");
        cacheFiles.Should().NotBeEmpty("a cache file should have been written after the first call");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenFetchUpdatesUpstreamRef_CacheIsInvalidatedAndReflectsNewBehindCount()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        using var cacheDir = new TestHelpers.TemporaryDirectory();
        using var cacheOverride = new TestHelpers.GitStatusCacheOverride(cacheDir.DirectoryPath);

        var remoteRepositoryPath = Path.Combine(sandbox.DirectoryPath, "remote.git");
        var sourceRepositoryPath = Path.Combine(sandbox.DirectoryPath, "source");
        var localRepositoryPath = Path.Combine(sandbox.DirectoryPath, "local");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --bare --initial-branch=main {TestHelpers.Quote(remoteRepositoryPath)}");
        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"clone {TestHelpers.Quote(remoteRepositoryPath)} {TestHelpers.Quote(sourceRepositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(sourceRepositoryPath);

        await File.WriteAllTextAsync(Path.Combine(sourceRepositoryPath, "base.txt"), "base\n");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "add base.txt");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "commit -m \"base\"");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "push -u origin main");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"clone {TestHelpers.Quote(remoteRepositoryPath)} {TestHelpers.Quote(localRepositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(localRepositoryPath);

        // Warm the cache: local is up-to-date with remote
        var beforeFetchResult = await GitStatusSegmentBuilder.BuildAsync(localRepositoryPath);
        beforeFetchResult.Should().NotBeEmpty();
        beforeFetchResult.Should().NotContain(PromptIcons.IconBehind.ToString());

        // Remote advances by one commit
        await File.WriteAllTextAsync(Path.Combine(sourceRepositoryPath, "remote-new.txt"), "new\n");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "add remote-new.txt");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "commit -m \"remote advance\"");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "push");

        // Local fetches (updates FETCH_HEAD and origin/main ref → cache fingerprint changes)
        await TestHelpers.RunGitAsync(localRepositoryPath, "fetch origin");

        // Act – should bypass the stale cache entry and compute fresh ahead/behind
        var afterFetchResult = await GitStatusSegmentBuilder.BuildAsync(localRepositoryPath);

        // Assert
        afterFetchResult.Should().Contain(TestHelpers.Indicator(PromptIcons.IconBehind, 1));
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenLinkedWorktreeAndMainWorktree_HaveIndependentCacheFiles()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        using var cacheDir = new TestHelpers.TemporaryDirectory();
        using var cacheOverride = new TestHelpers.GitStatusCacheOverride(cacheDir.DirectoryPath);

        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");
        var worktreePath = Path.Combine(sandbox.DirectoryPath, "feature-worktree");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "base.txt"), "base\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add base.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"base\"");

        await TestHelpers.RunGitAsync(repositoryPath, $"worktree add -b feature {TestHelpers.Quote(worktreePath)}");

        await File.WriteAllTextAsync(Path.Combine(worktreePath, "feature.txt"), "feature\n");
        await TestHelpers.RunGitAsync(worktreePath, "add feature.txt");
        await TestHelpers.RunGitAsync(worktreePath, "commit -m \"feature commit\"");

        // Act
        var mainResult = await GitStatusSegmentBuilder.BuildAsync(repositoryPath);
        var worktreeResult = await GitStatusSegmentBuilder.BuildAsync(worktreePath);

        // Assert – each path gets its own prompt segment reflecting its own branch
        mainResult.Should().Contain(TestHelpers.NoUpstreamBranchLabel("main"));
        worktreeResult.Should().Contain(TestHelpers.NoUpstreamBranchLabel("feature"));

        // Each root path produces its own cache file (keyed on the working-tree root)
        var cacheFiles = Directory.GetFiles(cacheDir.DirectoryPath, "*.cache");
        cacheFiles.Length.Should().BeGreaterThanOrEqualTo(2, "main worktree and linked worktree should have separate cache entries");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenLinkedWorktreeBranchAdvances_WorktreeCacheIsInvalidated()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        using var cacheDir = new TestHelpers.TemporaryDirectory();
        using var cacheOverride = new TestHelpers.GitStatusCacheOverride(cacheDir.DirectoryPath);

        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");
        var worktreePath = Path.Combine(sandbox.DirectoryPath, "feature-worktree");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "base.txt"), "base\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add base.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"base\"");

        await TestHelpers.RunGitAsync(repositoryPath, $"worktree add -b feature {TestHelpers.Quote(worktreePath)}");

        await File.WriteAllTextAsync(Path.Combine(worktreePath, "feature-v1.txt"), "v1\n");
        await TestHelpers.RunGitAsync(worktreePath, "add feature-v1.txt");
        await TestHelpers.RunGitAsync(worktreePath, "commit -m \"feature v1\"");

        // Warm the worktree cache (1 commit ahead)
        var beforeResult = await GitStatusSegmentBuilder.BuildAsync(worktreePath);
        beforeResult.Should().Contain(TestHelpers.Indicator(PromptIcons.IconAhead, 1));

        // Worktree branch advances (HEAD ref changes → cache fingerprint changes)
        await File.WriteAllTextAsync(Path.Combine(worktreePath, "feature-v2.txt"), "v2\n");
        await TestHelpers.RunGitAsync(worktreePath, "add feature-v2.txt");
        await TestHelpers.RunGitAsync(worktreePath, "commit -m \"feature v2\"");

        // Act – stale cache should be rejected and fresh result computed
        var afterResult = await GitStatusSegmentBuilder.BuildAsync(worktreePath);

        // Assert
        afterResult.Should().Contain(TestHelpers.Indicator(PromptIcons.IconAhead, 2));
    }
}
