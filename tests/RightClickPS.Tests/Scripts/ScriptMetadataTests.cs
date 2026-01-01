using RightClickPS.Scripts;

namespace RightClickPS.Tests.Scripts;

public class ScriptMetadataTests
{
    [Fact]
    public void ScriptMetadata_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var metadata = new ScriptMetadata();

        // Assert
        Assert.Equal(string.Empty, metadata.Name);
        Assert.Null(metadata.Description);
        Assert.Single(metadata.Extensions);
        Assert.Contains("*", metadata.Extensions);
        Assert.Equal(TargetType.Both, metadata.TargetType);
        Assert.False(metadata.RunAsAdmin);
        Assert.Equal(string.Empty, metadata.FilePath);
        Assert.Equal(string.Empty, metadata.RelativePath);
    }

    [Theory]
    [InlineData(TargetType.Files)]
    [InlineData(TargetType.Folders)]
    [InlineData(TargetType.Both)]
    public void TargetType_AllEnumValues_AreAccessible(TargetType targetType)
    {
        // Arrange
        var metadata = new ScriptMetadata();

        // Act
        metadata.TargetType = targetType;

        // Assert
        Assert.Equal(targetType, metadata.TargetType);
    }

    [Fact]
    public void TargetType_EnumValues_HaveCorrectCount()
    {
        // Arrange & Act
        var values = Enum.GetValues<TargetType>();

        // Assert
        Assert.Equal(3, values.Length);
    }

    [Fact]
    public void ScriptMetadata_SetProperties_RetainsValues()
    {
        // Arrange
        var metadata = new ScriptMetadata();
        var extensions = new List<string> { ".png", ".jpg", ".gif" };

        // Act
        metadata.Name = "Convert to JPG";
        metadata.Description = "Converts images to JPEG format";
        metadata.Extensions = extensions;
        metadata.TargetType = TargetType.Files;
        metadata.RunAsAdmin = true;
        metadata.FilePath = @"C:\Scripts\Images\Convert.ps1";
        metadata.RelativePath = @"Images\Convert.ps1";

        // Assert
        Assert.Equal("Convert to JPG", metadata.Name);
        Assert.Equal("Converts images to JPEG format", metadata.Description);
        Assert.Equal(extensions, metadata.Extensions);
        Assert.Equal(TargetType.Files, metadata.TargetType);
        Assert.True(metadata.RunAsAdmin);
        Assert.Equal(@"C:\Scripts\Images\Convert.ps1", metadata.FilePath);
        Assert.Equal(@"Images\Convert.ps1", metadata.RelativePath);
    }

    [Theory]
    [InlineData(".png", true)]
    [InlineData(".PNG", true)]
    [InlineData(".jpg", true)]
    [InlineData(".gif", false)]
    [InlineData(".bmp", false)]
    public void AppliesToExtension_WithSpecificExtensions_ReturnsExpectedResult(string extension, bool expected)
    {
        // Arrange
        var metadata = new ScriptMetadata
        {
            Extensions = new List<string> { ".png", ".jpg" }
        };

        // Act
        var result = metadata.AppliesToExtension(extension);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(".png")]
    [InlineData(".jpg")]
    [InlineData(".docx")]
    [InlineData(".anything")]
    public void AppliesToExtension_WithWildcard_ReturnsTrue(string extension)
    {
        // Arrange
        var metadata = new ScriptMetadata
        {
            Extensions = new List<string> { "*" }
        };

        // Act
        var result = metadata.AppliesToExtension(extension);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(".png")]
    [InlineData(".jpg")]
    [InlineData(".docx")]
    public void AppliesToExtension_WithEmptyExtensions_ReturnsTrue(string extension)
    {
        // Arrange
        var metadata = new ScriptMetadata
        {
            Extensions = new List<string>()
        };

        // Act
        var result = metadata.AppliesToExtension(extension);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(TargetType.Files, true, false)]
    [InlineData(TargetType.Folders, false, true)]
    [InlineData(TargetType.Both, true, true)]
    public void AppliesToFilesAndFolders_ReturnsCorrectResults(TargetType targetType, bool appliesToFiles, bool appliesToFolders)
    {
        // Arrange
        var metadata = new ScriptMetadata
        {
            TargetType = targetType
        };

        // Act & Assert
        Assert.Equal(appliesToFiles, metadata.AppliesToFiles());
        Assert.Equal(appliesToFolders, metadata.AppliesToFolders());
    }

    [Fact]
    public void ScriptMetadata_WithDefaultExtensions_AppliesToAnyExtension()
    {
        // Arrange - use default constructor which sets Extensions to ["*"]
        var metadata = new ScriptMetadata();

        // Act & Assert
        Assert.True(metadata.AppliesToExtension(".txt"));
        Assert.True(metadata.AppliesToExtension(".exe"));
        Assert.True(metadata.AppliesToExtension(".anything"));
    }

    [Fact]
    public void ScriptMetadata_DefaultTargetType_AppliesToBothFilesAndFolders()
    {
        // Arrange - use default constructor which sets TargetType to Both
        var metadata = new ScriptMetadata();

        // Act & Assert
        Assert.True(metadata.AppliesToFiles());
        Assert.True(metadata.AppliesToFolders());
    }
}
