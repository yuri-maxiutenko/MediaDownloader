using System.Windows.Data;

using MediaDownloader.Data.Models;

namespace MediaDownloader.Services;

public interface IDownloadFolderService
{
    CollectionViewSource FoldersView { get; }

    void AddOrUpdate(string path, DateTime lastSelectionDate);

    void Touch(DownloadFolder folder, DateTime lastSelectionDate);
}
