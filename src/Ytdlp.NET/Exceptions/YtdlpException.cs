namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Represents an error that occurs while configuring, launching,
/// or interacting with the yt-dlp process.
/// </summary>
/// <remarks>
/// This exception is used by Ytdlp.NET to wrap failures such as:
/// <list type="bullet">
/// <item><description>Invalid configuration or arguments.</description></item>
/// <item><description>Missing or inaccessible executable files.</description></item>
/// <item><description>Process startup failures.</description></item>
/// <item><description>Unexpected errors during command execution.</description></item>
/// </list>
/// </remarks>
[Serializable]
public sealed class YtdlpException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YtdlpException"/> class with the specified error message.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    public YtdlpException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="YtdlpException"/> class with the specified error message and the exception that caused it.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    /// <param name="inner">The exception that caused the current exception.</param>
    public YtdlpException(string message, Exception inner) : base(message, inner) { }
}