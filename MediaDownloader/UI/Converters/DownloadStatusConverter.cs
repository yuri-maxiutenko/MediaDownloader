﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

using MediaDownloader.Download.Models;
using MediaDownloader.Properties;

namespace MediaDownloader.UI.Converters;

[ValueConversion(typeof(DownloadStatus), typeof(string))]
internal class DownloadStatusConverter : IValueConverter
{
    private readonly Dictionary<string, DownloadStatus> _downloadStatusKeys = new()
    {
        { Resources.DownloadStatusSuccess, DownloadStatus.Success },
        { Resources.DownloadStatusFail, DownloadStatus.Fail },
        { Resources.DownloadStatusCancel, DownloadStatus.Cancel },
        { Resources.DownloadStatusUnknown, DownloadStatus.Unknown }
    };

    private readonly Dictionary<DownloadStatus, string> _downloadStatusValues = new()
    {
        { DownloadStatus.Success, Resources.DownloadStatusSuccess },
        { DownloadStatus.Fail, Resources.DownloadStatusFail },
        { DownloadStatus.Cancel, Resources.DownloadStatusCancel },
        { DownloadStatus.Unknown, Resources.DownloadStatusUnknown }
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return Resources.DownloadStatusUnknown;
        }

        var status = (DownloadStatus)value;
        return _downloadStatusValues[status];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = (string)value;
        return _downloadStatusKeys[status ?? Resources.DownloadStatusUnknown];
    }
}