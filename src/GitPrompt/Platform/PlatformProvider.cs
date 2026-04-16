namespace GitPrompt.Platform;

internal abstract class PlatformProvider
{
    internal readonly record struct WorkingDirectoryContext(string Path, bool IsFromFallback);

    internal static readonly PlatformProvider System = new SystemPlatformProvider();

    internal abstract bool IsWindows();

    internal abstract string? User { get; }

    internal abstract string? WindowsUserName { get; }

    internal abstract string? Host { get; }

    internal abstract WorkingDirectoryContext WorkingDirectory { get; }

    internal abstract string? HomeDirectoryPath { get; }

    private sealed class SystemPlatformProvider : PlatformProvider
    {
        internal override bool IsWindows() => OperatingSystem.IsWindows();

        internal override string? User => Environment.GetEnvironmentVariable("USER");

        internal override string? WindowsUserName => Environment.GetEnvironmentVariable("USERNAME");

        internal override string Host => Environment.MachineName;

        internal override WorkingDirectoryContext WorkingDirectory
        {
            get
            {
                try
                {
                    return new WorkingDirectoryContext(Directory.GetCurrentDirectory(), IsFromFallback: false);
                }
                catch
                {
                    return new WorkingDirectoryContext(Environment.GetEnvironmentVariable("PWD") ?? string.Empty, IsFromFallback: true);
                }
            }
        }

        internal override string HomeDirectoryPath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
}
