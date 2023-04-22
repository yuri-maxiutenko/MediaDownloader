using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using MediaDownloader.Models;
using MediaDownloader.Properties;

using Newtonsoft.Json;

using NLog;

namespace MediaDownloader;

public class Downloader
{
    private const int DownloadTimeoutSec = 60;
    private const int ProcessWaitTimeoutMs = 500;

    private readonly string _converterPath;
    private readonly string _downloaderPath;

    public Downloader(string downloaderPath, string converterPath)
    {
        _downloaderPath = downloaderPath;
        _converterPath = converterPath;
    }

    public Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    public bool TryGetItems(string link, string downloadOption, DataReceivedEventHandler onErrorReceived,
        CancellationToken cancelToken, out DownloadItem result)
    {
        result = new DownloadItem();

        var arguments = new StringBuilder();
        arguments.Append(Resources.DownloaderOptionEncodingUtf8);
        arguments.Append(' ');
        arguments.Append($"{Resources.DownloaderOptionSocketTimeout} {DownloadTimeoutSec}");
        arguments.Append(' ');
        arguments.Append($"-f \"{downloadOption}\"");
        arguments.Append(' ');
        arguments.Append("-J");
        arguments.Append(' ');
        arguments.Append(link);
        var downloaderProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _downloaderPath,
                Arguments = arguments.ToString(),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            }
        };

        try
        {
            var outputReader = new StringBuilder();

            downloaderProcess.ErrorDataReceived += onErrorReceived;

            downloaderProcess.OutputDataReceived += (sender, args) => { outputReader.Append(args.Data); };

            downloaderProcess.Start();

            downloaderProcess.BeginErrorReadLine();
            downloaderProcess.BeginOutputReadLine();

            while (!downloaderProcess.HasExited)
            {
                downloaderProcess.WaitForExit(ProcessWaitTimeoutMs);
                if (!cancelToken.IsCancellationRequested)
                {
                    continue;
                }

                downloaderProcess.Kill();
                downloaderProcess.WaitForExit();
                cancelToken.ThrowIfCancellationRequested();
            }

            var info = JsonConvert.DeserializeObject<DownloadItemJson>(outputReader.ToString());

            result.Name = Utilities.SanitizeFileName(info.Title);
            if (info.Entries != null)
            {
                result.Entries = info.Entries.Select(item => new DownloadItem
                {
                    Name = Path.ChangeExtension(Utilities.SanitizeFileName(item.Title), item.Ext),
                    Url = item.WebpageUrl
                }).ToList();
            }
            else
            {
                result.Entries = new List<DownloadItem>
                {
                    new()
                    {
                        Name = Path.ChangeExtension(Utilities.SanitizeFileName(info.Title), info.Ext),
                        Url = info.WebpageUrl
                    }
                };
            }

            return downloaderProcess.ExitCode == 0;
        }
        finally
        {
            downloaderProcess.ErrorDataReceived -= onErrorReceived;
        }
    }

    public bool TryDownloadItem(string downloadFilePath, string link, string downloadOption,
        DataReceivedEventHandler onOutputReceived, DataReceivedEventHandler onErrorReceived,
        CancellationToken cancelToken)
    {
        var arguments = new StringBuilder();
        arguments.Append(Resources.DownloaderOptionEncodingUtf8);
        arguments.Append(' ');
        arguments.Append($"{Resources.DownloaderOptionSocketTimeout} {DownloadTimeoutSec}");
        arguments.Append(' ');
        arguments.Append(Resources.DownloaderOptionNoOriginalDateTime);
        arguments.Append(' ');
        arguments.Append(Resources.DownloaderOptionNoPlaylist);
        arguments.Append(' ');
        arguments.Append($"-f \"{downloadOption}\"");
        arguments.Append(' ');
        arguments.Append($"-o \"{downloadFilePath}\"");
        arguments.Append(' ');
        arguments.Append($"{Resources.DownloaderOptionConverterLocation} \"{_converterPath}\"");
        arguments.Append(' ');
        arguments.Append(link);
        var downloaderProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _downloaderPath,
                Arguments = arguments.ToString(),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            }
        };

        try
        {
            downloaderProcess.OutputDataReceived += onOutputReceived;
            downloaderProcess.ErrorDataReceived += onErrorReceived;

            downloaderProcess.Start();

            downloaderProcess.BeginErrorReadLine();
            downloaderProcess.BeginOutputReadLine();

            while (!downloaderProcess.HasExited)
            {
                downloaderProcess.WaitForExit(ProcessWaitTimeoutMs);
                if (cancelToken.IsCancellationRequested)
                {
                    downloaderProcess.Kill();
                    downloaderProcess.WaitForExit();
                    cancelToken.ThrowIfCancellationRequested();
                }
            }

            return downloaderProcess.ExitCode == 0;
        }
        finally
        {
            downloaderProcess.OutputDataReceived -= onOutputReceived;
            downloaderProcess.ErrorDataReceived -= onErrorReceived;
        }
    }
}