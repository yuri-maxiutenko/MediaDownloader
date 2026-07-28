using System.Windows.Data;

using MediaDownloader.Data.Models;

namespace MediaDownloader.Services;

public interface IHistoryService
{
    CollectionViewSource HistoryView { get; }

    Task AddOrUpdateAsync(string fileName, string path, string url, int downloadStatus, int downloadFormat);

    Task RemoveAsync(HistoryRecord record);

    Task ClearAsync();
}
