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

    public void AddOrUpdate(string fileName, string path, string url, int downloadStatus, int downloadFormat)
    {
        _storage.AddOrUpdateHistoryRecord(fileName, path, url, downloadStatus, downloadFormat);
        HistoryView.View.Refresh();
    }

    public void Remove(HistoryRecord record)
    {
        _storage.RemoveHistoryRecord(record);
        HistoryView.View.Refresh();
    }

    public void Clear()
    {
        _storage.ClearHistory();
        HistoryView.View.Refresh();
    }
}
