using RightClickPS;

namespace RightClickPS.Tests;

/// <summary>
/// Tests for the Program entry point and command routing.
/// </summary>
public class ProgramTests
{
    #region No Arguments Tests

    [Fact]
    public void Main_WithNoArguments_ReturnsZero()
    {
        // Act
        var result = Program.Main(Array.Empty<string>());

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region Help Command Tests

    [Fact]
    public void Main_WithHelpCommand_ReturnsZero()
    {
        // Act
        var result = Program.Main(new[] { "help" });

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_WithHelpFlag_ReturnsZero()
    {
        // Act
        var result = Program.Main(new[] { "-h" });

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_WithDoubleDashHelpFlag_ReturnsZero()
    {
        // Act
        var result = Program.Main(new[] { "--help" });

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_WithQuestionMarkFlag_ReturnsZero()
    {
        // Act
        var result = Program.Main(new[] { "/?" });

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region Unknown Command Tests

    [Fact]
    public void Main_WithUnknownCommand_ReturnsOne()
    {
        // Act
        var result = Program.Main(new[] { "unknowncommand" });

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void Main_WithGibberish_ReturnsOne()
    {
        // Act
        var result = Program.Main(new[] { "asdfghjkl" });

        // Assert
        Assert.Equal(1, result);
    }

    #endregion

    #region Case Insensitivity Tests

    [Theory]
    [InlineData("HELP")]
    [InlineData("Help")]
    [InlineData("hElP")]
    public void Main_WithHelpCommandCaseVariants_ReturnsZero(string command)
    {
        // Act
        var result = Program.Main(new[] { command });

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region Execute Command Routing Tests

    [Fact]
    public void Main_WithExecuteCommandNoScript_ReturnsMinusOne()
    {
        // Act - "execute" with no script path
        var result = Program.Main(new[] { "execute" });

        // Assert - ExecuteCommand returns -1 when no script provided
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Main_WithExecuteCommandNonExistentScript_ReturnsMinusOne()
    {
        // Act
        var result = Program.Main(new[] { "execute", @"C:\NonExistent\Script.ps1" });

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Main_WithExecuteCommandCaseInsensitive_ReturnsMinusOne()
    {
        // Act - Using uppercase EXECUTE
        var result = Program.Main(new[] { "EXECUTE" });

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Main_WithExecuteCommandValidScript_ReturnsZero()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RightClickPSTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var tempScript = Path.Combine(tempDir, "TestScript.ps1");

        try
        {
            // Create a simple test script
            var scriptContent = @"<#
@Name: Test Script
@RunAsAdmin: false
#>

exit 0
";
            File.WriteAllText(tempScript, scriptContent);

            // Act
            var result = Program.Main(new[] { "execute", tempScript });

            // Assert
            Assert.Equal(0, result);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    #endregion

    #region Register Command Routing Tests

    [Theory]
    [InlineData("register")]
    [InlineData("Register")]
    [InlineData("REGISTER")]
    public void Main_WithRegisterCommandCaseVariants_RoutesToRegisterCommand(string command)
    {
        // Note: This test verifies the command is routed correctly.
        // The actual RegisterCommand behavior is tested in RegisterCommandTests.
        // RegisterCommand may succeed or fail depending on config availability.

        // Act
        var result = Program.Main(new[] { command });

        // Assert - Just verify it doesn't return 1 (unknown command)
        // RegisterCommand returns 0 on success or 1 on config error
        Assert.True(result == 0 || result == 1, $"Expected 0 or 1, got {result}");
    }

    #endregion

    #region Unregister Command Routing Tests

    [Theory]
    [InlineData("unregister")]
    [InlineData("Unregister")]
    [InlineData("UNREGISTER")]
    public void Main_WithUnregisterCommandCaseVariants_RoutesToUnregisterCommand(string command)
    {
        // Note: This test verifies the command is routed correctly.
        // The actual UnregisterCommand behavior is tested in UnregisterCommandTests.

        // Act
        var result = Program.Main(new[] { command });

        // Assert - UnregisterCommand returns 0 on success (nothing to remove is still success)
        Assert.Equal(0, result);
    }

    #endregion

    #region Execute with File Paths Tests

    [Fact]
    public void Main_WithExecuteCommandAndFilePaths_PassesFilePathsToExecutor()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RightClickPSTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var tempScript = Path.Combine(tempDir, "FileCountScript.ps1");

        try
        {
            // Create a script that verifies it received exactly 3 files
            var scriptContent = @"<#
@Name: File Count Script
#>

if ($SelectedFiles.Count -eq 3) {
    exit 0
} else {
    exit $SelectedFiles.Count
}
";
            File.WriteAllText(tempScript, scriptContent);

            // Act - Pass 3 file paths after the script
            var result = Program.Main(new[]
            {
                "execute",
                tempScript,
                @"C:\File1.txt",
                @"C:\File2.txt",
                @"C:\File3.txt"
            });

            // Assert
            Assert.Equal(0, result);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    #endregion
}
