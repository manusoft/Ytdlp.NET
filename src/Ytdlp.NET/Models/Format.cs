using System.Globalization;
using System.Text.RegularExpressions;

namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Represents a single media format entry returned by yt-dlp (-F output).
/// Contains both video/audio metadata and convenience flags for selection logic.
/// </summary>
public sealed class Format
{
    // Core identifiers

    /// <summary>
    /// Unique format identifier (yt-dlp format code / ID).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// File extension (e.g., mp4, webm, m4a).
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    // Video-specific

    /// <summary>
    /// Human-readable resolution (e.g., "1920x1080", "1080p", "audio only").
    /// </summary>
    public string Resolution { get; set; } = string.Empty;

    /// <summary>
    /// Video height in pixels if available.
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Video width in pixels if available.
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Frames per second (FPS) if available.
    /// </summary>
    public double? Fps { get; set; }


    // Audio-specific

    /// <summary>
    /// Number of audio channels (e.g., "2", "stereo", "6").
    /// </summary>
    public string? Channels { get; set; }

    /// <summary>
    /// Audio sample rate in Hz (asr from yt-dlp output).
    /// </summary>
    public double? AudioSampleRate { get; set; }

    // Bitrates

    /// <summary>
    /// Total bitrate (tbr) if available.
    /// </summary>
    public string? TotalBitrate { get; set; }

    /// <summary>
    /// Video bitrate (vbr) if available.
    /// </summary>
    public string? VideoBitrate { get; set; }

    /// <summary>
    /// Audio bitrate (abr) if available.
    /// </summary>
    public string? AudioBitrate { get; set; }

    // Codecs

    /// <summary>
    /// Video codec (e.g., avc1, vp9, none).
    /// </summary>
    public string? VideoCodec { get; set; }

    /// <summary>
    /// Audio codec (e.g., opus, mp4a, none).
    /// </summary>
    public string? AudioCodec { get; set; }

    // Protocol / delivery

    /// <summary>
    /// Streaming protocol (e.g., https, m3u8_native).
    /// </summary>
    public string? Protocol { get; set; }

    /// <summary>
    /// Container format inferred from extension or metadata.
    /// </summary>
    public string? Container { get; set; }

    // Size & approximate data

    /// <summary>
    /// Approximate file size as string (e.g., "~123MiB").
    /// </summary>
    public string? FileSizeApprox { get; set; }

    /// <summary>
    /// Approximate file size in bytes if parsed.
    /// </summary>
    public long? ApproxFileSizeBytes { get; set; }

    // Other metadata

    /// <summary>
    /// Language code if available (e.g., subtitles/audio language).
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Additional metadata or descriptive info.
    /// </summary>
    public string? MoreInfo { get; set; }

    /// <summary>
    /// Extra note field from yt-dlp output if present.
    /// </summary>
    public string? Note { get; set; }

    // Convenience flags

    /// <summary>
    /// Indicates whether this format contains video stream data.
    /// </summary>
    public bool IsVideo => !string.IsNullOrEmpty(VideoCodec) && VideoCodec != "none" && Resolution != "audio only";

    /// <summary>
    /// Indicates whether this format is audio-only.
    /// </summary>
    public bool IsAudioOnly => Resolution == "audio only" || (VideoCodec == "none" && !string.IsNullOrEmpty(AudioCodec));

    /// <summary>
    /// Indicates whether this format is a storyboard/thumbnail stream.
    /// </summary>
    public bool IsStoryboard => VideoCodec == "images" || MoreInfo?.Contains("storyboard") == true;

    /// <summary>
    /// Returns a formatted string representation of the format entry.
    /// </summary>
    public override string ToString()
    {
        var parts = new[]
        {
            Id.PadRight(6),
            Extension.PadRight(5),
            (Resolution ?? "unknown").PadRight(12),
            Fps?.ToString("F0", CultureInfo.InvariantCulture) ?? "-".PadRight(4),
            Channels ?? "-".PadRight(3),
            FileSizeApprox ?? "-".PadRight(12),
            Protocol ?? "-".PadRight(8),
            VideoCodec ?? "-".PadRight(10),
            AudioCodec ?? "-".PadRight(10),
            MoreInfo
        };

        return string.Join("  ", parts.Where(p => !string.IsNullOrEmpty(p)));
    }


    /// <summary>
    /// Factory method to create Format from a single line of yt-dlp -F output.
    /// Attempts to parse all columns based on typical yt-dlp table layout.
    /// </summary>
    public static Format FromParsedLine(string line)
    {
        var format = new Format();

        // Step 1: Replace pipes with space (they are just column separators)
        string cleaned = Regex.Replace(line, @"\s*\|\s*", " ");

        // Step 2: Collapse multiple whitespace → single space, trim
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ").Trim();

        // Step 3: Split into tokens
        var tokens = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .ToList();

        if (tokens.Count < 3) return format;

        int idx = 0;

        // ID (usually alphanumeric like sb0, 139, 160...)
        format.Id = tokens[idx++];

        // Extension (m4a, webm, mp4, mhtml...)
        format.Extension = tokens[idx++];

        // Resolution or "audio only"
        string token = tokens[idx];
        if (token == "audio" && idx + 1 < tokens.Count && tokens[idx + 1] == "only")
        {
            format.Resolution = "audio only";
            format.VideoCodec = "none";
            idx += 2;
        }
        else
        {
            format.Resolution = token;

            // Parse 1920x1080 or 1080p etc.
            var resMatch = Regex.Match(token, @"(\d+)x(\d+)|(\d+)p");
            if (resMatch.Success)
            {
                if (resMatch.Groups[1].Success && resMatch.Groups[2].Success)
                {
                    format.Width = int.TryParse(resMatch.Groups[1].Value, out int w) ? w : null;
                    format.Height = int.TryParse(resMatch.Groups[2].Value, out int h) ? h : null;
                }
                else if (resMatch.Groups[3].Success)
                {
                    format.Height = int.TryParse(resMatch.Groups[3].Value, out int h) ? h : null;
                }
            }
            idx++;
        }

        // FPS (usually integer or -)
        if (idx < tokens.Count && double.TryParse(tokens[idx], NumberStyles.Any, CultureInfo.InvariantCulture, out double fpsVal) && fpsVal > 0)
        {
            format.Fps = fpsVal;
            idx++;
        }

        // Channels (2, stereo, etc. — mostly for audio)
        if (idx < tokens.Count && Regex.IsMatch(tokens[idx], @"^\d+$"))
        {
            format.Channels = tokens[idx];
            idx++;
        }

        // File size (~5.52MiB, 14.66MiB, etc.)
        if (idx < tokens.Count && (tokens[idx].Contains("MiB") || tokens[idx].Contains("GiB") || tokens[idx].StartsWith("~")))
        {
            format.FileSizeApprox = tokens[idx];

            var sizeMatch = Regex.Match(tokens[idx], @"~?([\d\.]+)\s*([KMG]?i?B)");
            if (sizeMatch.Success && double.TryParse(sizeMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double sizeVal))
            {
                string unit = sizeMatch.Groups[2].Value.ToLowerInvariant();
                long multiplier = unit switch
                {
                    "gib" => 1073741824L,
                    "mib" => 1048576L,
                    "kib" => 1024L,
                    _ => 1L
                };
                format.FileSizeApprox = (sizeVal * multiplier).ToString();
            }
            idx++;
        }

        // Total Bitrate (49k, 128k, etc.)
        if (idx < tokens.Count && tokens[idx].EndsWith("k"))
        {
            format.TotalBitrate = tokens[idx];
            idx++;
        }

        // Protocol (https, m3u8, mhtml...)
        if (idx < tokens.Count && (tokens[idx].StartsWith("http") || tokens[idx].Contains("m3u8") || tokens[idx] == "mhtml"))
        {
            format.Protocol = tokens[idx];
            idx++;
        }

        // Video codec (avc1..., vp9, av01..., images, none)
        if (idx < tokens.Count)
        {
            string vc = tokens[idx];
            if (vc == "audio" && idx + 1 < tokens.Count && tokens[idx + 1] == "only")
            {
                format.VideoCodec = "none";
                idx += 2;
            }
            else if (vc == "images" || vc.StartsWith("avc1") || vc.StartsWith("vp") || vc.StartsWith("av01"))
            {
                format.VideoCodec = vc;
                idx++;
            }
        }

        // Audio codec (opus, mp4a..., none)
        if (idx < tokens.Count)
        {
            string ac = tokens[idx];
            if (ac == "video" && idx + 1 < tokens.Count && tokens[idx + 1] == "only")
            {
                format.AudioCodec = "none";
                idx += 2;
            }
            else if (ac == "audio" && idx + 1 < tokens.Count && tokens[idx + 1] == "only")
            {
                format.AudioCodec = "none";
                idx += 2;
            }
            else if (ac.Contains(".") || ac == "opus")
            {
                format.AudioCodec = ac;
                idx++;
            }
        }

        // All remaining → MoreInfo
        if (idx < tokens.Count)
        {
            format.MoreInfo = string.Join(" ", tokens.GetRange(idx, tokens.Count - idx));
        }

        return format;
    }

    /// <summary>
    /// Filters a collection of available formats based on a specified type.
    /// </summary>
    /// <param name="formats">The full list of available formats to filter.</param>
    /// <param name="type">
    /// The filter type to apply (e.g. "video", "audio", "best", "worst").
    /// </param>
    /// <returns>
    /// A filtered sequence of <see cref="Format"/> matching the specified criteria.
    /// </returns>
    /// <remarks>
    /// This method does not modify the original collection; it returns a new filtered sequence.
    /// </remarks>
    public IEnumerable<Format> FilterFormats(IEnumerable<Format> formats, string type)
    {
        return formats.Where(f => type == "audio" ? f.Resolution == "audio only" :
                                  type == "video" ? f.Resolution != "audio only" && f.Extension != "mhtml" :
                                  true);
    }
}
