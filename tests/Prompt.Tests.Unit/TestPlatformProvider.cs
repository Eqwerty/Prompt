namespace Prompt.Tests.Unit;

internal sealed class TestPlatformProvider(
    bool isWindows = false,
    string? user = null,
    string? windowsUserName = null,
    string? host = null,
    string? workingDirectoryPath = null,
    string? homeDirectoryPath = null) : PlatformProvider
{
    private readonly bool _isWindows = isWindows;

    internal override bool IsWindows() => _isWindows;

    internal override string? User { get; } = user;

    internal override string? WindowsUserName { get; } = windowsUserName;

    internal override string? Host { get; } = host;

    internal override string? WorkingDirectoryPath { get; } = workingDirectoryPath;

    internal override string? HomeDirectoryPath { get; } = homeDirectoryPath;
}
