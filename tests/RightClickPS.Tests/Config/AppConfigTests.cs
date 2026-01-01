using System.Text.Json;
using RightClickPS.Config;

namespace RightClickPS.Tests.Config;

/// <summary>
/// Unit tests for the AppConfig class.
/// </summary>
public class AppConfigTests
{
    [Fact]
    public void AppConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new AppConfig();

        // Assert
        Assert.Equal("PowerShell Scripts", config.MenuName);
        Assert.Equal("./Scripts", config.ScriptsPath);
        Assert.Equal(3, config.MaxDepth);
        Assert.Null(config.IconPath);
    }

    [Fact]
    public void AppConfig_Serialization_UseCamelCasePropertyNames()
    {
        // Arrange
        var config = new AppConfig
        {
            MenuName = "Test Menu",
            ScriptsPath = "C:\\Scripts",
            MaxDepth = 5,
            IconPath = "C:\\icon.ico"
        };

        // Act
        var json = JsonSerializer.Serialize(config);

        // Assert
        Assert.Contains("\"menuName\":", json);
        Assert.Contains("\"scriptsPath\":", json);
        Assert.Contains("\"maxDepth\":", json);
        Assert.Contains("\"iconPath\":", json);
    }

    [Fact]
    public void AppConfig_Deserialization_FromValidJson_ReturnsCorrectValues()
    {
        // Arrange
        var json = """
        {
            "menuName": "Custom Scripts",
            "scriptsPath": "D:\\MyScripts",
            "maxDepth": 10,
            "iconPath": "D:\\menu.ico"
        }
        """;

        // Act
        var config = JsonSerializer.Deserialize<AppConfig>(json);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("Custom Scripts", config.MenuName);
        Assert.Equal("D:\\MyScripts", config.ScriptsPath);
        Assert.Equal(10, config.MaxDepth);
        Assert.Equal("D:\\menu.ico", config.IconPath);
    }

    [Fact]
    public void AppConfig_Deserialization_WithMissingOptionalFields_UsesDefaults()
    {
        // Arrange - only scriptsPath provided, all optional fields missing
        var json = """
        {
            "scriptsPath": "C:\\Scripts"
        }
        """;

        // Act
        var config = JsonSerializer.Deserialize<AppConfig>(json);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("PowerShell Scripts", config.MenuName);
        Assert.Equal("C:\\Scripts", config.ScriptsPath);
        Assert.Equal(3, config.MaxDepth);
        Assert.Null(config.IconPath);
    }

    [Fact]
    public void AppConfig_Deserialization_WithNullValues_HandlesCorrectly()
    {
        // Arrange
        var json = """
        {
            "menuName": "Test",
            "scriptsPath": null,
            "iconPath": null
        }
        """;

        // Act
        var config = JsonSerializer.Deserialize<AppConfig>(json);

        // Assert
        Assert.NotNull(config);
        Assert.Null(config.ScriptsPath);
        Assert.Null(config.IconPath);
    }

    [Fact]
    public void AppConfig_Serialization_RoundTrip_PreservesValues()
    {
        // Arrange
        var original = new AppConfig
        {
            MenuName = "RoundTrip Test",
            ScriptsPath = "E:\\Scripts\\Test",
            MaxDepth = 7,
            IconPath = "E:\\icon.ico"
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<AppConfig>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.MenuName, deserialized.MenuName);
        Assert.Equal(original.ScriptsPath, deserialized.ScriptsPath);
        Assert.Equal(original.MaxDepth, deserialized.MaxDepth);
        Assert.Equal(original.IconPath, deserialized.IconPath);
    }

    [Fact]
    public void AppConfig_Deserialization_EmptyJson_UsesAllDefaults()
    {
        // Arrange
        var json = "{}";

        // Act
        var config = JsonSerializer.Deserialize<AppConfig>(json);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("PowerShell Scripts", config.MenuName);
        Assert.Equal("./Scripts", config.ScriptsPath);
        Assert.Equal(3, config.MaxDepth);
        Assert.Null(config.IconPath);
    }
}
