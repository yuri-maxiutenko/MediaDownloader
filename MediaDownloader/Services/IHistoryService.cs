using System.Windows.Data;

using MediaDownloader.Data.Models;

namespace MediaDownloader.Services;

public interface IHistoryService
{
    CollectionViewSource HistoryView { get; }

    void AddOrUpdate(string fileName, string path, string url, int downloadStatus, int downloadFormat);

    void Remove(HistoryRecord record);

    void Clear();
}
