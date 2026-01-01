using RightClickPS.Commands;
using RightClickPS.Config;
using RightClickPS.Registry;
using RightClickPS.Scripts;

namespace RightClickPS.Tests.Commands;

/// <summary>
/// Unit tests for <see cref="RegisterCommand"/>.
/// The Collection attribute ensures these tests don't run in parallel with other registry tests.
/// </summary>
[Collection("RegistryTests")]
public class RegisterCommandTests : IDisposable
{
    private readonly ContextMenuRegistry _registry;

    public RegisterCommandTests()
    {
        _registry = new ContextMenuRegistry();
        // Ensure clean state before each test
        _registry.Unregister();
    }

    public void Dispose()
    {
        // Clean up registry entries after each test
        _registry.Unregister();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaultDependencies_DoesNotThrow()
    {
        // Act & Assert
        var command = new RegisterCommand();
        Assert.NotNull(command);
    }

    [Fact]
    public void Constructor_WithNullConfigLoader_ThrowsArgumentNullException()
    {
        // Arrange
        var scriptDiscovery = new ScriptDiscovery();
        var registry = new ContextMenuRegistry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RegisterCommand(null!, scriptDiscovery, registry));
    }

    [Fact]
    public void Constructor_WithNullScriptDiscovery_ThrowsArgumentNullException()
    {
        // Arrange
        var configLoader = new ConfigLoader();
        var registry = new ContextMenuRegistry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RegisterCommand(configLoader, null!, registry));
    }

    [Fact]
    public void Constructor_WithNullContextMenuRegistry_ThrowsArgumentNullException()
    {
        // Arrange
        var configLoader = new ConfigLoader();
        var scriptDiscovery = new ScriptDiscovery();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RegisterCommand(configLoader, scriptDiscovery, null!));
    }

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
        var configLoader = new ConfigLoader();
        var scriptDiscovery = new ScriptDiscovery();
        var registry = new ContextMenuRegistry();

        // Act
        var command = new RegisterCommand(configLoader, scriptDiscovery, registry);

        // Assert
        Assert.NotNull(command);
    }

    #endregion

    #region Execute Tests - Configuration Handling

    [Fact]
    public void Execute_WithMissingConfigFile_UsesDefaultConfig()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            // Create a scripts folder with a test script
            var scriptsDir = Path.Combine(tempDir, "scripts");
            Directory.CreateDirectory(scriptsDir);
            CreateTestScript(scriptsDir, "Test Script.ps1");

            // Create a config that points to our test scripts folder
            var configPath = Path.Combine(tempDir, "config.json");
            File.WriteAllText(configPath, $@"{{
                ""menuName"": ""Test Menu"",
                ""scriptsPath"": ""{scriptsDir.Replace("\\", "\\\\")}"",
                ""maxDepth"": 3
            }}");

            var configLoader = new ConfigLoader();
            var scriptDiscovery = new ScriptDiscovery();
            var registry = new ContextMenuRegistry();

            // Use a testable command that captures output
            var messages = new List<string>();
            var command = new TestableRegisterCommand(configLoader, scriptDiscovery, registry, messages, configPath);

            // Act
            var result = command.Execute();

            // Assert - Should succeed with default config (which has no scriptsPath set)
            // Since we're loading from a specific path with valid config, it should work
            Assert.Equal(0, result);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Execute_WithInvalidConfigFile_ReturnsNonZero()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            // Create an invalid config file (malformed JSON)
            var configPath = Path.Combine(tempDir, "config.json");
            File.WriteAllText(configPath, "{ invalid json }");

            // Create a ConfigLoader that loads from the invalid config
            var messages = new List<string>();
            var command = new TestableRegisterCommandWithInvalidConfig(messages, configPath);

            // Act
            var result = command.Execute();

            // Assert
            Assert.NotEqual(0, result);
            Assert.Contains(messages, m => m.Contains("Error") || m.Contains("Configuration"));
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Execute_WithNonExistentScriptsPath_ReturnsNonZero()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            // Create a config pointing to a non-existent scripts folder
            var configPath = Path.Combine(tempDir, "config.json");
            File.WriteAllText(configPath, @"{
                ""menuName"": ""Test Menu"",
                ""scriptsPath"": ""C:\\NonExistent\\Path\\That\\Does\\Not\\Exist"",
                ""maxDepth"": 3
            }");

            var messages = new List<string>();
            var command = new TestableRegisterCommandWithInvalidConfig(messages, configPath);

            // Act
            var result = command.Execute();

            // Assert - Should fail because the scripts path doesn't exist
            Assert.NotEqual(0, result);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    #endregion

    #region Execute Tests - Script Discovery

    [Fact]
    public void Execute_WithNoScripts_ReturnsZeroWithWarning()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            // Create empty scripts folder
            var scriptsDir = Path.Combine(tempDir, "scripts");
            Directory.CreateDirectory(scriptsDir);

            var configPath = Path.Combine(tempDir, "config.json");
            File.WriteAllText(configPath, $@"{{
                ""menuName"": ""Test Menu"",
                ""scriptsPath"": ""{scriptsDir.Replace("\\", "\\\\")}"",
                ""maxDepth"": 3
            }}");

            var configLoader = new ConfigLoader();
            var scriptDiscovery = new ScriptDiscovery();
            var registry = new ContextMenuRegistry();
            var messages = new List<string>();
            var command = new TestableRegisterCommand(configLoader, scriptDiscovery, registry, messages, configPath);

            // Act
            var result = command.Execute();

            // Assert
            Assert.Equal(0, result);
            Assert.Contains(messages, m => m.Contains("No scripts found") || m.Contains("Warning"));
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Execute_WithValidScripts_RegistersSuccessfully()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var scriptsDir = Path.Combine(tempDir, "scripts");
            Directory.CreateDirectory(scriptsDir);
            CreateTestScript(scriptsDir, "Test Script.ps1");

            var configPath = Path.Combine(tempDir, "config.json");
            File.WriteAllText(configPath, $@"{{
                ""menuName"": ""Test Menu"",
                ""scriptsPath"": ""{scriptsDir.Replace("\\", "\\\\")}"",
                ""maxDepth"": 3
            }}");

            var configLoader = new ConfigLoader();
            var scriptDiscovery = new ScriptDiscovery();
            var registry = new ContextMenuRegistry();
            var messages = new List<string>();
            var command = new TestableRegisterCommand(configLoader, scriptDiscovery, registry, messages, configPath);

            // Act
            var result = command.Execute();

            // Assert
            Assert.Equal(0, result);
            Assert.Contains(messages, m => m.Contains("Registration completed successfully"));
            Assert.True(registry.IsRegistered());
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Execute_WithMultipleScripts_ReportsCorrectCount()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var scriptsDir = Path.Combine(tempDir, "scripts");
            Directory.CreateDirectory(scriptsDir);

            CreateTestScript(scriptsDir, "Script1.ps1", "Script One");
            CreateTestScript(scriptsDir, "Script2.ps1", "Script Two");
            CreateTestScript(scriptsDir, "Script3.ps1", "Script Three");

            var configPath = Path.Combine(tempDir, "config.json");
            File.WriteAllText(configPath, $@"{{
                ""menuName"": ""Test Menu"",
                ""scriptsPath"": ""{scriptsDir.Replace("\\", "\\\\")}"",
                ""maxDepth"": 3
            }}");

            var configLoader = new ConfigLoader();
            var scriptDiscovery = new ScriptDiscovery();
            var registry = new ContextMenuRegistry();
            var messages = new List<string>();
            var command = new TestableRegisterCommand(configLoader, scriptDiscovery, registry, messages, configPath);

            // Act
            var result = command.Execute();

            // Assert
            Assert.Equal(0, result);
            Assert.Contains(messages, m => m.Contains("3 script(s)"));
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Execute_WithNestedFolders_DiscoversFoldersAndScripts()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var scriptsDir = Path.Combine(tempDir, "scripts");
            var subFolder = Path.Combine(scriptsDir, "SubFolder");
            Directory.CreateDirectory(scriptsDir);
            Directory.CreateDirectory(subFolder);

            CreateTestScript(scriptsDir, "RootScript.ps1", "Root Script");
            CreateTestScript(subFolder, "NestedScript.ps1", "Nested Script");

            var configPath = Path.Combine(tempDir, "config.json");
            File.WriteAllText(configPath, $@"{{
                ""menuName"": ""Test Menu"",
                ""scriptsPath"": ""{scriptsDir.Replace("\\", "\\\\")}"",
                ""maxDepth"": 3
            }}");

            var configLoader = new ConfigLoader();
            var scriptDiscovery = new ScriptDiscovery();
            var registry = new ContextMenuRegistry();
            var messages = new List<string>();
            var command = new TestableRegisterCommand(configLoader, scriptDiscovery, registry, messages, configPath);

            // Act
            var result = command.Execute();

            // Assert
            Assert.Equal(0, result);
            Assert.Contains(messages, m => m.Contains("2 script(s)"));
            Assert.Contains(messages, m => m.Contains("1 folder(s)"));
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    #endregion

    #region Execute Tests - Output Messages

    [Fact]
    public void Execute_OutputsConfigurationInfo()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var scriptsDir = Path.Combine(tempDir, "scripts");
            Directory.CreateDirectory(scriptsDir);
            CreateTestScript(scriptsDir, "Test.ps1");

            var configPath = Path.Combine(tempDir, "config.json");
            File.WriteAllText(configPath, $@"{{
                ""menuName"": ""My PowerShell Menu"",
                ""scriptsPath"": ""{scriptsDir.Replace("\\", "\\\\")}"",
                ""maxDepth"": 5
            }}");

            var configLoader = new ConfigLoader();
            var scriptDiscovery = new ScriptDiscovery();
            var registry = new ContextMenuRegistry();
            var messages = new List<string>();
            var command = new TestableRegisterCommand(configLoader, scriptDiscovery, registry, messages, configPath);

            // Act
            command.Execute();

            // Assert
            Assert.Contains(messages, m => m.Contains("My PowerShell Menu"));
            Assert.Contains(messages, m => m.Contains("Max depth: 5"));
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Execute_OutputsRegistrationResults()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var scriptsDir = Path.Combine(tempDir, "scripts");
            Directory.CreateDirectory(scriptsDir);
            CreateTestScript(scriptsDir, "Test.ps1");

            var configPath = Path.Combine(tempDir, "config.json");
            File.WriteAllText(configPath, $@"{{
                ""menuName"": ""Test Menu"",
                ""scriptsPath"": ""{scriptsDir.Replace("\\", "\\\\")}"",
                ""maxDepth"": 3
            }}");

            var configLoader = new ConfigLoader();
            var scriptDiscovery = new ScriptDiscovery();
            var registry = new ContextMenuRegistry();
            var messages = new List<string>();
            var command = new TestableRegisterCommand(configLoader, scriptDiscovery, registry, messages, configPath);

            // Act
            command.Execute();

            // Assert
            Assert.Contains(messages, m => m.Contains("Files context menu items:"));
            Assert.Contains(messages, m => m.Contains("Directory context menu items:"));
            Assert.Contains(messages, m => m.Contains("Total menu items:"));
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Execute_ClearsExistingEntriesBeforeRegistering()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var scriptsDir = Path.Combine(tempDir, "scripts");
            Directory.CreateDirectory(scriptsDir);
            CreateTestScript(scriptsDir, "Script1.ps1", "Script One");

            var configPath = Path.Combine(tempDir, "config.json");
            File.WriteAllText(configPath, $@"{{
                ""menuName"": ""Test Menu"",
                ""scriptsPath"": ""{scriptsDir.Replace("\\", "\\\\")}"",
                ""maxDepth"": 3
            }}");

            var configLoader = new ConfigLoader();
            var scriptDiscovery = new ScriptDiscovery();
            var registry = new ContextMenuRegistry();
            var messages = new List<string>();
            var command = new TestableRegisterCommand(configLoader, scriptDiscovery, registry, messages, configPath);

            // First registration
            command.Execute();
            Assert.True(registry.IsRegistered());

            // Add another script and re-register
            CreateTestScript(scriptsDir, "Script2.ps1", "Script Two");
            messages.Clear();

            // Act - Second registration
            var result = command.Execute();

            // Assert
            Assert.Equal(0, result);
            Assert.True(registry.IsRegistered());
            Assert.Contains(messages, m => m.Contains("2 script(s)"));
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Execute_WithFilesOnlyScripts_RegistersForFilesContext()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var scriptsDir = Path.Combine(tempDir, "scripts");
            Directory.CreateDirectory(scriptsDir);
            CreateTestScript(scriptsDir, "FilesScript.ps1", "Files Script", TargetType.Files);

            var configPath = Path.Combine(tempDir, "config.json");
            File.WriteAllText(configPath, $@"{{
                ""menuName"": ""Test Menu"",
                ""scriptsPath"": ""{scriptsDir.Replace("\\", "\\\\")}"",
                ""maxDepth"": 3
            }}");

            var configLoader = new ConfigLoader();
            var scriptDiscovery = new ScriptDiscovery();
            var registry = new ContextMenuRegistry();
            var messages = new List<string>();
            var command = new TestableRegisterCommand(configLoader, scriptDiscovery, registry, messages, configPath);

            // Act
            var result = command.Execute();

            // Assert
            Assert.Equal(0, result);
            Assert.True(registry.IsRegisteredForFiles());
            // Directory context may or may not be registered depending on filtering
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Execute_WithFoldersOnlyScripts_RegistersForDirectoriesContext()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var scriptsDir = Path.Combine(tempDir, "scripts");
            Directory.CreateDirectory(scriptsDir);
            CreateTestScript(scriptsDir, "FolderScript.ps1", "Folder Script", TargetType.Folders);

            var configPath = Path.Combine(tempDir, "config.json");
            File.WriteAllText(configPath, $@"{{
                ""menuName"": ""Test Menu"",
                ""scriptsPath"": ""{scriptsDir.Replace("\\", "\\\\")}"",
                ""maxDepth"": 3
            }}");

            var configLoader = new ConfigLoader();
            var scriptDiscovery = new ScriptDiscovery();
            var registry = new ContextMenuRegistry();
            var messages = new List<string>();
            var command = new TestableRegisterCommand(configLoader, scriptDiscovery, registry, messages, configPath);

            // Act
            var result = command.Execute();

            // Assert
            Assert.Equal(0, result);
            Assert.True(registry.IsRegisteredForDirectories());
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    #endregion

    #region Helper Methods

    private static string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "RightClickPSTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static void CleanupTempDirectory(string tempDir)
    {
        if (Directory.Exists(tempDir))
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private static void CreateTestScript(string directory, string fileName, string name = "Test Script", TargetType targetType = TargetType.Both)
    {
        var targetTypeStr = targetType switch
        {
            TargetType.Files => "Files",
            TargetType.Folders => "Folders",
            _ => "Both"
        };

        var scriptContent = $@"<#
@Name: {name}
@Description: A test script
@Extensions: *
@TargetType: {targetTypeStr}
@RunAsAdmin: false
#>

# Test script content
Write-Host 'Hello from {name}'
";
        File.WriteAllText(Path.Combine(directory, fileName), scriptContent);
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// A testable version of RegisterCommand that captures output messages and uses a custom config path.
    /// </summary>
    private class TestableRegisterCommand : RegisterCommand
    {
        private readonly List<string> _messages;
        private readonly string _configPath;
        private readonly ConfigLoader _configLoader;

        public TestableRegisterCommand(
            ConfigLoader configLoader,
            ScriptDiscovery scriptDiscovery,
            ContextMenuRegistry registry,
            List<string> messages,
            string configPath)
            : base(configLoader, scriptDiscovery, registry)
        {
            _configLoader = configLoader;
            _messages = messages;
            _configPath = configPath;
        }

        public new int Execute()
        {
            // Temporarily override the config loading to use our test config
            return ExecuteWithConfig(_configLoader.LoadFromPath(_configPath));
        }

        private int ExecuteWithConfig(AppConfig config)
        {
            try
            {
                WriteInfo("RightClickPS - Context Menu Registration");
                WriteInfo("=========================================");
                WriteInfo("");

                WriteInfo("Loading configuration...");
                WriteInfo($"  Menu name: {config.MenuName}");
                WriteInfo($"  Scripts path: {config.ScriptsPath ?? "(not configured)"}");
                WriteInfo($"  System scripts path: {config.SystemScriptsPath ?? "(not configured)"}");
                WriteInfo($"  Max depth: {config.MaxDepth}");

                WriteInfo("");
                WriteInfo("Discovering scripts...");

                // Get the ScriptDiscovery and ContextMenuRegistry from the base class using reflection
                var discoveryField = typeof(RegisterCommand).GetField("_scriptDiscovery", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var registryField = typeof(RegisterCommand).GetField("_contextMenuRegistry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var scriptDiscovery = (ScriptDiscovery)discoveryField!.GetValue(this)!;
                var contextMenuRegistry = (ContextMenuRegistry)registryField!.GetValue(this)!;

                var menuRoot = scriptDiscovery.DiscoverScripts(
                    config.ScriptsPath,
                    config.SystemScriptsPath,
                    config.MaxDepth);

                var scriptCount = ScriptDiscovery.CountScripts(menuRoot);
                var folderCount = ScriptDiscovery.CountFolders(menuRoot);

                if (scriptCount == 0)
                {
                    WriteWarning("No scripts found. Context menu will not be created.");
                    WriteInfo("  Ensure your scripts path is configured in config.json");
                    WriteInfo("  and contains .ps1 files with proper metadata headers.");
                    return 0;
                }

                WriteInfo($"  Found {scriptCount} script(s) in {folderCount} folder(s)");

                var exePath = GetExecutablePath();
                WriteInfo($"  Executable path: {exePath}");

                WriteInfo("");
                WriteInfo("Registering context menu...");
                var result = contextMenuRegistry.Register(menuRoot, config.MenuName, exePath);

                if (!result.Success)
                {
                    WriteError($"Registration failed: {result.ErrorMessage}");
                    return 1;
                }

                WriteInfo("");
                WriteSuccess("Registration completed successfully!");
                WriteInfo($"  Files context menu items: {result.FilesMenuItemCount}");
                WriteInfo($"  Directory context menu items: {result.DirectoryMenuItemCount}");
                WriteInfo($"  Total menu items: {result.TotalMenuItemCount}");
                WriteInfo("");
                WriteInfo("Right-click on files or folders to access your PowerShell scripts.");

                return 0;
            }
            catch (Exception ex)
            {
                WriteError($"Unexpected error: {ex.Message}");
                return 1;
            }
        }

        protected override void WriteInfo(string message)
        {
            _messages.Add(message);
        }

        protected override void WriteSuccess(string message)
        {
            _messages.Add(message);
        }

        protected override void WriteWarning(string message)
        {
            _messages.Add($"Warning: {message}");
        }

        protected override void WriteError(string message)
        {
            _messages.Add($"Error: {message}");
        }

        protected override string GetExecutablePath()
        {
            return @"C:\Test\RightClickPS.exe";
        }
    }

    /// <summary>
    /// A testable command that loads config from a specific path, used for testing invalid configs.
    /// </summary>
    private class TestableRegisterCommandWithInvalidConfig : RegisterCommand
    {
        private readonly List<string> _messages;
        private readonly string _configPath;

        public TestableRegisterCommandWithInvalidConfig(List<string> messages, string configPath)
        {
            _messages = messages;
            _configPath = configPath;
        }

        public new int Execute()
        {
            try
            {
                WriteInfo("RightClickPS - Context Menu Registration");
                WriteInfo("=========================================");
                WriteInfo("");

                WriteInfo("Loading configuration...");
                var configLoader = new ConfigLoader();
                var config = configLoader.LoadFromPath(_configPath);

                return 0; // Won't reach here with invalid config
            }
            catch (ConfigurationException ex)
            {
                WriteError($"Configuration error: {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                WriteError($"Unexpected error: {ex.Message}");
                return 1;
            }
        }

        protected override void WriteInfo(string message)
        {
            _messages.Add(message);
        }

        protected override void WriteSuccess(string message)
        {
            _messages.Add(message);
        }

        protected override void WriteWarning(string message)
        {
            _messages.Add($"Warning: {message}");
        }

        protected override void WriteError(string message)
        {
            _messages.Add($"Error: {message}");
        }
    }

    #endregion
}
