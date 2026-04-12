using FluentAssertions;
using Prompt.Constants;
using Prompt.Prompting;

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
        symbol.Should().Be(PromptSymbols.Windows);
    }

    [Fact]
    public void Build_WhenOnUnixAndUserIsRoot_ShouldReturnHash()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(isWindows: false, user: "root");

        // Act
        var symbol = PromptSymbolBuilder.Build(platformProvider);

        // Assert
        symbol.Should().Be(PromptSymbols.UnixRoot);
    }

    [Fact]
    public void Build_WhenOnUnixAndUserIsNotRoot_ShouldReturnDollar()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(isWindows: false, user: "me");

        // Act
        var symbol = PromptSymbolBuilder.Build(platformProvider);

        // Assert
        symbol.Should().Be(PromptSymbols.Unix);
    }
}
