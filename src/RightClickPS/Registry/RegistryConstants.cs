namespace RightClickPS.Registry;

/// <summary>
/// Contains constants for Windows registry paths used by the RightClickPS context menu extension.
/// </summary>
/// <remarks>
/// These constants define the registry locations where context menu entries are registered.
/// All paths are under HKEY_CURRENT_USER, which allows registration without administrative privileges.
/// </remarks>
public static class RegistryConstants
{
    /// <summary>
    /// The application-specific key name used for all RightClickPS registry entries.
    /// </summary>
    public const string AppKeyName = "RightClickPS";

    /// <summary>
    /// The root registry key under HKEY_CURRENT_USER where class registrations are stored.
    /// </summary>
    public const string ClassesRoot = @"Software\Classes";

    /// <summary>
    /// The base path for file context menu shell extensions.
    /// The asterisk (*) matches all file types.
    /// </summary>
    public const string FilesShellPath = @"Software\Classes\*\shell";

    /// <summary>
    /// The base path for folder context menu shell extensions.
    /// </summary>
    public const string DirectoryShellPath = @"Software\Classes\Directory\shell";

    /// <summary>
    /// The full path to the RightClickPS context menu entry for files.
    /// </summary>
    public const string FilesAppPath = @"Software\Classes\*\shell\" + AppKeyName;

    /// <summary>
    /// The full path to the RightClickPS context menu entry for folders/directories.
    /// </summary>
    public const string DirectoryAppPath = @"Software\Classes\Directory\shell\" + AppKeyName;

    /// <summary>
    /// The registry value name for specifying the display text of a menu item.
    /// MUIVerb supports multilingual user interface strings.
    /// </summary>
    public const string MUIVerbValueName = "MUIVerb";

    /// <summary>
    /// The registry value name for specifying subcommands in a cascading menu.
    /// An empty string value indicates that subcommands are defined in a child "shell" key.
    /// </summary>
    public const string SubCommandsValueName = "SubCommands";

    /// <summary>
    /// The registry value name for specifying an icon for a menu item.
    /// </summary>
    public const string IconValueName = "Icon";

    /// <summary>
    /// The subkey name where shell commands are defined.
    /// </summary>
    public const string ShellSubKeyName = "shell";

    /// <summary>
    /// The subkey name where the actual command executable is specified.
    /// </summary>
    public const string CommandSubKeyName = "command";

    /// <summary>
    /// Constructs the full registry path for a menu item under the files context.
    /// </summary>
    /// <param name="itemKey">The key name of the menu item.</param>
    /// <returns>The full registry path to the menu item under the files shell.</returns>
    public static string GetFilesMenuItemPath(string itemKey)
    {
        return $@"{FilesAppPath}\{ShellSubKeyName}\{itemKey}";
    }

    /// <summary>
    /// Constructs the full registry path for a menu item under the directory context.
    /// </summary>
    /// <param name="itemKey">The key name of the menu item.</param>
    /// <returns>The full registry path to the menu item under the directory shell.</returns>
    public static string GetDirectoryMenuItemPath(string itemKey)
    {
        return $@"{DirectoryAppPath}\{ShellSubKeyName}\{itemKey}";
    }

    /// <summary>
    /// Constructs the full registry path for the command subkey of a menu item.
    /// </summary>
    /// <param name="menuItemPath">The registry path to the menu item.</param>
    /// <returns>The full registry path to the command subkey.</returns>
    public static string GetCommandPath(string menuItemPath)
    {
        return $@"{menuItemPath}\{CommandSubKeyName}";
    }

    /// <summary>
    /// Constructs the full registry path for the shell subkey of a menu item (for submenus).
    /// </summary>
    /// <param name="menuItemPath">The registry path to the menu item.</param>
    /// <returns>The full registry path to the shell subkey.</returns>
    public static string GetShellPath(string menuItemPath)
    {
        return $@"{menuItemPath}\{ShellSubKeyName}";
    }
}
