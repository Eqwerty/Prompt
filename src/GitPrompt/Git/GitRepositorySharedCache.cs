using System.Security.Cryptography;
using System.Text;
using GitPrompt.Configuration;

namespace GitPrompt.Git;

internal static class GitRepositorySharedCache
{
    private const string CacheDirectoryName = "repository-cache-v1";
    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromSeconds(60);
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
                return false;
            }

            var cacheLines = File.ReadAllLines(cacheFilePath);
            if (!TryParseRecord(cacheLines, out var cacheRecord))
            {
                return false;
            }

            if (!string.Equals(cacheRecord.StartDirectoryPath, normalizedStartDirectoryPath, Utilities.FileSystemPathComparison))
            {
                return false;
            }

            if (IsExpired(cacheRecord.CachedAtUtcTicks))
            {
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
                WriteAtomically(cacheFilePath, SerializeRecord(cacheRecord));
            }

            TryCleanupStaleEntries(cacheDirectoryPath);
        }
        catch
        {
            // Keep cache as a best-effort optimization.
        }
    }

    private static void WriteAtomically(string targetFilePath, string[] lines)
    {
        var tempFilePath = targetFilePath + "." + Path.GetRandomFileName() + ".tmp";

        try
        {
            File.WriteAllLines(tempFilePath, lines);
            File.Move(tempFilePath, targetFilePath, overwrite: true);
            tempFilePath = null;
        }
        finally
        {
            if (tempFilePath is not null)
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception)
                {
                    /* best-effort temp file cleanup */
                }
            }
        }
    }

    private static bool IsExpired(long cachedAtUtcTicks)
    {
        var cacheTtl = GetCacheTtl();
        if (cacheTtl <= TimeSpan.Zero)
        {
            return true;
        }

        var cachedAtUtc = new DateTime(cachedAtUtcTicks, DateTimeKind.Utc);

        return GetUtcNow() - cachedAtUtc > cacheTtl;
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
        CleanupStaleEntries(cacheDirectoryPath, utcNow.UtcDateTime - StaleCacheEntryThreshold);
    }

    private static void CleanupStaleEntries(string cacheDirectoryPath, DateTime staleBeforeUtc)
    {
        foreach (var cacheFilePath in Directory.EnumerateFiles(cacheDirectoryPath, "*.cache"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(cacheFilePath) < staleBeforeUtc)
                {
                    File.Delete(cacheFilePath);
                }
            }
            catch
            {
                // Keep cleanup as best-effort and never fail prompt rendering.
            }
        }
    }

    internal static void ResetCleanupScheduleForTesting()
    {
        Interlocked.Exchange(ref _nextCleanupUtcTicks, 0);
    }

    internal static IDisposable OverrideTimeProviderForTesting(TimeProvider timeProvider)
    {
        var previousTimeProvider = _timeProvider;
        _timeProvider = timeProvider;

        return new TimeProviderOverride(() => _timeProvider = previousTimeProvider);
    }

    internal static IDisposable OverrideCacheDirectoryForTesting(string cacheDirectoryPath)
    {
        _cacheDirectoryOverride = cacheDirectoryPath;

        return new TimeProviderOverride(() => _cacheDirectoryOverride = null);
    }

    private static TimeSpan GetCacheTtl()
    {
        var configured = ConfigReader.Config.Cache.RepositoryTtl;
        if (configured.HasValue)
        {
            return configured.Value > TimeSpan.Zero ? configured.Value : TimeSpan.Zero;
        }

        return DefaultCacheTtl;
    }

    private static DateTimeOffset GetUtcNow()
    {
        return _timeProvider.GetUtcNow();
    }

    private static string GetCacheDirectoryPath()
    {
        return _cacheDirectoryOverride ?? Path.Combine(AppContext.BaseDirectory, CacheDirectoryName);
    }

    private static string GetCacheFilePath(string normalizedStartDirectoryPath)
    {
        var pathHash = HashPath(normalizedStartDirectoryPath);

        return Path.Combine(GetCacheDirectoryPath(), pathHash + ".cache");
    }

    private static string HashPath(string path)
    {
        var pathBytes = Encoding.UTF8.GetBytes(path);
        var hashBytes = SHA256.HashData(pathBytes);

        var hashBuilder = new StringBuilder(hashBytes.Length * 2);
        foreach (var hashByte in hashBytes)
        {
            hashBuilder.Append(hashByte.ToString("x2"));
        }

        return hashBuilder.ToString();
    }

    private static string NormalizePathOrEmpty(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? string.Empty : Utilities.NormalizePath(path);
    }

    private static string[] SerializeRecord(RepositorySharedCacheRecord cacheRecord)
    {
        return
        [
            "v1",
            cacheRecord.CachedAtUtcTicks.ToString(),
            Encode(cacheRecord.StartDirectoryPath),
            Encode(cacheRecord.WorkingTreePath),
            Encode(cacheRecord.GitDirectoryPath)
        ];
    }

    private static bool TryParseRecord(string[] lines, out RepositorySharedCacheRecord cacheRecord)
    {
        cacheRecord = default;
        if (lines.Length < 5 || !string.Equals(lines[0], "v1", StringComparison.Ordinal))
        {
            return false;
        }

        if (!long.TryParse(lines[1], out var cachedAtUtcTicks))
        {
            return false;
        }

        var startDirectoryPath = Decode(lines[2]);
        var workingTreePath = Decode(lines[3]);
        var gitDirectoryPath = Decode(lines[4]);
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
    }

    private static string Encode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    private static string Decode(string encoded)
    {
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }
        catch
        {
            return string.Empty;
        }
    }

    private readonly record struct RepositorySharedCacheRecord(
        string StartDirectoryPath,
        string WorkingTreePath,
        string GitDirectoryPath,
        long CachedAtUtcTicks);

    private sealed class TimeProviderOverride(Action restore) : IDisposable
    {
        private readonly Action _restore = restore;

        public void Dispose()
        {
            _restore();
        }
    }
}
