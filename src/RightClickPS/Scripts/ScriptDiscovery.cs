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
    /// Discovers scripts from the scripts folder and builds a hierarchical menu structure.
    /// </summary>
    /// <param name="scriptsPath">The path to the scripts folder.</param>
    /// <param name="maxDepth">Maximum folder depth to scan (1 = only root level scripts).</param>
    /// <returns>The root <see cref="MenuNode"/> containing the complete menu hierarchy.</returns>
    public MenuNode DiscoverScripts(string scriptsPath, int maxDepth)
    {
        var root = MenuNode.CreateRoot();

        // Ensure maxDepth is at least 1
        maxDepth = Math.Max(1, maxDepth);

        // Scan scripts folder
        if (!string.IsNullOrEmpty(scriptsPath) && Directory.Exists(scriptsPath))
        {
            ScanDirectory(scriptsPath, scriptsPath, root, maxDepth, currentDepth: 0);
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
