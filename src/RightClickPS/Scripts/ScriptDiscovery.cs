namespace RightClickPS.Scripts;

/// <summary>
/// Represents a node in the context menu hierarchy.
/// Can be either a folder (submenu) or a script (menu item).
/// </summary>
public class MenuNode
{
    /// <summary>
    /// Gets or sets the display name for this menu node.
    /// For folders, this is the folder name. For scripts, this is the script's Name metadata.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the child nodes (submenus and scripts).
    /// Only populated for folder nodes.
    /// </summary>
    public List<MenuNode> Children { get; set; } = new();

    /// <summary>
    /// Gets or sets the script metadata for this node.
    /// Only populated for script (leaf) nodes, null for folder nodes.
    /// </summary>
    public ScriptMetadata? Script { get; set; }

    /// <summary>
    /// Gets a value indicating whether this node represents a folder (submenu).
    /// </summary>
    public bool IsFolder => Script == null && Children.Count > 0;

    /// <summary>
    /// Gets a value indicating whether this node represents a script (menu item).
    /// </summary>
    public bool IsScript => Script != null;

    /// <summary>
    /// Creates an empty root node.
    /// </summary>
    public static MenuNode CreateRoot(string name = "Root")
    {
        return new MenuNode { Name = name };
    }

    /// <summary>
    /// Creates a folder node with the specified name.
    /// </summary>
    /// <param name="name">The folder display name.</param>
    public static MenuNode CreateFolder(string name)
    {
        return new MenuNode { Name = name };
    }

    /// <summary>
    /// Creates a script node from script metadata.
    /// </summary>
    /// <param name="metadata">The script metadata.</param>
    public static MenuNode CreateScript(ScriptMetadata metadata)
    {
        return new MenuNode
        {
            Name = metadata.Name,
            Script = metadata
        };
    }

    /// <summary>
    /// Finds or creates a child folder with the specified name.
    /// </summary>
    /// <param name="folderName">The folder name to find or create.</param>
    /// <returns>The existing or newly created folder node.</returns>
    public MenuNode GetOrCreateChildFolder(string folderName)
    {
        var existing = Children.FirstOrDefault(c =>
            c.Script == null && c.Name.Equals(folderName, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            return existing;
        }

        var newFolder = CreateFolder(folderName);
        Children.Add(newFolder);
        return newFolder;
    }

    /// <summary>
    /// Adds a script as a child of this node.
    /// </summary>
    /// <param name="metadata">The script metadata to add.</param>
    public void AddScript(ScriptMetadata metadata)
    {
        Children.Add(CreateScript(metadata));
    }

    /// <summary>
    /// Sorts children alphabetically, with folders before scripts.
    /// Recursively sorts all descendant folders as well.
    /// </summary>
    public void SortChildren()
    {
        // First, recursively sort children of all folder nodes
        foreach (var child in Children.Where(c => c.IsFolder))
        {
            child.SortChildren();
        }

        // Sort: folders first (alphabetically), then scripts (alphabetically)
        Children = Children
            .OrderBy(c => c.IsScript ? 1 : 0) // Folders before scripts
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

/// <summary>
/// Discovers PowerShell scripts in folder hierarchies and builds a menu structure.
/// </summary>
public class ScriptDiscovery
{
    private readonly ScriptParser _parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptDiscovery"/> class.
    /// </summary>
    public ScriptDiscovery()
    {
        _parser = new ScriptParser();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptDiscovery"/> class with a custom parser.
    /// </summary>
    /// <param name="parser">The script parser to use.</param>
    public ScriptDiscovery(ScriptParser parser)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    /// <summary>
    /// Discovers scripts from user and system script paths and builds a hierarchical menu structure.
    /// </summary>
    /// <param name="scriptsPath">The path to user scripts folder. Can be null if no user scripts.</param>
    /// <param name="systemScriptsPath">The path to system scripts folder. Can be null if no system scripts.</param>
    /// <param name="maxDepth">Maximum folder depth to scan (1 = only root level scripts).</param>
    /// <returns>The root <see cref="MenuNode"/> containing the complete menu hierarchy.</returns>
    public MenuNode DiscoverScripts(string? scriptsPath, string? systemScriptsPath, int maxDepth)
    {
        var root = MenuNode.CreateRoot();

        // Ensure maxDepth is at least 1
        maxDepth = Math.Max(1, maxDepth);

        // Scan user scripts
        if (!string.IsNullOrEmpty(scriptsPath) && Directory.Exists(scriptsPath))
        {
            ScanDirectory(scriptsPath, scriptsPath, root, maxDepth, currentDepth: 0);
        }

        // Scan system scripts and place them in a "_System" submenu
        if (!string.IsNullOrEmpty(systemScriptsPath) && Directory.Exists(systemScriptsPath))
        {
            ScanSystemScripts(systemScriptsPath, root, maxDepth);
        }

        // Sort all children recursively
        root.SortChildren();

        return root;
    }

    /// <summary>
    /// Recursively scans a directory for .ps1 scripts and builds the menu hierarchy.
    /// </summary>
    /// <param name="currentPath">The current directory being scanned.</param>
    /// <param name="basePath">The root scripts folder path for relative path calculation.</param>
    /// <param name="parentNode">The parent menu node to add discovered items to.</param>
    /// <param name="maxDepth">Maximum folder depth to scan.</param>
    /// <param name="currentDepth">Current depth in the folder hierarchy (0 = root).</param>
    private void ScanDirectory(string currentPath, string basePath, MenuNode parentNode, int maxDepth, int currentDepth)
    {
        // Add scripts in current directory
        try
        {
            var scriptFiles = Directory.GetFiles(currentPath, "*.ps1", SearchOption.TopDirectoryOnly);
            foreach (var scriptFile in scriptFiles.OrderBy(f => f))
            {
                try
                {
                    var metadata = _parser.Parse(scriptFile, basePath);
                    parentNode.AddScript(metadata);
                }
                catch (Exception)
                {
                    // Skip scripts that fail to parse
                    // In a production environment, we might want to log this
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we don't have access to
        }
        catch (IOException)
        {
            // Skip directories with I/O issues
        }

        // Recurse into subdirectories if within depth limit
        if (currentDepth < maxDepth - 1)
        {
            try
            {
                var subdirectories = Directory.GetDirectories(currentPath);
                foreach (var subdir in subdirectories.OrderBy(d => d))
                {
                    var folderName = Path.GetFileName(subdir);

                    // Skip hidden and system folders
                    if (folderName.StartsWith('.'))
                    {
                        continue;
                    }

                    var folderNode = parentNode.GetOrCreateChildFolder(folderName);
                    ScanDirectory(subdir, basePath, folderNode, maxDepth, currentDepth + 1);

                    // Remove empty folders
                    if (folderNode.Children.Count == 0)
                    {
                        parentNode.Children.Remove(folderNode);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we don't have access to
            }
            catch (IOException)
            {
                // Skip directories with I/O issues
            }
        }
    }

    /// <summary>
    /// Scans system scripts directory and adds them to a "_System" submenu.
    /// </summary>
    /// <param name="systemScriptsPath">The path to system scripts folder.</param>
    /// <param name="root">The root menu node.</param>
    /// <param name="maxDepth">Maximum folder depth to scan.</param>
    private void ScanSystemScripts(string systemScriptsPath, MenuNode root, int maxDepth)
    {
        // Create a temporary node to scan into
        var tempNode = MenuNode.CreateRoot();
        ScanDirectory(systemScriptsPath, systemScriptsPath, tempNode, maxDepth, currentDepth: 0);

        // If we found any scripts, merge them into root
        // The _System folder from the file system becomes the _System submenu
        foreach (var child in tempNode.Children)
        {
            // Check if this is the _System folder or any other folder/script from system scripts
            var existingChild = root.Children.FirstOrDefault(c =>
                c.Name.Equals(child.Name, StringComparison.OrdinalIgnoreCase) && c.Script == null);

            if (existingChild != null && child.Script == null)
            {
                // Merge children from the scanned folder into existing folder
                MergeNodes(existingChild, child);
            }
            else
            {
                // Add the node directly
                root.Children.Add(child);
            }
        }
    }

    /// <summary>
    /// Merges the children from source node into target node.
    /// </summary>
    /// <param name="target">The target node to merge into.</param>
    /// <param name="source">The source node to merge from.</param>
    private static void MergeNodes(MenuNode target, MenuNode source)
    {
        foreach (var child in source.Children)
        {
            if (child.IsScript)
            {
                // Add scripts directly
                target.Children.Add(child);
            }
            else
            {
                // For folders, find or create and recurse
                var existingFolder = target.Children.FirstOrDefault(c =>
                    c.Script == null && c.Name.Equals(child.Name, StringComparison.OrdinalIgnoreCase));

                if (existingFolder != null)
                {
                    MergeNodes(existingFolder, child);
                }
                else
                {
                    target.Children.Add(child);
                }
            }
        }
    }

    /// <summary>
    /// Counts the total number of scripts in a menu hierarchy.
    /// </summary>
    /// <param name="node">The root node to count from.</param>
    /// <returns>The total number of script nodes in the hierarchy.</returns>
    public static int CountScripts(MenuNode node)
    {
        int count = 0;

        foreach (var child in node.Children)
        {
            if (child.IsScript)
            {
                count++;
            }
            else
            {
                count += CountScripts(child);
            }
        }

        return count;
    }

    /// <summary>
    /// Counts the total number of folders in a menu hierarchy.
    /// </summary>
    /// <param name="node">The root node to count from.</param>
    /// <returns>The total number of folder nodes in the hierarchy.</returns>
    public static int CountFolders(MenuNode node)
    {
        int count = 0;

        foreach (var child in node.Children)
        {
            if (child.IsFolder)
            {
                count++;
                count += CountFolders(child);
            }
        }

        return count;
    }
}
