using System.Windows;

using Serilog;

namespace MediaDownloader.Services;

public sealed class ClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to copy text to the clipboard");
        }
    }
}
