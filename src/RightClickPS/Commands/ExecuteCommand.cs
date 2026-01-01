using RightClickPS.Scripts;

namespace RightClickPS.Commands;

// BatchCollector is used from RightClickPS.Scripts namespace

/// <summary>
/// Command to execute a PowerShell script with provided file paths.
/// This command is invoked from the context menu via registry entries.
/// </summary>
/// <remarks>
/// Usage: RightClickPS.exe execute "script-path" "file1" "file2" ...
///
/// The command will:
/// 1. Parse and validate command line arguments
/// 2. Load script metadata to check for @RunAsAdmin requirement
/// 3. Execute the script via ScriptExecutor, handling elevation if needed
/// 4. Return appropriate exit codes (0 = success, non-zero = failure)
/// </remarks>
public class ExecuteCommand
{
    private readonly ScriptParser _scriptParser;
    private readonly ScriptExecutor _scriptExecutor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteCommand"/> class
    /// with default dependencies.
    /// </summary>
    public ExecuteCommand() : this(new ScriptParser(), new ScriptExecutor())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteCommand"/> class
    /// with specified dependencies (useful for testing).
    /// </summary>
    /// <param name="scriptParser">The script parser to use for loading metadata.</param>
    /// <param name="scriptExecutor">The script executor to use for running scripts.</param>
    public ExecuteCommand(ScriptParser scriptParser, ScriptExecutor scriptExecutor)
    {
        _scriptParser = scriptParser ?? throw new ArgumentNullException(nameof(scriptParser));
        _scriptExecutor = scriptExecutor ?? throw new ArgumentNullException(nameof(scriptExecutor));
    }

    /// <summary>
    /// Executes the command with the provided arguments.
    /// </summary>
    /// <param name="args">
    /// Command line arguments: the first element should be the script path,
    /// followed by zero or more file paths to pass to the script.
    /// </param>
    /// <returns>
    /// 0 on success, -1 on argument/validation errors, or the script's exit code.
    /// </returns>
    public int Execute(string[] args)
    {
        // Validate arguments - need at least the script path
        if (args == null || args.Length == 0)
        {
            ShowError("No script path provided. Usage: RightClickPS.exe execute \"<script-path>\" [file1] [file2] ...");
            return -1;
        }

        string scriptPath = args[0];

        if (string.IsNullOrWhiteSpace(scriptPath))
        {
            ShowError("Script path cannot be empty.");
            return -1;
        }

        // Check if the script file exists
        if (!File.Exists(scriptPath))
        {
            ShowError($"Script not found: {scriptPath}");
            return -1;
        }

        // Extract file paths from remaining arguments
        var filePaths = args.Skip(1).ToList();

        // When Windows Explorer invokes the context menu on multiple selected files,
        // it calls our command once per file. Use BatchCollector to gather all files
        // from parallel invocations and execute the script once with all of them.
        if (filePaths.Count == 1)
        {
            using var batchCollector = CreateBatchCollector(scriptPath);
            var (shouldExecute, allFiles) = batchCollector.AddFileAndWait(filePaths[0]);

            if (!shouldExecute)
            {
                // Another process will execute the script with all files
                return 0;
            }

            filePaths = allFiles;
        }

        try
        {
            // Parse script metadata to check for RunAsAdmin requirement
            var metadata = _scriptParser.Parse(scriptPath, Path.GetDirectoryName(scriptPath) ?? string.Empty);

            // Execute the script via ScriptExecutor
            // The executor handles elevation internally if RunAsAdmin is true
            return _scriptExecutor.Execute(scriptPath, filePaths, metadata.RunAsAdmin);
        }
        catch (FileNotFoundException ex)
        {
            ShowError($"Script not found: {ex.FileName ?? scriptPath}");
            return -1;
        }
        catch (IOException ex)
        {
            ShowError($"Failed to read script: {ex.Message}");
            return -1;
        }
        catch (Exception ex)
        {
            ShowError($"Failed to execute script: {ex.Message}");
            return -1;
        }
    }

    /// <summary>
    /// Creates a BatchCollector for the specified script.
    /// Virtual to allow overriding in tests.
    /// </summary>
    /// <param name="scriptPath">The path to the script.</param>
    /// <returns>A new BatchCollector instance.</returns>
    protected virtual BatchCollector CreateBatchCollector(string scriptPath)
    {
        return new BatchCollector(scriptPath);
    }

    /// <summary>
    /// Displays an error message to the user.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    protected virtual void ShowError(string message)
    {
        Console.Error.WriteLine($"Error: {message}");
    }
}
