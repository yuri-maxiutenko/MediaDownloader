using System.IO;
using System.Reflection;

using MediaDownloader.Properties;

using Serilog;
using Serilog.Exceptions;

namespace MediaDownloader;

public static class LogConfigurator
{
    public static void SetupLogs(string userDataFolderPath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName();

        var logDirectoryPath = Path.Combine(userDataFolderPath, Resources.LogFolderName);

        const string outputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}]<{ThreadId}> {Message:lj}{NewLine}{Exception}";

        var logFileName = $"{Resources.AppName}-.log";
        var logFilePath = Path.Combine(logDirectoryPath, logFileName);

        Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Logger(lc => lc
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, outputTemplate: outputTemplate).Enrich
            .WithThreadId().Enrich.WithExceptionDetails().Enrich.FromLogContext()).CreateLogger();

        Log.Information("{AppName} v{AppVersion} successfully started", Resources.AppName,
            assemblyName.Version?.ToString(3));

        Log.Information("Data directory: {Path}", userDataFolderPath);
        Log.Information("Log directory: {Path}", logDirectoryPath);
    }
}