using System.ComponentModel;
using System.Windows.Data;

using MediaDownloader.Data;
using MediaDownloader.Data.Models;

namespace MediaDownloader.Services;

public sealed class DownloadFolderService : IDownloadFolderService
{
    private readonly Storage _storage;

    public DownloadFolderService(Storage storage)
    {
        _storage = storage;

        FoldersView = new CollectionViewSource
        {
            Source = storage.DownloadFolders
        };
        FoldersView.SortDescriptions.Add(
            new SortDescription(nameof(DownloadFolder.LastSelectionDate), ListSortDirection.Descending));
        MoveCurrentToFirst();
    }

    public CollectionViewSource FoldersView { get; }

    public async Task AddOrUpdateAsync(string path, DateTime lastSelectionDate)
    {
        await _storage.AddOrUpdateDownloadFolderAsync(path, lastSelectionDate);
        RefreshAndMoveCurrentToFirst();
    }

    public async Task TouchAsync(DownloadFolder folder, DateTime lastSelectionDate)
    {
        await _storage.UpdateDownloadFolderAsync(folder.DownloadFolderId, folder.Path, lastSelectionDate);
        RefreshAndMoveCurrentToFirst();
    }

    private void RefreshAndMoveCurrentToFirst()
    {
        FoldersView.View.Refresh();
        MoveCurrentToFirst();
    }

    private void MoveCurrentToFirst()
    {
        var firstItem = (FoldersView.View as CollectionView)?.GetItemAt(0);
        FoldersView.View.MoveCurrentTo(firstItem);
    }
}
