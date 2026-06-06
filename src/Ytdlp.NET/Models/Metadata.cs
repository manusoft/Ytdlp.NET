using System.Text.Json.Serialization;

namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Represents the full metadata response returned by yt-dlp.
/// This can describe a single video, playlist, or nested media structure.
/// </summary>
/// <remarks>
/// Supports recursive entries for playlists and rich format/video/audio metadata.
/// </remarks>
public record class Metadata
{
    /// <summary>
    /// Video identifier.
    /// </summary>
    [JsonPropertyName("id")] public string? Id { get; set; }

    /// <summary>
    /// Type of media entry (e.g., video, playlist, season).
    /// </summary>
    [JsonPropertyName("_type")] public string? Type { get; set; }

    /// <summary>
    /// Video title, unescaped.
    /// </summary>
    [JsonPropertyName("title")] public string? Title { get; set; }

    /// <summary>
    /// Full video description.
    /// </summary>
    [JsonPropertyName("description")] public string? Description { get; set; }

    /// <summary>
    /// Full URL to a video thumbnail image.
    /// </summary>
    [JsonPropertyName("thumbnail")] public string? Thumbnail { get; set; }

    /// <summary>
    /// The total number of videos in a playlist. 
    /// If not given, YoutubeDL tries to calculate it from "entries"
    /// </summary>
    [JsonPropertyName("playlist_count")] public long? PlaylistCount { get; set; }

    /// <summary>
    /// A list of categories that the video falls in, for example ["Sports", "Berlin"]
    /// </summary>
    [JsonPropertyName("categories")] public List<string>? Categories { get; set; }

    /// <summary>
    /// A list of tags assigned to the video, e.g. ["sweden", "pop music"]
    /// </summary>
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }

    /// <summary>
    /// Id of the channel.
    /// </summary>
    [JsonPropertyName("channel_id")] public string? ChannelId { get; set; }

    /// <summary>
    /// Full name of the channel the video is uploaded on.
    /// Note that channel fields may or may not repeat uploader fields. This depends on a particular extractor.
    /// </summary>
    [JsonPropertyName("channel")] public string? Channel { get; set; }

    /// <summary>
    /// Full URL to a channel webpage.
    /// </summary>
    [JsonPropertyName("channel_url")] public string? ChannelUrl { get; set; }

    /// <summary>
    /// Number of followers of the channel.
    /// </summary>
    [JsonPropertyName("channel_follower_count")] public long? ChannelFollowerCount { get; set; }

    /// <summary>
    /// Nickname or id of the video uploader.
    /// </summary>
    [JsonPropertyName("uploader_id")] public string? UploaderId { get; set; }

    /// <summary>
    /// Full name of the video uploader.
    /// </summary>
    [JsonPropertyName("uploader")] public string? Uploader { get; set; }

    /// <summary>
    /// Full URL to a personal webpage of the video uploader.
    /// </summary>
    [JsonPropertyName("uploader_url")] public string? UploaderUrl { get; set; }

    /// <summary>
    /// Video upload date in UTC (YYYYMMDD). If not explicitly set, calculated from timestamp
    /// </summary>
    [JsonPropertyName("upload_date")] public string? UploadDate { get; set; }

    /// <summary>
    /// The URL to the video webpage, if given to yt-dlp it should allow to get the same result again. 
    /// (It will be set by YoutubeDL if it's missing)
    /// </summary>
    [JsonPropertyName("webpage_url")] public string? WebpageUrl { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("original_url")] public string? OriginalUrl { get; set; }

    /// <summary>
    /// Under what condition the video is available. 
    /// One of'private', 'premium_only', 'subscriber_only', 'needs_auth','unlisted' or 'public'. 
    /// Use 'InfoExtractor._availability' to set it
    /// </summary>
    [JsonPropertyName("availability")] public string? Availability { get; set; }


    /// <summary>
    /// Extractor name
    /// </summary>
    [JsonPropertyName("extractor")] public string? Extractor { get; set; }

    /// <summary>
    /// Extractor key
    /// </summary>
    [JsonPropertyName("extractor_key")] public string? ExtractorKey { get; set; }

    /// <summary>
    /// How many users have watched the video on the platform
    /// </summary>
    [JsonPropertyName("view_count")] public float? ViewCount { get; set; }

    /// <summary>
    /// Length of the video in seconds, as an integer or float.
    /// </summary>
    [JsonPropertyName("duration")] public float? Duration { get; set; }

    /// <summary>
    /// Age restriction for the video, as an integer (years)
    /// </summary>
    [JsonPropertyName("age_limit")] public int? AgeLimit { get; set; }

    /// <summary>
    /// Whether this video is allowed to play in embedded players on other sites.
    /// Can be True (=always allowed), False(=never allowed), None(=unknown), 
    /// or a string specifying the criteria for embedability; e.g. 'whitelist'
    /// </summary>
    [JsonPropertyName("playable_in_embed")] public bool? PlayableInEmbed { get; set; }

    /// <summary>
    /// None (=unknown), 'is_live', 'is_upcoming', 'was_live', 'not_live', or 
    /// 'post_live' (was live, but VOD is not yet processed)
    /// If absent, automatically set from is_live, was_live
    /// </summary>
    [JsonPropertyName("live_status")] public string? LiveStatus { get; set; }

    /// <summary>
    /// Number of comments on the video
    /// </summary>
    [JsonPropertyName("comment_count")] public long? CommentCount { get; set; }

    /// <summary>
    /// Number of positive ratings of the video
    /// </summary>
    [JsonPropertyName("like_count")] public long? LikeCount { get; set; }

    /// <summary>
    /// Timestamp
    /// </summary>
    [JsonPropertyName("timestamp")] public long? Timestamp { get; set; }

    /// <summary>
    /// Length of the video in seconds
    /// </summary>
    [JsonPropertyName("duration_string")] public string? DurationString { get; set; }

    /// <summary>
    /// True, False, or None (=unknown). Whether this video is a live stream that goes on instead of a fixed-length video.
    /// </summary>
    [JsonPropertyName("is_live")] public bool? IsLive { get; set; }

    /// <summary>
    /// True, False, or None (=unknown). Whether this video was originally a live stream.
    /// </summary>
    [JsonPropertyName("was_live")] public bool? WasLive { get; set; }

    /// <summary>
    /// A list of dictionaries, with the following entries
    /// </summary>
    [JsonPropertyName("entries")] public List<Entry>? Entries { get; set; }

    /// <summary>
    /// A list of dictionaries, with the following formats
    /// </summary>
    [JsonPropertyName("formats")] public List<FormatMetadata>? Formats { get; set; }

    /// <summary>
    /// A list of dictionaries, with the following thubnails
    /// </summary>
    [JsonPropertyName("thumbnails")] public List<ThumbnailMetadata>? Thumbnails { get; set; }

    /// <summary>
    /// A list of dictionaries, with the following requested formats
    /// </summary>
    [JsonPropertyName("requested_formats")] public List<FormatMetadata>? RequestedFormats { get; set; }

    /// <summary>
    /// A list of dictionaries, with the following subtitles
    /// </summary>
    [JsonPropertyName("automatic_captions")] public Dictionary<string, List<SubtitleMetadata>>? AutomaticCaptions { get; set; }
}

/// <summary>
/// Represents a thumbnail image associated with a video or playlist.
/// </summary>
public class ThumbnailMetadata
{
    /// <summary>
    ///  Thumbnail format ID (optional)
    /// </summary>
    [JsonPropertyName("id")] public string? Id { get; set; }

    /// <summary>
    /// Url of the image
    /// </summary>
    [JsonPropertyName("url")] public string? Url { get; set; }

    /// <summary>
    /// Quality of the image (optional)
    /// </summary>
    [JsonPropertyName("preference")] public int? Preference { get; set; }


    /// <summary>
    /// Height of the image (optional)
    /// </summary>
    [JsonPropertyName("height")] public int? Height { get; set; }

    /// <summary>
    /// Width of the image (optional)
    /// </summary>
    [JsonPropertyName("width")] public int? Width { get; set; }

    /// <summary>
    /// Resolution of the image (optional, deprecated)
    /// </summary>
    [JsonPropertyName("resolution")] public string? Resolution { get; set; }

    /// <summary>
    /// Size of the image
    /// </summary>
    [JsonPropertyName("")] public int? FileSize { get; set; }
}

/// <summary>
/// A list of dictionaries for each format available, ordered from worst to best quality.
/// </summary>
public class FormatMetadata
{
    /// <summary>
    /// A short description of the format ("mp4_h264_opus" or "19"). 
    /// Technically optional, but strongly recommended.
    /// </summary>
    [JsonPropertyName("format_id")] public string? FormatId { get; set; }

    /// <summary>
    /// Additional info about the format ("3D" or "DASH video")
    /// </summary>
    [JsonPropertyName("format_note")] public string? FormatNote { get; set; }

    /// <summary>
    /// Video filename extension
    /// </summary>
    [JsonPropertyName("ext")] public string? Ext { get; set; }

    /// <summary>
    /// Audio extension
    /// </summary>
    [JsonPropertyName("audio_ext")] public string? AudioExt { get; set; }

    /// <summary>
    /// Video extension
    /// </summary>
    [JsonPropertyName("video_ext")] public string? VideoExt { get; set; }

    /// <summary>
    /// The protocol that will be used for the actualdownload, lower-case. 
    /// One of "http", "https" or one of the protocols defined in downloader.PROTOCOL_MAP
    /// </summary>
    [JsonPropertyName("protocol")] public string? Protocol { get; set; }

    /// <summary>
    /// Name of the audio codec in use
    /// </summary>
    [JsonPropertyName("acodec")] public string? Acodec { get; set; }

    /// <summary>
    /// Name of the video codec in use
    /// </summary>
    [JsonPropertyName("vcodec")] public string? Vcodec { get; set; }

    /// <summary>
    /// Video url
    /// </summary>
    [JsonPropertyName("url")] public string? Url { get; set; }

    /// <summary>
    /// Textual description of width and height
    /// </summary>
    [JsonPropertyName("resolution")] public string? Resolution { get; set; }

    /// <summary>
    /// Frame rate
    /// </summary>
    [JsonPropertyName("fps")] public double? Fps { get; set; }

    /// <summary>
    /// Number of audio channels
    /// </summary>
    [JsonPropertyName("audio_channels")] public int? AudioChannels { get; set; }

    /// <summary>
    /// Available at
    /// </summary>
    [JsonPropertyName("available_at")] public int? AvailableAt { get; set; }

    /// <summary>
    /// Width of the video, if known
    /// </summary>
    [JsonPropertyName("width")]
    public int? Width { get; set; }

    /// <summary>
    /// Height of the video, if known
    /// </summary>
    [JsonPropertyName("height")] public int? Height { get; set; }

    /// <summary>
    /// Aspect ratio
    /// </summary>
    [JsonPropertyName("aspect_ratio")] public double? AspectRatio { get; set; }

    /// <summary>
    /// Average audio bitrate in KBit/s
    /// </summary>
    [JsonPropertyName("abr")] public double? Abr { get; set; }

    /// <summary>
    /// Average video bitrate in KBit/s
    /// </summary>
    [JsonPropertyName("vbr")] public double? Vbr { get; set; }

    /// <summary>
    /// Average bitrate of audio and video in KBit/s
    /// </summary>
    [JsonPropertyName("tbr")] public double? Tbr { get; set; }

    /// <summary>
    /// The number of bytes, if known in advance
    /// </summary>
    [JsonPropertyName("filesize")] public long? Filesize { get; set; }

    /// <summary>
    /// An estimate for the number of bytes
    /// </summary>
    [JsonPropertyName("filesize_approx")] public long? FilesizeApprox { get; set; }

    /// <summary>
    /// A human-readable description of the format ("mp4 container with h264/opus").
    /// Calculated from the format_id, width, height and format_note fields if missing.
    /// </summary>
    [JsonPropertyName("format")] public string? Format { get; set; }

    /// <summary>
    /// Audio sampling rate in Hertz
    /// </summary>
    [JsonPropertyName("asr")] public int? Asr { get; set; }

    /// <summary>
    /// Order number for this video source (quality takes higher priority)
    /// -1 for default (order by other properties), -2 or smaller for less than default.
    /// </summary>
    [JsonPropertyName("source_preference")] public int? SourcePreference { get; set; }

    /// <summary>
    /// Order number of the video quality of this format, irrespective of the file format.
    /// -1 for default (order by other properties), -2 or smaller for less than default.
    /// </summary>
    [JsonPropertyName("quality")] public double? Quality { get; set; }

    /// <summary>
    /// The format has DRM and cannot be downloaded.
    /// </summary>
    [JsonPropertyName("has_drm")] public bool? HasDrm { get; set; }

    /// <summary>
    /// Language code, e.g. "de" or "en-US".
    /// </summary>
    [JsonPropertyName("language")] public string? Language { get; set; }

    /// <summary>
    /// Is this in the language mentioned in the URL? 10 if it's what the URL is about,
    /// -1 for default (don't know), -10 otherwise, other values reserved for now.
    /// </summary>
    [JsonPropertyName("language_preference")] public int? LanguagePreference { get; set; }

    /// <summary>
    /// Order number of this format. If this field is present and not None, the formats get sorted
    /// by this field, regardless of all other values. -1 for default (order by other properties),
    /// -2 or smaller for less than default. les -1000 to hide the format (if there is another one which is strictly better)
    /// </summary>
    [JsonPropertyName("preference")] public int? Preference { get; set; }

    /// <summary>
    /// The dynamic range of the video. One of:"SDR" (None), "HDR10", "HDR10+, "HDR12", "HLG, "DV"
    /// </summary>
    [JsonPropertyName("dynamic_range")] public string? DynamicRange { get; set; }

    /// <summary>
    /// Name of the container format
    /// </summary>
    [JsonPropertyName("container")] public string? Container { get; set; }

    /// <summary>
    /// A dictionary of additional HTTP headers to add to the request.
    /// </summary>
    [JsonPropertyName("http_headers")] public Dictionary<string, string>? HttpHeaders { get; set; }

    /// <summary>
    /// A dictionary of downloader options (For internal use only)
    /// </summary>
    [JsonPropertyName("downloader_options")] public Dictionary<string, object>? DownloaderOptions { get; set; }

    /// <summary>
    /// A list of fragments of a fragmented media.
    /// </summary>
    [JsonPropertyName("fragments")] public List<FragmentMetadata>? Fragments { get; set; }

    /// <summary>
    /// Has audio
    /// </summary>
    public bool IsAudio => Acodec != "none";

    /// <summary>
    /// Has fragments
    /// </summary>
    public bool HasFragments => Fragments != null && Fragments.Count > 0;
}

/// <summary>
/// Represents subtitle or caption track information for a video.
/// </summary>
public class SubtitleMetadata
{
    /// <summary>
    /// Name or description of the subtitles
    /// </summary>
    [JsonPropertyName("name")] public string? Name { get; set; }

    /// <summary>
    /// A URL pointing to the subtitles file
    /// </summary>
    [JsonPropertyName("url")] public string? Url { get; set; }

    /// <summary>
    /// Extension of subtitle
    /// </summary>
    [JsonPropertyName("ext")] public string? Ext { get; set; }

    /// <summary>
    /// Impersonate
    /// </summary>
    [JsonPropertyName("impersonate")] public bool Impersonate { get; set; }
}

/// <summary>
/// Represents a chapter segment within a video timeline.
/// </summary>
public class ChapterMetadata
{
    /// <summary>
    /// Time in seconds where the reproduction should start, as specified in the URL.
    /// </summary>
    [JsonPropertyName("start_time")] public double StartTime { get; set; }

    /// <summary>
    /// Time in seconds where the reproduction should end, as specified in the URL.
    /// </summary>
    [JsonPropertyName("end_time")] public double EndTime { get; set; }

    /// <summary>
    /// Title of the chapter
    /// </summary>
    [JsonPropertyName("title")] public string? Title { get; set; }
}

/// <summary>
/// Represents engagement heatmap data for a video segment.
/// Used for visualizing viewer interest over time.
/// </summary>
public class HeatmapMetadata
{
    /// <summary>
    /// Time in seconds where the reproduction should start, as specified in the URL.
    /// </summary>
    [JsonPropertyName("start_time")] public double StartTime { get; set; }

    /// <summary>
    /// Time in seconds where the reproduction should end, as specified in the URL.
    /// </summary>
    [JsonPropertyName("end_time")] public double EndTime { get; set; }

    /// <summary>
    /// Value
    /// </summary>
    [JsonPropertyName("value")] public double Value { get; set; }
}

/// <summary>
/// Represents a streaming fragment used in HLS/DASH segmented downloads.
/// </summary>
public class FragmentMetadata
{
    /// <summary>
    /// If an url is present it should be considered by a client
    /// </summary>
    [JsonPropertyName("url")] public string Url { get; set; } = default!;

    /// <summary>
    /// Fragment duration
    /// </summary>
    [JsonPropertyName("duration")] public double Duration { get; set; }
}

/// <summary>
/// Represents a media entry in a playlist or nested structure.
/// This can be a video, playlist item, season, or nested playlist.
/// </summary>
/// <remarks>
/// Supports recursive nesting via the Entries property.
/// </remarks>
public record class Entry
{
    /// <summary>
    /// Type of video
    /// </summary>
    [JsonPropertyName("type")] public string? Type { get; set; }

    /// <summary>
    /// Video identifier.
    /// </summary>
    [JsonPropertyName("id")] public string? Id { get; set; }

    /// <summary>
    /// Video url
    /// </summary>
    [JsonPropertyName("url")] public string? Url { get; set; }

    /// <summary>
    /// Video title
    /// </summary>
    [JsonPropertyName("title")] public string? Title { get; set; }

    /// <summary>
    /// Full video description.
    /// </summary>
    [JsonPropertyName("description")] public string? Description { get; set; }

    /// <summary>
    /// Length of the video in seconds, as an integer or float.
    /// </summary>
    [JsonPropertyName("duration")] public float? Duration { get; set; }

    /// <summary>
    /// Id of the channel.
    /// </summary>
    [JsonPropertyName("channel_id")] public string? ChannelId { get; set; }

    /// <summary>
    /// Full name of the channel the video is uploaded on.
    /// Note that channel fields may or may not repeat uploader fields. 
    /// This depends on a particular extractor.
    /// </summary>
    [JsonPropertyName("channel")] public string? Channel { get; set; }

    /// <summary>
    /// Full URL to a channel webpage.
    /// </summary>
    [JsonPropertyName("channel_url")] public string? ChannelUrl { get; set; }

    /// <summary>
    /// Full name of the video uploader.
    /// </summary>
    [JsonPropertyName("uploader")] public string? Uploader { get; set; }

    /// <summary>
    /// Nickname or id of the video uploader.
    /// </summary>
    [JsonPropertyName("uploader_id")] public string? UploaderId { get; set; }

    /// <summary>
    /// Full URL to a personal webpage of the video uploader.
    /// </summary>
    [JsonPropertyName("uploader_url")] public string? UploaderUrl { get; set; }

    /// <summary>
    /// A list of dictionaries, with the following thumbnails
    /// </summary>
    [JsonPropertyName("thumbnails")] public List<ThumbnailMetadata>? Thumbnails { get; set; }

    /// <summary>
    /// How many users have watched the video on the platform.
    /// </summary>
    [JsonPropertyName("view_count")] public float? ViewCount { get; set; }

    /// <summary>
    /// The URL to the video webpage, if given to yt-dlp it should allow to get the same result again.
    /// </summary>
    [JsonPropertyName("webpage_url")] public string? WebpageUrl { get; set; }

    /// <summary>
    /// Original URL
    /// </summary>
    [JsonPropertyName("original_url")] public string? OriginalUrl { get; set; }

    /// <summary>
    /// A list of dictionaries, with the following entries
    /// </summary>
    [JsonPropertyName("entries")] public List<Entry>? Entries { get; set; }

}
