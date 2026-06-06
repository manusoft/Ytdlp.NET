namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Provides data for the command completion event.
/// </summary>
/// <remarks>
/// Contains the final execution status and a descriptive message
/// indicating the outcome of the yt-dlp process.
/// </remarks>
public class CommandCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Gets a value indicating whether the command completed successfully.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets a message describing the result of the command execution.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandCompletedEventArgs"/> class.
    /// </summary>
    /// <param name="success">Indicates whether the command was successful.</param>
    /// <param name="message">A message describing the execution result.</param>
    public CommandCompletedEventArgs(bool success, string message) => (Success, Message) = (success, message);
}