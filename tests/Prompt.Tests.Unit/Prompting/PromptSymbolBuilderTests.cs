using FluentAssertions;
using Prompt.Prompting;
using Prompt.Tests.Unit.Platform;

namespace Prompt.Tests.Unit.Prompting;

public sealed class PromptSymbolBuilderTests
{
    [Fact]
    public void Build_WhenOnWindows_ShouldReturnGreaterThanRegardlessOfUser()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(isWindows: true);

        // Act
        var symbol = PromptSymbolBuilder.Build(platformProvider);

        // Assert
        symbol.Should().Be(">");
    }

    [Fact]
    public void Build_WhenOnUnixAndUserIsRoot_ShouldReturnHash()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(isWindows: false, user: "root");

        // Act
        var symbol = PromptSymbolBuilder.Build(platformProvider);

        // Assert
        symbol.Should().Be("#");
    }

    [Fact]
    public void Build_WhenOnUnixAndUserIsNotRoot_ShouldReturnDollar()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(isWindows: false, user: "me");

        // Act
        var symbol = PromptSymbolBuilder.Build(platformProvider);

        // Assert
        symbol.Should().Be("$");
    }
}
