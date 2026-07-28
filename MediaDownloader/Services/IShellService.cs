namespace MediaDownloader.Services;

public interface IShellService
{
    /// <summary>Opens Explorer with the given file or folder selected.</summary>
    void OpenFileLocation(string path);

    /// <summary>Opens the given folder in Explorer.</summary>
    void OpenFolder(string folderPath);
}
