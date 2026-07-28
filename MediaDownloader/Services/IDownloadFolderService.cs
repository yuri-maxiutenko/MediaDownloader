using System.Windows.Data;

using MediaDownloader.Data.Models;

namespace MediaDownloader.Services;

public interface IDownloadFolderService
{
    CollectionViewSource FoldersView { get; }

    Task AddOrUpdateAsync(string path, DateTime lastSelectionDate);

    Task TouchAsync(DownloadFolder folder, DateTime lastSelectionDate);
}
