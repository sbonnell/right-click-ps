using RightClickPS.Commands;
using RightClickPS.Scripts;

namespace RightClickPS.Tests.Commands;

public class ExecuteCommandTests
{
    #region Argument Parsing Tests

    [Fact]
    public void Execute_WithNullArgs_ReturnsMinusOne()
    {
        // Arrange
        var command = new ExecuteCommand();

        // Act
        var result = command.Execute(null!);

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Execute_WithEmptyArgs_ReturnsMinusOne()
    {
        // Arrange
        var command = new ExecuteCommand();

        // Act
        var result = command.Execute(Array.Empty<string>());

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Execute_WithEmptyScriptPath_ReturnsMinusOne()
    {
        // Arrange
        var command = new ExecuteCommand();

        // Act
        var result = command.Execute(new[] { "" });

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Execute_WithWhitespaceScriptPath_ReturnsMinusOne()
    {
        // Arrange
        var command = new ExecuteCommand();

        // Act
        var result = command.Execute(new[] { "   " });

        // Assert
        Assert.Equal(-1, result);
    }

    #endregion

    #region Script Not Found Tests

    [Fact]
    public void Execute_WithNonExistentScript_ReturnsMinusOne()
    {
        // Arrange
        var command = new ExecuteCommand();
        var nonExistentPath = @"C:\NonExistent\Script\that\does\not\exist.ps1";

        // Act
        var result = command.Execute(new[] { nonExistentPath });

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Execute_WithNonExistentScriptAndFiles_ReturnsMinusOne()
    {
        // Arrange
        var command = new ExecuteCommand();
        var nonExistentPath = @"C:\NonExistent\Script.ps1";

        // Act
        var result = command.Execute(new[] { nonExistentPath, @"C:\file1.txt", @"C:\file2.txt" });

        // Assert
        Assert.Equal(-1, result);
    }

    #endregion

    #region Valid Script Execution Tests

    [Fact]
    public void Execute_WithValidScript_CallsScriptExecutor()
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

# This script does nothing but exit successfully
exit 0
";
            File.WriteAllText(tempScript, scriptContent);

            var command = new ExecuteCommand();

            // Act
            var result = command.Execute(new[] { tempScript });

            // Assert
            // Script executes and returns an exit code (0 for success)
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

    [Fact]
    public void Execute_WithValidScriptAndFiles_PassesFilesToExecutor()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RightClickPSTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var tempScript = Path.Combine(tempDir, "TestScript.ps1");

        try
        {
            // Create a script that verifies it received the files
            // The script will exit 0 if $SelectedFiles has elements, exit 1 otherwise
            var scriptContent = @"<#
@Name: Test Script With Files
@RunAsAdmin: false
#>

if ($SelectedFiles.Count -gt 0) {
    exit 0
} else {
    exit 1
}
";
            File.WriteAllText(tempScript, scriptContent);

            var command = new ExecuteCommand();

            // Act
            var result = command.Execute(new[] { tempScript, @"C:\FakeFile1.txt", @"C:\FakeFile2.txt" });

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

    [Fact]
    public void Execute_WithValidScriptNoMetadata_UsesDefaults()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RightClickPSTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var tempScript = Path.Combine(tempDir, "NoMetadataScript.ps1");

        try
        {
            // Create a script with no metadata block
            var scriptContent = @"# Simple script with no metadata
exit 0
";
            File.WriteAllText(tempScript, scriptContent);

            var command = new ExecuteCommand();

            // Act
            var result = command.Execute(new[] { tempScript });

            // Assert
            // Should still work with default metadata (RunAsAdmin = false)
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

    #region Error Handling Tests

    [Fact]
    public void Execute_WithScriptThatFails_ReturnsNonZeroExitCode()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RightClickPSTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var tempScript = Path.Combine(tempDir, "FailingScript.ps1");

        try
        {
            // Create a script that throws an error (which will cause a non-zero exit)
            // Note: Using throw instead of exit because PowerShell -Command exit code behavior
            // can be inconsistent with specific numeric codes. However, errors are reliably
            // reflected as non-zero exit codes.
            var scriptContent = @"<#
@Name: Failing Script
@RunAsAdmin: false
#>

throw 'This script intentionally fails'
";
            File.WriteAllText(tempScript, scriptContent);

            var command = new ExecuteCommand();

            // Act
            var result = command.Execute(new[] { tempScript });

            // Assert
            // Script execution failure should return a non-zero exit code
            Assert.NotEqual(0, result);
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

    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaultDependencies_DoesNotThrow()
    {
        // Act & Assert
        var command = new ExecuteCommand();
        Assert.NotNull(command);
    }

    [Fact]
    public void Constructor_WithNullScriptParser_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ExecuteCommand(null!, new ScriptExecutor()));
    }

    [Fact]
    public void Constructor_WithNullScriptExecutor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ExecuteCommand(new ScriptParser(), null!));
    }

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Act
        var command = new ExecuteCommand(new ScriptParser(), new ScriptExecutor());

        // Assert
        Assert.NotNull(command);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Execute_WithOnlyScriptPath_NoFilePaths_Succeeds()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RightClickPSTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var tempScript = Path.Combine(tempDir, "ScriptWithoutFiles.ps1");

        try
        {
            // Create a script that works without any files
            var scriptContent = @"<#
@Name: Script Without Files
#>

# Script that doesn't need any files
exit 0
";
            File.WriteAllText(tempScript, scriptContent);

            var command = new ExecuteCommand();

            // Act - Execute with just the script path, no file arguments
            var result = command.Execute(new[] { tempScript });

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

    [Fact]
    public void Execute_WithPathContainingSpaces_Succeeds()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RightClickPS Tests With Spaces_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var tempScript = Path.Combine(tempDir, "Script With Spaces.ps1");

        try
        {
            // Create a script in a path with spaces
            var scriptContent = @"<#
@Name: Script With Spaces
#>

exit 0
";
            File.WriteAllText(tempScript, scriptContent);

            var command = new ExecuteCommand();

            // Act
            var result = command.Execute(new[] { tempScript });

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

    [Fact]
    public void Execute_WithMultipleFilePaths_AllFilesPassedToScript()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RightClickPSTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var tempScript = Path.Combine(tempDir, "MultiFileScript.ps1");

        try
        {
            // Create a script that checks it received exactly 5 files
            var scriptContent = @"<#
@Name: Multi File Script
#>

if ($SelectedFiles.Count -eq 5) {
    exit 0
} else {
    exit $SelectedFiles.Count
}
";
            File.WriteAllText(tempScript, scriptContent);

            var command = new ExecuteCommand();

            // Act - Pass 5 file paths
            var result = command.Execute(new[]
            {
                tempScript,
                @"C:\File1.txt",
                @"C:\File2.txt",
                @"C:\File3.txt",
                @"C:\File4.txt",
                @"C:\File5.txt"
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
