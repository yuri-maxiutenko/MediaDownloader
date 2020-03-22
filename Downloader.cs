using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Newtonsoft.Json;

using YoutubeDownloader.Properties;

namespace YoutubeDownloader
{
    public class DownloaderItemInfo
    {
        public string FileName { get; set; }
        public string Link { get; set; }
    }

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

        public bool TryGetItems(string downloadFolderPath, string link, string downloadOption,
            DataReceivedEventHandler onErrorReceived, CancellationToken cancelToken,
            out List<DownloaderItemInfo> items)
        {
            items = new List<DownloaderItemInfo>();

            var arguments = new StringBuilder();
            arguments.Append(Resources.DownloaderEncodingUtf8Option);
            arguments.Append(" ");
            arguments.Append($"-f \"{downloadOption}\"");
            arguments.Append(" ");
            arguments.Append("-J");
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
                var outputReader = new StringBuilder();

                downloaderProcess.ErrorDataReceived += onErrorReceived;

                downloaderProcess.OutputDataReceived += (sender, args) =>
                {
                    outputReader.Append(args.Data);
                };

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

                var info = JsonConvert.DeserializeObject<ItemInfo>(outputReader.ToString());

                if (info.entries != null)
                {
                    items.AddRange(info.entries.Select(item => new DownloaderItemInfo
                    {
                        FileName = Path.ChangeExtension(Utilities.SanitizeFileName(item.title), item.ext),
                        Link = item.webpage_url
                    }));
                }
                else
                {
                    items.Add(new DownloaderItemInfo
                    {
                        FileName = Path.ChangeExtension(Utilities.SanitizeFileName(info.title), info.ext),
                        Link = info.webpage_url
                    });
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

        private class ItemInfo
        {
            public string id { get; set; }
            public string ext { get; set; }
            public string title { get; set; }
            public string webpage_url { get; set; }
            public ItemInfo[] entries { get; set; }
            public ItemFormat[] requested_formats { get; set; }
        }

        private class ItemFormat
        {
            public string format { get; set; }
            public string ext { get; set; }
            public string vcodec { get; set; }
            public string acodec { get; set; }
        }
    }
}