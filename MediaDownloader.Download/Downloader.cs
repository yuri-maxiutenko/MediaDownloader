using System.Diagnostics;
using System.Text;

using MediaDownloader.Download.Models;
using MediaDownloader.Download.Properties;
using MediaDownloader.Download.Utilities;

using Newtonsoft.Json;

namespace MediaDownloader.Download;

public class Downloader : IDownloader
{
    private const int DownloadTimeoutSec = 60;

    private readonly string _converterPath;

    private readonly Dictionary<DownloadFormatType, string> _downloadFormats = new()
    {
        {
            DownloadFormatType.Best, Resources.DownloaderOptionFormatBest
        },
        {
            DownloadFormatType.BestMp4, Resources.DownloaderOptionFormatBestMp4
        },
        {
            DownloadFormatType.BestDirectLink, Resources.DownloaderOptionFormatBestDirectLink
        },
        {
            DownloadFormatType.AudioOnly, Resources.DownloaderOptionFormatAudioOnly
        }
    };

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

    public async Task<DownloadItem> GetItemsAsync(string link, DownloadFormatType downloadFormatType,
        DataReceivedEventHandler onErrorDataReceived, CancellationToken cancellationToken)
    {
        var arguments = new StringBuilder();
        arguments.Append(Resources.DownloaderOptionEncodingUtf8);
        arguments.Append(' ');
        arguments.Append($"{Resources.DownloaderOptionSocketTimeout} {DownloadTimeoutSec}");
        arguments.Append(' ');
        arguments.Append($"-f \"{_downloadFormats[downloadFormatType]}\"");
        arguments.Append(' ');
        arguments.Append("-J");
        arguments.Append(' ');
        arguments.Append(link);
        _processStartInfo.Arguments = arguments.ToString();
        var downloaderProcess = new Process
        {
            StartInfo = _processStartInfo
        };

        var outputReader = new StringBuilder();

        void OnOutputDataReceived(object _, DataReceivedEventArgs args)
        {
            outputReader.Append(args.Data);
        }

        try
        {
            downloaderProcess.ErrorDataReceived += onErrorDataReceived;
            downloaderProcess.OutputDataReceived += OnOutputDataReceived;

            downloaderProcess.Start();

            downloaderProcess.BeginErrorReadLine();
            downloaderProcess.BeginOutputReadLine();

            await downloaderProcess.WaitForExitAsync(cancellationToken);

            if (downloaderProcess.ExitCode != 0)
            {
                return null;
            }

            var info = JsonConvert.DeserializeObject<DownloadItemJson>(outputReader.ToString());

            var result = new DownloadItem
            {
                Name = DownloadHelper.SanitizeFileName(info.Title),
                Entries = new List<DownloadItem>()
            };

            if (info.Entries is not null)
            {
                result.Entries.AddRange(info.Entries.Select(item => new DownloadItem
                {
                    Name = Path.ChangeExtension(DownloadHelper.SanitizeFileName(item.Title), item.Ext),
                    Url = item.WebpageUrl
                }));
            }
            else
            {
                result.Entries.Add(new DownloadItem
                {
                    Name = Path.ChangeExtension(DownloadHelper.SanitizeFileName(info.Title), info.Ext),
                    Url = info.WebpageUrl
                });
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            if (!downloaderProcess.HasExited)
            {
                downloaderProcess.Kill();
            }

            throw;
        }
        finally
        {
            downloaderProcess.ErrorDataReceived -= onErrorDataReceived;
            downloaderProcess.OutputDataReceived -= OnOutputDataReceived;
        }
    }

    public async Task<bool> DownloadItemAsync(string downloadFilePath, string link,
        DownloadFormatType downloadFormatType, DataReceivedEventHandler onOutputReceived,
        DataReceivedEventHandler onErrorReceived, CancellationToken cancellationToken)
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
        arguments.Append($"-f \"{_downloadFormats[downloadFormatType]}\"");
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

        return await ExecuteDownloaderAsync(downloaderProcess, onOutputReceived, onErrorReceived, cancellationToken);
    }

    public async Task<bool> UpdateAsync(DataReceivedEventHandler onOutputReceived,
        DataReceivedEventHandler onErrorReceived, CancellationToken cancellationToken)
    {
        _processStartInfo.Arguments = Resources.DownloaderOptionUpdate;
        var downloaderProcess = new Process
        {
            StartInfo = _processStartInfo
        };

        return await ExecuteDownloaderAsync(downloaderProcess, onOutputReceived, onErrorReceived, cancellationToken);
    }

    private static async Task<bool> ExecuteDownloaderAsync(Process downloaderProcess,
        DataReceivedEventHandler onOutputReceived, DataReceivedEventHandler onErrorReceived,
        CancellationToken cancellationToken)
    {
        try
        {
            downloaderProcess.OutputDataReceived += onOutputReceived;
            downloaderProcess.ErrorDataReceived += onErrorReceived;

            downloaderProcess.Start();

            downloaderProcess.BeginErrorReadLine();
            downloaderProcess.BeginOutputReadLine();

            await downloaderProcess.WaitForExitAsync(cancellationToken);

            return downloaderProcess.ExitCode == 0;
        }
        catch (OperationCanceledException)
        {
            if (!downloaderProcess.HasExited)
            {
                downloaderProcess.Kill();
            }

            throw;
        }
        finally
        {
            downloaderProcess.OutputDataReceived -= onOutputReceived;
            downloaderProcess.ErrorDataReceived -= onErrorReceived;
        }
    }
}