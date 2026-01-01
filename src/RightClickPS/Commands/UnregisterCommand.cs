using RightClickPS.Registry;

namespace RightClickPS.Commands;

/// <summary>
/// Command to remove all RightClickPS context menu entries from Windows registry.
/// This command removes entries from both files (*) and directories (Directory) contexts.
/// </summary>
/// <remarks>
/// Usage: RightClickPS.exe unregister
///
/// The command will:
/// 1. Remove all RightClickPS entries from HKCU\Software\Classes\*\shell
/// 2. Remove all RightClickPS entries from HKCU\Software\Classes\Directory\shell
/// 3. Report success or failure to the user
/// </remarks>
public class UnregisterCommand
{
    private readonly ContextMenuRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnregisterCommand"/> class
    /// with default dependencies.
    /// </summary>
    public UnregisterCommand() : this(new ContextMenuRegistry())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnregisterCommand"/> class
    /// with specified dependencies (useful for testing).
    /// </summary>
    /// <param name="registry">The context menu registry to use for unregistration.</param>
    public UnregisterCommand(ContextMenuRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// Executes the unregister command.
    /// </summary>
    /// <returns>
    /// 0 on success, non-zero on failure.
    /// </returns>
    public int Execute()
    {
        try
        {
            // Check if anything is currently registered before attempting removal
            bool wasRegisteredForFiles = _registry.IsRegisteredForFiles();
            bool wasRegisteredForDirectories = _registry.IsRegisteredForDirectories();
            bool wasRegistered = wasRegisteredForFiles || wasRegisteredForDirectories;

            // Attempt to unregister all entries
            bool success = _registry.Unregister();

            if (!success)
            {
                WriteError("Failed to remove some registry entries. You may need to remove them manually.");
                return 1;
            }

            // Report what was removed
            if (wasRegistered)
            {
                WriteSuccess("Successfully removed RightClickPS context menu entries.");

                if (wasRegisteredForFiles)
                {
                    WriteInfo("  - Removed entries for files context");
                }

                if (wasRegisteredForDirectories)
                {
                    WriteInfo("  - Removed entries for directories context");
                }
            }
            else
            {
                WriteInfo("No RightClickPS context menu entries were registered. Nothing to remove.");
            }

            return 0;
        }
        catch (UnauthorizedAccessException)
        {
            WriteError("Access denied. Unable to modify registry entries.");
            return 1;
        }
        catch (Exception ex)
        {
            WriteError($"An error occurred while removing registry entries: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Writes a success message to the console.
    /// </summary>
    /// <param name="message">The message to display.</param>
    protected virtual void WriteSuccess(string message)
    {
        Console.WriteLine(message);
    }

    /// <summary>
    /// Writes an informational message to the console.
    /// </summary>
    /// <param name="message">The message to display.</param>
    protected virtual void WriteInfo(string message)
    {
        Console.WriteLine(message);
    }

    /// <summary>
    /// Writes an error message to the console.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    protected virtual void WriteError(string message)
    {
        Console.Error.WriteLine($"Error: {message}");
    }
}
