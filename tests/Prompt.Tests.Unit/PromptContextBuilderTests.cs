using FluentAssertions;
using static Prompt.Constants.PromptColors;

namespace Prompt.Tests.Unit;

public sealed class PromptContextBuilderTests
{
    [Fact]
    public void Build_WhenUserAndUsernameExist_ShouldPreferUser()
    {
        var segment = PromptContextBuilder.Build(new TestPlatformProvider(
            user: "unix-user",
            windowsUserName: "windows-user",
            host: "workstation",
            workingDirectoryPath: "/repo"));

        segment.Should().Be($"{ColorUser}unix-user{ColorReset} {ColorHost}workstation{ColorReset} {ColorPath}/repo{ColorReset}");
    }

    [Fact]
    public void Build_WhenOnlyUsernameExists_ShouldUseUsername()
    {
        var segment = PromptContextBuilder.Build(new TestPlatformProvider(
            user: null,
            windowsUserName: "windows-user",
            host: "workstation",
            workingDirectoryPath: "/repo"));

        segment.Should().Be($"{ColorUser}windows-user{ColorReset} {ColorHost}workstation{ColorReset} {ColorPath}/repo{ColorReset}");
    }

    [Fact]
    public void Build_WhenNoUserExists_ShouldUseUnknownMarker()
    {
        var segment = PromptContextBuilder.Build(new TestPlatformProvider(
            host: "workstation",
            workingDirectoryPath: "/repo"));

        segment.Should().Be($"{ColorUser}?{ColorReset} {ColorHost}workstation{ColorReset} {ColorPath}/repo{ColorReset}");
    }

    [Fact]
    public void Build_WhenHostContainsDomain_ShouldTrimSuffix()
    {
        var segment = PromptContextBuilder.Build(new TestPlatformProvider(
            user: "me",
            host: "machine.example.local",
            workingDirectoryPath: "/repo"));

        segment.Should().Contain($"{ColorHost}machine{ColorReset}");
    }

    [Fact]
    public void Build_WhenPathEqualsHome_ShouldRenderTilde()
    {
        using var home = new TemporaryDirectory();

        var segment = PromptContextBuilder.Build(new TestPlatformProvider(
            user: "me",
            host: "machine",
            workingDirectoryPath: home.DirectoryPath,
            homeDirectoryPath: home.DirectoryPath,
            isWindows: false));

        segment.Should().EndWith($" {ColorPath}~{ColorReset}");
    }

    [Fact]
    public void Build_WhenPathIsInsideHome_ShouldRenderTildeRelativePath()
    {
        using var home = new TemporaryDirectory();
        var projectPath = Path.Combine(home.DirectoryPath, "src", "project");
        Directory.CreateDirectory(projectPath);

        var segment = PromptContextBuilder.Build(new TestPlatformProvider(
            user: "me",
            host: "machine",
            workingDirectoryPath: projectPath,
            homeDirectoryPath: home.DirectoryPath,
            isWindows: false));

        segment.Should().EndWith($" {ColorPath}~/src/project{ColorReset}");
    }

    [Fact]
    public void Build_WhenPathContainsBackslashes_ShouldNormalizeToForwardSlashes()
    {
        var segment = PromptContextBuilder.Build(new TestPlatformProvider(
            user: "me",
            host: "machine",
            workingDirectoryPath: "folder\\nested",
            isWindows: true));

        segment.Should().EndWith($" {ColorPath}folder/nested{ColorReset}");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), "Prompt.Tests.Unit", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
    }
}
