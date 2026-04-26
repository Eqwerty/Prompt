using FluentAssertions;
using GitPrompt.Configuration;
using GitPrompt.Git;

namespace GitPrompt.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class GitStatusTimeoutIntegrationTests
{
    [Fact]
    public async Task Build_WhenCommandTimeoutExpiresBeforeGitCompletes_ShouldReturnTimeoutSegment()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "file.txt"), "hello\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add file.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"initial\"");

        // Use a 1ms timeout (below git startup time) and disable the cache so git is always invoked.
        using var configOverride = ConfigReader.OverrideForTesting(new Config
        {
            CommandTimeoutMs = 1.0,
            Cache = new Config.CacheConfig { GitStatusTtlSeconds = 0 }
        });

        // Act
        var segment = GitStatusSegmentBuilder.Build(repositoryPath);

        // Assert
        segment.Should().Contain("[timeout]");
    }

    [Fact]
    public async Task Build_WhenCommandTimeoutIsDisabled_ShouldReturnNormalGitSegment()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "file.txt"), "hello\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add file.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"initial\"");

        // Disable timeout and cache to get a fresh git result.
        using var configOverride = ConfigReader.OverrideForTesting(new Config
        {
            CommandTimeoutMs = 0,
            Cache = new Config.CacheConfig { GitStatusTtlSeconds = 0 }
        });

        // Act
        var segment = GitStatusSegmentBuilder.Build(repositoryPath);

        // Assert
        segment.Should().Contain("main");
        segment.Should().NotContain("[timeout]");
    }
}
