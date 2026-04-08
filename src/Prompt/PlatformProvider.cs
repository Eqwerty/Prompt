namespace Prompt;

internal abstract class PlatformProvider
{
    internal static readonly PlatformProvider System = new SystemPlatformProvider();

    internal abstract bool IsWindows();

    internal abstract string? User { get; }

    internal abstract string? WindowsUserName { get; }

    internal abstract string? Host { get; }

    internal abstract string? WorkingDirectoryPath { get; }

    internal abstract string? HomeDirectoryPath { get; }

    private sealed class SystemPlatformProvider : PlatformProvider
    {
        internal override bool IsWindows() => OperatingSystem.IsWindows();

        internal override string? User => Environment.GetEnvironmentVariable("USER");

        internal override string? WindowsUserName => Environment.GetEnvironmentVariable("USERNAME");

        internal override string Host => Environment.MachineName;

        internal override string WorkingDirectoryPath
        {
            get
            {
                try { return Directory.GetCurrentDirectory(); }
                catch { return "?"; }
            }
        }

        internal override string HomeDirectoryPath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
}
