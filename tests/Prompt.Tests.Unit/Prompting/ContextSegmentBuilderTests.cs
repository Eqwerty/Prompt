using FluentAssertions;
using Prompt.Prompting;
using static Prompt.Constants.PromptColors;

namespace Prompt.Tests.Unit.Prompting;

public sealed class ContextSegmentBuilderTests
{
    [Fact]
    public void Build_WhenUserAndUsernameExist_ShouldPreferUser()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(
            user: "unix-user",
            windowsUserName: "windows-user",
            host: "workstation",
            workingDirectoryPath: "/repo");

        // Act
        var segment = ContextSegmentBuilder.Build(platformProvider);

        // Assert
        segment.Should().Be($"{ColorUser}unix-user{ColorReset} {ColorHost}workstation{ColorReset} {ColorPath}/repo{ColorReset}");
    }

    [Fact]
    public void Build_WhenOnlyUsernameExists_ShouldUseUsername()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(
            user: null,
            windowsUserName: "windows-user",
            host: "workstation",
            workingDirectoryPath: "/repo");

        // Act
        var segment = ContextSegmentBuilder.Build(platformProvider);

        // Assert
        segment.Should().Be($"{ColorUser}windows-user{ColorReset} {ColorHost}workstation{ColorReset} {ColorPath}/repo{ColorReset}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Build_WhenNoUserExists_ShouldUseUnknownMarker(string? user)
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(
            user: user,
            host: "workstation",
            workingDirectoryPath: "/repo");

        // Act
        var segment = ContextSegmentBuilder.Build(platformProvider);

        // Assert
        segment.Should().Be($"{ColorUser}?{ColorReset} {ColorHost}workstation{ColorReset} {ColorPath}/repo{ColorReset}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Build_WhenNoHostExists_ShouldUseUnknownMarker(string? host)
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(
            user: "me",
            host: host,
            workingDirectoryPath: "/repo");

        // Act
        var segment = ContextSegmentBuilder.Build(platformProvider);

        // Assert
        segment.Should().Be($"{ColorUser}me{ColorReset} {ColorHost}?{ColorReset} {ColorPath}/repo{ColorReset}");
    }

    [Fact]
    public void Build_WhenHostExists_ShouldRenderHostValue()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(
            user: "me",
            host: "workstation",
            workingDirectoryPath: "/repo");

        // Act
        var segment = ContextSegmentBuilder.Build(platformProvider);

        // Assert
        segment.Should().Be($"{ColorUser}me{ColorReset} {ColorHost}workstation{ColorReset} {ColorPath}/repo{ColorReset}");
    }

    [Fact]
    public void Build_WhenPathEqualsHome_ShouldRenderTilde()
    {
        // Arrange
        using var home = new TemporaryDirectory();
        var platformProvider = new TestPlatformProvider(
            user: "me",
            host: "machine",
            workingDirectoryPath: home.DirectoryPath,
            homeDirectoryPath: home.DirectoryPath,
            isWindows: false);

        // Act
        var segment = ContextSegmentBuilder.Build(platformProvider);

        // Assert
        segment.Should().Be($"{ColorUser}me{ColorReset} {ColorHost}machine{ColorReset} {ColorPath}~{ColorReset}");
    }

    [Fact]
    public void Build_WhenPathIsInsideHome_ShouldRenderTildeRelativePath()
    {
        // Arrange
        using var home = new TemporaryDirectory();
        var projectPath = Path.Combine(home.DirectoryPath, "src", "project");
        Directory.CreateDirectory(projectPath);
        
        var platformProvider = new TestPlatformProvider(
            user: "me",
            host: "machine",
            workingDirectoryPath: projectPath,
            homeDirectoryPath: home.DirectoryPath,
            isWindows: false);

        // Act
        var segment = ContextSegmentBuilder.Build(platformProvider);

        // Assert
        segment.Should().Be($"{ColorUser}me{ColorReset} {ColorHost}machine{ColorReset} {ColorPath}~/src/project{ColorReset}");
    }

    [Fact]
    public void Build_WhenPathContainsBackslashes_ShouldNormalizeToForwardSlashes()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(
            user: "me",
            host: "machine",
            workingDirectoryPath: "folder\\nested",
            isWindows: true);

        // Act
        var segment = ContextSegmentBuilder.Build(platformProvider);

        // Assert
        segment.Should().Be($"{ColorUser}me{ColorReset} {ColorHost}machine{ColorReset} {ColorPath}folder/nested{ColorReset}");
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
