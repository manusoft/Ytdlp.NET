using ManuHub.Ytdlp.NET.Core;

namespace ManuHub.Ytdlp.NET;

public sealed partial class Ytdlp
{

    #region Advanced Execution & Utility Methods

    /// <summary>
    /// Executes a raw argument string directly. 
    /// If <paramref name="onLineReceived"/> is provided, output is streamed and captureFullOutput is false.
    /// If <paramref name="onLineReceived"/> is null, output is captured fully and returned in the result.
    /// </summary>
    public async Task<ProcessResult> ExecuteRawAsync(string arguments,
                                                     Action<string>? onLineReceived = null,
                                                     CancellationToken ct = default,
                                                     bool tuneProcess = true)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            throw new ArgumentException("Arguments cannot be null or empty.", nameof(arguments));

        // 1. Basic Security Filter
        if (arguments.Contains("&") || arguments.Contains("|") || arguments.Contains(";") || arguments.Contains(">"))
            throw new ArgumentException("Forbidden shell operators detected.");

        // 2. Sanitize & Re-escape
        var tokens = ParseArguments(arguments);
        var sanitizedArgs = string.Join(" ", tokens.Select(EscapeArgument));

        var runner = CreateRunner();
        bool captureFullOutput = (onLineReceived == null);

        return await runner.ExecuteAsync(
            arguments: arguments, // Now we know it's "cleaner"
            auth: _auth,
            adobePass: _adobePass,
            onLineReceived: onLineReceived,
            ct: ct,
            tuneProcess: tuneProcess,
            captureFullOutput: captureFullOutput
        );
    }

    /// <summary>
    /// Simple helper to split space-separated arguments, respecting quotes
    /// </summary>
    /// <param name="commandLine"></param>
    /// <returns></returns>
    private static IEnumerable<string> ParseArguments(string commandLine)
    {
        var pattern = @"(?<=\s|^)(?:""([^""]*)""|'([^']*)'|(\S+))";
        return System.Text.RegularExpressions.Regex.Matches(commandLine, pattern)
            .Select(m => m.Groups[1].Value + m.Groups[2].Value + m.Groups[3].Value);
    }

    #endregion
}
