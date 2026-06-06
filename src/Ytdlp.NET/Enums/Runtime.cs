namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Represents supported JavaScript runtime engines used by yt-dlp
/// for executing extractor scripts or external runtime-based features.
/// </summary>
/// <remarks>
/// These runtimes are used when yt-dlp requires external JS execution
/// environments such as Deno, Node.js, QuickJS, or Bun.
/// </remarks>
public enum Runtime
{
    /// <summary>
    /// Deno runtime engine.
    /// </summary>
    Deno,

    /// <summary>
    /// Node.js runtime engine.
    /// </summary>
    Node,

    /// <summary>
    /// QuickJS lightweight JavaScript engine.
    /// </summary>
    QuickJs,

    /// <summary>
    /// Bun JavaScript runtime.
    /// </summary>
    Bun,
}