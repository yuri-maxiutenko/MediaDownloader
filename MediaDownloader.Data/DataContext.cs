using System.Linq;

using MediaDownloader.Data.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MediaDownloader.Data;

public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        var connectionString = args?.FirstOrDefault();
        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
        optionsBuilder.UseSqlite(connectionString);
        return new DataContext(optionsBuilder.Options);
    }
}

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> optionsBuilderOptions) : base(optionsBuilderOptions)
    {
    }

    public DbSet<HistoryRecord> History { get; set; }
    public DbSet<DownloadFolder> DownloadFolders { get; set; }
}