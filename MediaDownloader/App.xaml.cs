using System.Windows;

using MediaDownloader.Data;
using MediaDownloader.Download;
using MediaDownloader.Services;
using MediaDownloader.UI.ViewModels;
using MediaDownloader.UI.Views;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using AppResources = MediaDownloader.Properties.Resources;
using Log = Serilog.Log;

namespace MediaDownloader;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private readonly IHost _host;

    public App()
    {
        var pathProvider = new UserDataPathProvider();
        LogConfigurator.SetupLogs(pathProvider.UserDataFolderPath);

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            // appsettings.json must resolve against the install directory, not the CWD.
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Services.AddSingleton<IUserDataPathProvider>(pathProvider);

        builder.Services.AddOptions<DownloaderOptions>()
            .Bind(builder.Configuration)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.DownloaderPath) &&
                           !string.IsNullOrWhiteSpace(options.ConverterPath),
                "appsettings.json must define DownloaderPath and ConverterPath")
            .ValidateOnStart();

        builder.Services.AddSingleton(Log.Logger);
        builder.Services.AddSingleton<IDownloader>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<DownloaderOptions>>().Value;
            return new Downloader(options.DownloaderPath, options.ConverterPath);
        });
        builder.Services.AddSingleton<IDownloadManager, DownloadManager>();

        builder.Services.AddSingleton(provider =>
        {
            var paths = provider.GetRequiredService<IUserDataPathProvider>();
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>()
                .UseSqlite($"Data Source={paths.DatabaseFilePath}");
            return new Storage(optionsBuilder.Options);
        });

        builder.Services.AddSingleton<IHistoryService, HistoryService>();
        builder.Services.AddSingleton<IDownloadFolderService, DownloadFolderService>();
        builder.Services.AddSingleton<IShellService, ShellService>();
        builder.Services.AddSingleton<IClipboardService, ClipboardService>();

        builder.Services.AddSingleton<MainWindowViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        _host = builder.Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            await _host.StartAsync();
            _host.Services.GetRequiredService<MainWindow>().Show();
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Application failed to start");
            MessageBox.Show(exception.Message, AppResources.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
        Log.CloseAndFlush();

        base.OnExit(e);
    }
}
