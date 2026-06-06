namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Represents the severity level of log messages produced by the library.
/// </summary>
public enum LogType
{
    /// <summary>
    /// Detailed diagnostic information used for debugging purposes.
    /// </summary>
    Debug,

    /// <summary>
    /// General informational message about normal operation.
    /// </summary>
    Information,

    /// <summary>
    /// A warning indicating a potential issue that does not stop execution.
    /// </summary>
    Warning,

    /// <summary>
    /// An error that occurred during execution.
    /// </summary>
    Error
}