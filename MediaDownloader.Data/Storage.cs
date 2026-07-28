using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediaDownloader.Data.Models;

using Microsoft.EntityFrameworkCore;

namespace MediaDownloader.Data;

public sealed class Storage : IDisposable
{
    private const int HistoryRecordsMax = 20;
    private const int DownloadFoldersMax = 10;

    private readonly DataContext _context;

    public Storage(DbContextOptions<DataContext> options)
    {
        _context = new DataContext(options);
    }

    public ObservableCollection<DownloadFolder> DownloadFolders =>
        _context.DownloadFolders.Local.ToObservableCollection();

    public ObservableCollection<HistoryRecord> History => _context.History.Local.ToObservableCollection();

    public void Dispose()
    {
        _context.Dispose();
    }

    /// <summary>Applies pending migrations and loads both tables into the local views.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken);
        await _context.DownloadFolders.LoadAsync(cancellationToken);
        await _context.History.LoadAsync(cancellationToken);
    }

    public async Task AddOrUpdateDownloadFolderAsync(string path, DateTime lastSelectionDate)
    {
        var entry = _context.DownloadFolders.Local.FirstOrDefault(item => item.Path == path);
        if (entry != null)
        {
            await UpdateDownloadFolderAsync(entry.DownloadFolderId, path, lastSelectionDate);
        }
        else
        {
            await AddDownloadFolderAsync(path, lastSelectionDate);
        }
    }

    public async Task AddDownloadFolderAsync(string path, DateTime lastSelectionDate)
    {
        if (_context.DownloadFolders.Local.Count >= DownloadFoldersMax)
        {
            var oldestEntry = _context.DownloadFolders.Local.OrderBy(item => item.LastSelectionDate).FirstOrDefault();
            if (oldestEntry != null)
            {
                _context.DownloadFolders.Remove(oldestEntry);
            }
        }

        _context.DownloadFolders.Add(new DownloadFolder
        {
            Path = path,
            LastSelectionDate = lastSelectionDate
        });
        await _context.SaveChangesAsync();
    }

    public async Task UpdateDownloadFolderAsync(int id, string path, DateTime lastSelectionDate)
    {
        var entry = _context.DownloadFolders.Local.FirstOrDefault(item => item.DownloadFolderId == id);
        if (entry == null)
        {
            return;
        }

        entry.LastSelectionDate = lastSelectionDate;
        entry.Path = path;
        await _context.SaveChangesAsync();
    }

    public async Task AddHistoryRecordAsync(
        string fileName,
        string path,
        string url,
        int downloadStatus,
        int downloadFormat)
    {
        if (_context.History.Local.Count >= HistoryRecordsMax)
        {
            var oldestEntry = _context.History.Local.OrderBy(item => item.DownloadDate).FirstOrDefault();
            if (oldestEntry != null)
            {
                _context.History.Remove(oldestEntry);
            }
        }

        _context.History.Add(new HistoryRecord
        {
            FileName = fileName,
            Path = path,
            Url = url,
            DownloadStatus = downloadStatus,
            DownloadFormat = downloadFormat,
            DownloadDate = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }

    public async Task AddOrUpdateHistoryRecordAsync(
        string fileName,
        string path,
        string url,
        int downloadStatus,
        int downloadFormat)
    {
        var entry = _context.History.Local.FirstOrDefault(
            item => string.Equals(item.Url, url, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
        {
            await AddHistoryRecordAsync(fileName, path, url, downloadStatus, downloadFormat);
        }
        else
        {
            entry.FileName = fileName;
            entry.Path = path;
            entry.Url = url;
            entry.DownloadStatus = downloadStatus;
            entry.DownloadFormat = downloadFormat;
            entry.DownloadDate = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveHistoryRecordAsync(HistoryRecord? record)
    {
        if (record is null)
        {
            return;
        }

        _context.History.Remove(record);
        await _context.SaveChangesAsync();
    }

    public async Task ClearHistoryAsync()
    {
        _context.History.RemoveRange(_context.History.Local.ToList());
        await _context.SaveChangesAsync();
    }
}
