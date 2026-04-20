using FluentAssertions;
using GitPrompt.Configuration;

namespace GitPrompt.Tests.Unit.Configuration;

public sealed class ConfigInitializerTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _configDirectory;

    public ConfigInitializerTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GitPrompt.Tests.ConfigInitializer", Guid.NewGuid().ToString("N"));
        _configDirectory = Path.Combine(_tempDirectory, "config", "gitprompt");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void InitializeDefaultConfig_WhenConfigIsMissing_ShouldWriteDefaultContent()
    {
        // Arrange
        using var stream = typeof(ConfigInitializer).Assembly.GetManifestResourceStream("default-config.json")!;
        using var reader = new StreamReader(stream);
        var expectedContent = reader.ReadToEnd();

        // Act
        ConfigInitializer.InitializeDefaultConfig(_configDirectory);

        // Assert
        var actual = File.ReadAllText(Path.Combine(_configDirectory, "config.json"));
        actual.Should().Be(expectedContent);
    }

    [Fact]
    public void InitializeDefaultConfig_WhenConfigAlreadyExists_ShouldNotOverwrite()
    {
        // Arrange
        Directory.CreateDirectory(_configDirectory);
        var configFile = Path.Combine(_configDirectory, "config.json");
        var existingContent = "{ \"custom\": true }";
        File.WriteAllText(configFile, existingContent);

        // Act
        ConfigInitializer.InitializeDefaultConfig(_configDirectory);

        // Assert
        File.ReadAllText(configFile).Should().Be(existingContent);
    }

    [Fact]
    public void InitializeDefaultConfig_WhenConfigDirectoryMissing_ShouldCreateDirectory()
    {
        // Arrange
        Directory.Exists(_configDirectory).Should().BeFalse();

        // Act
        ConfigInitializer.InitializeDefaultConfig(_configDirectory);

        // Assert
        Directory.Exists(_configDirectory).Should().BeTrue();
    }
}
