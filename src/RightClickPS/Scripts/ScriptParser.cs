using System.Text;
using System.Text.RegularExpressions;

namespace RightClickPS.Scripts;

/// <summary>
/// Parses PowerShell script files to extract metadata from block comment headers.
/// </summary>
/// <remarks>
/// The parser looks for a block comment at the start of the file in the format:
/// <code>
/// &lt;#
/// @Name: Display Name
/// @Description: What the script does
/// @Extensions: .png,.jpg,.gif
/// @TargetType: Files
/// @RunAsAdmin: false
/// #&gt;
/// </code>
/// All fields are optional and have sensible defaults.
/// </remarks>
public partial class ScriptParser
{
    // Regex to extract the block comment content between <# and #>
    [GeneratedRegex(@"^\s*<#(.*?)#>", RegexOptions.Singleline)]
    private static partial Regex BlockCommentRegex();

    // Regex to extract @Field: Value pairs (case-insensitive field names)
    [GeneratedRegex(@"@(\w+)\s*:\s*(.+?)(?=\r?\n|$)", RegexOptions.IgnoreCase)]
    private static partial Regex FieldValueRegex();

    /// <summary>
    /// Parses a PowerShell script file and extracts its metadata.
    /// </summary>
    /// <param name="filePath">The absolute path to the .ps1 script file.</param>
    /// <param name="basePath">The base scripts folder path, used to calculate RelativePath.</param>
    /// <returns>A <see cref="ScriptMetadata"/> object containing the parsed metadata.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the script file does not exist.</exception>
    /// <exception cref="IOException">Thrown when the file cannot be read.</exception>
    public ScriptMetadata Parse(string filePath, string basePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Script file not found.", filePath);
        }

        // Read file content with UTF-8 encoding (handles BOM automatically)
        string content = File.ReadAllText(filePath, Encoding.UTF8);

        return ParseContent(content, filePath, basePath);
    }

    /// <summary>
    /// Parses script content directly (useful for testing without file I/O).
    /// </summary>
    /// <param name="content">The script content to parse.</param>
    /// <param name="filePath">The full path to use for FilePath property.</param>
    /// <param name="basePath">The base scripts folder path for RelativePath calculation.</param>
    /// <returns>A <see cref="ScriptMetadata"/> object containing the parsed metadata.</returns>
    public ScriptMetadata ParseContent(string content, string filePath, string basePath)
    {
        var metadata = new ScriptMetadata
        {
            FilePath = filePath,
            RelativePath = CalculateRelativePath(filePath, basePath)
        };

        // Set default name from filename
        metadata.Name = Path.GetFileNameWithoutExtension(filePath);

        // Try to extract block comment
        var blockMatch = BlockCommentRegex().Match(content);
        if (!blockMatch.Success)
        {
            // No metadata block found, return defaults
            return metadata;
        }

        string blockContent = blockMatch.Groups[1].Value;

        // Parse all @Field: Value pairs
        var fieldMatches = FieldValueRegex().Matches(blockContent);

        foreach (Match match in fieldMatches)
        {
            string fieldName = match.Groups[1].Value.Trim();
            string fieldValue = match.Groups[2].Value.Trim();

            ProcessField(metadata, fieldName, fieldValue);
        }

        return metadata;
    }

    /// <summary>
    /// Processes a single metadata field and updates the ScriptMetadata object.
    /// </summary>
    private static void ProcessField(ScriptMetadata metadata, string fieldName, string fieldValue)
    {
        switch (fieldName.ToLowerInvariant())
        {
            case "name":
                if (!string.IsNullOrWhiteSpace(fieldValue))
                {
                    metadata.Name = fieldValue;
                }
                break;

            case "description":
                metadata.Description = string.IsNullOrWhiteSpace(fieldValue) ? null : fieldValue;
                break;

            case "extensions":
                metadata.Extensions = ParseExtensions(fieldValue);
                break;

            case "targettype":
                metadata.TargetType = ParseTargetType(fieldValue);
                break;

            case "runasadmin":
                metadata.RunAsAdmin = ParseBool(fieldValue);
                break;
        }
    }

    /// <summary>
    /// Parses a comma-separated list of file extensions.
    /// </summary>
    private static List<string> ParseExtensions(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string> { "*" };
        }

        var extensions = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ext => ext == "*" ? ext : (ext.StartsWith('.') ? ext : "." + ext))
            .ToList();

        return extensions.Count == 0 ? new List<string> { "*" } : extensions;
    }

    /// <summary>
    /// Parses a TargetType value (case-insensitive).
    /// </summary>
    private static TargetType ParseTargetType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TargetType.Both;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "files" or "file" => TargetType.Files,
            "folders" or "folder" or "directories" or "directory" => TargetType.Folders,
            _ => TargetType.Both
        };
    }

    /// <summary>
    /// Parses a boolean value supporting true/false, yes/no (case-insensitive).
    /// </summary>
    private static bool ParseBool(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "true" or "yes" or "1" => true,
            _ => false
        };
    }

    /// <summary>
    /// Calculates the relative path from basePath to filePath.
    /// </summary>
    private static string CalculateRelativePath(string filePath, string basePath)
    {
        if (string.IsNullOrEmpty(basePath))
        {
            return Path.GetFileName(filePath);
        }

        try
        {
            // Normalize paths
            var normalizedFilePath = Path.GetFullPath(filePath);
            var normalizedBasePath = Path.GetFullPath(basePath);

            // Ensure base path ends with directory separator
            if (!normalizedBasePath.EndsWith(Path.DirectorySeparatorChar))
            {
                normalizedBasePath += Path.DirectorySeparatorChar;
            }

            // Calculate relative path
            var relativePath = Path.GetRelativePath(normalizedBasePath, normalizedFilePath);
            return relativePath;
        }
        catch
        {
            // Fallback to filename if relative path calculation fails
            return Path.GetFileName(filePath);
        }
    }
}
