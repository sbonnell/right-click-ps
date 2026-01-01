using System.Security.Cryptography;
using System.Text;

namespace RightClickPS.Scripts;

/// <summary>
/// Collects files from multiple rapid context menu invocations and batches them
/// for a single script execution. This solves the Windows Explorer multi-select
/// issue where the context menu command is called once per file.
/// </summary>
/// <remarks>
/// When multiple files are selected in Windows Explorer and a context menu action
/// is invoked, Explorer calls the command separately for each file. This class
/// uses a temp file and mutex-based synchronization to collect all files and
/// execute the script once with the full list.
///
/// Strategy:
/// 1. Each invocation adds its file to a batch file (unique per script)
/// 2. Uses a mutex to ensure thread-safe batch file access
/// 3. The first process becomes the "executor" and waits for more files
/// 4. After a timeout with no new files, the executor runs the script
/// 5. Non-executor processes exit after contributing their file
/// </remarks>
public class BatchCollector : IDisposable
{
    private const int DefaultBatchTimeoutMs = 300;
    private const int PollIntervalMs = 50;
    private const string BatchFilePrefix = "RightClickPS_Batch_";

    private readonly string _scriptPath;
    private readonly string _batchFilePath;
    private readonly string _mutexName;
    private readonly int _batchTimeoutMs;
    private Mutex? _mutex;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchCollector"/> class.
    /// </summary>
    /// <param name="scriptPath">The path to the script being executed.</param>
    /// <param name="batchTimeoutMs">Timeout in ms to wait for additional files. Default is 300ms.</param>
    public BatchCollector(string scriptPath, int batchTimeoutMs = DefaultBatchTimeoutMs)
    {
        _scriptPath = scriptPath ?? throw new ArgumentNullException(nameof(scriptPath));
        _batchTimeoutMs = batchTimeoutMs;

        // Create a unique identifier for this script's batch based on script path
        var hash = ComputeHash(scriptPath);
        _batchFilePath = Path.Combine(Path.GetTempPath(), $"{BatchFilePrefix}{hash}.txt");
        _mutexName = $"Global\\RightClickPS_Batch_{hash}";
    }

    /// <summary>
    /// Adds a file to the batch and determines if this process should execute the script.
    /// </summary>
    /// <param name="filePath">The file path to add to the batch.</param>
    /// <returns>
    /// A tuple containing:
    /// - shouldExecute: true if this process should execute the script
    /// - allFiles: the list of all files in the batch (only valid if shouldExecute is true)
    /// </returns>
    public (bool shouldExecute, List<string> allFiles) AddFileAndWait(string filePath)
    {
        bool isNewBatch = false;
        bool ownsMutex = false;

        try
        {
            // Create or open the mutex
            _mutex = new Mutex(false, _mutexName, out bool createdNew);

            // Wait to acquire the mutex (with timeout to avoid deadlocks)
            ownsMutex = _mutex.WaitOne(TimeSpan.FromSeconds(5));

            if (!ownsMutex)
            {
                // Couldn't acquire mutex, fall back to single-file execution
                return (true, new List<string> { filePath });
            }

            // Check if this is a new batch (no existing batch file or it's stale)
            isNewBatch = !File.Exists(_batchFilePath) || IsStale(_batchFilePath);

            if (isNewBatch)
            {
                // Start a new batch - this process will be the executor
                File.WriteAllText(_batchFilePath, filePath + Environment.NewLine);
            }
            else
            {
                // Add to existing batch
                File.AppendAllText(_batchFilePath, filePath + Environment.NewLine);
            }

            // Record the last write time
            var lastWriteTime = File.GetLastWriteTimeUtc(_batchFilePath);

            // Release mutex so other processes can add their files
            _mutex.ReleaseMutex();
            ownsMutex = false;

            if (!isNewBatch)
            {
                // Not the first process - just exit, the executor will handle it
                return (false, new List<string>());
            }

            // This process is the executor - wait for more files to arrive
            return WaitForBatchCompletion(lastWriteTime);
        }
        catch (AbandonedMutexException)
        {
            // Previous process died holding the mutex - we now own it
            // Treat this as starting a new batch
            ownsMutex = true;
            File.WriteAllText(_batchFilePath, filePath + Environment.NewLine);
            var lastWriteTime = File.GetLastWriteTimeUtc(_batchFilePath);
            _mutex?.ReleaseMutex();
            ownsMutex = false;
            return WaitForBatchCompletion(lastWriteTime);
        }
        catch (Exception)
        {
            // On any error, fall back to single-file execution
            return (true, new List<string> { filePath });
        }
        finally
        {
            if (ownsMutex && _mutex != null)
            {
                try { _mutex.ReleaseMutex(); } catch { }
            }
        }
    }

    /// <summary>
    /// Waits for the batch to be complete (no new files added within timeout).
    /// </summary>
    private (bool shouldExecute, List<string> allFiles) WaitForBatchCompletion(DateTime lastWriteTime)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(_batchTimeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            Thread.Sleep(PollIntervalMs);

            // Check if new files were added
            try
            {
                var currentWriteTime = File.GetLastWriteTimeUtc(_batchFilePath);
                if (currentWriteTime > lastWriteTime)
                {
                    // New file was added, extend the deadline
                    lastWriteTime = currentWriteTime;
                    deadline = DateTime.UtcNow.AddMilliseconds(_batchTimeoutMs);
                }
            }
            catch
            {
                // File might have been deleted/recreated, continue waiting
            }
        }

        // Timeout reached - read all files and clean up
        return CollectAndCleanup();
    }

    /// <summary>
    /// Collects all files from the batch and cleans up.
    /// </summary>
    private (bool shouldExecute, List<string> allFiles) CollectAndCleanup()
    {
        var files = new List<string>();

        try
        {
            // Acquire mutex for final read
            _mutex?.WaitOne(TimeSpan.FromSeconds(2));

            if (File.Exists(_batchFilePath))
            {
                var lines = File.ReadAllLines(_batchFilePath);
                files = lines
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => line.Trim())
                    .Distinct() // Remove duplicates
                    .ToList();

                // Delete the batch file
                try { File.Delete(_batchFilePath); } catch { }
            }

            _mutex?.ReleaseMutex();
        }
        catch (AbandonedMutexException)
        {
            // We got the mutex from an abandoned state
            if (File.Exists(_batchFilePath))
            {
                var lines = File.ReadAllLines(_batchFilePath);
                files = lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).Distinct().ToList();
                try { File.Delete(_batchFilePath); } catch { }
            }
            try { _mutex?.ReleaseMutex(); } catch { }
        }
        catch
        {
            // Fall back to empty list on error
        }

        return (true, files.Count > 0 ? files : new List<string>());
    }

    /// <summary>
    /// Checks if a batch file is stale (older than 5 seconds).
    /// </summary>
    private bool IsStale(string path)
    {
        try
        {
            var lastWrite = File.GetLastWriteTimeUtc(path);
            return (DateTime.UtcNow - lastWrite).TotalSeconds > 5;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Computes a short hash for use in file names.
    /// </summary>
    private static string ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input.ToLowerInvariant());
        var hash = SHA256.HashData(bytes);
        // Use first 8 bytes for a reasonably unique but short identifier
        return Convert.ToHexString(hash, 0, 8);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _mutex?.Dispose();
            _disposed = true;
        }
    }
}
