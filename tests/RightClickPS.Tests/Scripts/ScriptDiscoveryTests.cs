using RightClickPS.Scripts;

namespace RightClickPS.Tests.Scripts;

public class ScriptDiscoveryTests
{
    private readonly ScriptDiscovery _discovery = new();

    #region MenuNode Tests

    [Fact]
    public void MenuNode_CreateRoot_CreatesEmptyNode()
    {
        // Act
        var root = MenuNode.CreateRoot("TestRoot");

        // Assert
        Assert.Equal("TestRoot", root.Name);
        Assert.Empty(root.Children);
        Assert.Null(root.Script);
        Assert.False(root.IsFolder);
        Assert.False(root.IsScript);
    }

    [Fact]
    public void MenuNode_CreateFolder_CreatesFolderNode()
    {
        // Act
        var folder = MenuNode.CreateFolder("TestFolder");

        // Assert
        Assert.Equal("TestFolder", folder.Name);
        Assert.Empty(folder.Children);
        Assert.Null(folder.Script);
        Assert.False(folder.IsFolder); // Empty folder is not considered a folder
    }

    [Fact]
    public void MenuNode_CreateScript_CreatesScriptNode()
    {
        // Arrange
        var metadata = new ScriptMetadata
        {
            Name = "Test Script",
            FilePath = @"C:\Scripts\test.ps1"
        };

        // Act
        var node = MenuNode.CreateScript(metadata);

        // Assert
        Assert.Equal("Test Script", node.Name);
        Assert.Empty(node.Children);
        Assert.NotNull(node.Script);
        Assert.Same(metadata, node.Script);
        Assert.False(node.IsFolder);
        Assert.True(node.IsScript);
    }

    [Fact]
    public void MenuNode_IsFolder_ReturnsTrueWhenHasChildren()
    {
        // Arrange
        var folder = MenuNode.CreateFolder("Folder");
        folder.AddScript(new ScriptMetadata { Name = "Script", FilePath = @"C:\test.ps1" });

        // Assert
        Assert.True(folder.IsFolder);
        Assert.False(folder.IsScript);
    }

    [Fact]
    public void MenuNode_GetOrCreateChildFolder_CreatesNewFolder()
    {
        // Arrange
        var parent = MenuNode.CreateRoot();

        // Act
        var child = parent.GetOrCreateChildFolder("SubFolder");

        // Assert
        Assert.Equal("SubFolder", child.Name);
        Assert.Single(parent.Children);
        Assert.Same(child, parent.Children[0]);
    }

    [Fact]
    public void MenuNode_GetOrCreateChildFolder_ReturnsExistingFolder()
    {
        // Arrange
        var parent = MenuNode.CreateRoot();
        var first = parent.GetOrCreateChildFolder("SubFolder");
        first.AddScript(new ScriptMetadata { Name = "Test", FilePath = @"C:\test.ps1" });

        // Act
        var second = parent.GetOrCreateChildFolder("SubFolder");

        // Assert
        Assert.Same(first, second);
        Assert.Single(parent.Children);
    }

    [Fact]
    public void MenuNode_GetOrCreateChildFolder_IsCaseInsensitive()
    {
        // Arrange
        var parent = MenuNode.CreateRoot();
        var first = parent.GetOrCreateChildFolder("SubFolder");
        first.AddScript(new ScriptMetadata { Name = "Test", FilePath = @"C:\test.ps1" });

        // Act
        var second = parent.GetOrCreateChildFolder("SUBFOLDER");

        // Assert
        Assert.Same(first, second);
        Assert.Single(parent.Children);
    }

    [Fact]
    public void MenuNode_AddScript_AddsScriptAsChild()
    {
        // Arrange
        var parent = MenuNode.CreateRoot();
        var metadata = new ScriptMetadata { Name = "Test Script", FilePath = @"C:\test.ps1" };

        // Act
        parent.AddScript(metadata);

        // Assert
        Assert.Single(parent.Children);
        Assert.True(parent.Children[0].IsScript);
        Assert.Same(metadata, parent.Children[0].Script);
    }

    [Fact]
    public void MenuNode_SortChildren_SortsFoldersBeforeScripts()
    {
        // Arrange
        var root = MenuNode.CreateRoot();
        root.AddScript(new ScriptMetadata { Name = "Zebra Script", FilePath = @"C:\zebra.ps1" });
        var folder = root.GetOrCreateChildFolder("Apple Folder");
        folder.AddScript(new ScriptMetadata { Name = "Inner", FilePath = @"C:\inner.ps1" });
        root.AddScript(new ScriptMetadata { Name = "Alpha Script", FilePath = @"C:\alpha.ps1" });

        // Act
        root.SortChildren();

        // Assert
        Assert.Equal(3, root.Children.Count);
        Assert.True(root.Children[0].IsFolder); // Folder first
        Assert.Equal("Apple Folder", root.Children[0].Name);
        Assert.True(root.Children[1].IsScript); // Then scripts alphabetically
        Assert.Equal("Alpha Script", root.Children[1].Name);
        Assert.True(root.Children[2].IsScript);
        Assert.Equal("Zebra Script", root.Children[2].Name);
    }

    [Fact]
    public void MenuNode_SortChildren_SortsRecursively()
    {
        // Arrange
        var root = MenuNode.CreateRoot();
        var folder = root.GetOrCreateChildFolder("Parent");
        folder.AddScript(new ScriptMetadata { Name = "Z Script", FilePath = @"C:\z.ps1" });
        folder.AddScript(new ScriptMetadata { Name = "A Script", FilePath = @"C:\a.ps1" });

        // Act
        root.SortChildren();

        // Assert
        Assert.Equal("A Script", folder.Children[0].Name);
        Assert.Equal("Z Script", folder.Children[1].Name);
    }

    #endregion

    #region DiscoverScripts Basic Tests

    [Fact]
    public void DiscoverScripts_WithNullPaths_ReturnsEmptyRoot()
    {
        // Act
        var root = _discovery.DiscoverScripts(null, null, 3);

        // Assert
        Assert.NotNull(root);
        Assert.Empty(root.Children);
    }

    [Fact]
    public void DiscoverScripts_WithNonExistentPath_ReturnsEmptyRoot()
    {
        // Arrange
        var nonExistentPath = @"C:\NonExistent\Path\That\Does\Not\Exist\" + Guid.NewGuid().ToString();

        // Act
        var root = _discovery.DiscoverScripts(nonExistentPath, null, 3);

        // Assert
        Assert.NotNull(root);
        Assert.Empty(root.Children);
    }

    [Fact]
    public void DiscoverScripts_WithEmptyFolder_ReturnsEmptyRoot()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            // Act
            var root = _discovery.DiscoverScripts(tempDir, null, 3);

            // Assert
            Assert.NotNull(root);
            Assert.Empty(root.Children);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void DiscoverScripts_WithSingleScript_ReturnsScriptNode()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            CreateScript(tempDir, "TestScript.ps1", "@Name: Test Script");

            // Act
            var root = _discovery.DiscoverScripts(tempDir, null, 3);

            // Assert
            Assert.Single(root.Children);
            Assert.True(root.Children[0].IsScript);
            Assert.Equal("Test Script", root.Children[0].Name);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void DiscoverScripts_WithMultipleScripts_ReturnsAllScripts()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            CreateScript(tempDir, "Alpha.ps1", "@Name: Alpha Script");
            CreateScript(tempDir, "Beta.ps1", "@Name: Beta Script");
            CreateScript(tempDir, "Gamma.ps1", "@Name: Gamma Script");

            // Act
            var root = _discovery.DiscoverScripts(tempDir, null, 3);

            // Assert
            Assert.Equal(3, root.Children.Count);
            Assert.All(root.Children, c => Assert.True(c.IsScript));
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void DiscoverScripts_ScriptsAreSortedAlphabetically()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            CreateScript(tempDir, "Zebra.ps1", "@Name: Zebra");
            CreateScript(tempDir, "Alpha.ps1", "@Name: Alpha");
            CreateScript(tempDir, "Middle.ps1", "@Name: Middle");

            // Act
            var root = _discovery.DiscoverScripts(tempDir, null, 3);

            // Assert
            Assert.Equal(3, root.Children.Count);
            Assert.Equal("Alpha", root.Children[0].Name);
            Assert.Equal("Middle", root.Children[1].Name);
            Assert.Equal("Zebra", root.Children[2].Name);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    #endregion

    #region Folder Hierarchy Tests

    [Fact]
    public void DiscoverScripts_WithSubfolders_CreatesFolderNodes()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            var subDir = Path.Combine(tempDir, "SubFolder");
            Directory.CreateDirectory(subDir);
            CreateScript(subDir, "Nested.ps1", "@Name: Nested Script");

            // Act
            var root = _discovery.DiscoverScripts(tempDir, null, 3);

            // Assert
            Assert.Single(root.Children);
            Assert.True(root.Children[0].IsFolder);
            Assert.Equal("SubFolder", root.Children[0].Name);
            Assert.Single(root.Children[0].Children);
            Assert.True(root.Children[0].Children[0].IsScript);
            Assert.Equal("Nested Script", root.Children[0].Children[0].Name);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void DiscoverScripts_WithNestedFolders_CreatesNestedHierarchy()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            var level1 = Path.Combine(tempDir, "Level1");
            var level2 = Path.Combine(level1, "Level2");
            Directory.CreateDirectory(level2);
            CreateScript(level2, "Deep.ps1", "@Name: Deep Script");

            // Act
            var root = _discovery.DiscoverScripts(tempDir, null, 3);

            // Assert
            Assert.Single(root.Children);
            Assert.True(root.Children[0].IsFolder);
            Assert.Equal("Level1", root.Children[0].Name);
            Assert.Single(root.Children[0].Children);
            Assert.True(root.Children[0].Children[0].IsFolder);
            Assert.Equal("Level2", root.Children[0].Children[0].Name);
            Assert.Single(root.Children[0].Children[0].Children);
            Assert.Equal("Deep Script", root.Children[0].Children[0].Children[0].Name);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void DiscoverScripts_FoldersBeforeScriptsInSorting()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            // Create scripts with names that would sort before folder alphabetically
            CreateScript(tempDir, "AAA.ps1", "@Name: AAA Script");
            var folder = Path.Combine(tempDir, "ZZZ Folder");
            Directory.CreateDirectory(folder);
            CreateScript(folder, "Test.ps1", "@Name: Test");

            // Act
            var root = _discovery.DiscoverScripts(tempDir, null, 3);

            // Assert
            Assert.Equal(2, root.Children.Count);
            Assert.True(root.Children[0].IsFolder); // Folder first even though "Z" > "A"
            Assert.True(root.Children[1].IsScript);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void DiscoverScripts_EmptySubfolders_AreNotIncluded()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            Directory.CreateDirectory(Path.Combine(tempDir, "EmptyFolder"));
            CreateScript(tempDir, "Script.ps1", "@Name: Script");

            // Act
            var root = _discovery.DiscoverScripts(tempDir, null, 3);

            // Assert
            Assert.Single(root.Children);
            Assert.True(root.Children[0].IsScript);
            Assert.Equal("Script", root.Children[0].Name);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void DiscoverScripts_HiddenFolders_AreSkipped()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            var hiddenFolder = Path.Combine(tempDir, ".hidden");
            Directory.CreateDirectory(hiddenFolder);
            CreateScript(hiddenFolder, "Hidden.ps1", "@Name: Hidden Script");
            CreateScript(tempDir, "Visible.ps1", "@Name: Visible Script");

            // Act
            var root = _discovery.DiscoverScripts(tempDir, null, 3);

            // Assert
            Assert.Single(root.Children);
            Assert.Equal("Visible Script", root.Children[0].Name);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    #endregion

    #region MaxDepth Tests

    [Fact]
    public void DiscoverScripts_MaxDepth1_OnlyScansRootLevel()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            CreateScript(tempDir, "Root.ps1", "@Name: Root Script");
            var subDir = Path.Combine(tempDir, "SubFolder");
            Directory.CreateDirectory(subDir);
            CreateScript(subDir, "Nested.ps1", "@Name: Nested Script");

            // Act
            var root = _discovery.DiscoverScripts(tempDir, null, 1);

            // Assert
            Assert.Single(root.Children);
            Assert.Equal("Root Script", root.Children[0].Name);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void DiscoverScripts_MaxDepth2_ScansOneLevel()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            var level1 = Path.Combine(tempDir, "Level1");
            var level2 = Path.Combine(level1, "Level2");
            Directory.CreateDirectory(level2);
            CreateScript(tempDir, "Root.ps1", "@Name: Root");
            CreateScript(level1, "Level1.ps1", "@Name: Level 1");
            CreateScript(level2, "Level2.ps1", "@Name: Level 2");

            // Act
            var root = _discovery.DiscoverScripts(tempDir, null, 2);

            // Assert
            // Should have folder for Level1 and Root script
            Assert.Equal(2, root.Children.Count);

            var folder = root.Children.First(c => c.IsFolder);
            Assert.Equal("Level1", folder.Name);

            // Level1 folder should only contain Level1.ps1, not Level2 folder
            Assert.Single(folder.Children);
            Assert.Equal("Level 1", folder.Children[0].Name);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void DiscoverScripts_MaxDepth3_ScansTwoLevels()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            var level1 = Path.Combine(tempDir, "Level1");
            var level2 = Path.Combine(level1, "Level2");
            var level3 = Path.Combine(level2, "Level3");
            Directory.CreateDirectory(level3);
            CreateScript(level2, "Level2.ps1", "@Name: Level 2");
            CreateScript(level3, "Level3.ps1", "@Name: Level 3");

            // Act
            var root = _discovery.DiscoverScripts(tempDir, null, 3);

            // Assert
            // Should include Level1 -> Level2 -> Level2.ps1, but not Level3
            var level1Node = root.Children.FirstOrDefault(c => c.Name == "Level1");
            Assert.NotNull(level1Node);

            var level2Node = level1Node.Children.FirstOrDefault(c => c.Name == "Level2");
            Assert.NotNull(level2Node);

            Assert.Single(level2Node.Children);
            Assert.Equal("Level 2", level2Node.Children[0].Name);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void DiscoverScripts_MaxDepthZeroOrNegative_TreatedAsOne()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            CreateScript(tempDir, "Root.ps1", "@Name: Root");
            var subDir = Path.Combine(tempDir, "Sub");
            Directory.CreateDirectory(subDir);
            CreateScript(subDir, "Nested.ps1", "@Name: Nested");

            // Act
            var rootZero = _discovery.DiscoverScripts(tempDir, null, 0);
            var rootNegative = _discovery.DiscoverScripts(tempDir, null, -1);

            // Assert
            Assert.Single(rootZero.Children);
            Assert.Single(rootNegative.Children);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    #endregion

    #region System Scripts Merging Tests

    [Fact]
    public void DiscoverScripts_WithSystemScripts_AddsSystemFolder()
    {
        // Arrange
        var userDir = CreateTempDirectory();
        var systemDir = CreateTempDirectory();

        try
        {
            CreateScript(userDir, "User.ps1", "@Name: User Script");

            var systemSubDir = Path.Combine(systemDir, "_System");
            Directory.CreateDirectory(systemSubDir);
            CreateScript(systemSubDir, "Refresh.ps1", "@Name: Refresh");

            // Act
            var root = _discovery.DiscoverScripts(userDir, systemDir, 3);

            // Assert
            Assert.Equal(2, root.Children.Count);

            var systemFolder = root.Children.FirstOrDefault(c => c.Name == "_System");
            Assert.NotNull(systemFolder);
            Assert.True(systemFolder.IsFolder);
            Assert.Single(systemFolder.Children);
            Assert.Equal("Refresh", systemFolder.Children[0].Name);
        }
        finally
        {
            CleanupTempDirectory(userDir);
            CleanupTempDirectory(systemDir);
        }
    }

    [Fact]
    public void DiscoverScripts_WithBothUserAndSystemScripts_MergesCorrectly()
    {
        // Arrange
        var userDir = CreateTempDirectory();
        var systemDir = CreateTempDirectory();

        try
        {
            // User scripts
            CreateScript(userDir, "Alpha.ps1", "@Name: Alpha");
            var userImages = Path.Combine(userDir, "Images");
            Directory.CreateDirectory(userImages);
            CreateScript(userImages, "Convert.ps1", "@Name: Convert");

            // System scripts
            var systemSub = Path.Combine(systemDir, "_System");
            Directory.CreateDirectory(systemSub);
            CreateScript(systemSub, "Refresh.ps1", "@Name: Refresh");

            // Act
            var root = _discovery.DiscoverScripts(userDir, systemDir, 3);

            // Assert
            // Should have: _System folder, Images folder, Alpha script
            Assert.Equal(3, root.Children.Count);

            Assert.True(root.Children.Any(c => c.IsFolder && c.Name == "_System"));
            Assert.True(root.Children.Any(c => c.IsFolder && c.Name == "Images"));
            Assert.True(root.Children.Any(c => c.IsScript && c.Name == "Alpha"));
        }
        finally
        {
            CleanupTempDirectory(userDir);
            CleanupTempDirectory(systemDir);
        }
    }

    [Fact]
    public void DiscoverScripts_SystemScriptsOnly_WorksCorrectly()
    {
        // Arrange
        var systemDir = CreateTempDirectory();

        try
        {
            var systemSub = Path.Combine(systemDir, "_System");
            Directory.CreateDirectory(systemSub);
            CreateScript(systemSub, "Refresh.ps1", "@Name: Refresh");
            CreateScript(systemSub, "Config.ps1", "@Name: Config");

            // Act
            var root = _discovery.DiscoverScripts(null, systemDir, 3);

            // Assert
            Assert.Single(root.Children);
            Assert.True(root.Children[0].IsFolder);
            Assert.Equal("_System", root.Children[0].Name);
            Assert.Equal(2, root.Children[0].Children.Count);
        }
        finally
        {
            CleanupTempDirectory(systemDir);
        }
    }

    [Fact]
    public void DiscoverScripts_MergeDuplicateFolders_CombinesChildren()
    {
        // Arrange
        var userDir = CreateTempDirectory();
        var systemDir = CreateTempDirectory();

        try
        {
            // Both user and system have a "Shared" folder
            var userShared = Path.Combine(userDir, "Shared");
            var systemShared = Path.Combine(systemDir, "Shared");
            Directory.CreateDirectory(userShared);
            Directory.CreateDirectory(systemShared);
            CreateScript(userShared, "UserScript.ps1", "@Name: User Script");
            CreateScript(systemShared, "SystemScript.ps1", "@Name: System Script");

            // Act
            var root = _discovery.DiscoverScripts(userDir, systemDir, 3);

            // Assert
            Assert.Single(root.Children);
            Assert.True(root.Children[0].IsFolder);
            Assert.Equal("Shared", root.Children[0].Name);
            Assert.Equal(2, root.Children[0].Children.Count);
        }
        finally
        {
            CleanupTempDirectory(userDir);
            CleanupTempDirectory(systemDir);
        }
    }

    #endregion

    #region Counting Helper Methods Tests

    [Fact]
    public void CountScripts_ReturnsCorrectCount()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            CreateScript(tempDir, "Script1.ps1", "@Name: Script 1");
            CreateScript(tempDir, "Script2.ps1", "@Name: Script 2");
            var subDir = Path.Combine(tempDir, "Sub");
            Directory.CreateDirectory(subDir);
            CreateScript(subDir, "Script3.ps1", "@Name: Script 3");

            var root = _discovery.DiscoverScripts(tempDir, null, 3);

            // Act
            var count = ScriptDiscovery.CountScripts(root);

            // Assert
            Assert.Equal(3, count);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void CountFolders_ReturnsCorrectCount()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            var folder1 = Path.Combine(tempDir, "Folder1");
            var folder2 = Path.Combine(tempDir, "Folder2");
            var nested = Path.Combine(folder1, "Nested");
            Directory.CreateDirectory(nested);
            Directory.CreateDirectory(folder2);
            CreateScript(folder1, "S1.ps1", "@Name: S1");
            CreateScript(nested, "S2.ps1", "@Name: S2");
            CreateScript(folder2, "S3.ps1", "@Name: S3");

            var root = _discovery.DiscoverScripts(tempDir, null, 3);

            // Act
            var count = ScriptDiscovery.CountFolders(root);

            // Assert
            Assert.Equal(3, count); // Folder1, Folder2, Nested
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void CountScripts_WithEmptyTree_ReturnsZero()
    {
        // Arrange
        var root = MenuNode.CreateRoot();

        // Act
        var count = ScriptDiscovery.CountScripts(root);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void CountFolders_WithEmptyTree_ReturnsZero()
    {
        // Arrange
        var root = MenuNode.CreateRoot();

        // Act
        var count = ScriptDiscovery.CountFolders(root);

        // Assert
        Assert.Equal(0, count);
    }

    #endregion

    #region Script Parsing Integration Tests

    [Fact]
    public void DiscoverScripts_ParsesScriptMetadataCorrectly()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            var content = @"<#
@Name: Full Metadata Script
@Description: A script with all metadata
@Extensions: .png,.jpg
@TargetType: Files
@RunAsAdmin: true
#>
# Script content
";
            File.WriteAllText(Path.Combine(tempDir, "Full.ps1"), content);

            // Act
            var root = _discovery.DiscoverScripts(tempDir, null, 3);

            // Assert
            Assert.Single(root.Children);
            var script = root.Children[0].Script;
            Assert.NotNull(script);
            Assert.Equal("Full Metadata Script", script.Name);
            Assert.Equal("A script with all metadata", script.Description);
            Assert.Contains(".png", script.Extensions);
            Assert.Contains(".jpg", script.Extensions);
            Assert.Equal(TargetType.Files, script.TargetType);
            Assert.True(script.RunAsAdmin);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void DiscoverScripts_ScriptWithoutMetadata_UsesFilename()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "NoMetadata.ps1"), "# Just a script");

            // Act
            var root = _discovery.DiscoverScripts(tempDir, null, 3);

            // Assert
            Assert.Single(root.Children);
            Assert.Equal("NoMetadata", root.Children[0].Name);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void DiscoverScripts_SetsFilePathCorrectly()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            var scriptPath = Path.Combine(tempDir, "TestScript.ps1");
            CreateScript(tempDir, "TestScript.ps1", "@Name: Test");

            // Act
            var root = _discovery.DiscoverScripts(tempDir, null, 3);

            // Assert
            Assert.Single(root.Children);
            Assert.Equal(scriptPath, root.Children[0].Script?.FilePath);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void DiscoverScripts_SetsRelativePathCorrectly()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            var subDir = Path.Combine(tempDir, "SubFolder");
            Directory.CreateDirectory(subDir);
            CreateScript(subDir, "Nested.ps1", "@Name: Nested");

            // Act
            var root = _discovery.DiscoverScripts(tempDir, null, 3);

            // Assert
            var folder = root.Children.First(c => c.IsFolder);
            var script = folder.Children[0];
            Assert.Equal(Path.Combine("SubFolder", "Nested.ps1"), script.Script?.RelativePath);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void DiscoverScripts_IgnoresNonPs1Files()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "script.ps1"), "<#\n@Name: PowerShell\n#>");
            File.WriteAllText(Path.Combine(tempDir, "readme.txt"), "Not a script");
            File.WriteAllText(Path.Combine(tempDir, "script.bat"), "@echo off");

            // Act
            var root = _discovery.DiscoverScripts(tempDir, null, 3);

            // Assert
            Assert.Single(root.Children);
            Assert.Equal("PowerShell", root.Children[0].Name);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Fact]
    public void DiscoverScripts_WithCustomParser_UsesProvidedParser()
    {
        // This test verifies the dependency injection constructor works
        var parser = new ScriptParser();
        var discovery = new ScriptDiscovery(parser);

        // Act
        var root = discovery.DiscoverScripts(null, null, 3);

        // Assert
        Assert.NotNull(root);
    }

    [Fact]
    public void DiscoverScripts_WithNullParser_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ScriptDiscovery(null!));
    }

    #endregion

    #region Helper Methods

    private static string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "RightClickPSTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static void CleanupTempDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch
        {
            // Ignore cleanup failures in tests
        }
    }

    private static void CreateScript(string directory, string filename, string metadata)
    {
        var content = $@"<#
{metadata}
#>
# Script content
$SelectedFiles | ForEach-Object {{ Write-Host $_ }}
";
        File.WriteAllText(Path.Combine(directory, filename), content);
    }

    #endregion
}
