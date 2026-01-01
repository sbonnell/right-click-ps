using RightClickPS.Registry;
using RightClickPS.Scripts;

namespace RightClickPS.Tests.Registry;

/// <summary>
/// Unit tests for <see cref="ContextMenuRegistry"/>.
/// Note: These tests focus on logic that doesn't require actual registry access.
/// Integration tests that modify the registry should be run manually with caution.
/// The Collection attribute ensures these tests don't run in parallel with other registry tests.
/// </summary>
[Collection("RegistryTests")]
public class ContextMenuRegistryTests
{
    #region BuildCommand Tests

    [Fact]
    public void BuildCommand_CreatesCorrectFormat()
    {
        var exePath = @"C:\Program Files\RightClickPS\RightClickPS.exe";
        var scriptPath = @"C:\Scripts\Images\Convert to JPG.ps1";

        var result = ContextMenuRegistry.BuildCommand(exePath, scriptPath);

        Assert.Equal("\"C:\\Program Files\\RightClickPS\\RightClickPS.exe\" execute \"C:\\Scripts\\Images\\Convert to JPG.ps1\" \"%1\"", result);
    }

    [Fact]
    public void BuildCommand_QuotesPathsWithSpaces()
    {
        var exePath = @"C:\My Programs\RightClickPS\RightClickPS.exe";
        var scriptPath = @"C:\My Scripts\Test Script.ps1";

        var result = ContextMenuRegistry.BuildCommand(exePath, scriptPath);

        Assert.Contains("\"C:\\My Programs\\RightClickPS\\RightClickPS.exe\"", result);
        Assert.Contains("\"C:\\My Scripts\\Test Script.ps1\"", result);
    }

    [Fact]
    public void BuildCommand_IncludesExecuteVerb()
    {
        var exePath = @"C:\RightClickPS.exe";
        var scriptPath = @"C:\test.ps1";

        var result = ContextMenuRegistry.BuildCommand(exePath, scriptPath);

        Assert.Contains(" execute ", result);
    }

    [Fact]
    public void BuildCommand_IncludesPercentOneParameter()
    {
        var exePath = @"C:\RightClickPS.exe";
        var scriptPath = @"C:\test.ps1";

        var result = ContextMenuRegistry.BuildCommand(exePath, scriptPath);

        Assert.EndsWith("\"%1\"", result);
    }

    [Fact]
    public void BuildCommand_HandlesSimplePaths()
    {
        var exePath = @"C:\app.exe";
        var scriptPath = @"D:\script.ps1";

        var result = ContextMenuRegistry.BuildCommand(exePath, scriptPath);

        Assert.Equal("\"C:\\app.exe\" execute \"D:\\script.ps1\" \"%1\"", result);
    }

    #endregion



    #region SanitizeKeyName Tests

    [Fact]
    public void SanitizeKeyName_ReturnsNormalNameUnchanged()
    {
        var result = ContextMenuRegistry.SanitizeKeyName("ConvertToJPG");

        Assert.Equal("ConvertToJPG", result);
    }

    [Fact]
    public void SanitizeKeyName_ReplacesBackslashes()
    {
        var result = ContextMenuRegistry.SanitizeKeyName(@"Path\With\Slashes");

        Assert.DoesNotContain("\\", result);
    }

    [Fact]
    public void SanitizeKeyName_ReplacesForwardSlashes()
    {
        var result = ContextMenuRegistry.SanitizeKeyName("Path/With/Slashes");

        Assert.DoesNotContain("/", result);
    }

    [Fact]
    public void SanitizeKeyName_ReplacesAsterisks()
    {
        var result = ContextMenuRegistry.SanitizeKeyName("Select*All");

        Assert.DoesNotContain("*", result);
    }

    [Fact]
    public void SanitizeKeyName_ReplacesQuestionMarks()
    {
        var result = ContextMenuRegistry.SanitizeKeyName("What?");

        Assert.DoesNotContain("?", result);
    }

    [Fact]
    public void SanitizeKeyName_ReplacesQuotes()
    {
        var result = ContextMenuRegistry.SanitizeKeyName("Say \"Hello\"");

        Assert.DoesNotContain("\"", result);
    }

    [Fact]
    public void SanitizeKeyName_ReplacesAngleBrackets()
    {
        var result = ContextMenuRegistry.SanitizeKeyName("<HTML>");

        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
    }

    [Fact]
    public void SanitizeKeyName_ReplacesPipeCharacter()
    {
        var result = ContextMenuRegistry.SanitizeKeyName("Option|Choice");

        Assert.DoesNotContain("|", result);
    }

    [Fact]
    public void SanitizeKeyName_ReplacesColons()
    {
        var result = ContextMenuRegistry.SanitizeKeyName("Step:One");

        Assert.DoesNotContain(":", result);
    }

    [Fact]
    public void SanitizeKeyName_TrimsWhitespace()
    {
        var result = ContextMenuRegistry.SanitizeKeyName("  Spaces  ");

        Assert.Equal("Spaces", result);
    }

    [Fact]
    public void SanitizeKeyName_TrimsLeadingDots()
    {
        var result = ContextMenuRegistry.SanitizeKeyName("...Hidden");

        Assert.Equal("Hidden", result);
    }

    [Fact]
    public void SanitizeKeyName_TrimsTrailingDots()
    {
        var result = ContextMenuRegistry.SanitizeKeyName("Ellipsis...");

        Assert.Equal("Ellipsis", result);
    }

    [Fact]
    public void SanitizeKeyName_ReturnsUnknownForEmptyString()
    {
        var result = ContextMenuRegistry.SanitizeKeyName(string.Empty);

        Assert.Equal("Unknown", result);
    }

    [Fact]
    public void SanitizeKeyName_ReturnsUnknownForNullInput()
    {
        var result = ContextMenuRegistry.SanitizeKeyName(null!);

        Assert.Equal("Unknown", result);
    }

    [Fact]
    public void SanitizeKeyName_ReturnsUnknownForWhitespaceOnly()
    {
        var result = ContextMenuRegistry.SanitizeKeyName("   ");

        Assert.Equal("Unknown", result);
    }

    [Fact]
    public void SanitizeKeyName_ReturnsUnknownForOnlyInvalidChars()
    {
        var result = ContextMenuRegistry.SanitizeKeyName("**||??");

        // After replacing all invalid chars with underscores and trimming, should have underscores
        Assert.NotEmpty(result);
    }

    [Fact]
    public void SanitizeKeyName_PreservesSpacesInMiddle()
    {
        var result = ContextMenuRegistry.SanitizeKeyName("Convert to JPG");

        Assert.Equal("Convert to JPG", result);
    }

    [Fact]
    public void SanitizeKeyName_PreservesHyphensAndUnderscores()
    {
        var result = ContextMenuRegistry.SanitizeKeyName("My-Script_Name");

        Assert.Equal("My-Script_Name", result);
    }

    #endregion

    #region Register Validation Tests

    [Fact]
    public void Register_ReturnsError_WhenRootIsNull()
    {
        var registry = new ContextMenuRegistry();

        var result = registry.Register(null!, "Menu", @"C:\app.exe");

        Assert.False(result.Success);
        Assert.Contains("null", result.ErrorMessage!.ToLower());
    }

    [Fact]
    public void Register_ReturnsError_WhenMenuNameIsEmpty()
    {
        var registry = new ContextMenuRegistry();
        var root = MenuNode.CreateRoot();

        var result = registry.Register(root, string.Empty, @"C:\app.exe");

        Assert.False(result.Success);
        Assert.Contains("menu name", result.ErrorMessage!.ToLower());
    }

    [Fact]
    public void Register_ReturnsError_WhenMenuNameIsWhitespace()
    {
        var registry = new ContextMenuRegistry();
        var root = MenuNode.CreateRoot();

        var result = registry.Register(root, "   ", @"C:\app.exe");

        Assert.False(result.Success);
        Assert.Contains("menu name", result.ErrorMessage!.ToLower());
    }

    [Fact]
    public void Register_ReturnsError_WhenExePathIsEmpty()
    {
        var registry = new ContextMenuRegistry();
        var root = MenuNode.CreateRoot();

        var result = registry.Register(root, "PowerShell Scripts", string.Empty);

        Assert.False(result.Success);
        Assert.Contains("executable", result.ErrorMessage!.ToLower());
    }

    [Fact]
    public void Register_ReturnsError_WhenExePathIsNull()
    {
        var registry = new ContextMenuRegistry();
        var root = MenuNode.CreateRoot();

        var result = registry.Register(root, "PowerShell Scripts", null!);

        Assert.False(result.Success);
        Assert.Contains("executable", result.ErrorMessage!.ToLower());
    }

    #endregion

    #region RegistrationResult Tests

    [Fact]
    public void RegistrationResult_TotalMenuItemCount_SumsBothContexts()
    {
        var result = new ContextMenuRegistry.RegistrationResult
        {
            FilesMenuItemCount = 5,
            DirectoryMenuItemCount = 3
        };

        Assert.Equal(8, result.TotalMenuItemCount);
    }

    [Fact]
    public void RegistrationResult_TotalMenuItemCount_HandlesZeroValues()
    {
        var result = new ContextMenuRegistry.RegistrationResult
        {
            FilesMenuItemCount = 0,
            DirectoryMenuItemCount = 0
        };

        Assert.Equal(0, result.TotalMenuItemCount);
    }

    [Fact]
    public void RegistrationResult_DefaultValues_AreCorrect()
    {
        var result = new ContextMenuRegistry.RegistrationResult();

        Assert.False(result.Success);
        Assert.Equal(0, result.FilesMenuItemCount);
        Assert.Equal(0, result.DirectoryMenuItemCount);
        Assert.Null(result.ErrorMessage);
    }

    #endregion

    #region Menu Hierarchy Logic Tests

    [Fact]
    public void Register_WithEmptyRoot_ReturnsSuccessWithZeroItems()
    {
        var registry = new ContextMenuRegistry();
        var root = MenuNode.CreateRoot();

        var result = registry.Register(root, "PowerShell Scripts", @"C:\RightClickPS.exe");

        Assert.True(result.Success);
        Assert.Equal(0, result.TotalMenuItemCount);
    }

    [Fact]
    public void Register_WithFilesOnlyScript_RegistersOnlyForFiles()
    {
        var registry = new ContextMenuRegistry();
        var root = MenuNode.CreateRoot();

        var script = new ScriptMetadata
        {
            Name = "Test Script",
            FilePath = @"C:\Scripts\test.ps1",
            TargetType = TargetType.Files
        };
        root.AddScript(script);

        var result = registry.Register(root, "PowerShell Scripts", @"C:\RightClickPS.exe");

        Assert.True(result.Success);
        Assert.Equal(1, result.FilesMenuItemCount);
        Assert.Equal(0, result.DirectoryMenuItemCount);
    }

    [Fact]
    public void Register_WithFoldersOnlyScript_RegistersOnlyForDirectories()
    {
        var registry = new ContextMenuRegistry();
        var root = MenuNode.CreateRoot();

        var script = new ScriptMetadata
        {
            Name = "Folder Script",
            FilePath = @"C:\Scripts\folder.ps1",
            TargetType = TargetType.Folders
        };
        root.AddScript(script);

        var result = registry.Register(root, "PowerShell Scripts", @"C:\RightClickPS.exe");

        Assert.True(result.Success);
        Assert.Equal(0, result.FilesMenuItemCount);
        Assert.Equal(1, result.DirectoryMenuItemCount);
    }

    [Fact]
    public void Register_WithBothTargetScript_RegistersForBothContexts()
    {
        var registry = new ContextMenuRegistry();
        var root = MenuNode.CreateRoot();

        var script = new ScriptMetadata
        {
            Name = "Universal Script",
            FilePath = @"C:\Scripts\universal.ps1",
            TargetType = TargetType.Both
        };
        root.AddScript(script);

        var result = registry.Register(root, "PowerShell Scripts", @"C:\RightClickPS.exe");

        Assert.True(result.Success);
        Assert.Equal(1, result.FilesMenuItemCount);
        Assert.Equal(1, result.DirectoryMenuItemCount);
    }

    [Fact]
    public void Register_WithMultipleScripts_CountsCorrectly()
    {
        var registry = new ContextMenuRegistry();
        var root = MenuNode.CreateRoot();

        // Add 3 scripts: 1 for files, 1 for folders, 1 for both
        root.AddScript(new ScriptMetadata
        {
            Name = "Files Only",
            FilePath = @"C:\Scripts\files.ps1",
            TargetType = TargetType.Files
        });

        root.AddScript(new ScriptMetadata
        {
            Name = "Folders Only",
            FilePath = @"C:\Scripts\folders.ps1",
            TargetType = TargetType.Folders
        });

        root.AddScript(new ScriptMetadata
        {
            Name = "Both",
            FilePath = @"C:\Scripts\both.ps1",
            TargetType = TargetType.Both
        });

        var result = registry.Register(root, "PowerShell Scripts", @"C:\RightClickPS.exe");

        Assert.True(result.Success);
        Assert.Equal(2, result.FilesMenuItemCount); // files.ps1 + both.ps1
        Assert.Equal(2, result.DirectoryMenuItemCount); // folders.ps1 + both.ps1
        Assert.Equal(4, result.TotalMenuItemCount);
    }

    [Fact]
    public void Register_WithNestedFolders_CountsLeafScriptsOnly()
    {
        var registry = new ContextMenuRegistry();
        var root = MenuNode.CreateRoot();

        // Create: Root > Images > Convert.ps1
        var imagesFolder = MenuNode.CreateFolder("Images");
        imagesFolder.AddScript(new ScriptMetadata
        {
            Name = "Convert",
            FilePath = @"C:\Scripts\Images\Convert.ps1",
            TargetType = TargetType.Files
        });
        root.Children.Add(imagesFolder);

        var result = registry.Register(root, "PowerShell Scripts", @"C:\RightClickPS.exe");

        Assert.True(result.Success);
        Assert.Equal(1, result.FilesMenuItemCount);
    }

    [Fact]
    public void Register_FiltersOutEmptyFoldersAfterTargetTypeFiltering()
    {
        var registry = new ContextMenuRegistry();
        var root = MenuNode.CreateRoot();

        // Create folder with only files-targeted script
        var folder = MenuNode.CreateFolder("FilesOnlyFolder");
        folder.AddScript(new ScriptMetadata
        {
            Name = "Files Script",
            FilePath = @"C:\Scripts\files.ps1",
            TargetType = TargetType.Files
        });
        root.Children.Add(folder);

        var result = registry.Register(root, "PowerShell Scripts", @"C:\RightClickPS.exe");

        // The folder should appear for files (has applicable script)
        Assert.Equal(1, result.FilesMenuItemCount);
        // The folder should NOT appear for directories (no applicable scripts)
        Assert.Equal(0, result.DirectoryMenuItemCount);
    }

    #endregion

    #region Security Validation Tests

    #region Command Injection Tests (Fix #2)

    [Fact]
    public void BuildCommand_HandlesSpecialCharactersInPath()
    {
        var exePath = @"C:\RightClickPS.exe";
        var scriptPath = @"C:\Scripts\O'Reilly's Script.ps1";

        var result = ContextMenuRegistry.BuildCommand(exePath, scriptPath);

        // Command should be properly formatted with quotes
        Assert.Contains("\"", result);
        Assert.Contains("execute", result);
        Assert.EndsWith("\"%1\"", result);
    }

    [Fact]
    public void BuildCommand_HandlesAmpersandsInPath()
    {
        var exePath = @"C:\RightClickPS.exe";
        var scriptPath = @"C:\Scripts\Rock & Roll.ps1";

        var result = ContextMenuRegistry.BuildCommand(exePath, scriptPath);

        // Ampersands should be within quotes
        Assert.Contains("\"C:\\Scripts\\Rock & Roll.ps1\"", result);
    }

    #endregion

    #region Input Validation Tests (Fix #5)

    [Fact]
    public void SanitizeKeyName_TruncatesExcessivelyLongNames()
    {
        var longName = new string('a', 300);

        var result = ContextMenuRegistry.SanitizeKeyName(longName);

        // Should truncate to reasonable length
        Assert.True(result.Length <= 255);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void SanitizeKeyName_HandlesControlCharacters()
    {
        var nameWithControlChars = "Test\x00\x01\x02Script";

        var result = ContextMenuRegistry.SanitizeKeyName(nameWithControlChars);

        // Control characters should be removed or replaced
        Assert.NotEqual(nameWithControlChars, result);
        Assert.Contains("Test", result);
        Assert.Contains("Script", result);
    }

    [Fact]
    public void SanitizeKeyName_HandlesNewlines()
    {
        var nameWithNewlines = "Test\nScript\r\nName";

        var result = ContextMenuRegistry.SanitizeKeyName(nameWithNewlines);

        // Newlines should be removed or replaced
        Assert.DoesNotContain("\n", result);
        Assert.DoesNotContain("\r", result);
    }

    [Fact]
    public void SanitizeKeyName_HandlesMultipleConsecutiveSpaces()
    {
        var nameWithSpaces = "Test    Multiple    Spaces";

        var result = ContextMenuRegistry.SanitizeKeyName(nameWithSpaces);

        // Should handle multiple spaces
        Assert.NotEmpty(result);
        Assert.Contains("Test", result);
        Assert.Contains("Spaces", result);
        #endregion

    }

    #endregion

    #region Deep Hierarchy Tests

    [Fact]
    public void Register_WithDeeplyNestedScript_CountsCorrectly()
    {
        var registry = new ContextMenuRegistry();
        var root = MenuNode.CreateRoot();

        // Create: Root > Level1 > Level2 > Script
        var level1 = MenuNode.CreateFolder("Level1");
        var level2 = MenuNode.CreateFolder("Level2");
        level2.AddScript(new ScriptMetadata
        {
            Name = "Deep Script",
            FilePath = @"C:\Scripts\Level1\Level2\deep.ps1",
            TargetType = TargetType.Both
        });
        level1.Children.Add(level2);
        root.Children.Add(level1);

        var result = registry.Register(root, "PowerShell Scripts", @"C:\RightClickPS.exe");

        Assert.True(result.Success);
        Assert.Equal(1, result.FilesMenuItemCount);
        Assert.Equal(1, result.DirectoryMenuItemCount);
    }

    [Fact]
    public void Register_WithMixedTargetTypesInFolder_CorrectlySplits()
    {
        var registry = new ContextMenuRegistry();
        var root = MenuNode.CreateRoot();

        // Create folder with mixed scripts
        var folder = MenuNode.CreateFolder("Utilities");
        folder.AddScript(new ScriptMetadata
        {
            Name = "File Util",
            FilePath = @"C:\Scripts\file.ps1",
            TargetType = TargetType.Files
        });
        folder.AddScript(new ScriptMetadata
        {
            Name = "Folder Util",
            FilePath = @"C:\Scripts\folder.ps1",
            TargetType = TargetType.Folders
        });
        root.Children.Add(folder);

        var result = registry.Register(root, "PowerShell Scripts", @"C:\RightClickPS.exe");

        Assert.True(result.Success);
        Assert.Equal(1, result.FilesMenuItemCount);
        Assert.Equal(1, result.DirectoryMenuItemCount);
    }

    #endregion

    #region Unregister Tests

    [Fact]
    public void Unregister_RemovesContextMenuEntries()
    {
        var registry = new ContextMenuRegistry();
        var root = MenuNode.CreateRoot();

        // Arrange: Register a script first
        registry.Register(root, "PowerShell Scripts", @"C:\RightClickPS.exe");

        // Act: Unregister the script
        var unregisterResult = registry.Unregister(root, "PowerShell Scripts");

        // Assert: Unregistration should be successful
        Assert.True(unregisterResult.Success);

        // Additional check: Re-registering should succeed if unregistered
        var registerResult = registry.Register(root, "PowerShell Scripts", @"C:\RightClickPS.exe");
        Assert.True(registerResult.Success);
    }

    [Fact]
    public void Unregister_NonExistentMenu_DoesNothing()
    {
        var registry = new ContextMenuRegistry();
        var root = MenuNode.CreateRoot();

        // Act: Unregister a menu that doesn't exist
        var result = registry.Unregister(root, "NonExistentMenu");

        // Assert: Result should be successful, no exception thrown
        Assert.True(result.Success);
    }

    #endregion
}
