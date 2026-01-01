using System.Reflection;
using RightClickPS.Config;
using RightClickPS.Registry;
using RightClickPS.Scripts;

namespace RightClickPS.Commands;

/// <summary>
/// Command to register the RightClickPS context menu entries.
/// Orchestrates loading configuration, discovering scripts, and creating registry entries.
/// </summary>
/// <remarks>
/// Usage: RightClickPS.exe register
///
/// The command will:
/// 1. Load configuration from config.json
/// 2. Discover scripts from the scripts folder
/// 3. Register context menu entries in the Windows registry
/// 4. Report results to the user
/// </remarks>
public class RegisterCommand
{
    private readonly ConfigLoader _configLoader;
    private readonly ScriptDiscovery _scriptDiscovery;
    private readonly ContextMenuRegistry _contextMenuRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterCommand"/> class
    /// with default dependencies.
    /// </summary>
    public RegisterCommand() : this(new ConfigLoader(), new ScriptDiscovery(), new ContextMenuRegistry())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterCommand"/> class
    /// with specified dependencies (useful for testing).
    /// </summary>
    /// <param name="configLoader">The configuration loader to use.</param>
    /// <param name="scriptDiscovery">The script discovery service to use.</param>
    /// <param name="contextMenuRegistry">The context menu registry manager to use.</param>
    public RegisterCommand(ConfigLoader configLoader, ScriptDiscovery scriptDiscovery, ContextMenuRegistry contextMenuRegistry)
    {
        _configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
        _scriptDiscovery = scriptDiscovery ?? throw new ArgumentNullException(nameof(scriptDiscovery));
        _contextMenuRegistry = contextMenuRegistry ?? throw new ArgumentNullException(nameof(contextMenuRegistry));
    }

    /// <summary>
    /// Executes the register command.
    /// </summary>
    /// <returns>0 on success, non-zero on failure.</returns>
    public int Execute()
    {
        try
        {
            WriteInfo("RightClickPS - Context Menu Registration");
            WriteInfo("=========================================");
            WriteInfo("");

            // Step 1: Load configuration
            WriteInfo("Loading configuration...");
            AppConfig config;
            try
            {
                config = _configLoader.Load();
                WriteInfo($"  Menu name: {config.MenuName}");
                WriteInfo($"  Scripts path: {config.ScriptsPath}");
                WriteInfo($"  Max depth: {config.MaxDepth}");
            }
            catch (ConfigurationException ex)
            {
                WriteError($"Configuration error: {ex.Message}");
                return 1;
            }

            // Step 2: Discover scripts
            WriteInfo("");
            WriteInfo("Discovering scripts...");
            var menuRoot = _scriptDiscovery.DiscoverScripts(
                config.ScriptsPath,
                config.MaxDepth);

            var scriptCount = ScriptDiscovery.CountScripts(menuRoot);
            var folderCount = ScriptDiscovery.CountFolders(menuRoot);

            if (scriptCount == 0)
            {
                WriteWarning("No scripts found. Context menu will not be created.");
                WriteInfo("  Ensure your scripts path is configured in config.json");
                WriteInfo("  and contains .ps1 files with proper metadata headers.");
                return 0;
            }

            WriteInfo($"  Found {scriptCount} script(s) in {folderCount} folder(s)");

            // Step 3: Get executable path for registry commands
            var exePath = GetExecutablePath();
            WriteInfo($"  Executable path: {exePath}");

            // Step 4: Register context menu
            WriteInfo("");
            WriteInfo("Registering context menu...");
            if (!string.IsNullOrEmpty(config.IconPath))
            {
                WriteInfo($"  Icon path: {config.IconPath}");
            }
            var result = _contextMenuRegistry.Register(menuRoot, config.MenuName, exePath, config.IconPath);

            if (!result.Success)
            {
                WriteError($"Registration failed: {result.ErrorMessage}");
                return 1;
            }

            // Step 5: Report success
            WriteInfo("");
            WriteSuccess("Registration completed successfully!");
            WriteInfo($"  Files context menu items: {result.FilesMenuItemCount}");
            WriteInfo($"  Directory context menu items: {result.DirectoryMenuItemCount}");
            WriteInfo($"  Total menu items: {result.TotalMenuItemCount}");
            WriteInfo("");
            WriteInfo("Right-click on files or folders to access your PowerShell scripts.");

            return 0;
        }
        catch (Exception ex)
        {
            WriteError($"Unexpected error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Gets the path to the currently executing RightClickPS executable.
    /// </summary>
    /// <returns>The full path to the executable.</returns>
    protected virtual string GetExecutablePath()
    {
        // Prefer Environment.ProcessPath as it returns the actual .exe
        var location = Environment.ProcessPath;

        // Fallback to entry assembly location
        if (string.IsNullOrEmpty(location))
        {
            location = Assembly.GetEntryAssembly()?.Location;
        }

        // Final fallback to executing assembly
        if (string.IsNullOrEmpty(location))
        {
            location = Assembly.GetExecutingAssembly().Location;
        }

        // Ensure we have a valid path
        if (string.IsNullOrEmpty(location))
        {
            throw new InvalidOperationException("Unable to determine executable path.");
        }

        // If we got a .dll path, try to find the corresponding .exe
        if (location.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            var exePath = Path.ChangeExtension(location, ".exe");
            if (File.Exists(exePath))
            {
                return exePath;
            }
        }

        return location;
    }

    /// <summary>
    /// Writes an informational message to the console.
    /// </summary>
    /// <param name="message">The message to write.</param>
    protected virtual void WriteInfo(string message)
    {
        Console.WriteLine(message);
    }

    /// <summary>
    /// Writes a success message to the console.
    /// </summary>
    /// <param name="message">The message to write.</param>
    protected virtual void WriteSuccess(string message)
    {
        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    /// <summary>
    /// Writes a warning message to the console.
    /// </summary>
    /// <param name="message">The message to write.</param>
    protected virtual void WriteWarning(string message)
    {
        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Warning: {message}");
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    /// <summary>
    /// Writes an error message to the console.
    /// </summary>
    /// <param name="message">The message to write.</param>
    protected virtual void WriteError(string message)
    {
        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Error: {message}");
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }
}
