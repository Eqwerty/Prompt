using FluentAssertions;
using GitPrompt.Configuration;
using GitPrompt.Git;

namespace GitPrompt.Tests.Unit.Git;

[Collection(CacheIsolationCollection.Name)]
public sealed class GitRepositorySharedCacheTests
{
    [Fact]
    public void TryGet_WhenEntryExistsWithinTtl_ShouldReturnCachedRepositoryContext()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride = ConfigReader.OverrideForTesting(new Config { Cache = new Config.CacheConfig { RepositoryTtlSeconds = 60.0 } });
        using var cacheDirectoryOverride = GitRepositorySharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var timeOverride = GitRepositorySharedCache.OverrideTimeProviderForTesting(fakeClock);

        var startDirectoryPath = Path.Combine(cacheDirectory.DirectoryPath, "work", "nested");
        var workingTreePath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(workingTreePath, ".git");

        Directory.CreateDirectory(startDirectoryPath);
        Directory.CreateDirectory(gitDirectoryPath);

        GitRepositorySharedCache.Set(
            [startDirectoryPath],
            new GitRepositoryLocator.RepositoryContext(workingTreePath, gitDirectoryPath));

        // Act
        var found = GitRepositorySharedCache.TryGet(startDirectoryPath, out var repositoryContext);

        // Assert
        found.Should().BeTrue();
        repositoryContext.WorkingTreePath.Should().Be(Path.GetFullPath(workingTreePath));
        repositoryContext.GitDirectoryPath.Should().Be(Path.GetFullPath(gitDirectoryPath));
    }

    [Fact]
    public void TryGet_WhenEntryIsExpired_ShouldReturnFalse()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride = ConfigReader.OverrideForTesting(new Config { Cache = new Config.CacheConfig { RepositoryTtlSeconds = 1.0 } });
        using var cacheDirectoryOverride = GitRepositorySharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var timeOverride = GitRepositorySharedCache.OverrideTimeProviderForTesting(fakeClock);

        var startDirectoryPath = Path.Combine(cacheDirectory.DirectoryPath, "work", "nested");
        var workingTreePath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(workingTreePath, ".git");

        Directory.CreateDirectory(startDirectoryPath);
        Directory.CreateDirectory(gitDirectoryPath);

        GitRepositorySharedCache.Set(
            [startDirectoryPath],
            new GitRepositoryLocator.RepositoryContext(workingTreePath, gitDirectoryPath));

        fakeClock.Advance(TimeSpan.FromMilliseconds(1200));

        // Act
        var found = GitRepositorySharedCache.TryGet(startDirectoryPath, out _);

        // Assert
        found.Should().BeFalse();
    }

    [Fact]
    public void TryGet_WhenCacheIsDisabled_ShouldReturnFalse()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride = ConfigReader.OverrideForTesting(new Config { Cache = new Config.CacheConfig { RepositoryTtlSeconds = 0 } });
        using var cacheDirectoryOverride = GitRepositorySharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);

        var startDirectoryPath = Path.Combine(cacheDirectory.DirectoryPath, "work");
        var workingTreePath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(workingTreePath, ".git");

        Directory.CreateDirectory(startDirectoryPath);
        Directory.CreateDirectory(gitDirectoryPath);

        // Set is a no-op when TTL is 0, but call it anyway to confirm nothing is stored.
        GitRepositorySharedCache.Set(
            [startDirectoryPath],
            new GitRepositoryLocator.RepositoryContext(workingTreePath, gitDirectoryPath));

        // Act
        var found = GitRepositorySharedCache.TryGet(startDirectoryPath, out _);

        // Assert
        found.Should().BeFalse();
    }

    [Fact]
    public void Set_WithMultiplePaths_ShouldWriteCacheFileForEachPathAndAllHit()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride = ConfigReader.OverrideForTesting(new Config { Cache = new Config.CacheConfig { RepositoryTtlSeconds = 60.0 } });
        using var cacheDirectoryOverride = GitRepositorySharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var timeOverride = GitRepositorySharedCache.OverrideTimeProviderForTesting(fakeClock);

        var pathA = Path.Combine(cacheDirectory.DirectoryPath, "src", "featureA");
        var pathB = Path.Combine(cacheDirectory.DirectoryPath, "src");
        var workingTreePath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(workingTreePath, ".git");

        Directory.CreateDirectory(pathA);
        Directory.CreateDirectory(pathB);
        Directory.CreateDirectory(gitDirectoryPath);

        var context = new GitRepositoryLocator.RepositoryContext(workingTreePath, gitDirectoryPath);

        // Act
        GitRepositorySharedCache.Set([pathA, pathB], context);

        // Assert – both paths should independently produce a cache hit
        var foundA = GitRepositorySharedCache.TryGet(pathA, out var contextA);
        var foundB = GitRepositorySharedCache.TryGet(pathB, out var contextB);

        foundA.Should().BeTrue();
        foundB.Should().BeTrue();
        contextA.WorkingTreePath.Should().Be(Path.GetFullPath(workingTreePath));
        contextB.WorkingTreePath.Should().Be(Path.GetFullPath(workingTreePath));

        // Two distinct cache files should exist (one per unique path hash).
        var cacheFiles = Directory.GetFiles(cacheDirectory.DirectoryPath, "*.cache");
        cacheFiles.Should().HaveCount(2);
    }

    [Fact]
    public void Set_WhenStaleCacheFilesExist_ShouldDeleteOnlyExpiredCacheFiles()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride = ConfigReader.OverrideForTesting(new Config { Cache = new Config.CacheConfig { RepositoryTtlSeconds = 60.0 } });
        using var cacheDirectoryOverride = GitRepositorySharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow.AddYears(1));
        using var timeOverride = GitRepositorySharedCache.OverrideTimeProviderForTesting(fakeClock);
        GitRepositorySharedCache.ResetCleanupScheduleForTesting();

        var staleCachePath = Path.Combine(cacheDirectory.DirectoryPath, "stale.cache");
        var freshCachePath = Path.Combine(cacheDirectory.DirectoryPath, "fresh.cache");
        File.WriteAllText(staleCachePath, "stale");
        File.WriteAllText(freshCachePath, "fresh");

        File.SetLastWriteTimeUtc(staleCachePath, fakeClock.GetUtcNow().UtcDateTime.AddDays(-8));
        File.SetLastWriteTimeUtc(freshCachePath, fakeClock.GetUtcNow().UtcDateTime.AddDays(-1));

        var startDirectoryPath = Path.Combine(cacheDirectory.DirectoryPath, "work");
        var workingTreePath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(workingTreePath, ".git");

        Directory.CreateDirectory(startDirectoryPath);
        Directory.CreateDirectory(gitDirectoryPath);

        // Act
        GitRepositorySharedCache.Set(
            [startDirectoryPath],
            new GitRepositoryLocator.RepositoryContext(workingTreePath, gitDirectoryPath));

        // Assert
        File.Exists(staleCachePath).Should().BeFalse();
        File.Exists(freshCachePath).Should().BeTrue();
    }

    [Fact]
    public void Set_WhenCleanupRanRecently_ShouldSkipCleanupUntilIntervalExpires()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride = ConfigReader.OverrideForTesting(new Config { Cache = new Config.CacheConfig { RepositoryTtlSeconds = 60.0 } });
        using var cacheDirectoryOverride = GitRepositorySharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow.AddYears(1));
        using var timeOverride = GitRepositorySharedCache.OverrideTimeProviderForTesting(fakeClock);
        GitRepositorySharedCache.ResetCleanupScheduleForTesting();

        var startDirectoryPath = Path.Combine(cacheDirectory.DirectoryPath, "work");
        var workingTreePath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(workingTreePath, ".git");

        Directory.CreateDirectory(startDirectoryPath);
        Directory.CreateDirectory(gitDirectoryPath);

        var context = new GitRepositoryLocator.RepositoryContext(workingTreePath, gitDirectoryPath);

        // First write triggers cleanup and sets next cleanup time.
        GitRepositorySharedCache.Set([startDirectoryPath], context);

        var staleCachePath = Path.Combine(cacheDirectory.DirectoryPath, "stale.cache");
        File.WriteAllText(staleCachePath, "stale");
        File.SetLastWriteTimeUtc(staleCachePath, fakeClock.GetUtcNow().UtcDateTime.AddDays(-8));

        // Act – second write within the cleanup interval should NOT trigger another cleanup.
        GitRepositorySharedCache.Set([startDirectoryPath], context);

        // Assert – stale file should still be there because cleanup was skipped.
        File.Exists(staleCachePath).Should().BeTrue();
    }
}
