using MediaDownloader.Download.Utilities;

namespace MediaDownloader.Tests;

public class DownloadItemMapperTests
{
    private static string ReadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", name));

    [Fact]
    public void Map_SingleVideo_ReturnsOneEntryWithSanitizedNameAndExtension()
    {
        var result = DownloadItemMapper.Map(ReadFixture("single_video.json"), "https://requested.link/");

        Assert.NotNull(result);
        Assert.Equal("Sample Video_ The _Best_ One", result.Name);
        var entry = Assert.Single(result.Entries);
        Assert.Equal("Sample Video_ The _Best_ One.mp4", entry.Name);
        Assert.Equal("https://www.youtube.com/watch?v=dQw4w9WgXcQ", entry.Url);
    }

    [Fact]
    public void Map_Playlist_WithoutTopLevelExt_MapsAllEntries()
    {
        var result = DownloadItemMapper.Map(ReadFixture("playlist.json"), null);

        Assert.NotNull(result);
        Assert.Equal("My Playlist", result.Name);
        Assert.Equal(2, result.Entries.Count);
        Assert.Equal("First Video.mp4", result.Entries[0].Name);
        Assert.Equal("https://www.youtube.com/watch?v=vid1", result.Entries[0].Url);
        Assert.Equal("Second Video.webm", result.Entries[1].Name);
        Assert.Equal("https://www.youtube.com/watch?v=vid2", result.Entries[1].Url);
    }

    [Fact]
    public void Map_PlaylistWithSparseEntries_SkipsEntriesWithoutUrl_AndFallsBackToId()
    {
        var result = DownloadItemMapper.Map(ReadFixture("playlist_sparse_entries.json"), null);

        Assert.NotNull(result);
        Assert.Equal(2, result.Entries.Count);
        // Entry without "ext" keeps its title as-is.
        Assert.Equal("No Extension", result.Entries[0].Name);
        // Entry without "webpage_url" is skipped; entry without "title" falls back to its id.
        Assert.Equal("vid3.mp4", result.Entries[1].Name);
        Assert.Equal("https://example.com/v/3", result.Entries[1].Url);
    }

    [Fact]
    public void Map_NullEntries_TreatedAsSingleItem_UsingRequestedLinkAsUrl()
    {
        var result = DownloadItemMapper.Map(ReadFixture("entries_null.json"), "https://requested.link/video");

        Assert.NotNull(result);
        var entry = Assert.Single(result.Entries);
        Assert.Equal("Single With Null Entries.mkv", entry.Name);
        Assert.Equal("https://requested.link/video", entry.Url);
    }

    [Fact]
    public void Map_MissingTitle_FallsBackToId()
    {
        var result = DownloadItemMapper.Map(ReadFixture("missing_title.json"), null);

        Assert.NotNull(result);
        Assert.Equal("xyz789", result.Name);
        var entry = Assert.Single(result.Entries);
        Assert.Equal("xyz789.mp4", entry.Name);
    }

    [Fact]
    public void Map_JsonNullLiteral_ReturnsNull()
    {
        Assert.Null(DownloadItemMapper.Map("null", null));
    }
}
