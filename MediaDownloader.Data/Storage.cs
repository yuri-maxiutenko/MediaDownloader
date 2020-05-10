using System;
using System.Collections.ObjectModel;
using System.Linq;

using MediaDownloader.Data.Models;

using Microsoft.EntityFrameworkCore;

namespace MediaDownloader.Data
{
    public class Storage
    {
        private readonly DataContext _context;

        public Storage()
        {
            _context = new DataContext();
            _context.Database.Migrate();
            _context.DownloadFolders.Load();
            _context.History.Load();
        }

        public ObservableCollection<DownloadFolder> DownloadFolders =>
            _context.DownloadFolders.Local.ToObservableCollection();

        public ObservableCollection<HistoryRecord> History => _context.History.Local.ToObservableCollection();

        public void AddOrUpdateDownloadFolder(string path, DateTime lastSelectionDate)
        {
            var entry = _context.DownloadFolders.FirstOrDefault(item =>
                item.Path == path);
            if (entry != null)
            {
                UpdateDownloadFolder(entry.DownloadFolderId, path, lastSelectionDate);
            }
            else
            {
                AddDownloadFolder(path, lastSelectionDate);
            }
        }

        public void AddDownloadFolder(string path, DateTime lastSelectionDate)
        {
            _context.DownloadFolders.Add(new DownloadFolder
            {
                Path = path,
                LastSelectionDate = lastSelectionDate
            });
            _context.SaveChanges();
        }

        public void UpdateDownloadFolder(int id, string path, DateTime lastSelectionDate)
        {
            var entry = _context.DownloadFolders.FirstOrDefault(item => item.DownloadFolderId == id);
            if (entry == null)
            {
                return;
            }

            entry.LastSelectionDate = lastSelectionDate;
            entry.Path = path;
            _context.SaveChanges();
        }

        public void AddHistoryRecord(string fileName, string path, string url, int downloadStatus, int downloadFormat)
        {
            _context.History.Add(new HistoryRecord
            {
                FileName = fileName,
                Path = path,
                Url = url,
                DownloadStatus = downloadStatus,
                DownloadFormat = downloadFormat,
                DownloadDate = DateTime.Now
            });
            _context.SaveChanges();
        }

        public void RemoveHistoryRecord(HistoryRecord record)
        {
            if (record == null)
            {
                return;
            }
            _context.History.Remove(record);
            _context.SaveChanges();
        }

        public void ClearHistory()
        {
            _context.Database.ExecuteSqlRaw("delete from History");
            _context.SaveChanges();
            _context.History.Load();
        }
    }
}