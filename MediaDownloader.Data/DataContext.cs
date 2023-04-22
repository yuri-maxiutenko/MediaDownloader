using MediaDownloader.Data.Models;

using Microsoft.EntityFrameworkCore;

namespace MediaDownloader.Data;

public class DataContext : DbContext
{
    public DbSet<HistoryRecord> History { get; set; }
    public DbSet<DownloadFolder> DownloadFolders { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=Data.db");
    }
}