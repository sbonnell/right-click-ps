using System.Text.Json.Serialization;

namespace RightClickPS.Config;

/// <summary>
/// Represents the application configuration loaded from config.json.
/// </summary>
public class AppConfig
{
    /// <summary>
    /// The display name for the root context menu item.
    /// </summary>
    [JsonPropertyName("menuName")]
    public string MenuName { get; set; } = "PowerShell Scripts";

    /// <summary>
    /// Path to the user scripts repository folder.
    /// Environment variables are expanded at load time.
    /// </summary>
    [JsonPropertyName("scriptsPath")]
    public string? ScriptsPath { get; set; }

    /// <summary>
    /// Path to built-in system scripts (relative to app directory).
    /// </summary>
    [JsonPropertyName("systemScriptsPath")]
    public string SystemScriptsPath { get; set; } = "./SystemScripts";

    /// <summary>
    /// Maximum folder depth for submenu generation.
    /// </summary>
    [JsonPropertyName("maxDepth")]
    public int MaxDepth { get; set; } = 3;

    /// <summary>
    /// Optional path to an icon file for the root menu item.
    /// </summary>
    [JsonPropertyName("iconPath")]
    public string? IconPath { get; set; }
}
