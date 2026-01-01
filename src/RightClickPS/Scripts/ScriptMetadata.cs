namespace RightClickPS.Scripts;

/// <summary>
/// Specifies the types of items (files, folders, or both) that a script can be applied to.
/// </summary>
public enum TargetType
{
    /// <summary>
    /// Script applies only to files.
    /// </summary>
    Files,

    /// <summary>
    /// Script applies only to folders/directories.
    /// </summary>
    Folders,

    /// <summary>
    /// Script applies to both files and folders.
    /// </summary>
    Both
}

/// <summary>
/// Represents metadata extracted from a PowerShell script's block comment header.
/// This metadata controls how the script appears in the context menu and how it executes.
/// </summary>
public class ScriptMetadata
{
    /// <summary>
    /// Gets or sets the display name for the script in the context menu.
    /// If not specified in the script, defaults to the filename without extension.
    /// </summary>
    /// <remarks>
    /// Corresponds to the @Name field in the script's metadata block.
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what the script does.
    /// Reserved for future use (e.g., tooltip text).
    /// </summary>
    /// <remarks>
    /// Corresponds to the @Description field in the script's metadata block.
    /// </remarks>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the list of file extensions this script applies to.
    /// An empty list or a list containing "*" indicates the script applies to all file types.
    /// Extensions should include the leading dot (e.g., ".png", ".jpg").
    /// </summary>
    /// <remarks>
    /// Corresponds to the @Extensions field in the script's metadata block.
    /// Default value is a list containing "*" (all extensions).
    /// </remarks>
    public List<string> Extensions { get; set; } = new() { "*" };

    /// <summary>
    /// Gets or sets the type of items (files, folders, or both) this script can be applied to.
    /// </summary>
    /// <remarks>
    /// Corresponds to the @TargetType field in the script's metadata block.
    /// Default value is <see cref="TargetType.Both"/>.
    /// </remarks>
    public TargetType TargetType { get; set; } = TargetType.Both;

    /// <summary>
    /// Gets or sets whether the script requires administrator privileges to run.
    /// When true, the script will be executed with elevated privileges via UAC.
    /// </summary>
    /// <remarks>
    /// Corresponds to the @RunAsAdmin field in the script's metadata block.
    /// Default value is false.
    /// </remarks>
    public bool RunAsAdmin { get; set; } = false;

    /// <summary>
    /// Gets or sets the full absolute path to the PowerShell script file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path of the script relative to the scripts folder root.
    /// Used for building the menu hierarchy structure.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Determines whether this script applies to the specified file extension.
    /// </summary>
    /// <param name="extension">The file extension to check, including the leading dot (e.g., ".png").</param>
    /// <returns>True if the script applies to the extension; otherwise, false.</returns>
    public bool AppliesToExtension(string extension)
    {
        if (Extensions.Count == 0 || Extensions.Contains("*"))
        {
            return true;
        }

        return Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether this script applies to files based on its TargetType.
    /// </summary>
    /// <returns>True if the script applies to files; otherwise, false.</returns>
    public bool AppliesToFiles()
    {
        return TargetType == TargetType.Files || TargetType == TargetType.Both;
    }

    /// <summary>
    /// Determines whether this script applies to folders based on its TargetType.
    /// </summary>
    /// <returns>True if the script applies to folders; otherwise, false.</returns>
    public bool AppliesToFolders()
    {
        return TargetType == TargetType.Folders || TargetType == TargetType.Both;
    }
}
