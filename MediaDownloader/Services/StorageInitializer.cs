using MediaDownloader.Data;

using Microsoft.Extensions.Hosting;

namespace MediaDownloader.Services;

/// <summary>Migrates and loads the database before any window is shown.</summary>
public sealed class StorageInitializer : IHostedService
{
    private readonly Storage _storage;

    public StorageInitializer(Storage storage)
    {
        _storage = storage;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _storage.InitializeAsync(cancellationToken);

        if (!_storage.DownloadFolders.Any())
        {
            await _storage.AddDownloadFolderAsync(
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), DateTime.Now);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
