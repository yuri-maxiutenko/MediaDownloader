using System.IO;

using MediaDownloader.Properties;

namespace MediaDownloader.Services;

public sealed class UserDataPathProvider : IUserDataPathProvider
{
    public UserDataPathProvider()
    {
        UserDataFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Resources.ManufacturerFolderName, Resources.AppFolderName);

        var dataFolderPath = Path.Combine(UserDataFolderPath, Resources.DataFolderName);
        Directory.CreateDirectory(dataFolderPath);

        DatabaseFilePath = Path.Combine(dataFolderPath, Resources.DatabaseName);
    }

    public string UserDataFolderPath { get; }

    public string DatabaseFilePath { get; }
}
