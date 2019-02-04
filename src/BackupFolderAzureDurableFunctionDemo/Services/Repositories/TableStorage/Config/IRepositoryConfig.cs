namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage.Config
{
    public interface IRepositoryConfig
    {
        string StorageAccountConnectionString { get; set; }

        string TableName { get; set; }
    }
}