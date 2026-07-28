using System.ComponentModel;
using System.Windows.Data;

using MediaDownloader.Data;
using MediaDownloader.Data.Models;

namespace MediaDownloader.Services;

public sealed class HistoryService : IHistoryService
{
    private readonly Storage _storage;

    public HistoryService(Storage storage)
    {
        _storage = storage;

        HistoryView = new CollectionViewSource
        {
            Source = storage.History
        };
        HistoryView.SortDescriptions.Add(
            new SortDescription(nameof(HistoryRecord.DownloadDate), ListSortDirection.Descending));
        HistoryView.SortDescriptions.Add(
            new SortDescription(nameof(HistoryRecord.FileName), ListSortDirection.Ascending));
    }

    public CollectionViewSource HistoryView { get; }

    public async Task AddOrUpdateAsync(string fileName, string path, string url, int downloadStatus,
        int downloadFormat)
    {
        await _storage.AddOrUpdateHistoryRecordAsync(fileName, path, url, downloadStatus, downloadFormat);
        HistoryView.View.Refresh();
    }

    public async Task RemoveAsync(HistoryRecord record)
    {
        await _storage.RemoveHistoryRecordAsync(record);
        HistoryView.View.Refresh();
    }

    public async Task ClearAsync()
    {
        await _storage.ClearHistoryAsync();
        HistoryView.View.Refresh();
    }
}
