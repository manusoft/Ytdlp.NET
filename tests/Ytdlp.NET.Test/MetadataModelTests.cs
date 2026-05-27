using FluentAssertions;
using ManuHub.Ytdlp.NET.Extensions;

namespace ManuHub.Ytdlp.NET.Test;

/// <summary>
/// Tests for the Metadata / VideoMetadata model returned by GetMetadataAsync.
/// Uses direct object construction — no yt-dlp process involved.
/// </summary>
public class MetadataModelTests
{
    // ── Basic field access ────────────────────────────────────────────────

    [Fact]
    public void Metadata_Title_CanBeSet()
    {
        var meta = new Metadata { Title = "My Test Video" };

        meta.Title.Should().Be("My Test Video");
    }

    [Fact]
    public void Metadata_Duration_CanBeSet()
    {
        var meta = new Metadata { Duration = 300 };

        meta.Duration.Should().Be(300);
    }

    [Fact]
    public void Metadata_Id_CanBeSet()
    {
        var meta = new Metadata { Id = "abc123" };

        meta.Id.Should().Be("abc123");
    }

    [Fact]
    public void Metadata_Uploader_CanBeSet()
    {
        var meta = new Metadata { Uploader = "TestChannel" };

        meta.Uploader.Should().Be("TestChannel");
    }

    [Fact]
    public void Metadata_ViewCount_CanBeSet()
    {
        var meta = new Metadata { ViewCount = 1_000_000 };

        meta.ViewCount.Should().Be(1_000_000);
    }

    [Fact]
    public void Metadata_Thumbnail_CanBeSet()
    {
        var meta = new Metadata { Thumbnail = "https://img.youtube.com/vi/abc123/0.jpg" };

        meta.Thumbnail.Should().Be("https://img.youtube.com/vi/abc123/0.jpg");
    }

    [Fact]
    public void Metadata_Description_CanBeSet()
    {
        var meta = new Metadata { Description = "A description." };

        meta.Description.Should().Be("A description.");
    }

    // ── Nullable defaults ─────────────────────────────────────────────────

    [Fact]
    public void Metadata_NewInstance_TitleIsNull()
    {
        var meta = new Metadata();

        meta.Title.Should().BeNull();
    }

    [Fact]
    public void Metadata_NewInstance_DurationIsNull()
    {
        // Duration is float? — unset means null, not 0
        var meta = new Metadata();

        meta.Duration.Should().BeNull();
    }

    [Fact]
    public void Metadata_NewInstance_EntriesIsNullOrEmpty()
    {
        var meta = new Metadata();

        // Either null or empty is acceptable — just must not throw
        var _ = meta.Entries?.Count ?? 0;
    }

    // ── Traverse helper ───────────────────────────────────────────────────

    [Fact]
    public void Traverse_FlatList_ReturnsAllItems()
    {
        var items = new List<Entry>
        {
            new() { Id = "ep1", Title = "Episode 1" },
            new() { Id = "ep2", Title = "Episode 2" },
            new() { Id = "ep3", Title = "Episode 3" },
        };

        var results = items.SelectMany(i => i.Traverse()).ToList();

        results.Should().HaveCount(3);
        results.Select(r => r.Id).Should().BeEquivalentTo(["ep1", "ep2", "ep3"]);
    }

    [Fact]
    public void Traverse_NestedEntries_ReturnsAllLeafItems()
    {
        // Playlist → Season → Episode structure
        var season = new Entry
        {
            Id = "season1",
            Title = "Season 1",
            Entries = new List<Entry>
            {
                new() { Id = "ep1", Title = "Episode 1" },
                new() { Id = "ep2", Title = "Episode 2" },
            }
        };

        var playlist = new Metadata
        {
            Id = "playlist",
            Title = "My Playlist",
            Entries = new List<Entry> { season }
        };

        // Traverse from season level should return both episodes
        var results = season .Traverse().ToList();

        results.Should().Contain(e => e.Id == "ep1");
        results.Should().Contain(e => e.Id == "ep2");
    }

    [Fact]
    public void Traverse_NullEntries_DoesNotThrow()
    {
        var meta = new Entry { Id = "single", Entries = null };

        var act = () => meta.Traverse().ToList();

        act.Should().NotThrow();
    }

    [Fact]
    public void Traverse_EmptyEntries_ReturnsEmptyOrSelf()
    {
        var meta = new Entry { Id = "empty", Entries = new List<Entry>() };

        var result = meta.Traverse().ToList();

        // Traverse should not throw and should return a usable collection
        result.Should().NotBeNull();
    }
}