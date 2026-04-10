using BenchmarkDotNet.Attributes;
using Prompt.Platform;
using Prompt.Prompting;

namespace Prompt.Benchmarks;

[MemoryDiagnoser]
public class ContextSegmentBenchmarks
{
    private readonly PlatformProvider _platformProvider = new BenchmarkPlatformProvider(
        isWindows: false,
        user: "benchmark-user",
        windowsUserName: "benchmark-windows-user",
        host: "benchmark-host",
        workingDirectoryPath: "/repo/path",
        homeDirectoryPath: "/repo");

    [Benchmark]
    public string BuildContextSegment()
    {
        return ContextSegmentBuilder.Build(_platformProvider);
    }

    private sealed class BenchmarkPlatformProvider(
        bool isWindows,
        string? user,
        string? windowsUserName,
        string? host,
        string? workingDirectoryPath,
        string? homeDirectoryPath) : PlatformProvider
    {
        private readonly bool _isWindows = isWindows;

        internal override bool IsWindows() => _isWindows;

        internal override string? User { get; } = user;

        internal override string? WindowsUserName { get; } = windowsUserName;

        internal override string? Host { get; } = host;

        internal override string? WorkingDirectoryPath { get; } = workingDirectoryPath;

        internal override string? HomeDirectoryPath { get; } = homeDirectoryPath;
    }
}
