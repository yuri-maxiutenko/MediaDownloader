using System.Diagnostics;

using MediaDownloader.Properties;

using Serilog;

namespace MediaDownloader.Services;

public sealed class ShellService : IShellService
{
    public void OpenFileLocation(string path)
    {
        try
        {
            Process.Start(Resources.ExplorerFileName, $"{Resources.ExplorerOptionSelect}, \"{path}\"");
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to open Explorer with {Path} selected", path);
        }
    }

    public void OpenFolder(string folderPath)
    {
        try
        {
            Process.Start(Resources.ExplorerFileName, folderPath);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to open folder {Path}", folderPath);
        }
    }
}
