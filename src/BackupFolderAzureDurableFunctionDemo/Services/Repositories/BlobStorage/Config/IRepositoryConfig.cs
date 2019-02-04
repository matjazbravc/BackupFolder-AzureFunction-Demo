namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Config
{
    public interface IRepositoryConfig : IRepositoryConfigBase
    {
        string ContainerName { get; }
    }
}
