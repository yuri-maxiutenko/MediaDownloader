using MediaDownloader.Data;

using Microsoft.Data.Sqlite;

namespace MediaDownloader.Tests;

public sealed class StorageTests : IDisposable
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"media-downloader-test-{Guid.NewGuid():N}.db");

    private Storage CreateStorage() => new($"Data Source={_dbPath}");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void NewDatabase_MigratesAndStartsEmpty()
    {
        var storage = CreateStorage();

        Assert.Empty(storage.History);
        Assert.Empty(storage.DownloadFolders);
    }

    [Fact]
    public void AddDownloadFolder_EvictsOldest_WhenAtCapacity()
    {
        var storage = CreateStorage();
        var baseDate = new DateTime(2026, 1, 1);

        for (var i = 0; i < 11; i++)
        {
            storage.AddDownloadFolder($@"C:\folders\{i}", baseDate.AddDays(i));
        }

        Assert.Equal(10, storage.DownloadFolders.Count);
        Assert.DoesNotContain(storage.DownloadFolders, folder => folder.Path == @"C:\folders\0");
        Assert.Contains(storage.DownloadFolders, folder => folder.Path == @"C:\folders\10");
    }

    [Fact]
    public void AddOrUpdateDownloadFolder_UpdatesExistingEntry()
    {
        var storage = CreateStorage();
        var firstDate = new DateTime(2026, 1, 1);
        var secondDate = new DateTime(2026, 2, 2);

        storage.AddOrUpdateDownloadFolder(@"C:\folders\same", firstDate);
        storage.AddOrUpdateDownloadFolder(@"C:\folders\same", secondDate);

        var folder = Assert.Single(storage.DownloadFolders);
        Assert.Equal(secondDate, folder.LastSelectionDate);
    }

    [Fact]
    public void AddOrUpdateHistoryRecord_MatchesUrlCaseInsensitively()
    {
        var storage = CreateStorage();

        storage.AddOrUpdateHistoryRecord("a.mp4", @"C:\out\a.mp4", "https://EXAMPLE.com/Video", 0, 0);
        storage.AddOrUpdateHistoryRecord("b.mp4", @"C:\out\b.mp4", "https://example.com/video", 1, 2);

        var record = Assert.Single(storage.History);
        Assert.Equal("b.mp4", record.FileName);
        Assert.Equal(1, record.DownloadStatus);
        Assert.Equal(2, record.DownloadFormat);
    }

    [Fact]
    public void AddHistoryRecord_CapsHistoryAtTwenty()
    {
        var storage = CreateStorage();

        for (var i = 0; i < 25; i++)
        {
            storage.AddHistoryRecord($"file{i}.mp4", $@"C:\out\file{i}.mp4", $"https://example.com/{i}", 0, 0);
        }

        Assert.Equal(20, storage.History.Count);
    }

    [Fact]
    public void RemoveHistoryRecord_RemovesEntry()
    {
        var storage = CreateStorage();
        storage.AddHistoryRecord("file.mp4", @"C:\out\file.mp4", "https://example.com/1", 0, 0);

        storage.RemoveHistoryRecord(storage.History.Single());

        Assert.Empty(storage.History);
    }

    [Fact]
    public void ClearHistory_RemovesAllEntries()
    {
        var storage = CreateStorage();
        storage.AddHistoryRecord("a.mp4", @"C:\out\a.mp4", "https://example.com/1", 0, 0);
        storage.AddHistoryRecord("b.mp4", @"C:\out\b.mp4", "https://example.com/2", 0, 0);

        storage.ClearHistory();

        Assert.Empty(storage.History);
    }

    [Fact]
    public void ExistingData_SurvivesReopen()
    {
        var storage = CreateStorage();
        storage.AddHistoryRecord("file.mp4", @"C:\out\file.mp4", "https://example.com/1", 0, 0);
        SqliteConnection.ClearAllPools();

        var reopened = CreateStorage();

        var record = Assert.Single(reopened.History);
        Assert.Equal("file.mp4", record.FileName);
    }
}
