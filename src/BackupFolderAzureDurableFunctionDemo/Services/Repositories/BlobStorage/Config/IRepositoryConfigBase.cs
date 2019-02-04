namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Config
{
    public interface IRepositoryConfigBase
    {
        string StorageAccountConnectionString { get; }
    }
}
