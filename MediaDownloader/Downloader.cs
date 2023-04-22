using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using MediaDownloader.Models;
using MediaDownloader.Properties;

using Newtonsoft.Json;

namespace MediaDownloader;

public class Downloader
{
    private const int DownloadTimeoutSec = 60;
    private const int ProcessWaitTimeoutMs = 500;

    private readonly string _converterPath;
    private readonly ProcessStartInfo _processStartInfo;

    public Downloader(string downloaderPath, string converterPath)
    {
        _converterPath = converterPath;
        _processStartInfo = new ProcessStartInfo
        {
            FileName = downloaderPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
    }

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
        _processStartInfo.Arguments = arguments.ToString();
        var downloaderProcess = new Process
        {
            StartInfo = _processStartInfo
        };

        try
        {
            var outputReader = new StringBuilder();

            downloaderProcess.ErrorDataReceived += onErrorReceived;

            downloaderProcess.OutputDataReceived += (_, args) => { outputReader.Append(args.Data); };

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
        _processStartInfo.Arguments = arguments.ToString();
        var downloaderProcess = new Process
        {
            StartInfo = _processStartInfo
        };

        return ExecuteDownloader(downloaderProcess, onOutputReceived, onErrorReceived, cancelToken);
    }

    private static bool ExecuteDownloader(Process downloaderProcess, DataReceivedEventHandler onOutputReceived,
        DataReceivedEventHandler onErrorReceived, CancellationToken cancelToken)
    {
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
                if (!cancelToken.IsCancellationRequested)
                {
                    continue;
                }

                downloaderProcess.Kill();
                downloaderProcess.WaitForExit();
                cancelToken.ThrowIfCancellationRequested();
            }

            return downloaderProcess.ExitCode == 0;
        }
        finally
        {
            downloaderProcess.OutputDataReceived -= onOutputReceived;
            downloaderProcess.ErrorDataReceived -= onErrorReceived;
        }
    }

    public bool Update(DataReceivedEventHandler onOutputReceived, DataReceivedEventHandler onErrorReceived,
        CancellationToken cancelToken)
    {
        _processStartInfo.Arguments = Resources.DownloaderOptionUpdate;
        var downloaderProcess = new Process
        {
            StartInfo = _processStartInfo
        };

        return ExecuteDownloader(downloaderProcess, onOutputReceived, onErrorReceived, cancelToken);
    }
}