using System;
using System.Collections.ObjectModel;
using System.Linq;

using MediaDownloader.Data.Models;

using Microsoft.EntityFrameworkCore;

namespace MediaDownloader.Data
{
    public class Storage
    {
        private const int HistoryRecordsMax = 20;
        private const int DownloadFoldersMax = 10;

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
            if (_context.DownloadFolders.Count() >= DownloadFoldersMax)
            {
                var oldestEntry = _context.DownloadFolders.OrderBy(item => item.LastSelectionDate)
                    .FirstOrDefault();
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
            if (_context.History.Count() >= HistoryRecordsMax)
            {
                var oldestEntry = _context.History.OrderBy(item => item.DownloadDate)
                    .FirstOrDefault();
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

            _context.SaveChanges();
        }

        public void AddOrUpdateHistoryRecord(string fileName, string path, string url, int downloadStatus,
            int downloadFormat)
        {
            var entry = _context.History.FirstOrDefault(item =>
                item.Url.ToLower() == url.ToLower());
            if (entry == null)
            {
                AddHistoryRecord(fileName, path, url, downloadStatus, downloadFormat);
            }
            else
            {
                entry.FileName = fileName;
                entry.Path = path;
                entry.Url = url;
                entry.DownloadStatus = downloadStatus;
                entry.DownloadFormat = downloadFormat;
                entry.DownloadDate = DateTime.Now;
            }

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
            foreach (var item in _context.History)
            {
                _context.History.Remove(item);
            }
            _context.SaveChanges();
        }
    }
}