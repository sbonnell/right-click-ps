using RightClickPS.Config;

namespace RightClickPS.Tests.Config;

/// <summary>
/// Unit tests for the ConfigLoader class.
/// </summary>
public class ConfigLoaderTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testConfigPath;

    public ConfigLoaderTests()
    {
        // Create a unique temp directory for each test
        _testDirectory = Path.Combine(Path.GetTempPath(), $"RightClickPS_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        _testConfigPath = Path.Combine(_testDirectory, "config.json");
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    #region Load Valid Config Tests

    [Fact]
    public void LoadFromPath_ValidConfig_ReturnsCorrectValues()
    {
        // Arrange
        var scriptsDir = Path.Combine(_testDirectory, "Scripts");
        Directory.CreateDirectory(scriptsDir);

        var json = $$"""
        {
            "menuName": "My Scripts",
            "scriptsPath": "{{scriptsDir.Replace("\\", "\\\\")}}",
            "maxDepth": 5,
            "iconPath": null
        }
        """;
        File.WriteAllText(_testConfigPath, json);

        var loader = new ConfigLoader();

        // Act
        var config = loader.LoadFromPath(_testConfigPath);

        // Assert
        Assert.Equal("My Scripts", config.MenuName);
        Assert.Equal(scriptsDir, config.ScriptsPath);
        Assert.Equal(5, config.MaxDepth);
    }

    [Fact]
    public void LoadFromPath_ValidConfigWithComments_ParsesSuccessfully()
    {
        // Arrange
        var scriptsDir = Path.Combine(_testDirectory, "Scripts");
        Directory.CreateDirectory(scriptsDir);

        var json = $$"""
        {
            // This is a comment
            "menuName": "Test Menu",
            "scriptsPath": "{{scriptsDir.Replace("\\", "\\\\")}}"
            /* Another comment */
        }
        """;
        File.WriteAllText(_testConfigPath, json);

        var loader = new ConfigLoader();

        // Act
        var config = loader.LoadFromPath(_testConfigPath);

        // Assert
        Assert.Equal("Test Menu", config.MenuName);
    }

    [Fact]
    public void LoadFromPath_ValidConfigWithTrailingCommas_ParsesSuccessfully()
    {
        // Arrange
        var scriptsDir = Path.Combine(_testDirectory, "Scripts");
        Directory.CreateDirectory(scriptsDir);

        var json = $$"""
        {
            "menuName": "Test Menu",
            "scriptsPath": "{{scriptsDir.Replace("\\", "\\\\")}}",
        }
        """;
        File.WriteAllText(_testConfigPath, json);

        var loader = new ConfigLoader();

        // Act
        var config = loader.LoadFromPath(_testConfigPath);

        // Assert
        Assert.Equal("Test Menu", config.MenuName);
    }

    #endregion

    #region Missing Config File Tests

    [Fact]
    public void LoadFromPath_MissingFile_ReturnsDefaults()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");
        var loader = new ConfigLoader();

        // Act
        var config = loader.LoadFromPath(nonExistentPath);

        // Assert
        Assert.Equal("PowerShell Scripts", config.MenuName);
        Assert.Equal("./Scripts", config.ScriptsPath);
        Assert.Equal(3, config.MaxDepth);
        Assert.Null(config.IconPath);
    }

    #endregion

    #region Environment Variable Expansion Tests

    [Fact]
    public void LoadFromPath_WithEnvironmentVariables_ExpandsCorrectly()
    {
        // Arrange
        var testEnvVar = "RIGHTCLICKPS_TEST_PATH";
        var testPath = Path.Combine(_testDirectory, "ExpandedScripts");
        Directory.CreateDirectory(testPath);

        Environment.SetEnvironmentVariable(testEnvVar, testPath);
        try
        {
            var json = $$"""
            {
                "menuName": "Env Test",
                "scriptsPath": "%{{testEnvVar}}%"
            }
            """;
            File.WriteAllText(_testConfigPath, json);

            var loader = new ConfigLoader();

            // Act
            var config = loader.LoadFromPath(_testConfigPath);

            // Assert
            Assert.Equal(testPath, config.ScriptsPath);
        }
        finally
        {
            Environment.SetEnvironmentVariable(testEnvVar, null);
        }
    }

    [Fact]
    public void LoadFromPath_WithUserProfileEnvVar_ExpandsCorrectly()
    {
        // Arrange
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var uniqueId = Guid.NewGuid().ToString("N");
        var scriptsDir = Path.Combine(userProfile, "TestScripts_" + uniqueId);
        Directory.CreateDirectory(scriptsDir);

        try
        {
            var json = $$"""
            {
                "menuName": "UserProfile Test",
                "scriptsPath": "%USERPROFILE%\\TestScripts_{{uniqueId}}"
            }
            """;
            File.WriteAllText(_testConfigPath, json);

            var loader = new ConfigLoader();

            // Act
            var config = loader.LoadFromPath(_testConfigPath);

            // Assert
            Assert.StartsWith(userProfile, config.ScriptsPath);
        }
        finally
        {
            if (Directory.Exists(scriptsDir))
            {
                Directory.Delete(scriptsDir);
            }
        }
    }

    [Fact]
    public void LoadFromPath_ExpandsEnvironmentVariablesInIconPath()
    {
        // Arrange
        var scriptsDir = Path.Combine(_testDirectory, "Scripts");
        Directory.CreateDirectory(scriptsDir);

        var testEnvVar = "RIGHTCLICKPS_ICON_PATH";
        var testPath = Path.Combine(_testDirectory, "icon.ico");

        Environment.SetEnvironmentVariable(testEnvVar, _testDirectory);
        try
        {
            var json = $$"""
            {
                "scriptsPath": "{{scriptsDir.Replace("\\", "\\\\")}}",
                "iconPath": "%{{testEnvVar}}%\\icon.ico"
            }
            """;
            File.WriteAllText(_testConfigPath, json);

            var loader = new ConfigLoader();

            // Act
            var config = loader.LoadFromPath(_testConfigPath);

            // Assert
            Assert.Equal(testPath, config.IconPath);
        }
        finally
        {
            Environment.SetEnvironmentVariable(testEnvVar, null);
        }
    }

    #endregion

    #region Path Validation Tests

    [Fact]
    public void LoadFromPath_ScriptsPathDoesNotExist_ThrowsConfigurationException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "NonExistentScripts");
        var json = $$"""
        {
            "scriptsPath": "{{nonExistentPath.Replace("\\", "\\\\")}}"
        }
        """;
        File.WriteAllText(_testConfigPath, json);

        var loader = new ConfigLoader();

        // Act & Assert
        var ex = Assert.Throws<ConfigurationException>(() => loader.LoadFromPath(_testConfigPath));
        Assert.Contains("Scripts path does not exist", ex.Message);
    }

    [Fact]
    public void LoadFromPath_RelativeScriptsPath_ResolvesRelativeToConfigFile()
    {
        // Arrange
        var relativeDir = "RelativeScripts";
        var absolutePath = Path.Combine(_testDirectory, relativeDir);
        Directory.CreateDirectory(absolutePath);

        var json = $$"""
        {
            "scriptsPath": "{{relativeDir}}"
        }
        """;
        File.WriteAllText(_testConfigPath, json);

        var loader = new ConfigLoader();

        // Act
        var config = loader.LoadFromPath(_testConfigPath);

        // Assert
        Assert.Equal(absolutePath, config.ScriptsPath);
    }

    [Fact]
    public void LoadFromPath_NullScriptsPath_DoesNotThrow()
    {
        // Arrange
        var json = """
        {
            "scriptsPath": null
        }
        """;
        File.WriteAllText(_testConfigPath, json);

        var loader = new ConfigLoader();

        // Act
        var config = loader.LoadFromPath(_testConfigPath);

        // Assert
        Assert.Null(config.ScriptsPath);
    }

    [Fact]
    public void LoadFromPath_EmptyScriptsPath_DoesNotThrow()
    {
        // Arrange
        var json = """
        {
            "scriptsPath": ""
        }
        """;
        File.WriteAllText(_testConfigPath, json);

        var loader = new ConfigLoader();

        // Act
        var config = loader.LoadFromPath(_testConfigPath);

        // Assert - empty string is treated as null/not provided
        Assert.Equal("", config.ScriptsPath);
    }

    #endregion

    #region MaxDepth Validation Tests

    [Fact]
    public void LoadFromPath_MaxDepthTooLow_ClampsToMinimum()
    {
        // Arrange
        var scriptsDir = Path.Combine(_testDirectory, "Scripts");
        Directory.CreateDirectory(scriptsDir);

        var json = $$"""
        {
            "scriptsPath": "{{scriptsDir.Replace("\\", "\\\\")}}",
            "maxDepth": 0
        }
        """;
        File.WriteAllText(_testConfigPath, json);

        var loader = new ConfigLoader();

        // Act
        var config = loader.LoadFromPath(_testConfigPath);

        // Assert
        Assert.Equal(1, config.MaxDepth);
    }

    [Fact]
    public void LoadFromPath_MaxDepthNegative_ClampsToMinimum()
    {
        // Arrange
        var scriptsDir = Path.Combine(_testDirectory, "Scripts");
        Directory.CreateDirectory(scriptsDir);

        var json = $$"""
        {
            "scriptsPath": "{{scriptsDir.Replace("\\", "\\\\")}}",
            "maxDepth": -5
        }
        """;
        File.WriteAllText(_testConfigPath, json);

        var loader = new ConfigLoader();

        // Act
        var config = loader.LoadFromPath(_testConfigPath);

        // Assert
        Assert.Equal(1, config.MaxDepth);
    }

    [Fact]
    public void LoadFromPath_MaxDepthTooHigh_ClampsToMaximum()
    {
        // Arrange
        var scriptsDir = Path.Combine(_testDirectory, "Scripts");
        Directory.CreateDirectory(scriptsDir);

        var json = $$"""
        {
            "scriptsPath": "{{scriptsDir.Replace("\\", "\\\\")}}",
            "maxDepth": 100
        }
        """;
        File.WriteAllText(_testConfigPath, json);

        var loader = new ConfigLoader();

        // Act
        var config = loader.LoadFromPath(_testConfigPath);

        // Assert
        Assert.Equal(10, config.MaxDepth);
    }

    #endregion

    #region JSON Parsing Error Tests

    [Fact]
    public void LoadFromPath_InvalidJson_ThrowsConfigurationException()
    {
        // Arrange
        var invalidJson = "{ this is not valid json }";
        File.WriteAllText(_testConfigPath, invalidJson);

        var loader = new ConfigLoader();

        // Act & Assert
        var ex = Assert.Throws<ConfigurationException>(() => loader.LoadFromPath(_testConfigPath));
        Assert.Contains("Failed to parse config file", ex.Message);
    }

    [Fact]
    public void LoadFromPath_EmptyFile_ThrowsConfigurationException()
    {
        // Arrange
        File.WriteAllText(_testConfigPath, "");

        var loader = new ConfigLoader();

        // Act & Assert
        Assert.Throws<ConfigurationException>(() => loader.LoadFromPath(_testConfigPath));
    }

    [Fact]
    public void LoadFromPath_InvalidJsonType_ThrowsConfigurationException()
    {
        // Arrange - JSON array instead of object
        var json = "[1, 2, 3]";
        File.WriteAllText(_testConfigPath, json);

        var loader = new ConfigLoader();

        // Act & Assert
        Assert.Throws<ConfigurationException>(() => loader.LoadFromPath(_testConfigPath));
    }

    #endregion

    #region GetConfigPath Tests

    [Fact]
    public void GetConfigPath_ReturnsPathInAppDirectory()
    {
        // Act
        var path = ConfigLoader.GetConfigPath();

        // Assert
        Assert.EndsWith("config.json", path);
        Assert.True(Path.IsPathRooted(path), "Config path should be absolute");
    }

    #endregion

    #region ConfigurationException Tests

    [Fact]
    public void ConfigurationException_WithMessage_PreservesMessage()
    {
        // Arrange & Act
        var ex = new ConfigurationException("Test error message");

        // Assert
        Assert.Equal("Test error message", ex.Message);
    }

    [Fact]
    public void ConfigurationException_WithInnerException_PreservesInnerException()
    {
        // Arrange
        var inner = new IOException("Inner error");

        // Act
        var ex = new ConfigurationException("Outer error", inner);

        // Assert
        Assert.Equal("Outer error", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    #endregion
}
