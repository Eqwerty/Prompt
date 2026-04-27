using GitPrompt.Platform;

namespace GitPrompt.Tests.Unit.Prompting;

internal sealed class TestPlatformProvider(
    bool isWindows = false,
    bool isWorkingDirectoryFromFallback = false,
    string? user = null,
    string? windowsUserName = null,
    string? host = null,
    string? workingDirectoryPath = null,
    string? homeDirectoryPath = null,
    long? lastCommandDurationMs = null) : PlatformProvider
{
    private readonly bool _isWindows = isWindows;

    internal override bool IsWindows() => _isWindows;

    internal override string? User { get; } = user;

    internal override string? WindowsUserName { get; } = windowsUserName;

    internal override string? Host { get; } = host;

    internal override WorkingDirectoryContext WorkingDirectory { get; } = new(workingDirectoryPath ?? string.Empty, isWorkingDirectoryFromFallback);

    internal override string? HomeDirectoryPath { get; } = homeDirectoryPath;

    internal override long? LastCommandDurationMs { get; } = lastCommandDurationMs;
}
