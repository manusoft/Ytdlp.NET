namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Provides a minimal logging abstraction for the SDK.
/// </summary>
/// <remarks>
/// This interface is intentionally lightweight to avoid coupling the library
/// to any specific logging framework (e.g. Microsoft.Extensions.Logging).
/// </remarks>
public interface ILogger
{
    /// <summary>
    /// Writes a log entry with the specified log level and message.
    /// </summary>
    /// <param name="type">The severity/type of the log entry.</param>
    /// <param name="message">The log message.</param>
    void Log(LogType type, string message);
}
