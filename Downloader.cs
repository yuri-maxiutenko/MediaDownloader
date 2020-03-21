using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using YoutubeDownloader.Properties;

namespace YoutubeDownloader
{
    public class Downloader
    {
        private const int ProcessWaitTimeoutMs = 500;
        private readonly string _converterPath;

        private readonly string _downloaderPath;

        public Downloader()
        {
            _downloaderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Resources.DownloaderFileName);
            _converterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Resources.ConverterDirectoryName,
                Resources.BinDirectoryName);
        }

        public bool TryGetItemFilePath(string downloadFolderPath, string link, string downloadOption,
            DataReceivedEventHandler onErrorReceived, CancellationToken cancelToken, out string filePath)
        {
            var arguments = new StringBuilder();
            arguments.Append(Resources.DownloaderEncodingUtf8Option);
            arguments.Append(" ");
            arguments.Append($"-f \"{downloadOption}\"");
            arguments.Append(" ");
            arguments.Append(Resources.DownloaderGetFilenameOption);
            arguments.Append(" ");
            arguments.Append($"-o \"{downloadFolderPath}\\{Resources.DownloaderItemTitleTemplate}\"");
            arguments.Append(" ");
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
                downloaderProcess.ErrorDataReceived += onErrorReceived;

                downloaderProcess.Start();

                downloaderProcess.BeginErrorReadLine();

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

                filePath = downloaderProcess.ExitCode == 0
                    ? downloaderProcess.StandardOutput.ReadToEnd().Trim()
                    : string.Empty;

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
            arguments.Append(Resources.DownloaderEncodingUtf8Option);
            arguments.Append(" ");
            arguments.Append($"-f \"{downloadOption}\"");
            arguments.Append(" ");
            arguments.Append($"-o \"{downloadFilePath}\"");
            arguments.Append(" ");
            arguments.Append($"{Resources.DownloaderConverterLocationOption} \"{_converterPath}\"");
            arguments.Append(" ");
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
}