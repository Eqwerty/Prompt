using FluentAssertions;
using GitPrompt.Configuration;
using GitPrompt.Git;

namespace GitPrompt.Tests.Unit.Git;

[Collection(CacheIsolationCollection.Name)]
public sealed class GitStatusSharedCacheTests
{
    [Fact]
    public void TryGet_WhenCacheEnabledAndFingerprintMatches_ShouldReturnSegment()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride =
            ConfigReader.OverrideForTesting(new Config { Cache = new Config.CacheConfig { GitStatusTtl = TimeSpan.FromMilliseconds(5000) } });

        using var cacheDirectoryOverride = GitStatusSharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);

        var repositoryPath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(repositoryPath, ".git");
        var branchRefPath = Path.Combine(gitDirectoryPath, "refs", "heads");

        Directory.CreateDirectory(branchRefPath);
        File.WriteAllText(Path.Combine(gitDirectoryPath, "HEAD"), "ref: refs/heads/main\n");
        File.WriteAllText(Path.Combine(gitDirectoryPath, "index"), "index-v1");
        File.WriteAllText(Path.Combine(branchRefPath, "main"), "0000000000000000000000000000000000000001\n");

        const string expectedSegment = "cached-segment";
        GitStatusSharedCache.Set(repositoryPath, gitDirectoryPath, expectedSegment);

        // Act
        var found = GitStatusSharedCache.TryGet(repositoryPath, gitDirectoryPath, out var segment);

        // Assert
        found.Should().BeTrue();
        segment.Should().Be(expectedSegment);
    }

    [Fact]
    public void TryGet_WhenHeadReferenceChanges_ShouldReturnFalse()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride =
            ConfigReader.OverrideForTesting(new Config { Cache = new Config.CacheConfig { GitStatusTtl = TimeSpan.FromMilliseconds(5000) } });

        using var cacheDirectoryOverride = GitStatusSharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);

        var repositoryPath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(repositoryPath, ".git");
        var branchRefPath = Path.Combine(gitDirectoryPath, "refs", "heads");

        Directory.CreateDirectory(branchRefPath);
        var branchPath = Path.Combine(branchRefPath, "main");
        File.WriteAllText(Path.Combine(gitDirectoryPath, "HEAD"), "ref: refs/heads/main\n");
        File.WriteAllText(Path.Combine(gitDirectoryPath, "index"), "index-v1");
        File.WriteAllText(branchPath, "0000000000000000000000000000000000000001\n");

        GitStatusSharedCache.Set(repositoryPath, gitDirectoryPath, "cached-segment");

        File.WriteAllText(branchPath, "0000000000000000000000000000000000000002\n");

        // Act
        var found = GitStatusSharedCache.TryGet(repositoryPath, gitDirectoryPath, out _);

        // Assert
        found.Should().BeFalse();
    }

    [Fact]
    public void TryGet_WhenUpstreamTrackingReferenceChanges_ShouldReturnFalse()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride =
            ConfigReader.OverrideForTesting(new Config { Cache = new Config.CacheConfig { GitStatusTtl = TimeSpan.FromMilliseconds(5000) } });

        using var cacheDirectoryOverride = GitStatusSharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);

        var repositoryPath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(repositoryPath, ".git");
        var localBranchDirectoryPath = Path.Combine(gitDirectoryPath, "refs", "heads");
        var remoteBranchDirectoryPath = Path.Combine(gitDirectoryPath, "refs", "remotes", "origin");

        Directory.CreateDirectory(localBranchDirectoryPath);
        Directory.CreateDirectory(remoteBranchDirectoryPath);

        File.WriteAllText(Path.Combine(gitDirectoryPath, "HEAD"), "ref: refs/heads/main\n");
        File.WriteAllText(Path.Combine(gitDirectoryPath, "index"), "index-v1");
        File.WriteAllText(Path.Combine(localBranchDirectoryPath, "main"), "0000000000000000000000000000000000000001\n");
        File.WriteAllText(Path.Combine(remoteBranchDirectoryPath, "main"), "0000000000000000000000000000000000000001\n");
        File.WriteAllText(Path.Combine(gitDirectoryPath, "config"),
            "[branch \"main\"]\n" +
            "\tremote = origin\n" +
            "\tmerge = refs/heads/main\n");

        GitStatusSharedCache.Set(repositoryPath, gitDirectoryPath, "cached-segment");

        File.WriteAllText(Path.Combine(remoteBranchDirectoryPath, "main"), "0000000000000000000000000000000000000002\n");

        // Act
        var found = GitStatusSharedCache.TryGet(repositoryPath, gitDirectoryPath, out _);

        // Assert
        found.Should().BeFalse();
    }

    [Fact]
    public void TryGet_WhenLinkedWorktreeHeadReferenceChangesInCommonDirectory_ShouldReturnFalse()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride =
            ConfigReader.OverrideForTesting(new Config { Cache = new Config.CacheConfig { GitStatusTtl = TimeSpan.FromMilliseconds(5000) } });

        using var cacheDirectoryOverride = GitStatusSharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);

        var repositoryPath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var commonGitDirectoryPath = Path.Combine(cacheDirectory.DirectoryPath, "common", ".git");
        var gitDirectoryPath = Path.Combine(commonGitDirectoryPath, "worktrees", "repo-worktree");
        var sharedBranchDirectoryPath = Path.Combine(commonGitDirectoryPath, "refs", "heads");
        var sharedBranchPath = Path.Combine(sharedBranchDirectoryPath, "main");

        Directory.CreateDirectory(gitDirectoryPath);
        Directory.CreateDirectory(sharedBranchDirectoryPath);

        File.WriteAllText(Path.Combine(gitDirectoryPath, "commondir"), "../..\n");
        File.WriteAllText(Path.Combine(gitDirectoryPath, "HEAD"), "ref: refs/heads/main\n");
        File.WriteAllText(Path.Combine(gitDirectoryPath, "index"), "index-v1");
        File.WriteAllText(sharedBranchPath, "0000000000000000000000000000000000000001\n");

        GitStatusSharedCache.Set(repositoryPath, gitDirectoryPath, "cached-segment");

        File.WriteAllText(sharedBranchPath, "0000000000000000000000000000000000000002\n");

        // Act
        var found = GitStatusSharedCache.TryGet(repositoryPath, gitDirectoryPath, out _);

        // Assert
        found.Should().BeFalse();
    }

    [Fact]
    public void TryGet_WhenEntryIsExpired_ShouldReturnFalse()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride =
            ConfigReader.OverrideForTesting(new Config { Cache = new Config.CacheConfig { GitStatusTtl = TimeSpan.FromMilliseconds(500) } });

        using var cacheDirectoryOverride = GitStatusSharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var timeOverride = GitStatusSharedCache.OverrideTimeProviderForTesting(fakeClock);

        var repositoryPath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(repositoryPath, ".git");
        var branchRefPath = Path.Combine(gitDirectoryPath, "refs", "heads");

        Directory.CreateDirectory(branchRefPath);
        File.WriteAllText(Path.Combine(gitDirectoryPath, "HEAD"), "ref: refs/heads/main\n");
        File.WriteAllText(Path.Combine(gitDirectoryPath, "index"), "index-v1");
        File.WriteAllText(Path.Combine(branchRefPath, "main"), "0000000000000000000000000000000000000001\n");

        GitStatusSharedCache.Set(repositoryPath, gitDirectoryPath, "cached-segment");

        fakeClock.Advance(TimeSpan.FromMilliseconds(600));

        // Act
        var found = GitStatusSharedCache.TryGet(repositoryPath, gitDirectoryPath, out _);

        // Assert
        found.Should().BeFalse();
    }

    [Fact]
    public void TryGet_WhenCacheIsDisabled_ShouldReturnFalse()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride = ConfigReader.OverrideForTesting(new Config { Cache = new Config.CacheConfig { GitStatusTtl = TimeSpan.Zero } });
        using var cacheDirectoryOverride = GitStatusSharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);

        var repositoryPath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(repositoryPath, ".git");
        var branchRefPath = Path.Combine(gitDirectoryPath, "refs", "heads");

        Directory.CreateDirectory(branchRefPath);
        File.WriteAllText(Path.Combine(gitDirectoryPath, "HEAD"), "ref: refs/heads/main\n");
        File.WriteAllText(Path.Combine(gitDirectoryPath, "index"), "index-v1");
        File.WriteAllText(Path.Combine(branchRefPath, "main"), "0000000000000000000000000000000000000001\n");

        // Act – Set should be a no-op when TTL is 0
        GitStatusSharedCache.Set(repositoryPath, gitDirectoryPath, "cached-segment");
        var found = GitStatusSharedCache.TryGet(repositoryPath, gitDirectoryPath, out _);

        // Assert
        found.Should().BeFalse();
    }

    [Fact]
    public void TryGet_WhenInvalidationTokenChanges_ShouldReturnFalse()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride =
            ConfigReader.OverrideForTesting(new Config { Cache = new Config.CacheConfig { GitStatusTtl = TimeSpan.FromMilliseconds(5000) } });

        using var cacheDirectoryOverride = GitStatusSharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);

        var repositoryPath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(repositoryPath, ".git");
        var branchRefPath = Path.Combine(gitDirectoryPath, "refs", "heads");

        Directory.CreateDirectory(branchRefPath);
        File.WriteAllText(Path.Combine(gitDirectoryPath, "HEAD"), "ref: refs/heads/main\n");
        File.WriteAllText(Path.Combine(gitDirectoryPath, "index"), "index-v1");
        File.WriteAllText(Path.Combine(branchRefPath, "main"), "0000000000000000000000000000000000000001\n");

        GitStatusSharedCache.Set(repositoryPath, gitDirectoryPath, "cached-segment");

        // Token bump should invalidate any previously cached status entry.
        GitStatusSharedCache.Invalidate();

        // Act
        var found = GitStatusSharedCache.TryGet(repositoryPath, gitDirectoryPath, out _);

        // Assert
        found.Should().BeFalse();
    }

    [Fact]
    public void Invalidate_WhenCalled_ShouldWriteNewUniqueTokenToTokenFile()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride =
            ConfigReader.OverrideForTesting(new Config { Cache = new Config.CacheConfig { GitStatusTtl = TimeSpan.FromMilliseconds(5000) } });

        using var cacheDirectoryOverride = GitStatusSharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);

        var tokenFilePath = Path.Combine(cacheDirectory.DirectoryPath, "status-invalidation.token");

        // Act – first call creates the token file
        GitStatusSharedCache.Invalidate();
        var firstToken = File.ReadAllText(tokenFilePath);

        // Act – second call overwrites with a new, different token
        GitStatusSharedCache.Invalidate();
        var secondToken = File.ReadAllText(tokenFilePath);

        // Assert
        firstToken.Should().NotBeNullOrWhiteSpace();
        secondToken.Should().NotBeNullOrWhiteSpace();
        secondToken.Should().NotBe(firstToken, "each Invalidate() call must produce a distinct token");
    }

    [Fact]
    public void Set_WhenStaleCacheFilesExist_ShouldDeleteOnlyExpiredCacheFiles()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride =
            ConfigReader.OverrideForTesting(new Config { Cache = new Config.CacheConfig { GitStatusTtl = TimeSpan.FromMilliseconds(5000) } });

        using var cacheDirectoryOverride = GitStatusSharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow.AddYears(1));
        using var timeOverride = GitStatusSharedCache.OverrideTimeProviderForTesting(fakeClock);
        GitStatusSharedCache.ResetCleanupScheduleForTesting();

        var staleCachePath = Path.Combine(cacheDirectory.DirectoryPath, "stale.cache");
        var freshCachePath = Path.Combine(cacheDirectory.DirectoryPath, "fresh.cache");
        File.WriteAllText(staleCachePath, "stale");
        File.WriteAllText(freshCachePath, "fresh");

        File.SetLastWriteTimeUtc(staleCachePath, fakeClock.GetUtcNow().UtcDateTime.AddDays(-8));
        File.SetLastWriteTimeUtc(freshCachePath, fakeClock.GetUtcNow().UtcDateTime.AddDays(-1));

        var repositoryPath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(repositoryPath, ".git");
        CreateMinimalGitState(gitDirectoryPath);

        // Act
        GitStatusSharedCache.Set(repositoryPath, gitDirectoryPath, "cached-segment");

        // Assert
        File.Exists(staleCachePath).Should().BeFalse();
        File.Exists(freshCachePath).Should().BeTrue();
    }

    [Fact]
    public void Set_WhenCleanupRanRecently_ShouldSkipCleanupUntilIntervalExpires()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride =
            ConfigReader.OverrideForTesting(new Config { Cache = new Config.CacheConfig { GitStatusTtl = TimeSpan.FromMilliseconds(5000) } });

        using var cacheDirectoryOverride = GitStatusSharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow.AddYears(1));
        using var timeOverride = GitStatusSharedCache.OverrideTimeProviderForTesting(fakeClock);
        GitStatusSharedCache.ResetCleanupScheduleForTesting();

        var repositoryPath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(repositoryPath, ".git");
        CreateMinimalGitState(gitDirectoryPath);

        // First write triggers cleanup and sets next cleanup time.
        GitStatusSharedCache.Set(repositoryPath, gitDirectoryPath, "cached-segment");

        var staleCachePath = Path.Combine(cacheDirectory.DirectoryPath, "stale.cache");
        File.WriteAllText(staleCachePath, "stale");
        File.SetLastWriteTimeUtc(staleCachePath, fakeClock.GetUtcNow().UtcDateTime.AddDays(-8));

        // Act
        GitStatusSharedCache.Set(repositoryPath, gitDirectoryPath, "cached-segment-2");

        // Assert
        File.Exists(staleCachePath).Should().BeTrue();
    }

    private static void CreateMinimalGitState(string gitDirectoryPath)
    {
        var branchRefPath = Path.Combine(gitDirectoryPath, "refs", "heads");
        Directory.CreateDirectory(branchRefPath);
        File.WriteAllText(Path.Combine(gitDirectoryPath, "HEAD"), "ref: refs/heads/main\n");
        File.WriteAllText(Path.Combine(gitDirectoryPath, "index"), "index-v1");
        File.WriteAllText(Path.Combine(branchRefPath, "main"), "0000000000000000000000000000000000000001\n");
    }
}
