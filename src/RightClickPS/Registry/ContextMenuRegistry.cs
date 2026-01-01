using Microsoft.Win32;
using RightClickPS.Scripts;

namespace RightClickPS.Registry;

/// <summary>
/// Manages Windows registry entries for the RightClickPS context menu.
/// Creates cascading menu structures from the discovered script hierarchy.
/// </summary>
public class ContextMenuRegistry
{
    /// <summary>
    /// Represents the result of a registration operation.
    /// </summary>
    public class RegistrationResult
    {
        /// <summary>
        /// Gets or sets whether the registration was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the number of menu items registered for files.
        /// </summary>
        public int FilesMenuItemCount { get; set; }

        /// <summary>
        /// Gets or sets the number of menu items registered for folders/directories.
        /// </summary>
        public int DirectoryMenuItemCount { get; set; }

        /// <summary>
        /// Gets or sets any error message if registration failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets the total number of menu items registered.
        /// </summary>
        public int TotalMenuItemCount => FilesMenuItemCount + DirectoryMenuItemCount;
    }

    /// <summary>
    /// Registers context menu entries from the menu hierarchy.
    /// </summary>
    /// <param name="root">The root menu node containing the hierarchy to register.</param>
    /// <param name="menuName">The display name for the root context menu.</param>
    /// <param name="exePath">The full path to the RightClickPS executable.</param>
    /// <param name="iconPath">Optional path to an icon file for the root menu.</param>
    /// <returns>A <see cref="RegistrationResult"/> indicating success or failure.</returns>
    public RegistrationResult Register(MenuNode root, string menuName, string exePath, string? iconPath = null)
    {
        if (root == null)
        {
            return new RegistrationResult
            {
                Success = false,
                ErrorMessage = "Menu hierarchy is null"
            };
        }

        if (string.IsNullOrWhiteSpace(menuName))
        {
            return new RegistrationResult
            {
                Success = false,
                ErrorMessage = "Menu name is required"
            };
        }

        if (string.IsNullOrWhiteSpace(exePath))
        {
            return new RegistrationResult
            {
                Success = false,
                ErrorMessage = "Executable path is required"
            };
        }

        try
        {
            // First, clean up any existing entries
            Unregister();

            var result = new RegistrationResult { Success = true };

            // Register for files (*)
            result.FilesMenuItemCount = RegisterForContext(
                root,
                menuName,
                exePath,
                RegistryConstants.FilesAppPath,
                isForFiles: true,
                iconPath);

            // Register for directories
            result.DirectoryMenuItemCount = RegisterForContext(
                root,
                menuName,
                exePath,
                RegistryConstants.DirectoryAppPath,
                isForFiles: false,
                iconPath);

            return result;
        }
        catch (Exception ex)
        {
            return new RegistrationResult
            {
                Success = false,
                ErrorMessage = $"Registration failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Registers the context menu for a specific context (files or directories).
    /// </summary>
    /// <param name="root">The root menu node.</param>
    /// <param name="menuName">The display name for the root menu.</param>
    /// <param name="exePath">The path to the executable.</param>
    /// <param name="rootKeyPath">The registry path for the root key.</param>
    /// <param name="isForFiles">True if registering for files, false for directories.</param>
    /// <param name="iconPath">Optional path to an icon file.</param>
    /// <returns>The number of menu items registered.</returns>
    private int RegisterForContext(MenuNode root, string menuName, string exePath, string rootKeyPath, bool isForFiles, string? iconPath)
    {
        // Filter children based on target type
        var filteredRoot = FilterMenuNodes(root, isForFiles);

        // If no items to register after filtering, skip this context
        if (filteredRoot.Children.Count == 0)
        {
            return 0;
        }

        using var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
        using var rootKey = hkcu.CreateSubKey(rootKeyPath);

        if (rootKey == null)
        {
            throw new InvalidOperationException($"Failed to create registry key: {rootKeyPath}");
        }

        // Set the root menu display name and indicate it has subcommands
        rootKey.SetValue(RegistryConstants.MUIVerbValueName, menuName);
        rootKey.SetValue(RegistryConstants.SubCommandsValueName, string.Empty);

        // Set icon if provided
        if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
        {
            rootKey.SetValue(RegistryConstants.IconValueName, iconPath);
        }

        // Create shell subkey for child items
        using var shellKey = rootKey.CreateSubKey(RegistryConstants.ShellSubKeyName);
        if (shellKey == null)
        {
            throw new InvalidOperationException($"Failed to create shell subkey under: {rootKeyPath}");
        }

        // Register all children
        int itemCount = 0;
        foreach (var child in filteredRoot.Children)
        {
            itemCount += RegisterNode(child, shellKey, exePath, isForFiles);
        }

        return itemCount;
    }

    /// <summary>
    /// Recursively registers a menu node and its children.
    /// </summary>
    /// <param name="node">The menu node to register.</param>
    /// <param name="parentShellKey">The parent shell registry key.</param>
    /// <param name="exePath">The path to the executable.</param>
    /// <param name="isForFiles">True if registering for files context.</param>
    /// <returns>The number of menu items registered.</returns>
    private int RegisterNode(MenuNode node, RegistryKey parentShellKey, string exePath, bool isForFiles)
    {
        // Create a safe key name from the node name
        var keyName = SanitizeKeyName(node.Name);

        using var nodeKey = parentShellKey.CreateSubKey(keyName);
        if (nodeKey == null)
        {
            return 0;
        }

        // Set the display name
        nodeKey.SetValue(RegistryConstants.MUIVerbValueName, node.Name);

        int itemCount = 0;

        if (node.IsFolder)
        {
            // This is a submenu - set SubCommands and create child shell
            nodeKey.SetValue(RegistryConstants.SubCommandsValueName, string.Empty);

            using var childShellKey = nodeKey.CreateSubKey(RegistryConstants.ShellSubKeyName);
            if (childShellKey != null)
            {
                // Filter children based on target type before registering
                foreach (var child in node.Children)
                {
                    // Check if child should be included
                    if (ShouldIncludeNode(child, isForFiles))
                    {
                        itemCount += RegisterNode(child, childShellKey, exePath, isForFiles);
                    }
                }
            }
        }
        else if (node.IsScript && node.Script != null)
        {
            // This is a script - create the command entry
            var command = BuildCommand(exePath, node.Script.FilePath);

            using var commandKey = nodeKey.CreateSubKey(RegistryConstants.CommandSubKeyName);
            if (commandKey != null)
            {
                commandKey.SetValue(null, command); // Default value
                itemCount = 1;
            }
        }

        return itemCount;
    }

    /// <summary>
    /// Filters menu nodes based on target type (files or folders).
    /// Creates a new hierarchy with only applicable nodes.
    /// </summary>
    /// <param name="node">The node to filter.</param>
    /// <param name="isForFiles">True to filter for files, false for directories.</param>
    /// <returns>A new MenuNode containing only applicable children.</returns>
    private MenuNode FilterMenuNodes(MenuNode node, bool isForFiles)
    {
        var filteredNode = new MenuNode
        {
            Name = node.Name,
            Script = node.Script
        };

        foreach (var child in node.Children)
        {
            if (ShouldIncludeNode(child, isForFiles))
            {
                if (child.IsScript)
                {
                    filteredNode.Children.Add(child);
                }
                else if (child.IsFolder)
                {
                    var filteredChild = FilterMenuNodes(child, isForFiles);
                    if (filteredChild.Children.Count > 0)
                    {
                        filteredNode.Children.Add(filteredChild);
                    }
                }
            }
        }

        return filteredNode;
    }

    /// <summary>
    /// Determines whether a node should be included based on target type.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <param name="isForFiles">True if checking for files context, false for directories.</param>
    /// <returns>True if the node should be included.</returns>
    private bool ShouldIncludeNode(MenuNode node, bool isForFiles)
    {
        if (node.IsFolder)
        {
            // Include folders if they have any applicable children
            return node.Children.Any(c => ShouldIncludeNode(c, isForFiles));
        }

        if (node.IsScript && node.Script != null)
        {
            // Include scripts based on their target type
            return isForFiles ? node.Script.AppliesToFiles() : node.Script.AppliesToFolders();
        }

        return false;
    }

    /// <summary>
    /// Builds the command string for invoking the script.
    /// </summary>
    /// <param name="exePath">The path to the RightClickPS executable.</param>
    /// <param name="scriptPath">The path to the PowerShell script.</param>
    /// <returns>The command string for the registry.</returns>
    public static string BuildCommand(string exePath, string scriptPath)
    {
        // Quote both paths to handle spaces
        // Format: "exePath" execute "scriptPath" "%1"
        return $"\"{exePath}\" execute \"{scriptPath}\" \"%1\"";
    }

    /// <summary>
    /// Sanitizes a name for use as a registry key name.
    /// Removes or replaces characters that are invalid in registry key names.
    /// </summary>
    /// <param name="name">The name to sanitize.</param>
    /// <returns>A sanitized key name.</returns>
    public static string SanitizeKeyName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "Unknown";
        }

        // Registry key names cannot contain backslashes
        // Also remove other potentially problematic characters
        var sanitized = name
            .Replace("\\", "_")
            .Replace("/", "_")
            .Replace("*", "_")
            .Replace("?", "_")
            .Replace("\"", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace("|", "_")
            .Replace(":", "_");

        // Remove leading/trailing whitespace and dots
        sanitized = sanitized.Trim().Trim('.');

        // Ensure the key name isn't empty after sanitization
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return "Unknown";
        }

        return sanitized;
    }

    /// <summary>
    /// Unregisters all RightClickPS context menu entries.
    /// </summary>
    /// <returns>True if unregistration was successful, false otherwise.</returns>
    public bool Unregister()
    {
        bool success = true;

        try
        {
            // Remove from files context
            success &= DeleteRegistryKey(RegistryConstants.FilesShellPath, RegistryConstants.AppKeyName);

            // Remove from directory context
            success &= DeleteRegistryKey(RegistryConstants.DirectoryShellPath, RegistryConstants.AppKeyName);
        }
        catch (Exception)
        {
            success = false;
        }

        return success;
    }

    /// <summary>
    /// Deletes a registry key and all its subkeys.
    /// </summary>
    /// <param name="parentKeyPath">The path to the parent key.</param>
    /// <param name="subKeyName">The name of the subkey to delete.</param>
    /// <returns>True if the key was deleted or didn't exist, false on error.</returns>
    private bool DeleteRegistryKey(string parentKeyPath, string subKeyName)
    {
        try
        {
            using var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            using var parentKey = hkcu.OpenSubKey(parentKeyPath, writable: true);

            if (parentKey == null)
            {
                // Parent key doesn't exist, nothing to delete
                return true;
            }

            // Check if the subkey exists
            using var subKey = parentKey.OpenSubKey(subKeyName);
            if (subKey == null)
            {
                // Subkey doesn't exist, nothing to delete
                return true;
            }

            // Close the subkey before deleting
            subKey.Close();

            // Delete the subkey tree
            parentKey.DeleteSubKeyTree(subKeyName, throwOnMissingSubKey: false);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the RightClickPS context menu is currently registered.
    /// </summary>
    /// <returns>True if registered for at least one context, false otherwise.</returns>
    public bool IsRegistered()
    {
        return IsRegisteredForFiles() || IsRegisteredForDirectories();
    }

    /// <summary>
    /// Checks if the context menu is registered for files.
    /// </summary>
    /// <returns>True if registered for files, false otherwise.</returns>
    public bool IsRegisteredForFiles()
    {
        return KeyExists(RegistryConstants.FilesAppPath);
    }

    /// <summary>
    /// Checks if the context menu is registered for directories.
    /// </summary>
    /// <returns>True if registered for directories, false otherwise.</returns>
    public bool IsRegisteredForDirectories()
    {
        return KeyExists(RegistryConstants.DirectoryAppPath);
    }

    /// <summary>
    /// Checks if a registry key exists.
    /// </summary>
    /// <param name="keyPath">The path to the key.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    private bool KeyExists(string keyPath)
    {
        try
        {
            using var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            using var key = hkcu.OpenSubKey(keyPath);
            return key != null;
        }
        catch
        {
            return false;
        }
    }
}
