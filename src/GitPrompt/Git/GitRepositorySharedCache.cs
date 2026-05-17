using GitPrompt.Configuration;
using GitPrompt.Diagnostics;
using GitPrompt.Platform;

namespace GitPrompt.Git;

internal static class GitRepositorySharedCache
{
    private const string CacheDirectoryName = "repository-cache";
    private static readonly TimeSpan StaleCacheEntryThreshold = TimeSpan.FromDays(7);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
    private static TimeProvider _timeProvider = TimeProvider.System;
    private static string? _cacheDirectoryOverride;
    private static long _nextCleanupUtcTicks;

    internal static bool TryGet(string startDirectoryPath, out GitRepositoryLocator.RepositoryContext repositoryContext)
    {
        repositoryContext = default;

        try
        {
            var normalizedStartDirectoryPath = NormalizePathOrEmpty(startDirectoryPath);
            if (string.IsNullOrEmpty(normalizedStartDirectoryPath))
            {
                return false;
            }

            var cacheFilePath = GetCacheFilePath(normalizedStartDirectoryPath);
            if (!File.Exists(cacheFilePath))
            {
                PromptDiagnostics.RecordRepoCacheL2Miss(RepoCacheMissReason.NoEntry);

                return false;
            }

            var cacheContent = File.ReadAllText(cacheFilePath);
            if (!TryParseRecord(cacheContent, out var cacheRecord) ||
                !string.Equals(cacheRecord.StartDirectoryPath, normalizedStartDirectoryPath, Utilities.FileSystemPathComparison))
            {
                PromptDiagnostics.RecordRepoCacheL2Miss(RepoCacheMissReason.ParseError);

                return false;
            }

            var cacheTtl = GetCacheTtl();
            var cacheAge = GetUtcNow() - new DateTimeOffset(cacheRecord.CachedAtUtcTicks, TimeSpan.Zero);
            if (cacheTtl <= TimeSpan.Zero || cacheAge > cacheTtl)
            {
                PromptDiagnostics.RecordRepoCacheL2Miss(cacheTtl <= TimeSpan.Zero ? RepoCacheMissReason.Disabled : RepoCacheMissReason.TtlExpired);

                return false;
            }

            repositoryContext = new GitRepositoryLocator.RepositoryContext(cacheRecord.WorkingTreePath, cacheRecord.GitDirectoryPath);

            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static void Set(IEnumerable<string> startDirectoryPaths, GitRepositoryLocator.RepositoryContext repositoryContext)
    {
        try
        {
            var cacheDirectoryPath = GetCacheDirectoryPath();
            Directory.CreateDirectory(cacheDirectoryPath);

            var cachedAtUtcTicks = GetUtcNow().Ticks;
            foreach (var startDirectoryPath in startDirectoryPaths)
            {
                var normalizedStartDirectoryPath = NormalizePathOrEmpty(startDirectoryPath);
                if (string.IsNullOrEmpty(normalizedStartDirectoryPath))
                {
                    continue;
                }

                var cacheRecord = new RepositorySharedCacheRecord(
                    normalizedStartDirectoryPath,
                    NormalizePathOrEmpty(repositoryContext.WorkingTreePath),
                    NormalizePathOrEmpty(repositoryContext.GitDirectoryPath),
                    cachedAtUtcTicks);

                var cacheFilePath = GetCacheFilePath(normalizedStartDirectoryPath);
                SharedCacheUtilities.WriteAtomically(cacheFilePath, SerializeRecord(cacheRecord));
            }

            TryCleanupStaleEntries(cacheDirectoryPath);
        }
        catch
        {
            // Keep cache as a best-effort optimization.
        }
    }

    private static void TryCleanupStaleEntries(string cacheDirectoryPath)
    {
        var utcNow = GetUtcNow();
        var nextCleanupUtcTicks = Interlocked.Read(ref _nextCleanupUtcTicks);
        if (utcNow.UtcTicks < nextCleanupUtcTicks)
        {
            return;
        }

        Interlocked.Exchange(ref _nextCleanupUtcTicks, utcNow.Add(CleanupInterval).UtcTicks);
        SharedCacheUtilities.CleanupStaleEntries(cacheDirectoryPath, utcNow.UtcDateTime - StaleCacheEntryThreshold);
    }

    internal static void ResetCleanupScheduleForTesting()
    {
        Interlocked.Exchange(ref _nextCleanupUtcTicks, 0);
    }

    internal static IDisposable OverrideTimeProviderForTesting(TimeProvider timeProvider)
    {
        var previousTimeProvider = _timeProvider;
        _timeProvider = timeProvider;

        return new Utilities.RestoreAction(() => _timeProvider = previousTimeProvider);
    }

    internal static IDisposable OverrideCacheDirectoryForTesting(string cacheDirectoryPath)
    {
        _cacheDirectoryOverride = cacheDirectoryPath;

        return new Utilities.RestoreAction(() => _cacheDirectoryOverride = null);
    }

    private static TimeSpan GetCacheTtl()
    {
        var ttl = ConfigReader.Config.Cache!.RepositoryTtl;

        return ttl > TimeSpan.Zero ? ttl : TimeSpan.Zero;
    }

    private static DateTimeOffset GetUtcNow()
    {
        return _timeProvider.GetUtcNow();
    }

    private static string GetCacheDirectoryPath()
    {
        return _cacheDirectoryOverride ?? Path.Combine(XdgPaths.GetCacheDirectory(), CacheDirectoryName);
    }

    private static string GetCacheFilePath(string normalizedStartDirectoryPath)
    {
        return Path.Combine(GetCacheDirectoryPath(), SharedCacheUtilities.HashPath(normalizedStartDirectoryPath) + ".cache");
    }

    private static string NormalizePathOrEmpty(string path)
    {
        return SharedCacheUtilities.NormalizePathOrEmpty(path);
    }

    private static string[] SerializeRecord(RepositorySharedCacheRecord cacheRecord)
    {
        return
        [
            cacheRecord.CachedAtUtcTicks.ToString(),
            Encode(cacheRecord.StartDirectoryPath),
            Encode(cacheRecord.WorkingTreePath),
            Encode(cacheRecord.GitDirectoryPath)
        ];
    }

    private static bool TryParseRecord(string fileContent, out RepositorySharedCacheRecord cacheRecord)
    {
        cacheRecord = default;
        var lines = fileContent.Split('\n');
        var i = 0;

        if (NextLine() is not { } ticksText || !long.TryParse(ticksText, out var cachedAtUtcTicks)) { return false; }

        var startDirEncoded = NextLine();
        var workingTreeEncoded = NextLine();
        var gitDirEncoded = NextLine();
        if (startDirEncoded is null || workingTreeEncoded is null || gitDirEncoded is null)
        {
            return false;
        }

        var startDirectoryPath = Decode(startDirEncoded);
        var workingTreePath = Decode(workingTreeEncoded);
        var gitDirectoryPath = Decode(gitDirEncoded);
        if (string.IsNullOrEmpty(startDirectoryPath) || string.IsNullOrEmpty(workingTreePath) || string.IsNullOrEmpty(gitDirectoryPath))
        {
            return false;
        }

        cacheRecord = new RepositorySharedCacheRecord(
            startDirectoryPath,
            workingTreePath,
            gitDirectoryPath,
            cachedAtUtcTicks);

        return true;

        string? NextLine() => i < lines.Length ? lines[i++].TrimEnd('\r') : null;
    }

    private static string Encode(string value)
    {
        return SharedCacheUtilities.Encode(value);
    }

    private static string Decode(string encoded)
    {
        return SharedCacheUtilities.Decode(encoded);
    }

    private readonly record struct RepositorySharedCacheRecord(
        string StartDirectoryPath,
        string WorkingTreePath,
        string GitDirectoryPath,
        long CachedAtUtcTicks);
}

