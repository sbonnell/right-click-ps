using RightClickPS.Scripts;

namespace RightClickPS.Tests.Scripts;

public class ScriptParserTests
{
    private readonly ScriptParser _parser = new();
    private const string TestFilePath = @"C:\Scripts\Test\TestScript.ps1";
    private const string TestBasePath = @"C:\Scripts";

    [Fact]
    public void ParseContent_WithCompleteMetadataBlock_ParsesAllFields()
    {
        // Arrange
        var content = @"<#
@Name: Convert to JPG
@Description: Converts images to JPEG format
@Extensions: .png,.bmp,.gif
@TargetType: Files
@RunAsAdmin: false
#>

# Script code here
$SelectedFiles | ForEach-Object { Write-Host $_ }
";

        // Act
        var metadata = _parser.ParseContent(content, TestFilePath, TestBasePath);

        // Assert
        Assert.Equal("Convert to JPG", metadata.Name);
        Assert.Equal("Converts images to JPEG format", metadata.Description);
        Assert.Equal(3, metadata.Extensions.Count);
        Assert.Contains(".png", metadata.Extensions);
        Assert.Contains(".bmp", metadata.Extensions);
        Assert.Contains(".gif", metadata.Extensions);
        Assert.Equal(TargetType.Files, metadata.TargetType);
        Assert.False(metadata.RunAsAdmin);
        Assert.Equal(TestFilePath, metadata.FilePath);
        Assert.Equal(@"Test\TestScript.ps1", metadata.RelativePath);
    }

    [Fact]
    public void ParseContent_WithPartialMetadata_ParsesProvidedFieldsWithDefaults()
    {
        // Arrange
        var content = @"<#
@Name: My Script
@RunAsAdmin: true
#>

# Script code
";

        // Act
        var metadata = _parser.ParseContent(content, TestFilePath, TestBasePath);

        // Assert
        Assert.Equal("My Script", metadata.Name);
        Assert.Null(metadata.Description);
        Assert.Single(metadata.Extensions);
        Assert.Contains("*", metadata.Extensions);
        Assert.Equal(TargetType.Both, metadata.TargetType);
        Assert.True(metadata.RunAsAdmin);
    }

    [Fact]
    public void ParseContent_WithNoMetadataBlock_ReturnsDefaults()
    {
        // Arrange
        var content = @"# Just a simple script
$SelectedFiles | ForEach-Object { Write-Host $_ }
";

        // Act
        var metadata = _parser.ParseContent(content, TestFilePath, TestBasePath);

        // Assert
        Assert.Equal("TestScript", metadata.Name); // Filename without extension
        Assert.Null(metadata.Description);
        Assert.Single(metadata.Extensions);
        Assert.Contains("*", metadata.Extensions);
        Assert.Equal(TargetType.Both, metadata.TargetType);
        Assert.False(metadata.RunAsAdmin);
    }

    [Fact]
    public void ParseContent_WithEmptyFile_ReturnsDefaults()
    {
        // Arrange
        var content = "";

        // Act
        var metadata = _parser.ParseContent(content, TestFilePath, TestBasePath);

        // Assert
        Assert.Equal("TestScript", metadata.Name);
        Assert.Null(metadata.Description);
        Assert.Single(metadata.Extensions);
        Assert.Contains("*", metadata.Extensions);
        Assert.Equal(TargetType.Both, metadata.TargetType);
        Assert.False(metadata.RunAsAdmin);
    }

    #region Extension Parsing Tests

    [Theory]
    [InlineData(".png,.jpg,.gif", new[] { ".png", ".jpg", ".gif" })]
    [InlineData(".png, .jpg, .gif", new[] { ".png", ".jpg", ".gif" })]
    [InlineData("png,jpg,gif", new[] { ".png", ".jpg", ".gif" })]  // Without dots
    [InlineData(".PNG,.JPG", new[] { ".PNG", ".JPG" })]  // Case preserved
    [InlineData(".txt", new[] { ".txt" })]
    public void ParseContent_WithVariousExtensionFormats_ParsesCorrectly(string extensions, string[] expected)
    {
        // Arrange
        var content = $@"<#
@Extensions: {extensions}
#>
";

        // Act
        var metadata = _parser.ParseContent(content, TestFilePath, TestBasePath);

        // Assert
        Assert.Equal(expected.Length, metadata.Extensions.Count);
        foreach (var ext in expected)
        {
            Assert.Contains(ext, metadata.Extensions);
        }
    }

    [Fact]
    public void ParseContent_WithWildcardExtension_ReturnsWildcard()
    {
        // Arrange
        var content = @"<#
@Extensions: *
#>
";

        // Act
        var metadata = _parser.ParseContent(content, TestFilePath, TestBasePath);

        // Assert
        Assert.Single(metadata.Extensions);
        Assert.Contains("*", metadata.Extensions);
    }

    [Fact]
    public void ParseContent_WithEmptyExtensions_ReturnsWildcard()
    {
        // Arrange
        var content = @"<#
@Extensions:
#>
";

        // Act
        var metadata = _parser.ParseContent(content, TestFilePath, TestBasePath);

        // Assert
        Assert.Single(metadata.Extensions);
        Assert.Contains("*", metadata.Extensions);
    }

    #endregion

    #region TargetType Parsing Tests

    [Theory]
    [InlineData("Files", TargetType.Files)]
    [InlineData("files", TargetType.Files)]
    [InlineData("FILES", TargetType.Files)]
    [InlineData("File", TargetType.Files)]
    [InlineData("Folders", TargetType.Folders)]
    [InlineData("folders", TargetType.Folders)]
    [InlineData("FOLDERS", TargetType.Folders)]
    [InlineData("Folder", TargetType.Folders)]
    [InlineData("Directory", TargetType.Folders)]
    [InlineData("Directories", TargetType.Folders)]
    [InlineData("Both", TargetType.Both)]
    [InlineData("both", TargetType.Both)]
    [InlineData("BOTH", TargetType.Both)]
    [InlineData("all", TargetType.Both)]  // Unknown value defaults to Both
    [InlineData("", TargetType.Both)]     // Empty defaults to Both
    public void ParseContent_WithVariousTargetTypes_ParsesCorrectly(string targetType, TargetType expected)
    {
        // Arrange
        var content = $@"<#
@TargetType: {targetType}
#>
";

        // Act
        var metadata = _parser.ParseContent(content, TestFilePath, TestBasePath);

        // Assert
        Assert.Equal(expected, metadata.TargetType);
    }

    #endregion

    #region RunAsAdmin Parsing Tests

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("TRUE", true)]
    [InlineData("yes", true)]
    [InlineData("Yes", true)]
    [InlineData("YES", true)]
    [InlineData("1", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("FALSE", false)]
    [InlineData("no", false)]
    [InlineData("No", false)]
    [InlineData("0", false)]
    [InlineData("", false)]          // Empty defaults to false
    [InlineData("invalid", false)]   // Unknown defaults to false
    public void ParseContent_WithVariousRunAsAdminValues_ParsesCorrectly(string runAsAdmin, bool expected)
    {
        // Arrange
        var content = $@"<#
@RunAsAdmin: {runAsAdmin}
#>
";

        // Act
        var metadata = _parser.ParseContent(content, TestFilePath, TestBasePath);

        // Assert
        Assert.Equal(expected, metadata.RunAsAdmin);
    }

    #endregion

    #region Field Name Case Insensitivity Tests

    [Fact]
    public void ParseContent_WithMixedCaseFieldNames_ParsesCorrectly()
    {
        // Arrange
        var content = @"<#
@NAME: Test Name
@description: Test Description
@EXTENSIONS: .txt
@TargetType: Files
@runasadmin: true
#>
";

        // Act
        var metadata = _parser.ParseContent(content, TestFilePath, TestBasePath);

        // Assert
        Assert.Equal("Test Name", metadata.Name);
        Assert.Equal("Test Description", metadata.Description);
        Assert.Contains(".txt", metadata.Extensions);
        Assert.Equal(TargetType.Files, metadata.TargetType);
        Assert.True(metadata.RunAsAdmin);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ParseContent_WithWhitespaceInBlockComment_ParsesCorrectly()
    {
        // Arrange
        var content = @"  <#
    @Name:    Spaced Name
    @Description:   A description with spaces
#>
";

        // Act
        var metadata = _parser.ParseContent(content, TestFilePath, TestBasePath);

        // Assert
        Assert.Equal("Spaced Name", metadata.Name);
        Assert.Equal("A description with spaces", metadata.Description);
    }

    [Fact]
    public void ParseContent_WithColonsInValue_ParsesCorrectly()
    {
        // Arrange
        var content = @"<#
@Description: Time format: HH:mm:ss
#>
";

        // Act
        var metadata = _parser.ParseContent(content, TestFilePath, TestBasePath);

        // Assert
        Assert.Equal("Time format: HH:mm:ss", metadata.Description);
    }

    [Fact]
    public void ParseContent_WithOnlyBlockCommentMarkers_ReturnsDefaults()
    {
        // Arrange
        var content = @"<#
#>
";

        // Act
        var metadata = _parser.ParseContent(content, TestFilePath, TestBasePath);

        // Assert
        Assert.Equal("TestScript", metadata.Name); // Defaults to filename
        Assert.Null(metadata.Description);
        Assert.Single(metadata.Extensions);
        Assert.Contains("*", metadata.Extensions);
    }

    [Fact]
    public void ParseContent_WithUnknownFields_IgnoresUnknownFields()
    {
        // Arrange
        var content = @"<#
@Name: Known Name
@UnknownField: Should be ignored
@Description: Known Description
#>
";

        // Act
        var metadata = _parser.ParseContent(content, TestFilePath, TestBasePath);

        // Assert
        Assert.Equal("Known Name", metadata.Name);
        Assert.Equal("Known Description", metadata.Description);
    }

    [Fact]
    public void ParseContent_WithMultipleSameFields_LastValueWins()
    {
        // Arrange
        var content = @"<#
@Name: First Name
@Name: Second Name
#>
";

        // Act
        var metadata = _parser.ParseContent(content, TestFilePath, TestBasePath);

        // Assert
        Assert.Equal("Second Name", metadata.Name);
    }

    #endregion

    #region Path Tests

    [Fact]
    public void ParseContent_SetsFilePathCorrectly()
    {
        // Arrange
        var content = @"<#
@Name: Test
#>
";

        // Act
        var metadata = _parser.ParseContent(content, TestFilePath, TestBasePath);

        // Assert
        Assert.Equal(TestFilePath, metadata.FilePath);
    }

    [Fact]
    public void ParseContent_CalculatesRelativePathCorrectly()
    {
        // Arrange
        var content = @"<#
@Name: Test
#>
";
        var filePath = @"C:\Scripts\Subfolder\Nested\Script.ps1";
        var basePath = @"C:\Scripts";

        // Act
        var metadata = _parser.ParseContent(content, filePath, basePath);

        // Assert
        Assert.Equal(@"Subfolder\Nested\Script.ps1", metadata.RelativePath);
    }

    [Fact]
    public void ParseContent_WithEmptyBasePath_ReturnsFilename()
    {
        // Arrange
        var content = @"<#
@Name: Test
#>
";

        // Act
        var metadata = _parser.ParseContent(content, TestFilePath, "");

        // Assert
        Assert.Equal("TestScript.ps1", metadata.RelativePath);
    }

    #endregion

    #region File I/O Tests

    [Fact]
    public void Parse_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = @"C:\NonExistent\Script.ps1";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _parser.Parse(nonExistentPath, TestBasePath));
    }

    [Fact]
    public void Parse_WithRealFile_ParsesCorrectly()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RightClickPSTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "TestScript.ps1");

        try
        {
            var content = @"<#
@Name: Temp Script
@Description: A temporary script for testing
@Extensions: .tmp
@TargetType: Files
@RunAsAdmin: false
#>

# Script content
";
            File.WriteAllText(tempFile, content, System.Text.Encoding.UTF8);

            // Act
            var metadata = _parser.Parse(tempFile, tempDir);

            // Assert
            Assert.Equal("Temp Script", metadata.Name);
            Assert.Equal("A temporary script for testing", metadata.Description);
            Assert.Contains(".tmp", metadata.Extensions);
            Assert.Equal(TargetType.Files, metadata.TargetType);
            Assert.False(metadata.RunAsAdmin);
            Assert.Equal(tempFile, metadata.FilePath);
            Assert.Equal("TestScript.ps1", metadata.RelativePath);
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
    public void Parse_WithUtf8BomFile_ParsesCorrectly()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RightClickPSTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "Utf8BomScript.ps1");

        try
        {
            var content = @"<#
@Name: UTF-8 BOM Script
@Description: Script with special characters: Cafe, resume, naive
#>
";
            // Write with UTF-8 BOM
            File.WriteAllText(tempFile, content, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            // Act
            var metadata = _parser.Parse(tempFile, tempDir);

            // Assert
            Assert.Equal("UTF-8 BOM Script", metadata.Name);
            Assert.Equal("Script with special characters: Cafe, resume, naive", metadata.Description);
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
