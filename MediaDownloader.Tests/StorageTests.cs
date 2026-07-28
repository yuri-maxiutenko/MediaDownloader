using MediaDownloader.Data;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MediaDownloader.Tests;

public sealed class StorageTests : IDisposable
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"media-downloader-test-{Guid.NewGuid():N}.db");

    private async Task<Storage> CreateStorageAsync()
    {
        var storage = new Storage(
            new DbContextOptionsBuilder<DataContext>().UseSqlite($"Data Source={_dbPath}").Options);
        await storage.InitializeAsync();
        return storage;
    }

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
    public async Task NewDatabase_MigratesAndStartsEmpty()
    {
        using var storage = await CreateStorageAsync();

        Assert.Empty(storage.History);
        Assert.Empty(storage.DownloadFolders);
    }

    [Fact]
    public async Task AddDownloadFolder_EvictsOldest_WhenAtCapacity()
    {
        using var storage = await CreateStorageAsync();
        var baseDate = new DateTime(2026, 1, 1);

        for (var i = 0; i < 11; i++)
        {
            await storage.AddDownloadFolderAsync($@"C:\folders\{i}", baseDate.AddDays(i));
        }

        Assert.Equal(10, storage.DownloadFolders.Count);
        Assert.DoesNotContain(storage.DownloadFolders, folder => folder.Path == @"C:\folders\0");
        Assert.Contains(storage.DownloadFolders, folder => folder.Path == @"C:\folders\10");
    }

    [Fact]
    public async Task AddOrUpdateDownloadFolder_UpdatesExistingEntry()
    {
        using var storage = await CreateStorageAsync();
        var firstDate = new DateTime(2026, 1, 1);
        var secondDate = new DateTime(2026, 2, 2);

        await storage.AddOrUpdateDownloadFolderAsync(@"C:\folders\same", firstDate);
        await storage.AddOrUpdateDownloadFolderAsync(@"C:\folders\same", secondDate);

        var folder = Assert.Single(storage.DownloadFolders);
        Assert.Equal(secondDate, folder.LastSelectionDate);
    }

    [Fact]
    public async Task AddOrUpdateHistoryRecord_MatchesUrlCaseInsensitively()
    {
        using var storage = await CreateStorageAsync();

        await storage.AddOrUpdateHistoryRecordAsync("a.mp4", @"C:\out\a.mp4", "https://EXAMPLE.com/Video", 0, 0);
        await storage.AddOrUpdateHistoryRecordAsync("b.mp4", @"C:\out\b.mp4", "https://example.com/video", 1, 2);

        var record = Assert.Single(storage.History);
        Assert.Equal("b.mp4", record.FileName);
        Assert.Equal(1, record.DownloadStatus);
        Assert.Equal(2, record.DownloadFormat);
    }

    [Fact]
    public async Task AddHistoryRecord_CapsHistoryAtTwenty()
    {
        using var storage = await CreateStorageAsync();

        for (var i = 0; i < 25; i++)
        {
            await storage.AddHistoryRecordAsync($"file{i}.mp4", $@"C:\out\file{i}.mp4", $"https://example.com/{i}",
                0, 0);
        }

        Assert.Equal(20, storage.History.Count);
    }

    [Fact]
    public async Task RemoveHistoryRecord_RemovesEntry()
    {
        using var storage = await CreateStorageAsync();
        await storage.AddHistoryRecordAsync("file.mp4", @"C:\out\file.mp4", "https://example.com/1", 0, 0);

        await storage.RemoveHistoryRecordAsync(storage.History.Single());

        Assert.Empty(storage.History);
    }

    [Fact]
    public async Task ClearHistory_RemovesAllEntries()
    {
        using var storage = await CreateStorageAsync();
        await storage.AddHistoryRecordAsync("a.mp4", @"C:\out\a.mp4", "https://example.com/1", 0, 0);
        await storage.AddHistoryRecordAsync("b.mp4", @"C:\out\b.mp4", "https://example.com/2", 0, 0);

        await storage.ClearHistoryAsync();

        Assert.Empty(storage.History);
    }

    [Fact]
    public async Task ExistingData_SurvivesReopen()
    {
        using (var storage = await CreateStorageAsync())
        {
            await storage.AddHistoryRecordAsync("file.mp4", @"C:\out\file.mp4", "https://example.com/1", 0, 0);
        }

        SqliteConnection.ClearAllPools();

        using var reopened = await CreateStorageAsync();

        var record = Assert.Single(reopened.History);
        Assert.Equal("file.mp4", record.FileName);
    }
}
