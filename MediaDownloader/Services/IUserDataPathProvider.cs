namespace MediaDownloader.Services;

public interface IUserDataPathProvider
{
    /// <summary>Root of the per-user data folder (logs, database), created on first access.</summary>
    string UserDataFolderPath { get; }

    string DatabaseFilePath { get; }
}
