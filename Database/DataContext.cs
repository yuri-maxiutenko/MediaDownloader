using System;
using System.Configuration;

using Microsoft.EntityFrameworkCore;

using YoutubeDownloader.Database.Models;

namespace YoutubeDownloader.Database
{
    public class DataContext : DbContext
    {
        public DbSet<HistoryRecord> History { get; set; }
        public DbSet<DownloadFolder> DownloadFolders { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite(@"Data Source=.\data.db");
        }
    }
}