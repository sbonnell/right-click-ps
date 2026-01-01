using System.Text.Json;

namespace RightClickPS.Config;

/// <summary>
/// Loads and validates application configuration from config.json.
/// </summary>
public class ConfigLoader
{
    private const string ConfigFileName = "config.json";
    private const string ResourceFolderName = "RightClickPS";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Loads configuration from config.json in the RightClickPS resource folder.
    /// Returns sensible defaults if the file doesn't exist.
    /// </summary>
    /// <returns>The loaded and validated AppConfig.</returns>
    /// <exception cref="ConfigurationException">Thrown when the config file exists but is invalid.</exception>
    public AppConfig Load()
    {
        var configPath = GetConfigPath();
        return LoadFromPath(configPath);
    }

    /// <summary>
    /// Loads configuration from a specific path.
    /// </summary>
    /// <param name="configPath">The path to the config file.</param>
    /// <returns>The loaded and validated AppConfig.</returns>
    /// <exception cref="ConfigurationException">Thrown when the config file exists but is invalid.</exception>
    public AppConfig LoadFromPath(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return CreateDefaultConfig();
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions)
                ?? throw new ConfigurationException("Config file deserialized to null.");

            ExpandEnvironmentVariables(config);
            ValidateConfig(config, configPath);

            return config;
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException($"Failed to parse config file: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new ConfigurationException($"Failed to read config file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the path to the config file in the RightClickPS resource folder.
    /// Looks in the RightClickPS subfolder relative to the exe location.
    /// Falls back to the exe directory for backwards compatibility.
    /// </summary>
    /// <returns>The full path to config.json.</returns>
    public static string GetConfigPath()
    {
        var exeDirectory = GetExeDirectory();

        // First, try RightClickPS subfolder (new structure)
        var resourceFolder = Path.Combine(exeDirectory, ResourceFolderName);
        var configInSubfolder = Path.Combine(resourceFolder, ConfigFileName);
        if (File.Exists(configInSubfolder))
        {
            return configInSubfolder;
        }

        // Fall back to exe directory (old structure / development)
        return Path.Combine(exeDirectory, ConfigFileName);
    }

    /// <summary>
    /// Gets the directory containing the executable.
    /// </summary>
    /// <returns>The exe directory path.</returns>
    public static string GetExeDirectory()
    {
        // Use the process path to get the actual exe location
        var exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath))
        {
            return Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory;
        }
        return AppContext.BaseDirectory;
    }

    /// <summary>
    /// Gets the path to the RightClickPS resource folder.
    /// </summary>
    /// <returns>The resource folder path.</returns>
    public static string GetResourceFolder()
    {
        var exeDirectory = GetExeDirectory();
        var resourceFolder = Path.Combine(exeDirectory, ResourceFolderName);

        // Return subfolder if it exists, otherwise exe directory
        if (Directory.Exists(resourceFolder))
        {
            return resourceFolder;
        }
        return exeDirectory;
    }

    /// <summary>
    /// Creates a default configuration when no config file exists.
    /// </summary>
    private static AppConfig CreateDefaultConfig()
    {
        return new AppConfig
        {
            MenuName = "PowerShell Scripts",
            ScriptsPath = null,
            SystemScriptsPath = "./SystemScripts",
            MaxDepth = 3,
            IconPath = null
        };
    }

    /// <summary>
    /// Expands environment variables in path properties.
    /// </summary>
    private static void ExpandEnvironmentVariables(AppConfig config)
    {
        if (!string.IsNullOrEmpty(config.ScriptsPath))
        {
            config.ScriptsPath = Environment.ExpandEnvironmentVariables(config.ScriptsPath);
        }

        if (!string.IsNullOrEmpty(config.SystemScriptsPath))
        {
            config.SystemScriptsPath = Environment.ExpandEnvironmentVariables(config.SystemScriptsPath);
        }

        if (!string.IsNullOrEmpty(config.IconPath))
        {
            config.IconPath = Environment.ExpandEnvironmentVariables(config.IconPath);
        }
    }

    /// <summary>
    /// Validates the configuration and throws if invalid.
    /// </summary>
    private static void ValidateConfig(AppConfig config, string configPath)
    {
        // Validate scriptsPath exists if provided
        if (!string.IsNullOrEmpty(config.ScriptsPath))
        {
            // Resolve relative paths from the config file directory
            var resolvedPath = ResolvePathRelativeToConfig(config.ScriptsPath, configPath);

            if (!Directory.Exists(resolvedPath))
            {
                throw new ConfigurationException(
                    $"Scripts path does not exist: {resolvedPath}");
            }

            // Update to the resolved absolute path
            config.ScriptsPath = resolvedPath;
        }

        // Resolve systemScriptsPath relative to config file directory
        if (!string.IsNullOrEmpty(config.SystemScriptsPath))
        {
            config.SystemScriptsPath = ResolvePathRelativeToConfig(config.SystemScriptsPath, configPath);
        }

        // Resolve iconPath relative to config file directory if provided
        if (!string.IsNullOrEmpty(config.IconPath))
        {
            config.IconPath = ResolvePathRelativeToConfig(config.IconPath, configPath);
        }

        // Validate maxDepth is reasonable
        if (config.MaxDepth < 1)
        {
            config.MaxDepth = 1;
        }
        else if (config.MaxDepth > 10)
        {
            config.MaxDepth = 10;
        }
    }

    /// <summary>
    /// Resolves a path relative to the config file's directory.
    /// If the path is already absolute, returns it unchanged.
    /// </summary>
    private static string ResolvePathRelativeToConfig(string path, string configPath)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        var configDirectory = Path.GetDirectoryName(configPath) ?? AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(configDirectory, path));
    }
}

/// <summary>
/// Exception thrown when configuration loading or validation fails.
/// </summary>
public class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message)
    {
    }

    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
