using RightClickPS.Commands;

namespace RightClickPS;

/// <summary>
/// Main entry point for the RightClickPS application.
/// Routes CLI commands to the appropriate command handlers.
/// </summary>
/// <remarks>
/// Usage:
///   RightClickPS.exe register               - Create context menu entries from scripts folder
///   RightClickPS.exe unregister             - Remove all context menu entries
///   RightClickPS.exe execute "script" files - Run a script with provided file paths
///   RightClickPS.exe help                   - Show usage information
/// </remarks>
public class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code: 0 on success, non-zero on failure.</returns>
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return 0;
        }

        string command = args[0].ToLowerInvariant();

        return command switch
        {
            "register" => HandleRegister(),
            "unregister" => HandleUnregister(),
            "execute" => HandleExecute(args.Skip(1).ToArray()),
            "help" or "-h" or "--help" or "/?" => HandleHelp(),
            _ => HandleUnknownCommand(command)
        };
    }

    /// <summary>
    /// Handles the register command.
    /// </summary>
    /// <returns>Exit code from the register command.</returns>
    private static int HandleRegister()
    {
        var command = new RegisterCommand();
        return command.Execute();
    }

    /// <summary>
    /// Handles the unregister command.
    /// </summary>
    /// <returns>Exit code from the unregister command.</returns>
    private static int HandleUnregister()
    {
        var command = new UnregisterCommand();
        return command.Execute();
    }

    /// <summary>
    /// Handles the execute command.
    /// </summary>
    /// <param name="args">Arguments for the execute command (script path and file paths).</param>
    /// <returns>Exit code from the execute command.</returns>
    private static int HandleExecute(string[] args)
    {
        var command = new ExecuteCommand();
        return command.Execute(args);
    }

    /// <summary>
    /// Handles the help command.
    /// </summary>
    /// <returns>Always returns 0.</returns>
    private static int HandleHelp()
    {
        ShowHelp();
        return 0;
    }

    /// <summary>
    /// Handles an unknown command.
    /// </summary>
    /// <param name="command">The unknown command that was provided.</param>
    /// <returns>Always returns 1.</returns>
    private static int HandleUnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        Console.Error.WriteLine();
        ShowHelp();
        return 1;
    }

    /// <summary>
    /// Displays usage information.
    /// </summary>
    private static void ShowHelp()
    {
        Console.WriteLine("RightClickPS - Windows context menu extension for PowerShell scripts");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  RightClickPS.exe <command> [arguments]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  register      Scan scripts folder and create context menu entries");
        Console.WriteLine("  unregister    Remove all RightClickPS context menu entries");
        Console.WriteLine("  execute       Run a PowerShell script with file paths");
        Console.WriteLine("  help          Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  RightClickPS.exe register");
        Console.WriteLine("  RightClickPS.exe unregister");
        Console.WriteLine("  RightClickPS.exe execute \"C:\\Scripts\\Convert.ps1\" \"file1.png\" \"file2.png\"");
        Console.WriteLine();
        Console.WriteLine("Configuration:");
        Console.WriteLine("  Edit config.json in the application directory to configure:");
        Console.WriteLine("  - menuName:        Root context menu display name");
        Console.WriteLine("  - scriptsPath:     Path to your scripts folder");
        Console.WriteLine("  - systemScriptsPath: Path to built-in system scripts");
        Console.WriteLine("  - maxDepth:        Maximum folder depth for submenus");
    }
}
