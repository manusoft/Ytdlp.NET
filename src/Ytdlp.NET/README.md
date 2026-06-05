﻿![Static Badge](https://img.shields.io/badge/Ytdlp.NET-red) ![NuGet Version](https://img.shields.io/nuget/v/Ytdlp.NET)  ![NuGet Downloads](https://img.shields.io/nuget/dt/Ytdlp.NET)

# Ytdlp.NET

> **Ytdlp.NET** is a **fluent, strongly-typed .NET wrapper** around [`yt-dlp`](https://github.com/yt-dlp/yt-dlp). It provides a fully **async, event-driven interface** for downloading videos, extracting audio, retrieving metadata, and post-processing media from YouTube and hundreds of other platforms.

---

## ⚠️ Important Notes

* **Namespace migrated**: `ManuHub.Ytdlp.NET` — update your `using` directives.
* **External JS runtime**: yt-dlp requires an external JS runtime like **deno.exe** (from [denoland/deno](https://deno.land)) for YouTube downloads with JS challenges.
* **Required tools**:

```
Tools/
├─ yt-dlp.exe
├─ deno.exe
├─ ffmpeg.exe
└─ ffprobe.exe
```

> Recommended: Use companion NuGet packages:
>
> * `ManuHub.Ytdlp`
> * `ManuHub.Deno`
> * `ManuHub.FFmpeg`
> * `ManuHub.FFprobe`

Example path resolution in .NET:

```csharp
var ytdlpPath = Path.Combine(AppContext.BaseDirectory, "tools", "yt-dlp.exe");
var ffmpegPath = Path.Combine(AppContext.BaseDirectory, "tools");
```

---

## 📄 Get Subtitles
Ytdlp.NET now supports fetching subtitles with `GetSubtitlesAsync()`:
```csharp
var subtitles = await ytdlp.GetSubtitlesAsync(url);
```

## 📄 Get Adobe Pass MSO List
Ytdlp.NET now supports fetching the Adobe Pass MSO list with `GetAdobePassListAsync()`:
```csharp
var msoList = await ytdlp.GetAdobePassListAsync();
```

## 🌲 Deep Metadata Support

Ytdlp.NET now supports **deep playlist extraction** with full hierarchical structure support (seasons → episodes → nested playlists).

### 🔹 Flat Mode (default - no change)

```csharp
var metadata = await ytdlp.GetMetadataAsync(url);
```

* Fast
* Returns only top-level items
* Fully backward compatible

---

### 🔹 Deep Mode (NEW)

```csharp
var metadata = await ytdlp.GetDeepMetadataAsync(url);
```

* Returns full hierarchy
* Supports playlists → seasons → episodes
* Slightly slower but complete data

---

## 🔁 Traverse Nested Entries

Use this helper to read all items in deep mode:

```csharp
foreach (var root in metadata.Entries ?? [])
{
    foreach (var item in root.Traverse())
    {
        Console.WriteLine(item.Title);
    }
}
```

---

## 🚀 New in this release

* Add more WithXxx() methods for advanced options.
* New **GetAdobePassListAsync()** for Adobe Pass mso listing.
* New **GetSubtitlesAsync()** for subtitle extraction.
* New **Traverse()** method for easy iteration over nested playlist entries.
* New **GetDeepMetadataAsync()** method for comprehensive metadata extraction.
* New **GetDeepMetadataRawAsync()** for raw JSON metadata.
* Improved **Metadata** model with more fields and better parsing.
* Improved **UpdateAsync** with specific version support.
* Full support for **IAsyncDisposable** with **await using**.
* Immutable builder (**WithXxx**) for safe instance reuse.
* Updated examples for event-driven downloads.
* Simplified metadata fetching & format selection.
* High-performance probe methods with optional buffer size.
* Improved cancellation & error handling.

---

## ✨ Features

* **Fluent API**: Build yt-dlp commands with `WithXxx()` methods.
* **Immutable & thread-safe**: Each method returns a new instance, safe for parallel usage.
* **Async & IAsyncDisposable**: Automatic cleanup of child processes.
* **Progress & Events**: Real-time progress tracking and post-processing notifications.
* **Format Listing**: Retrieve and parse available formats.
* **Batch Downloads**: Sequential or parallel execution.
* **Output Templates**: Flexible naming with yt-dlp placeholders.
* **Custom Command Injection**: Add extra yt-dlp options safely.
* **Cross-platform**: Windows, macOS, Linux (where yt-dlp is supported).

---

## 🛠 Methods
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
* `ExecuteAsync(string url)`
* `ExecuteBatchAsync(IEnumerable<string> urls, int maxConcurrency)`


## 🔧 Thread Safety & Disposal

* **Immutable & thread-safe**: Each `WithXxx()` call returns a new instance.
* **Async disposal**: `Ytdlp` implements `IAsyncDisposable`.

### **Sequential download example**:

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe", new ConsoleLogger())
    .WithFormat("best")
    .WithOutputFolder("./downloads");

ytdlp.OnProgressDownload += (s, e) => Console.WriteLine($"Progress: {e.Percent:F2}%");
ytdlp.OnCompleteDownload += (s, msg) => Console.WriteLine($"Download complete: {msg}");

await ytdlp.DownloadAsync("https://www.youtube.com/watch?v=RGg-Qx1rL9U");
```

### **Parallel download example**:

```csharp
var urls = new[] { "https://youtu.be/video1", "https://youtu.be/video2" };

var tasks = urls.Select(async url =>
{
    var ytdlp = new Ytdlp("tools\\yt-dlp.exe", new ConsoleLogger())
        .WithFormat("best")
        .WithOutputFolder("./batch");

    ytdlp.OnProgressDownload += (s, e) => Console.WriteLine($"[{url}] {e.Percent:F2}%");
    ytdlp.OnCompleteDownload += (s, msg) => Console.WriteLine($"[{url}] Download complete: {msg}");

    await ytdlp.DownloadAsync(url);
});

await Task.WhenAll(tasks);
```

### **Key points**:

1. Always create a **new instance per download** for parallel operations.
2. No shared state between instances, so no need to worry about thread safety.
3. Attach events **after the `WithXxx()` call**.

---

## 📦 Basic Usage

### Download a Single Video

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe", new ConsoleLogger())
    .WithFormat("best")
    .WithOutputFolder("./downloads")
    .WithEmbedMetadata()
    .WithEmbedThumbnail();

ytdlp.OnProgressDownload += (s, e) => Console.WriteLine($"Progress: {e.Percent:F2}%");
ytdlp.OnCompleteDownload += (s, msg) => Console.WriteLine($"Download complete: {msg}");

await ytdlp.DownloadAsync("https://www.youtube.com/watch?v=RGg-Qx1rL9U");
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

---

### Fetch Metadata

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe");

var metadata = await ytdlp.GetMetadataAsync("https://www.youtube.com/watch?v=abc123");

Console.WriteLine($"Title: {metadata?.Title}, Duration: {metadata?.Duration}");
```

---

### Fetch Formats

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe");

var formats = await ytdlp.GetFormatsAsync("https://www.youtube.com/watch?v=abc123");

foreach(var format in formats)
    Console.WriteLine($"Id: {metadata?.Id}, Extension: {metadata?.Extension}");
```

---

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

---

## Get Subtitles
```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe");
var subtitles = await ytdlp.GetSubtitlesAsync("https://www.youtube.com/watch?v=abc123");
foreach (var sub in subtitles)
{
    Console.WriteLine($"Language: {sub.Language}, Format: {sub.Format}, Url: {sub.Url}");
}
```

### Batch Downloads

```csharp
var urls = new[] { "https://youtu.be/vid1", "https://youtu.be/vid2" };

var tasks = urls.Select(async url =>
{
    var ytdlp = new Ytdlp("tools\\yt-dlp.exe")
        .WithFormat("best")
        .WithOutputFolder("./batch");

    await ytdlp.DownloadAsync(url);
});

await Task.WhenAll(tasks);
```
**OR**

```csharp
var urls = new[] { "https://youtu.be/vid1", "https://youtu.be/vid2" };

var ytdlp = new Ytdlp("tools\\yt-dlp.exe")
        .WithFormat("best")
        .WithOutputFolder("./batch");

await ytdlp.DownloadBatchAsync(urls, maxConcurrency: 3);
```
---

## Fluent Methods

### General Options
* `.WithIgnoreErrors()`
* `.WithAbortOnError()`
* `.WithIgnoreConfig()`
* `.WithConfigLocations(string path)`
* `.WithPluginDirs(string path)`
* `.WithNoPluginDirs(string path)`
* `.WithJsRuntime(Runtime runtime, string runtimePath)`
* `.WithNoJsRuntime()`
* `.WithFlatPlaylist()`
* `.WithLiveFromStart()`
* `.WithWaitForVideo(TimeSpan? maxWait = null)`
* `.WithMarkWatched()`   

### Network Options
* `.WithProxy(string? proxy)`
* `.WithSocketTimeout(TimeSpan timeout)`
* `.WithForceIpv4()`
* `.WithForceIpv6()`
* `.WithEnableFileUrls()`

### Geo-restriction Options
* `.WithGeoVerificationProxy(string url)`
* `.WithGeoBypassCountry(string countryCode)`

### Video Selection
* `.WithPlaylistItems(string items)`
* `.WithMinFileSize(string size)`
* `.WithMaxFileSize(string size)`
* `.WithDate(string date)`
* `.WithDateBefore(string date)`
* `.WithDateAfter(string date)`
* `.WithMatchFilter(string filterExpression)`
* `.WithNoPlaylist()`
* `.WithYesPlaylist()`
* `.WithAgeLimit(int years)`
* `.WithDownloadArchive(string archivePath = "archive.txt")`
* `.WithMaxDownloads(int count)`
* `.WithBreakOnExisting()`

### Download Options
* `.WithConcurrentFragments(int count = 8)`
* `.WithLimitRate(string rate)`
* `.WithThrottledRate(string rate)`
* `.WithRetries(int maxRetries)`
* `.WithFileAccessRetries(int maxRetries)`
* `.WithFragmentRetries(int retries)`
* `.WithSkipUnavailableFragments()`
* `.WithAbortOnUnavailableFragments()`
* `.WithKeepFragments()`
* `.WithBufferSize(string size)`
* `.WithNoResizeBuffer()`
* `.WithPlaylistRandom()`
* `.WithHlsUseMpegts()`
* `.WithNoHlsUseMpegts()`
* `.WithDownloadSections(string regex)`

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

### Thumbnail Options
* `.WithThumbnails(bool allSizes = false)`

### Verbosity and Simulation Options
* `.WithQuiet()`
* `.WithNoWarnings()`
* `.WithSimulate()`
* `.WithNoSimulate()`
* `.WithSkipDownload()`
* `.WithVerbose()`

### Workgrounds
* `.WithAddHeader(string header, string value)`
* `.WithSleepInterval(double seconds, double? maxSeconds = null)`
* `.WithSleepSubtitles(double seconds)`

### Video Format Options
* `.WithFormat(string format)`
* `.WithMergeOutputFormat(string format)`

### Subtitle Options
* `.WithSubtitles(string languages = "all", bool auto = false)`

### Authentication Options
* `.WithAuthentication(string username, string password)`
* `.WithTwoFactor(string code)`
* `.WithVideoPassword(string password)`
* `.WithAdobePassAuthentication(string mso, string username, string password)`

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

### Advanced Options
* `.AddFlag(string flag)`
* `.AddOption(string key, string value)`

### Downloaders
* `.WithExternalDownloader(string downloaderName, string? downloaderArgs = null)`
* `.WithAria2(int connections = 16)`
* `.WithHlsNative()`
* `.WithFfmpegAsLiveDownloader(string? extraFfmpegArgs = null)`

AND MORE ...

---

### Events

```csharp
ytdlp.OnProgressDownload += (s, e) => Console.WriteLine($"Progress: {e.Percent:F2}%");
ytdlp.OnProgressMessage += (s, msg) => Console.WriteLine(msg);
ytdlp.OnCompleteDownload += (s, msg) => Console.WriteLine($"Done: {msg}");
ytdlp.OnPostProcessingStart += (s, msg) => Console.WriteLine($"Post-processing-start: {msg}")
ytdlp.OnPostProcessingComplete += (s, msg) => Console.WriteLine($"Post-processing-complete: {msg}");
ytdlp.OnErrorMessage += (s, err) => Console.WriteLine($"Error: {err}");
ytdlp.OnOutputMessage += (s, msg) => Console.WriteLine(msg);
ytdlp.OnCommandCompleted += (s, e) => Console.WriteLine($"Command finished: {e.Command}");
```

---

# 🔄 Upgrade Guide (v3 → v4)

v4 introduces a **new immutable fluent API**.

Old mutable commands were removed.

---

## ❌ Old API (v3)

```csharp
await using var ytdlp = new Ytdlp()
    .WithFormat("best")
    .WithOutputFolder("./downloads");

await ytdlp.DownloadAsync(url);
```

---

## ✅ New API (v4)

```csharp
var ytdlp = new Ytdlp();

await ytdlp
    .SetFormat("best")
    .SetOutputFolder("./downloads")
    .ExecuteAsync(url);
```

---

## Custom commands
```csharp
AddFlag("--no-check-certificate");
AddOption("--external-downloader", "aria2c");
```

## Important behavior changes

### Instances are immutable

Every `WithXxx()` call returns a **new instance**.

```csharp
var baseYtdlp = new Ytdlp();

var download = baseYtdlp
    .WithFormat("best")
    .WithOutputFolder("./downloads");
```

---

### Event subscription

Attach events **to the configured instance**.

```csharp
var download = baseYtdlp.WithFormat("best");

download.OnProgressDownload += ...
```

---

### Removed disposal of old instances

No need to dispose intermediate instances since they are immutable and stateless.
So you can create a base instance and reuse it without worrying about disposal:

```csharp
var ytdlp = new Ytdlp();
```

---


### ✅ Notes

* All commands now start with `WithXxx()`.
* Immutable: no shared state; safe for parallel usage.
* No need to dispose intermediate instances.
* Deprecated old methods removed.
* Probe methods remain the same (`GetMetadataAsync`, `GetFormatsAsync`, `GetBestVideoFormatIdAsync`, etc.).

---

### License

MIT License — see [LICENSE](https://github.com/manusoft/Ytdlp.NET/blob/master/LICENSE.md)

**Author:** Manojbabu (ManuHub)   
**Repository:** [Ytdlp.NET](https://github.com/manusoft/Ytdlp.NET)
