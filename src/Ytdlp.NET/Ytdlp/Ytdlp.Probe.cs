using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Provides methods to probe video URLs for metadata, formats, subtitles, and other information using yt-dlp.
/// </summary>
public sealed partial class Ytdlp
{
    #region Static JSON options for metadata parsing
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        TypeInfoResolver = YtdlpJsonContext.Default
    };

    #endregion

    #region Execution & Utility Methods

    /// <summary>
    /// Command preview ofr debug operatons
    /// </summary>
    /// <param name="url">The URL of the video to preview.</param>
    /// <returns>A string representing the command that would be executed for the given URL.</returns>
    public string Preview(string url)
    {
        var argsList = BuildArguments(url);
        return string.Join(" ", argsList.Select(EscapeArgument));
    }

    /// <summary>
    /// Retrieves the current version string of the underlying yt-dlp executable.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> to abort the version check process.</param>
    /// <returns>
    /// A <see cref="string"/> representing the yt-dlp version (e.g., "2023.03.04"); 
    /// returns an empty string or throws if the binary cannot be found.
    /// </returns>
    public async Task<string> VersionAsync(CancellationToken ct = default)
    {
        var output = await RunProbeAsync("--version", ct);
        return output?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Updates the underlying yt-dlp binary to the latest version on the specified release channel.
    /// </summary>
    /// <param name="channel">The release channel to pull updates from (Master, Nightly, Stable.).</param>
    /// <param name="specificVersion">The specific version to update to. e.g., "2026.03.17" or "latest"</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to abort the download and installation process.</param>
    /// <returns>
    /// A <see cref="string"/> containing the update log or the new version number; 
    /// returns an empty string or throws if the update process fails.
    /// </returns>
    public async Task<string> UpdateAsync(UpdateChannel channel = UpdateChannel.Stable, string? specificVersion = null, CancellationToken ct = default)
    {
        string target = channel.ToString().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(specificVersion))
            target += $"@{specificVersion.ToLowerInvariant()}";

        var output = await RunProbeAsync($"--update-to {target}", ct);
        if (string.IsNullOrWhiteSpace(output)) return string.Empty;

        // Analyze output for professional messages
        if (output.Contains("Updated", StringComparison.OrdinalIgnoreCase))
            return $"yt-dlp was successfully updated to the latest {target}.";

        if (output.Contains("up to date", StringComparison.OrdinalIgnoreCase))
            return $"yt-dlp is already up to date on {target}.";

        return output;
    }

    /// <summary>
    /// List all supported extractors and exit
    /// </summary>
    /// <param name="ct"></param>    
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>List of extractor names</returns>
    public async Task<List<string>> GetExtractorsAsync(CancellationToken ct = default, bool tuneProcess = true)
    {
        try
        {
            var result = await RunProbeAsync("--list-extractors", ct, tuneProcess);
            if (string.IsNullOrWhiteSpace(result)) return new List<string>();

            return result
               .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
               .ToList();
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Warning, "Extractors fetch cancelled.");
            return new List<string>();
        }
    }

    /// <summary>
    /// List all supported MSOs for Adobe Pass authentication and exit
    /// </summary>
    /// <param name="ct"></param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>List of Adobe Pass MSOs</returns>
    public async Task<List<string>> GetAdobePassListAsync(CancellationToken ct = default, bool tuneProcess = true)
    {
        try
        {
            var result = await RunProbeAsync("--ap-list-mso", ct, tuneProcess);
            if (string.IsNullOrWhiteSpace(result)) return new List<string>();

            return result
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Warning, "Adobe Pass list fetch cancelled.");
            return new List<string>();
        }
    }

    /// <summary>
    ///  Fetches video metadata from the specified URL.
    /// </summary>
    /// <param name = "url">The source URL(video or playlist) to probe.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to abort the process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>
    /// A <see cref="Metadata"/> object containing the parsed metadata output; 
    /// returns <see langword="null"/> if the process fails, returns empty, or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Metadata?> GetMetadataAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
    {
        var json = await GetMetadataInternalAsync(url, flat: true, ct, tuneProcess);
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonSerializer.Deserialize(json, YtdlpJsonContext.Default.Metadata);
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Metadata deserialize failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Fetches raw JSON metadata the specified URL.
    /// </summary>
    /// <param name="url">The source URL (video or playlist) to probe.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to abort the process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>
    /// A raw JSON <see cref="string"/> containing the parsed metadata output; 
    /// returns <see langword="null"/> if the process fails, returns empty, or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<string?> GetMetadataRawAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
        => await GetMetadataInternalAsync(url, flat: true, ct, tuneProcess);

    /// <summary>
    /// Gets deep metadata for the specified URL by requesting non-flat JSON and deserializing it into a Metadata object.
    /// </summary>
    /// <remarks>Requests non-flat JSON via GetMetadataInternalAsync. Logs a warning when the JSON output is
    /// empty. Deserialization uses case-insensitive property names and allows numeric values provided as strings; any
    /// deserialization error results in a null return.</remarks>
    /// <param name="url">The resource URL to retrieve metadata from.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <param name="tuneProcess">True to enable process tuning for metadata retrieval; otherwise false.</param>
    /// <returns>A Metadata instance if JSON is present and deserialization succeeds; otherwise null.</returns>
    public async Task<Metadata?> GetDeepMetadataAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
    {
        var json = await GetMetadataInternalAsync(url, flat: false, ct, tuneProcess);
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonSerializer.Deserialize(json, YtdlpJsonContext.Default.Metadata);
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Deep metadata deserialize failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Asynchronously retrieves deep (non-flattened) metadata for the specified URL as raw JSON.
    /// </summary>
    /// <remarks>Invokes GetMetadataInternalAsync with flat set to false. Logs a warning when the returned
    /// JSON is null or consists only of whitespace.</remarks>
    /// <param name="url">The URL from which to retrieve metadata.</param>
    /// <param name="ct">A cancellation token to cancel the asynchronous operation.</param>
    /// <param name="tuneProcess">Whether to apply process tuning for the metadata retrieval.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is a JSON string containing deep
    /// (non-flattened) metadata, or null if the output is empty.</returns>
    public async Task<string?> GetDeepMetadataRawAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
        => await GetMetadataInternalAsync(url, flat: false, ct, tuneProcess);

    /// <summary>
    /// Retrieves a list of all available stream formats for a given URL.
    /// </summary>
    /// <param name="url">The video or playlist URL to probe.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to abort the process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>
    /// A <see cref="List{Format}"/> containing all available streams; 
    /// returns an empty list or <see langword="null"/> if the probe fails or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<List<Format>> GetFormatsAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Video URL cannot be empty.", nameof(url));

        var output = await RunProbeAsync($"-F {Quote(url)}", ct, tuneProcess);
        return string.IsNullOrWhiteSpace(output)
            ? new List<Format>()
            : ParseFormats(output);
    }

    /// <summary>
    /// Fetches a lightweight version of video metadata.
    /// </summary>
    /// <param name="url">The video or playlist URL to probe.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to abort the process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>
    /// A <see cref="MetadataLight"/> object if successful; 
    /// returns <see langword="null"/> if the process fails or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<MetadataLight?> GetMetadataLiteAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        try
        {
            var separator = $"|||{Guid.NewGuid():N}|||";

            var fields = new[]
            {
                "%(id)s",
                "%(title)s",
                "%(duration)s",
                "%(thumbnail)s",
                "%(view_count)s",
                "%(filesize,filesize_approx)s",
                "%(description).500s"
            };

            var arguments =
                $"--print \"{string.Join(separator, fields)}\" " +
                $"--skip-download --no-playlist --quiet {Quote(url)}";

            var output = await RunProbeAsync(arguments, ct, tuneProcess);
            if (string.IsNullOrWhiteSpace(output)) return null;

            var parts = output.Trim().Split(separator);
            if (parts.Length < 6) return null;

            return new MetadataLight
            {
                Id = parts[0].Trim(),
                Title = parts[1].Trim(),
                Duration = double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var dur) ? dur : null,
                Thumbnail = parts[3].Trim(),
                ViewCount = long.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var views) ? views : null,
                FileSize = long.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out var size) ? size : null,
                Description = parts.Length > 6 ? parts[6].Trim() : null
            };
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Warning, "Metadata lite fetch cancelled.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Metadata lite failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Fetches a specific subset of metadata fields from the specified URL.
    /// </summary>
    /// <param name="url">The source URL to probe.</param>
    /// <param name="fields">A collection of field names to extract (e.g., "title", "uploader").</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to abort the yt-dlp process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>
    /// A <see cref="Dictionary{TKey, TValue}"/> containing the requested fields and their values; 
    /// returns <see langword="null"/> if the process fails, returns no data, or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Dictionary<string, string>?> GetMetadataLiteAsync(string url, IEnumerable<string> fields, CancellationToken ct = default, bool tuneProcess = true)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        var fieldList = fields?.ToList()
            ?? throw new ArgumentNullException(nameof(fields));

        if (fieldList.Count == 0)
            throw new ArgumentException("At least one field must be requested.", nameof(fields));

        try
        {
            var separator = $"|||{Guid.NewGuid():N}|||";
            var printFormat = string.Join(separator, fieldList.Select(f => $"%({f})s"));
            var arguments =
                $"--print \"{printFormat}\" " +
                $"--skip-download --no-playlist --quiet {Quote(url)}";

            var rawOutput = await RunProbeAsync(arguments, ct, tuneProcess);
            if (string.IsNullOrWhiteSpace(rawOutput)) return null;

            var parts = rawOutput.Trim().Split(separator);
            if (parts.Length != fieldList.Count) return null;

            var result = new Dictionary<string, string>(
                fieldList.Count,
                StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < fieldList.Count; i++)
                result[fieldList[i]] = parts[i].Trim();

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Warning, "Metadata lite (fields) fetch cancelled.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Metadata lite (fields) failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Probes the specified URL to find the ID of the best available audio format.
    /// </summary>
    /// <param name="url">The video or playlist URL to probe.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to abort the process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>
    /// A <see cref="string"/> representing the best audio format ID (e.g., "140"); 
    /// returns an empty string or throws if no suitable audio is found.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<string> GetBestAudioFormatIdAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
    {
        var meta = await GetMetadataAsync(url, ct, tuneProcess);
        var best = meta?.Formats?
            .Where(f => f.IsAudio && (f.Abr > 0 || f.Tbr > 0))
            .OrderByDescending(f => f.Abr ?? f.Tbr ?? 0)
            .FirstOrDefault();

        return best?.FormatId ?? "bestaudio";
    }

    /// <summary>
    /// Probes the specified URL to find the ID of the best available video format within the specified height.
    /// </summary>
    /// <param name="url">The source URL to probe for video formats.</param>
    /// <param name="maxHeight">The maximum vertical resolution allowed (default 1080p).</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to cancel the underlying yt-dlp process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>
    /// A <see cref="string"/> representing the best video format ID (e.g., "137" or "248"); 
    /// returns an empty string or <see langword="null"/> if no suitable format is found.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<string> GetBestVideoFormatIdAsync(string url, int maxHeight = 1080, CancellationToken ct = default, bool tuneProcess = true)
    {
        var meta = await GetMetadataAsync(url, ct, tuneProcess);
        var best = meta?.Formats?
            .Where(f => !f.IsAudio && f.Height.HasValue && f.Height <= maxHeight)
            .OrderByDescending(f => f.Height)
            .ThenByDescending(f => f.Fps ?? 0)
            .FirstOrDefault();

        return best?.FormatId ?? "bestvideo";
    }

    /// <summary>
    /// Probes the specified URL to retrieve a list of available subtitle tracks.
    /// </summary>
    /// <param name="url">The video URL to probe for subtitle tracks.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to cancel the underlying yt-dlp process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>A list of available subtitle tracks.</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<List<SubtitleTrack>> GetSubtitlesAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Video URL cannot be empty.", nameof(url));

        var output = await RunProbeAsync($"--list-subs {Quote(url)}", ct, tuneProcess);

        return string.IsNullOrWhiteSpace(output)
            ? new List<SubtitleTrack>()
            : ParseSubtitles(output);
    }

    #endregion

    #region Internal Probe Helpers and Utilities

    /// <summary>
    /// Executes any yt-dlp probe command (metadata, version, format list, etc.)
    /// and returns the full captured stdout as a string.
    /// Returns <see langword="null"/> if the process fails, is cancelled, or produces no output.
    /// </summary>
    private async Task<string?> RunProbeAsync(string arguments, CancellationToken ct = default, bool tuneProcess = true)
    {
        try
        {
            var result = await CreateRunner().ExecuteAsync(
                arguments,
                onLineReceived: null,
                ct: ct,
                tuneProcess: tuneProcess,
                captureFullOutput: true);

            return result.IsSuccess && !string.IsNullOrWhiteSpace(result.FullOutput)
                ? result.FullOutput.Trim()
                : null;
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Warning, "Probe cancelled.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Probe failed: {ex.Message}");
            return null;
        }
    }


    private async Task<string?> GetMetadataInternalAsync(string url, bool flat, CancellationToken ct, bool tuneProcess)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        var arguments =
            "--dump-single-json " +
            "--simulate " +
            "--skip-download " +
            (flat ? "--flat-playlist " : "") +
            "--lazy-playlist " +
            "--quiet " +
            "--no-warnings " +
            $"{Quote(url)}";

        return await RunProbeAsync(arguments, ct, tuneProcess);
    }

    private List<Format> ParseFormats(string result)
    {
        var formats = new List<Format>();
        if (string.IsNullOrWhiteSpace(result)) return formats;

        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        bool inFormatSection = false;

        foreach (var line in lines)
        {
            if (line.Contains("[info] Available formats")) { inFormatSection = true; continue; }
            if (!inFormatSection || line.Contains("RESOLUTION") || line.StartsWith("---")) continue;
            if (string.IsNullOrWhiteSpace(line) || !Regex.IsMatch(line, @"^\S+\s+\S+")) break;

            try
            {
                var format = Format.FromParsedLine(line);
                if (!string.IsNullOrEmpty(format.Id) && !formats.Exists(f => f.Id == format.Id))
                    formats.Add(format);
            }
            catch (Exception ex)
            {
                _logger.Log(LogType.Warning, $"Failed parsing format line: {line} → {ex.Message}");
            }
        }

        _logger.Log(LogType.Information, $"Parsed {formats.Count} formats");
        return formats;
    }

    private List<SubtitleTrack> ParseSubtitles(string output)
    {
        var subtitles = new List<SubtitleTrack>();
        var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        var rowRegex = new System.Text.RegularExpressions.Regex(@"^(\S+)\s+(.+?)\s{2,}([a-z0-9, ]+)$");

        bool foundStartPoint = false;
        bool foundHeader = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // 1. Look for the specific trigger point
            if (trimmed.Contains("[info] Available subtitles for"))
            {
                foundStartPoint = true;
                continue;
            }

            // 2. We only start parsing rows after the trigger AND the header
            if (foundStartPoint && trimmed.StartsWith("Language"))
            {
                foundHeader = true;
                continue;
            }

            if (foundHeader)
            {
                var match = rowRegex.Match(trimmed);
                if (match.Success)
                {
                    subtitles.Add(new SubtitleTrack
                    {
                        LanguageCode = match.Groups[1].Value,
                        Name = match.Groups[2].Value.Trim(),
                        Formats = match.Groups[3].Value.Split(',').Select(f => f.Trim()).ToList()
                    });
                }
            }
        }
        return subtitles;
    }

    #endregion
}
