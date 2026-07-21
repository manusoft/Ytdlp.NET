![Static Badge](https://img.shields.io/badge/ytdlp.NET-red) ![NuGet Version](https://img.shields.io/nuget/v/ytdlp.net) ![NuGet Downloads](https://img.shields.io/nuget/dt/ytdlp.net)

![Visitors](https://visitor-badge.laobi.icu/badge?page_id=manusoft/yt-dlp-wrapper)

# Ytdlp.NET

![icon](https://github.com/user-attachments/assets/2147c398-4e0f-43e2-99cb-32b34be7dc2f)

> **Ytdlp.NET** is a **fluent, strongly-typed, and **immutable** .NET wrapper** around [`yt-dlp`](https://github.com/yt-dlp/yt-dlp). It provides a fully **asynchronous, event-driven interface** for downloading, metadata extraction, and media processing from **YouTube** and hundreds of other platforms.
   
---

### ClipMate - MAUI.NET App - [Download](https://apps.microsoft.com/detail/9NTP1DH4CQ4X?hl=en&gl=IN&ocid=pdpshare)

<img width="986" height="693" alt="image" src="https://github.com/user-attachments/assets/39e09415-fa0c-4991-976f-8966e9c50c5b" />

---

### Video Downloader - .NET App - [Download](https://github.com/manusoft/Ytdlp.NET/releases/download/v1.0.0/video-downloader-app-v2.0.7z)

![Download ClipMate](https://github.com/user-attachments/assets/1b977927-ea26-4220-bd41-9f64d6716058)

---

## ✨ Features

- **Fluent API**: Build yt-dlp commands with `WithXxx()` methods.
- **Immutable & thread-safe**: Each method returns a new instance, safe for parallel usage.
- **Progress & Events**: Real-time progress tracking and post-processing notifications.
- **Format Listing**: Retrieve and parse available formats.
- **Batch Downloads**: Sequential or parallel execution.
- **Output Templates**: Flexible naming with yt-dlp placeholders.
- **Custom Command Injection**: Add extra yt-dlp options safely.
- **Cross-platform**: Windows, macOS, Linux.

---

## 🚀 New in v4.0.0

- **Lifecycle Refinement: No disposal required.** The library no longer implements ``IDisposable`` or ``IAsyncDisposable``. Instances are plain configuration objects.
- **Advanced Execution:** New `ExecuteRawAsync()` for power users who need to execute custom commands that bypass the fluent builder.
- **Deep Metadata Support:** Added ``GetDeepMetadataAsync()`` and ``GetDeepMetadataRawAsync()`` for full hierarchical structure (playlists → seasons → episodes).
- **Traverse Helper:** New ``Traverse()`` method for easy iteration over nested playlist entries.
- **Improved Auth:** Enhanced ``WithAdobePassAuthentication()`` and ``WithAuthentication()`` handling.
- **Subtitle Extraction:** New ``GetSubtitlesAsync()`` for streamlined subtitle retrieval.
- **MSO Listing:** New ``GetAdobePassListAsync()`` for Adobe Pass mso listing.
- **Robust Core:** Modernized ``ProcessRunner`` and ``ProcessFactory`` for efficient, isolated execution

---

## 🔧 Required Tools

- **Namespace**: `ManuHub.Ytdlp.NET` 
- **External JS runtime**: yt-dlp requires an external JS runtime like **deno.exe** (from [denoland/deno](https://deno.land)) for YouTube downloads with JS challenges.

```
tools/
├─ yt-dlp.exe
├─ ffmpeg.exe
├─ ffprobe.exe
└─ deno.exe
```

- **Recommended:** Use companion NuGet packages:

| Package | Description |
|---------|-------------|
| **ManuHub.Ytdlp** | Core download engine |
| **ManuHub.Deno** | JavaScript challenge resolution |
| **ManuHub.FFmpeg** | Post-processing, merging, and conversion |
| **ManuHub.FFprobe** | Format probing and metadata extraction |

Example path resolution in .NET:

```csharp
var ytdlpPath = Path.Combine(AppContext.BaseDirectory, "tools", "yt-dlp.exe");
var ffmpegPath = Path.Combine(AppContext.BaseDirectory, "tools");
```

---

## 🧬 Core Concepts

### No Disposal Required

**Ytdlp** holds no unmanaged resources. Create instances, share them, and let the GC collect them. All internal runners are created per-call.

### Immutable Fluent API

Every configuration method (e.g., ``WithOutputFolder``, ``WithFormat``) returns a new instance, ensuring the original is never modified. This makes branching configurations safe and clean.

### Thread Safety

A single ``Ytdlp`` instance can be shared across threads. Each execution creates isolated internal runners, allowing concurrent downloads without synchronization.

### Secure Authentication

Implemented secure authentication handling for various scenarios, including standard username/password and Adobe Pass authentication.

- .WithAuthentication(string username, string password)
- .WithAdobePassAuthentication(string mso, string username, string password)

> It securely handles credentials by passing them via standard input to the yt-dlp process, avoiding exposure in command-line arguments or logs. The library ensures that sensitive information is not stored in memory longer than necessary and is properly disposed of after use.

---

## 🚀 Quick Start

### 1. Basic Download

```csharp
var ytdlp = new Ytdlp("yt-dlp.exe")
    .WithOutputFolder("./downloads")
    .WithBestVideoPlusBestAudio()
    .WithEmbedMetadata();

// Subscribe to events
ytdlp.ProgressDownload += (s, e) => Console.WriteLine($"Progress: {e.Percent:F2}%");
ytdlp.DownloadCompleted += (s, msg) => Console.WriteLine($"Finished: {msg}");

// Execute
await ytdlp.DownloadAsync("https://www.youtube.com/watch?v=XXX");
```

### 2. Immutable Configuration Branching

```csharp
// Define a shared base configuration
var baseConfig = new Ytdlp("yt-dlp.exe").WithOutputFolder("./media");

// Create specialized versions
var audioOnly = baseConfig.WithBestAudioOnly();
var highRes = baseConfig.WithMaxHeightOrBest(1080);

// baseConfig, audioOnly, and highRes are independent, thread-safe instances
await Task.WhenAll(
    audioOnly.DownloadAsync(url1),
    highRes.DownloadAsync(url2)
);
```

---
## ⚡ Advanced Execution & Control

For power users who need to execute custom commands that bypass the fluent builder, use `ExecuteRawAsync`. This acts as an "escape hatch" for scenarios where specific, non-standard, or experimental `yt-dlp` flags are required.

### How it works
This method automatically intelligently switches output handling based on how you use it:

- **Streaming Mode:** Provide an `Action<string>` to `onLineReceived` to stream output in real-time (ideal for progress monitoring or logs).
- **Capture Mode:** Pass null to `onLineReceived` to capture the entire process output into result.FullOutput (ideal for JSON metadata probing or one-off commands).

```csharp
var ytdlp = new Ytdlp("yt-dlp.exe");

// 1. Capture Mode (Probe/Manual)
var result = await ytdlp.ExecuteRawAsync("--version");
Console.WriteLine($"yt-dlp version: {result.FullOutput}");

// 2. Streaming Mode (Real-time tracking)
await ytdlp.ExecuteRawAsync("--help", onLineReceived: line => Console.WriteLine(line));
```

> **Note:** While `ExecuteRawAsync` handles security and formatting, logical validation (e.g., passing valid `yt-dlp` flags) remains the responsibility of the developer. Always prefer the fluent `WithXxx()` methods for standard download tasks.

---

## 📦 Usage Examples

### Fetch Metadata

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe");

var metadata = await ytdlp.GetMetadataAsync("https://www.youtube.com/watch?v=abc123");

Console.WriteLine($"Title: {metadata?.Title}, Duration: {metadata?.Duration}");
```

### Deep Metadata Extraction

```csharp
var metadata = await ytdlp.GetDeepMetadataAsync(url);

foreach (var root in metadata.Entries ?? [])
{
    foreach (var item in root.Traverse())
    {
        Console.WriteLine(item.Title);
    }
}
```

### Parallel Execution

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe")
    .WithFormat("best")
    .WithOutputFolder("./batch");

var urls = new[] { "https://youtu.be/vid1", "https://youtu.be/vid2" };

// Safe: Concurrent usage of the same instance
await ytdlp.DownloadBatchAsync(urls, maxConcurrency: 3);
```

### Extract Audio

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe")
    .WithExtractAudio(AudioFormat.Mp3, 5)
    .WithOutputFolder("./audio")
    .WithEmbedThumbnail()
    .WithEmbedMetadata();

await ytdlp.DownloadAsync("https://www.youtube.com/watch?v=RGg-Qx1rL9U");
```

### Download a Playlist

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe")
    .WithFormat("best")
    .WithOutputFolder("./playlists")
    .WithPlaylistStart(1)
    .WithPlaylistEnd(5)
    .OutputTemplate("%(playlist)s/%(playlist_index)s - %(title)s.%(ext)s");

await ytdlp.DownloadAsync("https://www.youtube.com/playlist?list=PL12345");
```

### Fetch Formats

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe");

var formats = await ytdlp.GetFormatsAsync("https://www.youtube.com/watch?v=abc123");

foreach(var format in formats)
    Console.WriteLine($"Id: {metadata?.Id}, Extension: {metadata?.Extension}");
```

### Best Format Selection

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe");

string bestAudio = await ytdlp.GetBestAudioFormatIdAsync(url);
string bestVideo = await ytdlp.GetBestVideoFormatIdAsync(url, maxHeight: 720);

await ytdlp
    .WithFormat($"{bestVideo}+{bestAudio}/best")
    .WithOutputFolder("./downloads")
    .DownloadAsync(url);
```

### Get Subtitles

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe");
var subtitles = await ytdlp.GetSubtitlesAsync("https://www.youtube.com/watch?v=abc123");
foreach (var sub in subtitles)
{
    Console.WriteLine($"Language: {sub.Language}, Format: {sub.Format}, Url: {sub.Url}");
}
```

### Get Adobe Pass MSO List

```csharp
var msoList = await ytdlp.GetAdobePassListAsync();
```

---

## 📡 Events

| Event                     | Description              |
| --------------------------| ------------------------ |
| `ProgressDownload`        | Download progress        |
| `ProgressMessage`         | Informational messages   |
| `DownloadCompleted`       | File finished            |
| `PostProcessingStarted`   | Post‑processing start    |
| `PostProcessingCompleted` | Post‑processing finished |
| `OutputMessage`           | Raw output line          |
| `ErrorMessage`            | Error message            |
| `CommandCompleted`        | Process finished         |


### Example
```csharp
// Progress events
ytdlp.ProgressDownload += (s, e) => Console.WriteLine($"{e.Percent:F1}%  {e.Speed}  ETA {e.ETA}");
ytdlp.ProgressMessage += (s, msg) => Console.WriteLine(msg);

// Output events
ytdlp.ErrorMessage += (s, err) => Console.WriteLine($"Error: {err}");
ytdlp.OutputMessage += (s, msg) => Console.WriteLine(msg);

// Lifecycle events
ytdlp.DownloadCompleted += (s, msg) => Console.WriteLine($"Finished: {msg}");
ytdlp.CommandCompleted += (s, e) => Console.WriteLine($"Command finished: {e.Command}");

// Post-Processing events
ytdlp.PostProcessingStarted += (s, msg) => Console.WriteLine($"Post-processing-start: {msg}");
ytdlp.PostProcessingCompleted += (s, msg) => Console.WriteLine($"Post-processing-complete: {msg}");
```

---
## 🛠 Methods

### Probe
* `VersionAsync()`
* `UpdateAsync(UpdateChannel channel, string specificVersion)`
* `GetExtractorsAsync()`
* `GetAdobePassListAsync()`
* `GetSubtitlesAsync(string url)`
* `GetMetadataAsync(string url)`
* `GetMetadataRawAsync(string url)`
* `GetDeepMetadataAsync(string url)`
* `GetDeepMetadataRawAsync(string url)`
* `GetFormatsAsync(string url)`
* `GetMetadataLiteAsync(string url)`
* `GetMetadataLiteAsync(string url, IEnumerable<string> fields)`
* `GetBestAudioFormatIdAsync(string url)`
* `GetBestVideoFormatIdAsync(string url, int maxHeight)`

### Download
* `DownloadAsync(string url)`
* `DownloadBatchAsync(IEnumerable<string> urls, int maxConcurrency)`

### Advanced Execution
* `ExecuteRawAsync(string arguments, Action<string>? onLineReceived = null, CancellationToken ct = default, bool tuneProcess = true)`

---

## 🛠 Fluent Methods

### Authentication Options
* `.WithAuthentication(string username, string password)`
* `.WithTwoFactor(string code)`
* `.WithVideoPassword(string password)`
* `.WithAdobePassAuthentication(string mso, string username, string password)`

### Download Options
* `.WithConcurrentFragments(int count = 8)`
* `.WithLimitRate(string rate)`
* `.WithThrottledRate(string rate)`
* `.WithRetries(int maxRetries)`
* `.WithFileAccessRetries(int maxRetries)`
* `.WithFragmentRetries(int retries)`
* `.WithRetrySleep(string retrySleepExpression)`
* `.WithRetrySleep(int seconds, string? type = null)`
* `.WithLinearRetrySleep(int start, int? end = null, int? step = null, string? type = null)`
* `.WithExponentialRetrySleep(int start, int? end = null, double? @base = null, string? type = null)`
* `.WithSkipUnavailableFragments()`
* `.WithAbortOnUnavailableFragments()`
* `.WithKeepFragments()`
* `.WithBufferSize(string size)`
* `.WithNoResizeBuffer()`
* `.WithHttpChunkSize(string size)`
* `.WithHttpChunkSize(long bytes)`
* `.WithPlaylistRandom()`
* `.WithLazyPlaylist()`
* `.WithNoLazyPlaylist()`
* `.WithHlsUseMpegts()`
* `.WithNoHlsUseMpegts()`
* `.WithDownloadSections(string regex)`
* `.WithDownloader(string downloader)`
* `.WithDownloader(string downloader, params string[] protocols)`
* `.WithDownloaderArgs(string downloaderName, string args)`

### Extractor Options
* `.WithExtractorRetries(int retries)`
* `.WithInfiniteExtractorRetries()`
* `.WithAllowDynamicMpd()`
* `.WithIgnoreDynamicMpd()`
* `.WithHlsSplitDiscontinuity()`
* `.WithNoHlsSplitDiscontinuity()`
* `.WithExtractorArgs(string extractorKey, string args)`

### Filesystem Options
* `.WithHomeFolder(string path)`
* `.WithTempFolder(string path)`
* `.WithOutputFolder(string path)`
* `.WithFFmpegLocation(string path)`
* `.WithOutputTemplate(string template)`
* `.WithRestrictFilenames()`
* `.WithWindowsFilenames()`
* `.WithTrimFilenames(int length)`
* `.WithNoOverwrites()`
* `.WithForceOverwrites()`
* `.WithNoContinue()`
* `.WithNoPart()`
* `.WithMtime()`
* `.WithWriteDescription()`
* `.WithWriteInfoJson()`
* `.WithNoWritePlaylistMetafiles()`
* `.WithNoCleanInfoJson()`
* `.WriteComments()`
* `.WithNoWriteComments()`
* `.WithLoadInfoJson(string path)`
* `.WithCookiesFile(string path)`
* `.WithCookiesFromBrowser(string browser)`
* `.WithNoCacheDir()`
* `.WithRemoveCacheDir()`

### General Options
* `.WithIgnoreErrors()`
* `.WithAbortOnError()`
* `.WithIgnoreConfig()`
* `.WithConfigLocations(string path)`
* `.WithPluginDirs(string path)`
* `.WithNoPluginDirs(string path)`
* `.WithJsRuntime(Runtime runtime, string runtimePath)`
* `.WithNoJsRuntime()`
* `.WithRemoteComponent(string component)`
* `.WithRemoteComponents(params string[] components)`
* `.WithNoRemoteComponents()`
* `.WithFlatPlaylist()`
* `.WithLiveFromStart()`
* `.WithWaitForVideo(TimeSpan? maxWait = null)`
* `.WithMarkWatched()`   
* `.WithNoMarkWatched()`
* `.WithColor(string policy, string? stream = null)`
* `.WithCompatOptions(string options)`
* `.WithAlias(string alias, string options)`
* `.WithPresetAlias(string preset)`
* `.WithPresetAlias(YtdlpPreset preset)`

### Geo-restriction Options
* `.WithGeoVerificationProxy(string url)`
* `.WithGeoBypassCountry(string countryCode)`

### Network Options
* `.WithProxy(string? proxy)`
* `.WithSocketTimeout(TimeSpan timeout)`
* `.WithSourceAddress(string ipAddress)`
* `.WithImpersonate(string? client)`
* `.WithImpersonateAny()`
* `.WithForceIpv4()`
* `.WithForceIpv6()`
* `.WithEnableFileUrls()`

### Post-Processing Options
* `.WithExtractAudio(string format, int quality = 5)`
* `.WithRemuxVideo(string format)` usage 'mp4' or 'mp4>mkv'
* `.WithRecodeVideo(string format, string? videoCodec = null, string? audioCodec = null)`
* `.WithPostprocessorArgs(PostProcessors postprocessor, string args)`
* `.WithKeepVideo()`
* `.WithNoPostOverwrites()`
* `.WithEmbedSubtitles()`
* `.WithEmbedThumbnail()`
* `.WithEmbedMetadata()`
* `.WithEmbedChapters()`
* `.WithEmbedInfoJson()`
* `.WithNoEmbedInfoJson()`
* `.WithReplaceInMetadata(string field, string regex, string replacement)`
* `.WithConcatPlaylist(string policy = "always")`
* `.WithFFmpegLocation(string? ffmpegPath)`
* `.WithConvertSubtitles(string format = "none")`
* `.WithConvertThumbnails(string format = "jpg")`
* `.WithSplitChapters() => AddFlag("--split-chapters")`
* `.WithRemoveChapters(string regex)`
* `.WithForceKeyframesAtCuts()`
* `.WithUsePostProcessor(PostProcessors postProcessor, string? postProcessorArgs = null)`

### SponsorBlock Options
* `.WithSponsorblockMark(string categories = "all")`
* `.WithSponsorblockRemove(string categories = "all")`
* `.WithNoSponsorblock()`

### Subtitle Options
* `.WithSubtitles(string languages = "all", bool auto = false)`

### Thumbnail Options
* `.WithThumbnails(bool allSizes = false)`

### Verbosity and Simulation Options
* `.WithQuiet()`
* `.WithNoWarnings()`
* `.WithSimulate()`
* `.WithNoSimulate()`
* `.WithSkipDownload()`
* `.WithVerbose()`

### Video Selection
* `.WithPlaylistItems(string items)`
* `.WithPlaylistItems(params int[] indices)`
* `.WithPlaylistRange(int? start = null, int? stop = null, int? step = null)`
* `.WithMinFileSize(string size)`
* `.WithMaxFileSize(string size)`
* `.WithDate(string date)`
* `.WithDateBefore(string date)`
* `.WithDateAfter(string date)`
* `.WithMatchFilter(string filterExpression)`
* `.WithNoMatchFilters()`
* `.WithBreakMatchFilter(string filter)`
* `.WithNoBreakMatchFilters()`
* `.WithNoPlaylist()`
* `.WithYesPlaylist()`
* `.WithAgeLimit(int years)`
* `.WithDownloadArchive(string archivePath = "archive.txt")`
* `.WithNoDownloadArchive()`
* `.WithMaxDownloads(int count)`
* `.WithBreakOnExisting()`
* `.WithNoBreakOnExisting()`
* `.WithBreakPerInput()`
* `.WithNoBreakPerInput()`
* `.WithSkipPlaylistAfterErrors(int allowedFailures)`

### Video Format Options
* `.WithFormat(string format)`
* `.WithMergeOutputFormat(string format)`

### Workgrounds
* `.WithAddHeader(string header, string value)`
* `.WithSleepInterval(double seconds, double? maxSeconds = null)`
* `.WithSleepSubtitles(double seconds)`

### Downloaders
* `.WithAria2(int connections = 16)`
* `.WithHlsNative()`
* `.WithFfmpegAsLiveDownloader(string? extraFfmpegArgs = null)`

### Bonus
* `.With1440pOrBest()`
* `.With1080pOrBest()`
* `.With720pOrBest()`
* `.WithMp4PostProcessingPreset()`
* `.WithMkvOutput()`
* `.WithMaxHeight(int height)`
* `.WithMaxHeightOrBest(int height)`
* `.WithBestVideoPlusBestAudio()`
* `.WithBestAudioOnly()`
* `.WithNo4k()`
* `.WithBestM4aAudio()`

### Advanced Options
* `.AddFlag(string flag)`
* `.AddOption(string key, string value)`

---

## ⚙️ Customization

If you need specific arguments not covered by the fluent API:

```csharp
ytdlp.AddFlag("--no-check-certificate")
     .AddOption("--external-downloader", "aria2c")
     .DownloadAsync(url);
```

---

## 🔄 Migration Guide: Upgrading to v4

Version 4.0.0 is a major release that refines the API for better maintainability and removes the overhead of manual lifecycle management.

> **Note:** The primary breaking change is the removal of `IDisposable`/`IAsyncDisposable`. You no longer need to dispose of your `Ytdlp` instances.

### 1. Key Changes at a Glance

| Feature | v3.x | v4.x |
| --- | --- | --- |
| **Lifecycle** | Required `IAsyncDisposable` | **No disposal required** |
| **Architecture** | Immutable Fluent API | Immutable Fluent API (Refactored) |
| **Core Process** | `ProcessFactory` | `ProcessFactory` (Refactored) |
| **Core Runner** | `ProbeRunner` `DownloadRunner` | `ProcessRunner` |


### 2. Side-by-Side Comparison

#### ❌ Legacy API (v3)

```csharp
// Previously required disposal
await using var ytdlp = new Ytdlp()
    .WithFormat("best")
    .WithOutputFolder("./downloads");

await ytdlp.DownloadAsync(url);

```

#### ✅ New API (v4)

```csharp
// Cleaner: No disposal required
var ytdlp = new Ytdlp()
    .WithFormat("best")
    .WithOutputFolder("./downloads");

await ytdlp.DownloadAsync(url);

```

### 3. Why the change?

We have streamlined the `Ytdlp` lifecycle. Because the instance does not hold unmanaged resources that require explicit cleanup, we have removed the `IDisposable` and `IAsyncDisposable` interfaces.

* **Cleaner Code:** Your codebase is now free of `await using` or `using` statements for `Ytdlp` instances.
* **Refactored Core:** The internal `ProcessFactory` has been updated and introduce `ProcessRunner` to handle process execution more efficiently without needing to manage the object lifecycle manually.

### 4. Migration Checklist

* [ ] **Remove `await using` or `using`:** Simply delete the disposal keywords where you instantiate `Ytdlp`.
* [ ] **Verify Events:** Ensure event subscriptions are attached to the instance used for the specific execution.

---

## 💡 Notes

- **Dependencies:** Ensure ``yt-dlp`` (and optionally ``FFmpeg``/``FFprobe``) are available on your system path or point to their specific locations via ``WithFfmpegLocation()`` (if configured).
- **Performance:** ``tuneProcess: true`` (default) is enabled for download methods to optimize output buffer management.

---

## 🧪 Example Apps

* ClipMate downloader
* Console examples

---

## 🤝 Contributing

Contributions are welcome!

Open issues or PRs on GitHub.

---

## 📜 License

MIT License — see [LICENSE](https://github.com/manusoft/Ytdlp.NET/blob/master/LICENSE.md)

---

## 👨‍💻 Author

**Manoj Babu**, 
ManuHub

## 👥 Contributors

Thanks to all contributors ❤️

<a href="https://github.com/manusoft/Ytdlp.NET/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=manusoft/Ytdlp.NET" />
</a>
