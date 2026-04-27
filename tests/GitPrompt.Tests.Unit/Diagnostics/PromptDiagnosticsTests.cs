using FluentAssertions;
using GitPrompt.Configuration;
using GitPrompt.Diagnostics;
using GitPrompt.Prompting;

namespace GitPrompt.Tests.Unit.Diagnostics;

[Collection(DiagnosticsIsolationCollection.Name)]
public sealed class PromptDiagnosticsTests
{
    [Fact]
    public void GetReport_WhenStatusCacheHit_ShouldShowHitWithAge()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        PromptDiagnostics.RecordRepoCacheL2Hit();
        PromptDiagnostics.RecordStatusCacheHit(age: TimeSpan.FromSeconds(2), ttl: TimeSpan.FromSeconds(5));
        var result = new PromptResult("user host ~/repo", string.Empty, "(main)", "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(3), TimeSpan.FromMilliseconds(4));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("Status      hit");
        report.Should().Contain("2s old");
        report.Should().Contain("TTL 5s");
        report.Should().Contain("Git segment served from cache.");
    }

    [Fact]
    public void GetReport_WhenStatusCacheMissFingerprintChanged_ShouldShowGitStateChangedAndTip()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        PromptDiagnostics.RecordRepoCacheL2Hit();
        PromptDiagnostics.RecordStatusCacheMiss(StatusCacheMissReason.FingerprintChanged);
        PromptDiagnostics.RecordGitSubprocessElapsed(TimeSpan.FromMilliseconds(50));
        var result = new PromptResult("user host ~/repo", string.Empty, "(main) ~2", "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(51), TimeSpan.FromMilliseconds(52));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("miss · git state changed");
        report.Should().Contain("ran git");
        report.Should().Contain("Cache miss caused by a real git change");
    }

    [Fact]
    public void GetReport_WhenStatusCacheMissTtlExpired_ShouldShowTtlExpiredAndTip()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        PromptDiagnostics.RecordRepoCacheL2Hit();
        PromptDiagnostics.RecordStatusCacheMiss(
            StatusCacheMissReason.TtlExpired,
            age: TimeSpan.FromSeconds(6),
            ttl: TimeSpan.FromSeconds(5));
        var result = new PromptResult("user host ~/repo", string.Empty, "(main)", "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(51));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("TTL expired");
        report.Should().Contain("6s old");
        report.Should().Contain("TTL 5s");
        report.Should().Contain("Tip");
        report.Should().Contain("cache.gitStatusTtl");
    }

    [Fact]
    public void GetReport_WhenStatusCacheMissNoEntry_ShouldShowFirstRunTip()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        PromptDiagnostics.RecordRepoCacheWalk(dirsWalked: 2, repoFound: true);
        PromptDiagnostics.RecordStatusCacheMiss(StatusCacheMissReason.NoEntry);
        var result = new PromptResult("user host ~/repo", string.Empty, "(main)", "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(55), TimeSpan.FromMilliseconds(56));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("miss · no entry");
        report.Should().Contain("First render or cache was evicted");
    }

    [Fact]
    public void GetReport_WhenNotInRepo_ShouldShowSkippedStatusCacheAndNoRepoMessage()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        PromptDiagnostics.RecordRepoCacheL2Miss(RepoCacheMissReason.NoEntry);
        PromptDiagnostics.RecordRepoCacheWalk(dirsWalked: 4, repoFound: false);
        var result = new PromptResult("user host ~/documents", string.Empty, string.Empty, "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/documents", result);

        // Assert
        report.Should().Contain("Status      skipped");
        report.Should().Contain("walked 4 dirs, no repo found");
        report.Should().Contain("Not in a git repository.");
    }

    [Fact]
    public void GetReport_WhenRepoCacheL1Hit_ShouldShowInProcessHit()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        PromptDiagnostics.RecordRepoCacheL1Hit();
        PromptDiagnostics.RecordStatusCacheHit(age: TimeSpan.FromSeconds(1), ttl: TimeSpan.FromSeconds(5));
        var result = new PromptResult("user host ~/repo", string.Empty, "(main)", "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2), TimeSpan.FromMilliseconds(3));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("Repository  hit (in-process)");
    }

    [Fact]
    public void GetReport_WhenRepoCacheWalkFoundRepo_ShouldShowDirCount()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        PromptDiagnostics.RecordRepoCacheL2Miss(RepoCacheMissReason.NoEntry);
        PromptDiagnostics.RecordRepoCacheWalk(dirsWalked: 3, repoFound: true);
        PromptDiagnostics.RecordStatusCacheMiss(StatusCacheMissReason.NoEntry);
        var result = new PromptResult("user host ~/repo", string.Empty, "(main)", "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(55), TimeSpan.FromMilliseconds(56));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("no entry → walked 3 dirs");
        report.Should().NotContain("no repo found");
    }

    [Fact]
    public void RecordStatusCacheHit_WhenNotEnabled_ShouldNotBeReflectedInReport()
    {
        // Arrange — diagnostics not enabled
        PromptDiagnostics.Reset();

        // Act & Assert — calling record methods when disabled must not throw and must have no effect
        var act = () =>
        {
            PromptDiagnostics.RecordStatusCacheHit(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
            PromptDiagnostics.RecordRepoCacheL1Hit();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public void GetReport_WhenStatusCacheMissDisabled_ShouldShowDisabledMessage()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        PromptDiagnostics.RecordRepoCacheL2Hit();
        PromptDiagnostics.RecordStatusCacheMiss(StatusCacheMissReason.Disabled);
        var result = new PromptResult("user host ~/repo", string.Empty, string.Empty, "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("miss · cache disabled");
        report.Should().Contain("Status cache is disabled");
    }

    [Fact]
    public void GetReport_WhenConfigLoaded_ShouldShowPathAndEffectiveTtls()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        var config = new Config();
        var loadResult = new ConfigLoadResult("/home/user/.config/gitprompt/config.jsonc", ConfigLoadStatus.Loaded, config);
        PromptDiagnostics.RecordConfigLoaded(loadResult);
        var result = new PromptResult(string.Empty, string.Empty, string.Empty, "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("Config     /home/user/.config/gitprompt/config.jsonc");
        report.Should().Contain("Status      loaded");
        report.Should().Contain("gitStatus 5s · repo 60s");
    }

    [Fact]
    public void GetReport_WhenConfigMissing_ShouldShowMissingStatusAndDefaultTtls()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        var loadResult = new ConfigLoadResult("/home/user/.config/gitprompt/config.jsonc", ConfigLoadStatus.Missing, new Config());
        PromptDiagnostics.RecordConfigLoaded(loadResult);
        var result = new PromptResult(string.Empty, string.Empty, string.Empty, "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("Status      missing (using defaults)");
        report.Should().Contain("gitStatus 5s");
    }

    [Fact]
    public void GetReport_WhenConfigParseFailed_ShouldShowParseErrorStatus()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        var loadResult = new ConfigLoadResult("/home/user/.config/gitprompt/config.jsonc", ConfigLoadStatus.ParseFailed, new Config());
        PromptDiagnostics.RecordConfigLoaded(loadResult);
        var result = new PromptResult(string.Empty, string.Empty, string.Empty, "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("Status      invalid JSON (using defaults)");
    }

    [Fact]
    public void GetReport_WhenConfigReadFailed_ShouldShowReadErrorStatus()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        var loadResult = new ConfigLoadResult("/home/user/.config/gitprompt/config.jsonc", ConfigLoadStatus.ReadFailed, new Config());
        PromptDiagnostics.RecordConfigLoaded(loadResult);
        var result = new PromptResult(string.Empty, string.Empty, string.Empty, "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("Status      read error (using defaults)");
    }

    [Fact]
    public void GetReport_WhenConfigLoadedWithTtlZero_ShouldShowCacheDisabledAnnotation()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        var config = new Config { Cache = new Config.CacheConfig { GitStatusTtlSeconds = 0 } };
        var loadResult = new ConfigLoadResult("/home/user/.config/gitprompt/config.jsonc", ConfigLoadStatus.Loaded, config);
        PromptDiagnostics.RecordConfigLoaded(loadResult);
        var result = new PromptResult(string.Empty, string.Empty, string.Empty, "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("gitStatus 0s (disabled)");
    }

    [Fact]
    public void GetReport_WhenConfigNotRecorded_ShouldNotShowConfigSection()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        var result = new PromptResult(string.Empty, string.Empty, string.Empty, "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().NotContain("Config     ");
    }

    [Fact]
    public void RecordConfigLoaded_WhenNotEnabled_ShouldNotBeReflectedInReport()
    {
        // Arrange — diagnostics not enabled
        PromptDiagnostics.Reset();
        var loadResult = new ConfigLoadResult("/some/path", ConfigLoadStatus.Loaded, new Config());

        // Act & Assert
        var act = () => PromptDiagnostics.RecordConfigLoaded(loadResult);
        act.Should().NotThrow();
    }

    [Fact]
    public void GetReport_WhenConfigPathHasBackslashes_ShouldNormalizeToForwardSlashes()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        var loadResult = new ConfigLoadResult(@"C:\Users\user\AppData\Roaming\gitprompt\config.jsonc", ConfigLoadStatus.Loaded, new Config());
        PromptDiagnostics.RecordConfigLoaded(loadResult);
        var result = new PromptResult(string.Empty, string.Empty, string.Empty, "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("C:/Users/user/AppData/Roaming/gitprompt/config.jsonc");
    }
}
