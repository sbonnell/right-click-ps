using System.Diagnostics;
using System.Text;

namespace RightClickPS.Scripts;

/// <summary>
/// Executes PowerShell scripts with selected files passed as the $SelectedFiles array variable.
/// Handles admin elevation via UAC when required.
/// </summary>
public class ScriptExecutor
{
    private readonly string _powershellPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptExecutor"/> class.
    /// </summary>
    /// <param name="powershellPath">Optional custom path to powershell.exe. Defaults to "powershell.exe".</param>
    public ScriptExecutor(string? powershellPath = null)
    {
        _powershellPath = powershellPath ?? "powershell.exe";
    }

    /// <summary>
    /// Executes a PowerShell script with the specified files.
    /// </summary>
    /// <param name="scriptPath">The full path to the PowerShell script to execute.</param>
    /// <param name="selectedFiles">The list of file/folder paths to pass as $SelectedFiles.</param>
    /// <param name="runAsAdmin">Whether to run with elevated privileges.</param>
    /// <returns>The exit code from the PowerShell process, or -1 if an error occurred.</returns>
    public int Execute(string scriptPath, IEnumerable<string> selectedFiles, bool runAsAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(scriptPath))
        {
            ShowError("Script path cannot be empty.");
            return -1;
        }

        if (!File.Exists(scriptPath))
        {
            ShowError($"Script not found: {scriptPath}");
            return -1;
        }

        var filesList = selectedFiles?.ToList() ?? new List<string>();

        try
        {
            if (runAsAdmin)
            {
                return ExecuteElevated(scriptPath, filesList);
            }
            else
            {
                return ExecuteDirect(scriptPath, filesList);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to execute script: {ex.Message}");
            return -1;
        }
    }

    /// <summary>
    /// Builds the PowerShell command string that sets up $SelectedFiles and executes the script.
    /// </summary>
    /// <param name="scriptPath">The full path to the PowerShell script.</param>
    /// <param name="selectedFiles">The list of file/folder paths.</param>
    /// <returns>The PowerShell command string.</returns>
    public string BuildPowerShellCommand(string scriptPath, IEnumerable<string> selectedFiles)
    {
        var filesArray = BuildSelectedFilesArray(selectedFiles);
        var escapedScriptPath = EscapeForPowerShell(scriptPath);

        // Format: $SelectedFiles = @('file1','file2'); & 'script.ps1'
        return $"$SelectedFiles = {filesArray}; & '{escapedScriptPath}'";
    }

    /// <summary>
    /// Builds the $SelectedFiles array expression for PowerShell.
    /// </summary>
    /// <param name="selectedFiles">The list of file/folder paths.</param>
    /// <returns>A PowerShell array expression like @('file1','file2').</returns>
    public string BuildSelectedFilesArray(IEnumerable<string> selectedFiles)
    {
        var files = selectedFiles?.ToList() ?? new List<string>();

        if (files.Count == 0)
        {
            return "@()";
        }

        var escapedFiles = files.Select(f => $"'{EscapeForPowerShell(f)}'");
        return $"@({string.Join(",", escapedFiles)})";
    }

    /// <summary>
    /// Escapes a string for use in a PowerShell single-quoted string.
    /// In single-quoted strings, only single quotes need to be escaped by doubling them.
    /// </summary>
    /// <param name="value">The string to escape.</param>
    /// <returns>The escaped string (without surrounding quotes).</returns>
    public string EscapeForPowerShell(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // In PowerShell single-quoted strings, escape single quotes by doubling them
        return value.Replace("'", "''");
    }

    /// <summary>
    /// Executes the script directly in the current process context.
    /// </summary>
    private int ExecuteDirect(string scriptPath, List<string> selectedFiles)
    {
        // Create a temp wrapper script that sets up $SelectedFiles and calls the actual script
        // This avoids escaping issues with -Command and ensures Windows Forms work properly
        var tempScript = Path.Combine(Path.GetTempPath(), $"RightClickPS_{Guid.NewGuid():N}.ps1");

        try
        {
            var filesArray = BuildSelectedFilesArray(selectedFiles);
            var wrapperContent = $"$SelectedFiles = {filesArray}\r\n& '{EscapeForPowerShell(scriptPath)}'";
            File.WriteAllText(tempScript, wrapperContent);

            // Use -STA for Single-Threaded Apartment mode required by Windows Forms
            // Use -WindowStyle Hidden to hide the PowerShell console (dialogs still show)
            var startInfo = new ProcessStartInfo
            {
                FileName = _powershellPath,
                Arguments = $"-NoProfile -STA -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{tempScript}\"",
                UseShellExecute = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            process.WaitForExit();

            return process.ExitCode;
        }
        finally
        {
            // Clean up temp script
            try { File.Delete(tempScript); } catch { }
        }
    }

    /// <summary>
    /// Executes the script with elevated privileges by re-launching RightClickPS.exe with runas verb.
    /// </summary>
    private int ExecuteElevated(string scriptPath, List<string> selectedFiles)
    {
        // Get the path to the current executable
        var exePath = GetCurrentExecutablePath();

        // Build arguments for the elevated process
        // Format: execute "script.ps1" "file1" "file2" ...
        var argsBuilder = new StringBuilder();
        argsBuilder.Append("execute ");
        argsBuilder.Append($"\"{scriptPath}\"");

        foreach (var file in selectedFiles)
        {
            argsBuilder.Append($" \"{file}\"");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = argsBuilder.ToString(),
            UseShellExecute = true,
            Verb = "runas"  // This triggers UAC elevation
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process != null)
            {
                process.WaitForExit();
                return process.ExitCode;
            }
            return -1;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User cancelled the UAC prompt (ERROR_CANCELLED)
            ShowError("Administrator privileges are required to run this script. The operation was cancelled.");
            return -1;
        }
    }

    /// <summary>
    /// Gets the path to the currently running executable.
    /// </summary>
    /// <returns>The full path to the current executable.</returns>
    protected virtual string GetCurrentExecutablePath()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath))
        {
            return processPath;
        }

        // Fallback: use AppContext.BaseDirectory (works in single-file apps)
        var assembly = System.Reflection.Assembly.GetEntryAssembly();
        if (assembly != null)
        {
            var baseDir = AppContext.BaseDirectory;
            var exeName = assembly.GetName().Name + ".exe";
            return Path.Combine(baseDir, exeName);
        }

        // Final fallback: use current process main module
        using var currentProcess = Process.GetCurrentProcess();
        return currentProcess.MainModule?.FileName ?? "RightClickPS.exe";
    }

    /// <summary>
    /// Escapes a string for use as a command line argument within double quotes.
    /// </summary>
    /// <param name="argument">The argument to escape.</param>
    /// <returns>The escaped argument.</returns>
    private string EscapeCommandLineArgument(string argument)
    {
        if (string.IsNullOrEmpty(argument))
        {
            return string.Empty;
        }

        // For command line arguments in double quotes, we need to escape:
        // - Double quotes with backslash
        // - Backslashes before double quotes
        var result = new StringBuilder();

        for (int i = 0; i < argument.Length; i++)
        {
            char c = argument[i];

            if (c == '"')
            {
                // Escape double quote with backslash
                result.Append("\\\"");
            }
            else if (c == '\\')
            {
                // Count consecutive backslashes
                int backslashCount = 1;
                while (i + 1 < argument.Length && argument[i + 1] == '\\')
                {
                    backslashCount++;
                    i++;
                }

                // If followed by a double quote, double the backslashes
                if (i + 1 < argument.Length && argument[i + 1] == '"')
                {
                    result.Append(new string('\\', backslashCount * 2));
                }
                else
                {
                    result.Append(new string('\\', backslashCount));
                }
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Shows an error message to the user.
    /// Uses Console.Error for console applications.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    protected virtual void ShowError(string message)
    {
        // For a console application, write to stderr
        Console.Error.WriteLine($"Error: {message}");

        // Optionally, also show a MessageBox for GUI visibility
        // This requires a reference to System.Windows.Forms
        // For now, we'll keep it console-based as that's more appropriate for the CLI
        // MessageBox can be added if needed by uncommenting:
        // System.Windows.Forms.MessageBox.Show(message, "RightClickPS Error",
        //     System.Windows.Forms.MessageBoxButtons.OK,
        //     System.Windows.Forms.MessageBoxIcon.Error);
    }
}
