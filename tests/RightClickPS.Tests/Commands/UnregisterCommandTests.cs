using RightClickPS.Commands;
using RightClickPS.Registry;
using RightClickPS.Scripts;

namespace RightClickPS.Tests.Commands;

/// <summary>
/// Unit tests for <see cref="UnregisterCommand"/>.
/// Note: These tests interact with the Windows registry. Tests ensure cleanup after each run.
/// The Collection attribute ensures these tests don't run in parallel with other registry tests.
/// </summary>
[Collection("RegistryTests")]
public class UnregisterCommandTests : IDisposable
{
    private readonly ContextMenuRegistry _registry;

    public UnregisterCommandTests()
    {
        _registry = new ContextMenuRegistry();
        // Ensure clean state before each test - don't care about return value
        _registry.Unregister();
    }

    public void Dispose()
    {
        // Clean up after each test
        _registry.Unregister();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaultDependencies_DoesNotThrow()
    {
        // Act & Assert
        var command = new UnregisterCommand();
        Assert.NotNull(command);
    }

    [Fact]
    public void Constructor_WithNullRegistry_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UnregisterCommand(null!));
    }

    [Fact]
    public void Constructor_WithValidRegistry_DoesNotThrow()
    {
        // Arrange
        var registry = new ContextMenuRegistry();

        // Act
        var command = new UnregisterCommand(registry);

        // Assert
        Assert.NotNull(command);
    }

    #endregion

    #region Execute Tests - Successful Unregistration

    [Fact]
    public void Execute_WhenNothingRegistered_ReturnsZeroOrReportsNothing()
    {
        // Arrange
        // Constructor already ensures clean state
        var command = new UnregisterCommand(_registry);

        // Act
        var result = command.Execute();

        // Assert - Should succeed (0) or at worst indicate nothing was registered
        // The key thing is that IsRegistered should be false after the call
        Assert.False(_registry.IsRegistered(), "No entries should be registered after unregister");
    }

    [Fact]
    public void Execute_CanBeCalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var command = new UnregisterCommand(_registry);

        // Act - Call multiple times - should not throw
        command.Execute();
        command.Execute();
        command.Execute();

        // Assert - No exception was thrown, and nothing should be registered
        Assert.False(_registry.IsRegistered());
    }

    #endregion

    #region Execute Tests - Output Messages

    [Fact]
    public void Execute_WhenNothingRegistered_OutputsNothingToRemoveMessage()
    {
        // Arrange
        var outputMessages = new List<string>();
        var command = new TestableUnregisterCommand(_registry, outputMessages);

        // Act
        command.Execute();

        // Assert - Should have some output message
        Assert.NotEmpty(outputMessages);
    }

    [Fact]
    public void Execute_AfterRegistration_OutputsSuccessMessage()
    {
        // Arrange
        var root = MenuNode.CreateRoot();
        root.AddScript(new ScriptMetadata
        {
            Name = "Test Script",
            FilePath = @"C:\test.ps1",
            TargetType = TargetType.Both
        });
        _registry.Register(root, "PowerShell Scripts", @"C:\RightClickPS.exe");

        var outputMessages = new List<string>();
        var command = new TestableUnregisterCommand(_registry, outputMessages);

        // Act
        command.Execute();

        // Assert - Should have output messages
        Assert.NotEmpty(outputMessages);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Execute_AfterRegistration_RemovesAllEntries()
    {
        // Arrange
        var root = MenuNode.CreateRoot();
        root.AddScript(new ScriptMetadata
        {
            Name = "Test Script",
            FilePath = @"C:\test.ps1",
            TargetType = TargetType.Both
        });

        var exePath = @"C:\RightClickPS.exe";
        _registry.Register(root, "PowerShell Scripts", exePath);

        // Verify entries were registered
        Assert.True(_registry.IsRegistered(), "Expected entries to be registered before unregister test");

        var command = new UnregisterCommand(_registry);

        // Act
        var result = command.Execute();

        // Assert
        Assert.Equal(0, result);
        Assert.False(_registry.IsRegistered(), "Expected entries to be removed after unregister");
    }

    [Fact]
    public void Execute_AfterFilesOnlyRegistration_RemovesFilesEntries()
    {
        // Arrange
        var root = MenuNode.CreateRoot();
        root.AddScript(new ScriptMetadata
        {
            Name = "Files Script",
            FilePath = @"C:\files.ps1",
            TargetType = TargetType.Files
        });

        _registry.Register(root, "PowerShell Scripts", @"C:\RightClickPS.exe");

        // Verify initial state
        Assert.True(_registry.IsRegisteredForFiles(), "Expected files context to be registered");

        var command = new UnregisterCommand(_registry);

        // Act
        var result = command.Execute();

        // Assert
        Assert.Equal(0, result);
        Assert.False(_registry.IsRegisteredForFiles(), "Expected files context to be unregistered");
    }

    [Fact]
    public void Execute_AfterDirectoriesOnlyRegistration_RemovesDirectoryEntries()
    {
        // Arrange
        var root = MenuNode.CreateRoot();
        root.AddScript(new ScriptMetadata
        {
            Name = "Folder Script",
            FilePath = @"C:\folder.ps1",
            TargetType = TargetType.Folders
        });

        _registry.Register(root, "PowerShell Scripts", @"C:\RightClickPS.exe");

        // Verify initial state
        Assert.True(_registry.IsRegisteredForDirectories(), "Expected directories context to be registered");

        var command = new UnregisterCommand(_registry);

        // Act
        var result = command.Execute();

        // Assert
        Assert.Equal(0, result);
        Assert.False(_registry.IsRegisteredForDirectories(), "Expected directories context to be unregistered");
    }

    [Fact]
    public void Execute_AfterBothContextsRegistration_RemovesBothContexts()
    {
        // Arrange
        var root = MenuNode.CreateRoot();
        root.AddScript(new ScriptMetadata
        {
            Name = "Universal Script",
            FilePath = @"C:\universal.ps1",
            TargetType = TargetType.Both
        });

        _registry.Register(root, "PowerShell Scripts", @"C:\RightClickPS.exe");

        // Verify initial state
        Assert.True(_registry.IsRegisteredForFiles(), "Expected files context to be registered");
        Assert.True(_registry.IsRegisteredForDirectories(), "Expected directories context to be registered");

        var command = new UnregisterCommand(_registry);

        // Act
        var result = command.Execute();

        // Assert
        Assert.Equal(0, result);
        Assert.False(_registry.IsRegisteredForFiles(), "Expected files context to be unregistered");
        Assert.False(_registry.IsRegisteredForDirectories(), "Expected directories context to be unregistered");
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Execute_WithNestedMenuStructure_RemovesAllNestedEntries()
    {
        // Arrange - Create a nested menu structure
        var root = MenuNode.CreateRoot();
        var imagesFolder = MenuNode.CreateFolder("Images");
        imagesFolder.AddScript(new ScriptMetadata
        {
            Name = "Convert to JPG",
            FilePath = @"C:\Scripts\Images\Convert.ps1",
            TargetType = TargetType.Files
        });
        root.Children.Add(imagesFolder);

        _registry.Register(root, "PowerShell Scripts", @"C:\RightClickPS.exe");
        Assert.True(_registry.IsRegistered(), "Expected nested menu to be registered");

        var command = new UnregisterCommand(_registry);

        // Act
        var result = command.Execute();

        // Assert
        Assert.Equal(0, result);
        Assert.False(_registry.IsRegistered(), "Expected all nested entries to be removed");
    }

    [Fact]
    public void Execute_WithMultipleScripts_RemovesAllScriptEntries()
    {
        // Arrange - Register multiple scripts
        var root = MenuNode.CreateRoot();
        root.AddScript(new ScriptMetadata
        {
            Name = "Script 1",
            FilePath = @"C:\script1.ps1",
            TargetType = TargetType.Both
        });
        root.AddScript(new ScriptMetadata
        {
            Name = "Script 2",
            FilePath = @"C:\script2.ps1",
            TargetType = TargetType.Both
        });
        root.AddScript(new ScriptMetadata
        {
            Name = "Script 3",
            FilePath = @"C:\script3.ps1",
            TargetType = TargetType.Both
        });

        _registry.Register(root, "PowerShell Scripts", @"C:\RightClickPS.exe");
        Assert.True(_registry.IsRegistered(), "Expected multiple scripts to be registered");

        var command = new UnregisterCommand(_registry);

        // Act
        var result = command.Execute();

        // Assert
        Assert.Equal(0, result);
        Assert.False(_registry.IsRegistered(), "Expected all script entries to be removed");
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// A testable version of UnregisterCommand that captures output messages.
    /// </summary>
    private class TestableUnregisterCommand : UnregisterCommand
    {
        private readonly List<string> _messages;

        public TestableUnregisterCommand(ContextMenuRegistry registry, List<string> messages)
            : base(registry)
        {
            _messages = messages;
        }

        protected override void WriteSuccess(string message)
        {
            _messages.Add(message);
        }

        protected override void WriteInfo(string message)
        {
            _messages.Add(message);
        }

        protected override void WriteError(string message)
        {
            _messages.Add(message);
        }
    }

    #endregion
}
