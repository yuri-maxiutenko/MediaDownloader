using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

using MediaDownloader.Properties;

namespace MediaDownloader
{
    [ValueConversion(typeof(DownloadStatus), typeof(string))]
    internal class DownloadStatusConverter : IValueConverter
    {
        private Dictionary<DownloadStatus, string> _downloadStatusValues = new Dictionary<DownloadStatus, string>
        {
            {DownloadStatus.Success, Resources.DownloadStatusSuccess},
            {DownloadStatus.Fail, Resources.DownloadStatusFail},
            {DownloadStatus.Cancel, Resources.DownloadStatusCancel},
            {DownloadStatus.Unknown, Resources.DownloadStatusUnknown},
        };

        private Dictionary<string, DownloadStatus> _downloadStatusKeys = new Dictionary<string, DownloadStatus>
        {
            {Resources.DownloadStatusSuccess, DownloadStatus.Success},
            {Resources.DownloadStatusFail, DownloadStatus.Fail},
            {Resources.DownloadStatusCancel, DownloadStatus.Cancel},
            {Resources.DownloadStatusUnknown, DownloadStatus.Unknown},
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Resources.DownloadStatusUnknown;
            }

            var status = (DownloadStatus) value;
            return _downloadStatusValues[status];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = (string) value;
            return _downloadStatusKeys[status ?? Resources.DownloadStatusUnknown];
        }
    }
}