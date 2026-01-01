using RightClickPS.Scripts;

namespace RightClickPS.Tests.Scripts;

public class ScriptExecutorTests
{
    private readonly ScriptExecutor _executor;

    public ScriptExecutorTests()
    {
        _executor = new ScriptExecutor();
    }

    #region BuildSelectedFilesArray Tests

    [Fact]
    public void BuildSelectedFilesArray_WithEmptyList_ReturnsEmptyArray()
    {
        // Arrange
        var files = new List<string>();

        // Act
        var result = _executor.BuildSelectedFilesArray(files);

        // Assert
        Assert.Equal("@()", result);
    }

    [Fact]
    public void BuildSelectedFilesArray_WithSingleFile_ReturnsCorrectFormat()
    {
        // Arrange
        var files = new List<string> { @"C:\Users\Test\file.txt" };

        // Act
        var result = _executor.BuildSelectedFilesArray(files);

        // Assert
        Assert.Equal(@"@('C:\Users\Test\file.txt')", result);
    }

    [Fact]
    public void BuildSelectedFilesArray_WithMultipleFiles_ReturnsCorrectFormat()
    {
        // Arrange
        var files = new List<string>
        {
            @"C:\Users\Test\file1.txt",
            @"C:\Users\Test\file2.txt",
            @"C:\Users\Test\file3.txt"
        };

        // Act
        var result = _executor.BuildSelectedFilesArray(files);

        // Assert
        Assert.Equal(@"@('C:\Users\Test\file1.txt','C:\Users\Test\file2.txt','C:\Users\Test\file3.txt')", result);
    }

    [Fact]
    public void BuildSelectedFilesArray_WithNull_ReturnsEmptyArray()
    {
        // Arrange & Act
        var result = _executor.BuildSelectedFilesArray(null!);

        // Assert
        Assert.Equal("@()", result);
    }

    #endregion

    #region EscapeForPowerShell Tests

    [Fact]
    public void EscapeForPowerShell_WithSimplePath_ReturnsUnchanged()
    {
        // Arrange
        var path = @"C:\Users\Test\file.txt";

        // Act
        var result = _executor.EscapeForPowerShell(path);

        // Assert
        Assert.Equal(path, result);
    }

    [Fact]
    public void EscapeForPowerShell_WithSingleQuote_EscapesByDoubling()
    {
        // Arrange
        var path = @"C:\Users\Test\It's a file.txt";

        // Act
        var result = _executor.EscapeForPowerShell(path);

        // Assert
        Assert.Equal(@"C:\Users\Test\It''s a file.txt", result);
    }

    [Fact]
    public void EscapeForPowerShell_WithMultipleSingleQuotes_EscapesAll()
    {
        // Arrange
        var path = @"C:\Users\Test\It's a 'special' file.txt";

        // Act
        var result = _executor.EscapeForPowerShell(path);

        // Assert
        Assert.Equal(@"C:\Users\Test\It''s a ''special'' file.txt", result);
    }

    [Fact]
    public void EscapeForPowerShell_WithEmptyString_ReturnsEmpty()
    {
        // Arrange & Act
        var result = _executor.EscapeForPowerShell(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void EscapeForPowerShell_WithNull_ReturnsEmpty()
    {
        // Arrange & Act
        var result = _executor.EscapeForPowerShell(null!);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void EscapeForPowerShell_WithSpaces_PreservesSpaces()
    {
        // Arrange
        var path = @"C:\Program Files\My App\file.txt";

        // Act
        var result = _executor.EscapeForPowerShell(path);

        // Assert
        Assert.Equal(path, result);
    }

    [Fact]
    public void EscapeForPowerShell_WithSpecialCharacters_PreservesCharacters()
    {
        // Arrange - testing various special characters that don't need escaping in single-quoted strings
        var path = @"C:\Users\Test\file[1](2){3}$var%name#hash@at.txt";

        // Act
        var result = _executor.EscapeForPowerShell(path);

        // Assert
        Assert.Equal(path, result);
    }

    #endregion

    #region BuildPowerShellCommand Tests

    [Fact]
    public void BuildPowerShellCommand_WithSingleFile_ReturnsCorrectCommand()
    {
        // Arrange
        var scriptPath = @"C:\Scripts\test.ps1";
        var files = new List<string> { @"C:\Users\Test\file.txt" };

        // Act
        var result = _executor.BuildPowerShellCommand(scriptPath, files);

        // Assert
        Assert.Equal(@"$SelectedFiles = @('C:\Users\Test\file.txt'); & 'C:\Scripts\test.ps1'", result);
    }

    [Fact]
    public void BuildPowerShellCommand_WithMultipleFiles_ReturnsCorrectCommand()
    {
        // Arrange
        var scriptPath = @"C:\Scripts\test.ps1";
        var files = new List<string>
        {
            @"C:\Users\Test\file1.txt",
            @"C:\Users\Test\file2.txt"
        };

        // Act
        var result = _executor.BuildPowerShellCommand(scriptPath, files);

        // Assert
        Assert.Equal(@"$SelectedFiles = @('C:\Users\Test\file1.txt','C:\Users\Test\file2.txt'); & 'C:\Scripts\test.ps1'", result);
    }

    [Fact]
    public void BuildPowerShellCommand_WithNoFiles_ReturnsCommandWithEmptyArray()
    {
        // Arrange
        var scriptPath = @"C:\Scripts\test.ps1";
        var files = new List<string>();

        // Act
        var result = _executor.BuildPowerShellCommand(scriptPath, files);

        // Assert
        Assert.Equal(@"$SelectedFiles = @(); & 'C:\Scripts\test.ps1'", result);
    }

    [Fact]
    public void BuildPowerShellCommand_WithScriptPathContainingSingleQuote_EscapesPath()
    {
        // Arrange
        var scriptPath = @"C:\Scripts\It's a script.ps1";
        var files = new List<string> { @"C:\file.txt" };

        // Act
        var result = _executor.BuildPowerShellCommand(scriptPath, files);

        // Assert
        Assert.Equal(@"$SelectedFiles = @('C:\file.txt'); & 'C:\Scripts\It''s a script.ps1'", result);
    }

    [Fact]
    public void BuildPowerShellCommand_WithFilesContainingSingleQuotes_EscapesFiles()
    {
        // Arrange
        var scriptPath = @"C:\Scripts\test.ps1";
        var files = new List<string> { @"C:\Users\Test\John's file.txt" };

        // Act
        var result = _executor.BuildPowerShellCommand(scriptPath, files);

        // Assert
        Assert.Equal(@"$SelectedFiles = @('C:\Users\Test\John''s file.txt'); & 'C:\Scripts\test.ps1'", result);
    }

    [Fact]
    public void BuildPowerShellCommand_WithSpacesInPath_PreservesSpaces()
    {
        // Arrange
        var scriptPath = @"C:\Program Files\My Scripts\convert.ps1";
        var files = new List<string> { @"C:\Program Files\My App\document.txt" };

        // Act
        var result = _executor.BuildPowerShellCommand(scriptPath, files);

        // Assert
        Assert.Equal(@"$SelectedFiles = @('C:\Program Files\My App\document.txt'); & 'C:\Program Files\My Scripts\convert.ps1'", result);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaultPath_DoesNotThrow()
    {
        // Act & Assert
        var executor = new ScriptExecutor();
        Assert.NotNull(executor);
    }

    [Fact]
    public void Constructor_WithCustomPath_DoesNotThrow()
    {
        // Act & Assert
        var executor = new ScriptExecutor(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe");
        Assert.NotNull(executor);
    }

    #endregion

    #region BuildSelectedFilesArray Edge Cases

    [Fact]
    public void BuildSelectedFilesArray_WithUNCPath_HandlesCorrectly()
    {
        // Arrange
        var files = new List<string> { @"\\server\share\file.txt" };

        // Act
        var result = _executor.BuildSelectedFilesArray(files);

        // Assert
        Assert.Equal(@"@('\\server\share\file.txt')", result);
    }

    [Fact]
    public void BuildSelectedFilesArray_WithMixedPaths_HandlesAllCorrectly()
    {
        // Arrange
        var files = new List<string>
        {
            @"C:\Local\file.txt",
            @"\\server\share\file.txt",
            @"D:\Another Drive\file.txt"
        };

        // Act
        var result = _executor.BuildSelectedFilesArray(files);

        // Assert
        Assert.Equal(@"@('C:\Local\file.txt','\\server\share\file.txt','D:\Another Drive\file.txt')", result);
    }

    [Fact]
    public void BuildSelectedFilesArray_WithLongPath_HandlesCorrectly()
    {
        // Arrange
        var longPath = @"C:\Very\Long\Directory\Structure\That\Goes\On\And\On\For\A\While\file.txt";
        var files = new List<string> { longPath };

        // Act
        var result = _executor.BuildSelectedFilesArray(files);

        // Assert
        Assert.Equal($"@('{longPath}')", result);
    }

    [Fact]
    public void BuildSelectedFilesArray_WithEmptyStringsInList_IncludesEmptyStrings()
    {
        // Arrange
        var files = new List<string> { "", @"C:\file.txt" };

        // Act
        var result = _executor.BuildSelectedFilesArray(files);

        // Assert
        Assert.Equal(@"@('','C:\file.txt')", result);
    }

    #endregion

    #region EscapeForPowerShell Edge Cases

    [Fact]
    public void EscapeForPowerShell_WithOnlySingleQuotes_EscapesAll()
    {
        // Arrange
        var input = "'''";

        // Act
        var result = _executor.EscapeForPowerShell(input);

        // Assert
        Assert.Equal("''''''", result);
    }

    [Fact]
    public void EscapeForPowerShell_WithConsecutiveSingleQuotes_EscapesAll()
    {
        // Arrange
        var input = @"C:\Test''Name\file.txt";

        // Act
        var result = _executor.EscapeForPowerShell(input);

        // Assert
        Assert.Equal(@"C:\Test''''Name\file.txt", result);
    }

    [Fact]
    public void EscapeForPowerShell_WithDoubleQuotes_PreservesDoubleQuotes()
    {
        // Arrange - double quotes don't need escaping in single-quoted PowerShell strings
        var input = @"C:\Test ""Name""\file.txt";

        // Act
        var result = _executor.EscapeForPowerShell(input);

        // Assert
        Assert.Equal(@"C:\Test ""Name""\file.txt", result);
    }

    [Fact]
    public void EscapeForPowerShell_WithBacktick_PreservesBacktick()
    {
        // Arrange - backticks are literal in single-quoted strings
        var input = @"C:\Test`Name\file.txt";

        // Act
        var result = _executor.EscapeForPowerShell(input);

        // Assert
        Assert.Equal(@"C:\Test`Name\file.txt", result);
    }

    [Fact]
    public void EscapeForPowerShell_WithDollarSign_PreservesDollarSign()
    {
        // Arrange - dollar signs are literal in single-quoted strings (no variable expansion)
        var input = @"C:\Test$Name\file.txt";

        // Act
        var result = _executor.EscapeForPowerShell(input);

        // Assert
        Assert.Equal(@"C:\Test$Name\file.txt", result);
    }

    #endregion

    #region Execute Method Tests (Limited without mocking)

    [Fact]
    public void Execute_WithEmptyScriptPath_ReturnsMinusOne()
    {
        // Arrange & Act
        var result = _executor.Execute("", new List<string>());

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Execute_WithWhitespaceScriptPath_ReturnsMinusOne()
    {
        // Arrange & Act
        var result = _executor.Execute("   ", new List<string>());

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Execute_WithNonExistentScript_ReturnsMinusOne()
    {
        // Arrange
        var nonExistentPath = @"C:\NonExistent\Script\that\does\not\exist.ps1";

        // Act
        var result = _executor.Execute(nonExistentPath, new List<string>());

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Execute_WithNullFilesList_DoesNotThrow()
    {
        // Arrange
        var nonExistentPath = @"C:\NonExistent\Script.ps1";

        // Act
        var result = _executor.Execute(nonExistentPath, null!);

        // Assert
        // Should return -1 due to file not existing, not due to null files
        Assert.Equal(-1, result);
    }

    #endregion
}
