using RightClickPS.Registry;

namespace RightClickPS.Tests.Registry;

/// <summary>
/// Unit tests for <see cref="RegistryConstants"/>.
/// </summary>
public class RegistryConstantsTests
{
    [Fact]
    public void AppKeyName_HasExpectedValue()
    {
        Assert.Equal("RightClickPS", RegistryConstants.AppKeyName);
    }

    [Fact]
    public void ClassesRoot_HasExpectedValue()
    {
        Assert.Equal(@"Software\Classes", RegistryConstants.ClassesRoot);
    }

    [Fact]
    public void FilesShellPath_HasExpectedValue()
    {
        Assert.Equal(@"Software\Classes\*\shell", RegistryConstants.FilesShellPath);
    }

    [Fact]
    public void DirectoryShellPath_HasExpectedValue()
    {
        Assert.Equal(@"Software\Classes\Directory\shell", RegistryConstants.DirectoryShellPath);
    }

    [Fact]
    public void FilesAppPath_IncludesAppKeyName()
    {
        Assert.Contains(RegistryConstants.AppKeyName, RegistryConstants.FilesAppPath);
    }

    [Fact]
    public void FilesAppPath_HasExpectedValue()
    {
        Assert.Equal(@"Software\Classes\*\shell\RightClickPS", RegistryConstants.FilesAppPath);
    }

    [Fact]
    public void DirectoryAppPath_IncludesAppKeyName()
    {
        Assert.Contains(RegistryConstants.AppKeyName, RegistryConstants.DirectoryAppPath);
    }

    [Fact]
    public void DirectoryAppPath_HasExpectedValue()
    {
        Assert.Equal(@"Software\Classes\Directory\shell\RightClickPS", RegistryConstants.DirectoryAppPath);
    }

    [Fact]
    public void MUIVerbValueName_HasExpectedValue()
    {
        Assert.Equal("MUIVerb", RegistryConstants.MUIVerbValueName);
    }

    [Fact]
    public void SubCommandsValueName_HasExpectedValue()
    {
        Assert.Equal("SubCommands", RegistryConstants.SubCommandsValueName);
    }

    [Fact]
    public void IconValueName_HasExpectedValue()
    {
        Assert.Equal("Icon", RegistryConstants.IconValueName);
    }

    [Fact]
    public void ShellSubKeyName_HasExpectedValue()
    {
        Assert.Equal("shell", RegistryConstants.ShellSubKeyName);
    }

    [Fact]
    public void CommandSubKeyName_HasExpectedValue()
    {
        Assert.Equal("command", RegistryConstants.CommandSubKeyName);
    }

    [Fact]
    public void GetFilesMenuItemPath_ConstructsCorrectPath()
    {
        var result = RegistryConstants.GetFilesMenuItemPath("ConvertToJPG");

        Assert.Equal(@"Software\Classes\*\shell\RightClickPS\shell\ConvertToJPG", result);
    }

    [Fact]
    public void GetFilesMenuItemPath_HandlesSubMenuItems()
    {
        var result = RegistryConstants.GetFilesMenuItemPath("Images");

        Assert.StartsWith(RegistryConstants.FilesAppPath, result);
        Assert.Contains("Images", result);
    }

    [Fact]
    public void GetDirectoryMenuItemPath_ConstructsCorrectPath()
    {
        var result = RegistryConstants.GetDirectoryMenuItemPath("OpenInTerminal");

        Assert.Equal(@"Software\Classes\Directory\shell\RightClickPS\shell\OpenInTerminal", result);
    }

    [Fact]
    public void GetDirectoryMenuItemPath_HandlesSubMenuItems()
    {
        var result = RegistryConstants.GetDirectoryMenuItemPath("Utilities");

        Assert.StartsWith(RegistryConstants.DirectoryAppPath, result);
        Assert.Contains("Utilities", result);
    }

    [Fact]
    public void GetCommandPath_AppendsCommandSubKey()
    {
        var menuItemPath = @"Software\Classes\*\shell\RightClickPS\shell\ConvertToJPG";
        var result = RegistryConstants.GetCommandPath(menuItemPath);

        Assert.Equal(@"Software\Classes\*\shell\RightClickPS\shell\ConvertToJPG\command", result);
    }

    [Fact]
    public void GetCommandPath_EndsWithCommandKeyName()
    {
        var menuItemPath = @"Software\Classes\Directory\shell\RightClickPS\shell\TestItem";
        var result = RegistryConstants.GetCommandPath(menuItemPath);

        Assert.EndsWith(RegistryConstants.CommandSubKeyName, result);
    }

    [Fact]
    public void GetShellPath_AppendsShellSubKey()
    {
        var menuItemPath = @"Software\Classes\*\shell\RightClickPS\shell\Images";
        var result = RegistryConstants.GetShellPath(menuItemPath);

        Assert.Equal(@"Software\Classes\*\shell\RightClickPS\shell\Images\shell", result);
    }

    [Fact]
    public void GetShellPath_EndsWithShellKeyName()
    {
        var menuItemPath = @"Software\Classes\Directory\shell\RightClickPS\shell\Utilities";
        var result = RegistryConstants.GetShellPath(menuItemPath);

        Assert.EndsWith(RegistryConstants.ShellSubKeyName, result);
    }

    [Fact]
    public void FilesAppPath_IsWithinFilesShellPath()
    {
        Assert.StartsWith(RegistryConstants.FilesShellPath, RegistryConstants.FilesAppPath);
    }

    [Fact]
    public void DirectoryAppPath_IsWithinDirectoryShellPath()
    {
        Assert.StartsWith(RegistryConstants.DirectoryShellPath, RegistryConstants.DirectoryAppPath);
    }

    [Fact]
    public void PathConstruction_ProducesValidNestedPath()
    {
        // Simulate creating a nested menu structure: RightClickPS > Images > ConvertToJPG
        var imagesPath = RegistryConstants.GetFilesMenuItemPath("Images");
        var imagesShellPath = RegistryConstants.GetShellPath(imagesPath);
        var convertPath = $@"{imagesShellPath}\ConvertToJPG";
        var commandPath = RegistryConstants.GetCommandPath(convertPath);

        Assert.Equal(@"Software\Classes\*\shell\RightClickPS\shell\Images\shell\ConvertToJPG\command", commandPath);
    }
}
