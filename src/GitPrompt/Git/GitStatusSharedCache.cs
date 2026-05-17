using GitPrompt.Configuration;
using GitPrompt.Diagnostics;
using GitPrompt.Platform;

namespace GitPrompt.Git;

internal static class GitStatusSharedCache
{
    private const string CacheDirectoryName = "git-status-cache";
    private const string InvalidationTokenFileName = "status-invalidation.token";

    private static readonly TimeSpan StaleCacheEntryThreshold = TimeSpan.FromDays(7);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
    private static TimeProvider _timeProvider = TimeProvider.System;
    private static string? _cacheDirectoryOverride;
    private static long _nextCleanupUtcTicks;

    internal static bool TryGet(string repositoryRootPath, string gitDirectoryPath, out string segment)
    {
        segment = string.Empty;

        try
        {
            if (!IsCacheEnabled())
            {
                PromptDiagnostics.RecordStatusCacheMiss(StatusCacheMissReason.Disabled);

                return false;
            }

            var normalizedRepositoryRootPath = NormalizePathOrEmpty(repositoryRootPath);
            var normalizedGitDirectoryPath = NormalizePathOrEmpty(gitDirectoryPath);
            if (string.IsNullOrEmpty(normalizedRepositoryRootPath) || string.IsNullOrEmpty(normalizedGitDirectoryPath))
            {
                return false;
            }

            var cacheFilePath = GetCacheFilePath(normalizedRepositoryRootPath);
            if (!File.Exists(cacheFilePath))
            {
                PromptDiagnostics.RecordStatusCacheMiss(StatusCacheMissReason.NoEntry);

                return false;
            }

            var cacheContent = File.ReadAllText(cacheFilePath);
            if (!TryParseRecord(cacheContent, out var cacheRecord) ||
                !string.Equals(cacheRecord.RepositoryRootPath, normalizedRepositoryRootPath, Utilities.FileSystemPathComparison))
            {
                PromptDiagnostics.RecordStatusCacheMiss(StatusCacheMissReason.ParseError);

                return false;
            }

            var cacheTtl = GetCacheTtl();
            var cacheAge = GetUtcNow() - new DateTimeOffset(cacheRecord.CachedAtUtcTicks, TimeSpan.Zero);
            if (cacheAge > cacheTtl)
            {
                PromptDiagnostics.RecordStatusCacheMiss(StatusCacheMissReason.TtlExpired, cacheAge, cacheTtl);

                return false;
            }

            var currentInvalidationTokenValue = ReadInvalidationTokenValue();
            if (!string.Equals(cacheRecord.InvalidationTokenValue, currentInvalidationTokenValue, StringComparison.Ordinal))
            {
                PromptDiagnostics.RecordStatusCacheMiss(StatusCacheMissReason.InvalidationToken);

                return false;
            }

            var currentFingerprint = BuildFingerprint(normalizedGitDirectoryPath);
            if (!string.Equals(cacheRecord.Fingerprint, currentFingerprint, StringComparison.Ordinal))
            {
                PromptDiagnostics.RecordStatusCacheMiss(StatusCacheMissReason.FingerprintChanged);

                return false;
            }

            segment = cacheRecord.Segment;
            PromptDiagnostics.RecordStatusCacheHit(cacheAge, cacheTtl);

            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static void Set(string repositoryRootPath, string gitDirectoryPath, string segment)
    {
        try
        {
            if (!IsCacheEnabled() || string.IsNullOrEmpty(segment))
            {
                return;
            }

            var normalizedRepositoryRootPath = NormalizePathOrEmpty(repositoryRootPath);
            var normalizedGitDirectoryPath = NormalizePathOrEmpty(gitDirectoryPath);
            if (string.IsNullOrEmpty(normalizedRepositoryRootPath) || string.IsNullOrEmpty(normalizedGitDirectoryPath))
            {
                return;
            }

            var cacheDirectoryPath = GetCacheDirectoryPath();
            Directory.CreateDirectory(cacheDirectoryPath);

            var cacheRecord = new GitStatusSharedCacheRecord(
                normalizedRepositoryRootPath,
                BuildFingerprint(normalizedGitDirectoryPath),
                segment,
                GetUtcNow().Ticks,
                ReadInvalidationTokenValue());

            var cacheFilePath = GetCacheFilePath(normalizedRepositoryRootPath);
            SharedCacheUtilities.WriteAtomically(cacheFilePath, SerializeRecord(cacheRecord));
            TryCleanupStaleEntries(cacheDirectoryPath);
        }
        catch
        {
            // Keep cache as a best-effort optimization.
        }
    }

    private static bool IsCacheEnabled()
    {
        return GetCacheTtl() > TimeSpan.Zero;
    }

    private static TimeSpan GetCacheTtl()
    {
        var ttl = ConfigReader.Config.Cache!.GitStatusTtl;

        return ttl > TimeSpan.Zero ? ttl : TimeSpan.Zero;
    }

    internal static void Invalidate()
    {
        try
        {
            var cacheDirectoryPath = GetCacheDirectoryPath();
            Directory.CreateDirectory(cacheDirectoryPath);

            // A random token value avoids filesystem timestamp precision issues.
            File.WriteAllText(GetInvalidationTokenPath(), Guid.NewGuid().ToString("N"));
        }
        catch
        {
            // Keep cache invalidation as best-effort.
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

    internal static void ResetCleanupScheduleForTesting()
    {
        Interlocked.Exchange(ref _nextCleanupUtcTicks, 0);
    }

    private static DateTimeOffset GetUtcNow()
    {
        return _timeProvider.GetUtcNow();
    }

    private static string GetCacheDirectoryPath()
    {
        return _cacheDirectoryOverride ?? Path.Combine(XdgPaths.GetCacheDirectory(), CacheDirectoryName);
    }

    private static string GetCacheFilePath(string normalizedRepositoryRootPath)
    {
        return Path.Combine(GetCacheDirectoryPath(), SharedCacheUtilities.HashPath(normalizedRepositoryRootPath) + ".cache");
    }

    private static string GetInvalidationTokenPath()
    {
        return Path.Combine(GetCacheDirectoryPath(), InvalidationTokenFileName);
    }

    private static string ReadInvalidationTokenValue()
    {
        try
        {
            var tokenPath = GetInvalidationTokenPath();
            return File.Exists(tokenPath) ? File.ReadAllText(tokenPath) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string BuildFingerprint(string normalizedGitDirectoryPath)
    {
        var commonGitDirectoryPath = ResolveCommonGitDirectoryPath(normalizedGitDirectoryPath);
        var hasher = new SharedCacheUtilities.FingerprintHasher();

        hasher.AppendString("COMMON_GIT_DIR");
        hasher.AppendString(commonGitDirectoryPath);

        AppendStamp(ref hasher, normalizedGitDirectoryPath, "HEAD");
        AppendStamp(ref hasher, normalizedGitDirectoryPath, "index");
        AppendStamp(ref hasher, commonGitDirectoryPath, "packed-refs");
        AppendStamp(ref hasher, commonGitDirectoryPath, Path.Combine("refs", "stash"));
        AppendStamp(ref hasher, commonGitDirectoryPath, "FETCH_HEAD");
        AppendStamp(ref hasher, normalizedGitDirectoryPath, "MERGE_HEAD");
        AppendStamp(ref hasher, normalizedGitDirectoryPath, "REBASE_HEAD");
        AppendStamp(ref hasher, normalizedGitDirectoryPath, "CHERRY_PICK_HEAD");
        AppendStamp(ref hasher, normalizedGitDirectoryPath, "REVERT_HEAD");
        AppendStamp(ref hasher, normalizedGitDirectoryPath, "BISECT_LOG");

        var headRefPath = TryResolveHeadRefPath(normalizedGitDirectoryPath, commonGitDirectoryPath);
        if (!string.IsNullOrEmpty(headRefPath))
        {
            AppendAbsoluteFileStamp(ref hasher, headRefPath, "HEAD_REF");

            var upstreamRefPath = TryResolveUpstreamRefPath(commonGitDirectoryPath, headRefPath);
            if (!string.IsNullOrEmpty(upstreamRefPath))
            {
                AppendAbsoluteFileStamp(ref hasher, upstreamRefPath, "UPSTREAM_REF");
            }
        }

        return hasher.GetHexString();
    }

    private static string ResolveCommonGitDirectoryPath(string normalizedGitDirectoryPath)
    {
        try
        {
            var commonDirFilePath = Path.Combine(normalizedGitDirectoryPath, "commondir");
            if (!File.Exists(commonDirFilePath))
            {
                return normalizedGitDirectoryPath;
            }

            using var commonDirReader = new StreamReader(commonDirFilePath);
            var commonDirValue = commonDirReader.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(commonDirValue))
            {
                return normalizedGitDirectoryPath;
            }

            var resolvedCommonDirPath = Path.IsPathRooted(commonDirValue)
                ? commonDirValue
                : Path.GetFullPath(Path.Combine(normalizedGitDirectoryPath, commonDirValue));

            return Directory.Exists(resolvedCommonDirPath)
                ? Utilities.NormalizePath(resolvedCommonDirPath)
                : normalizedGitDirectoryPath;
        }
        catch
        {
            return normalizedGitDirectoryPath;
        }
    }

    private static string? TryResolveHeadRefPath(string normalizedGitDirectoryPath, string normalizedCommonGitDirectoryPath)
    {
        try
        {
            var headPath = Path.Combine(normalizedGitDirectoryPath, "HEAD");
            if (!File.Exists(headPath))
            {
                return null;
            }

            using var headReader = new StreamReader(headPath);
            var headLine = headReader.ReadLine()?.Trim();
            const string refPrefix = "ref:";
            if (string.IsNullOrEmpty(headLine) || !headLine.StartsWith(refPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var relativeRefPath = headLine[refPrefix.Length..].Trim();
            if (string.IsNullOrEmpty(relativeRefPath))
            {
                return null;
            }

            var normalizedRelativeRefPath = relativeRefPath.Replace('/', Path.DirectorySeparatorChar);

            var worktreeRefPath = Utilities.NormalizePath(Path.Combine(normalizedGitDirectoryPath, normalizedRelativeRefPath));
            if (File.Exists(worktreeRefPath))
            {
                return worktreeRefPath;
            }

            return Utilities.NormalizePath(Path.Combine(normalizedCommonGitDirectoryPath, normalizedRelativeRefPath));
        }
        catch
        {
            return null;
        }
    }

    private static string? TryResolveUpstreamRefPath(string normalizedCommonGitDirectoryPath, string normalizedHeadRefPath)
    {
        try
        {
            var branchName = TryExtractBranchNameFromHeadRefPath(normalizedCommonGitDirectoryPath, normalizedHeadRefPath);
            if (string.IsNullOrEmpty(branchName))
            {
                return null;
            }

            if (!TryReadBranchTrackingConfiguration(normalizedCommonGitDirectoryPath, branchName, out var remoteName, out var mergeReference))
            {
                return null;
            }

            if (string.Equals(remoteName, ".", StringComparison.Ordinal))
            {
                return Utilities.NormalizePath(Path.Combine(normalizedCommonGitDirectoryPath, mergeReference.Replace('/', Path.DirectorySeparatorChar)));
            }

            if (mergeReference.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase))
            {
                var branchRelativePath = mergeReference["refs/heads/".Length..];
                return Utilities.NormalizePath(Path.Combine(
                    normalizedCommonGitDirectoryPath,
                    "refs",
                    "remotes",
                    remoteName,
                    branchRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            }

            if (mergeReference.StartsWith("refs/remotes/", StringComparison.OrdinalIgnoreCase))
            {
                return Utilities.NormalizePath(Path.Combine(normalizedCommonGitDirectoryPath, mergeReference.Replace('/', Path.DirectorySeparatorChar)));
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? TryExtractBranchNameFromHeadRefPath(string normalizedCommonGitDirectoryPath, string normalizedHeadRefPath)
    {
        var refsHeadsPrefix = Utilities.NormalizePath(Path.Combine(normalizedCommonGitDirectoryPath, "refs", "heads") + Path.DirectorySeparatorChar);
        if (!normalizedHeadRefPath.StartsWith(refsHeadsPrefix, Utilities.FileSystemPathComparison))
        {
            return null;
        }

        return normalizedHeadRefPath[refsHeadsPrefix.Length..].Replace(Path.DirectorySeparatorChar, '/');
    }

    private static bool TryReadBranchTrackingConfiguration(
        string normalizedCommonGitDirectoryPath,
        string branchName,
        out string remoteName,
        out string mergeReference)
    {
        remoteName = string.Empty;
        mergeReference = string.Empty;

        var configPath = Path.Combine(normalizedCommonGitDirectoryPath, "config");
        if (!File.Exists(configPath))
        {
            return false;
        }

        string? activeBranchSection = null;
        using var configReader = new StreamReader(configPath);
        while (configReader.ReadLine() is { } rawLine)
        {
            var line = rawLine.Trim();
            if (line.Length is 0 || line.StartsWith('#') || line.StartsWith(';'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                activeBranchSection = TryParseBranchSection(line);
                continue;
            }

            if (!string.Equals(activeBranchSection, branchName, StringComparison.Ordinal))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex >= line.Length - 1)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            if (string.Equals(key, "remote", StringComparison.OrdinalIgnoreCase))
            {
                remoteName = value;
            }
            else if (string.Equals(key, "merge", StringComparison.OrdinalIgnoreCase))
            {
                mergeReference = value;
            }

            if (!string.IsNullOrEmpty(remoteName) && !string.IsNullOrEmpty(mergeReference))
            {
                return true;
            }
        }

        return false;
    }

    private static string? TryParseBranchSection(string sectionLine)
    {
        var section = sectionLine[1..^1].Trim();
        if (!section.StartsWith("branch", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var firstQuoteIndex = section.IndexOf('"');
        var lastQuoteIndex = section.LastIndexOf('"');
        if (firstQuoteIndex < 0 || lastQuoteIndex <= firstQuoteIndex)
        {
            return null;
        }

        return section[(firstQuoteIndex + 1)..lastQuoteIndex];
    }

    private static void AppendStamp(ref SharedCacheUtilities.FingerprintHasher hasher, string gitDirectoryPath, string relativePath)
    {
        var absolutePath = Path.Combine(gitDirectoryPath, relativePath);
        AppendAbsoluteFileStamp(ref hasher, absolutePath, relativePath);
    }

    private static void AppendAbsoluteFileStamp(ref SharedCacheUtilities.FingerprintHasher hasher, string filePath, string label)
    {
        hasher.AppendString(label);
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            hasher.AppendByte(0);
            return;
        }

        hasher.AppendByte(1);
        hasher.AppendInt64(fileInfo.Length);
        hasher.AppendInt64(fileInfo.LastWriteTimeUtc.Ticks);
    }

    private static string NormalizePathOrEmpty(string path)
    {
        return SharedCacheUtilities.NormalizePathOrEmpty(path);
    }

    private static string[] SerializeRecord(GitStatusSharedCacheRecord cacheRecord)
    {
        return
        [
            cacheRecord.CachedAtUtcTicks.ToString(),
            Encode(cacheRecord.RepositoryRootPath),
            Encode(cacheRecord.Fingerprint),
            Encode(cacheRecord.Segment),
            Encode(cacheRecord.InvalidationTokenValue)
        ];
    }

    private static bool TryParseRecord(string fileContent, out GitStatusSharedCacheRecord cacheRecord)
    {
        cacheRecord = default;
        var lines = fileContent.Split('\n');
        var i = 0;

        if (NextLine() is not { } ticksText || !long.TryParse(ticksText, out var cachedAtUtcTicks))
        {
            return false;
        }

        var repoEncoded = NextLine();
        var fingerprintEncoded = NextLine();
        var segmentEncoded = NextLine();
        var tokenEncoded = NextLine();
        if (repoEncoded is null || fingerprintEncoded is null || segmentEncoded is null || tokenEncoded is null)
        {
            return false;
        }

        var repositoryRootPath = Decode(repoEncoded);
        var fingerprint = Decode(fingerprintEncoded);
        var segment = Decode(segmentEncoded);
        var invalidationTokenValue = Decode(tokenEncoded);
        if (string.IsNullOrEmpty(repositoryRootPath) || string.IsNullOrEmpty(fingerprint))
        {
            return false;
        }

        cacheRecord = new GitStatusSharedCacheRecord(repositoryRootPath, fingerprint, segment, cachedAtUtcTicks, invalidationTokenValue);
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

    private readonly record struct GitStatusSharedCacheRecord(
        string RepositoryRootPath,
        string Fingerprint,
        string Segment,
        long CachedAtUtcTicks,
        string InvalidationTokenValue);
}

