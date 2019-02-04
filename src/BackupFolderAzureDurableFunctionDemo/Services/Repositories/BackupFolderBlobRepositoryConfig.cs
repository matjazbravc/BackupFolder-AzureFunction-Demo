using BackupFolderAzureDurableFunctionDemo.Services.Helpers;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Config;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories
{
    public class BackupFolderBlobRepositoryConfig : IRepositoryConfig
    {
        private const string STORAGE_ACCOUNT_CONNECTION_STRING = "StorageAccount.ConnectionString";
        private const string CONTAINER_NAME = "BackupFolder.ContainerName";
        private const string CONTAINER_NAME_DEFAULT = "backupfolder";
      
        public BackupFolderBlobRepositoryConfig(ConfigHelper configHelper)
        {
            StorageAccountConnectionString = configHelper.GetSetting(STORAGE_ACCOUNT_CONNECTION_STRING, "UseDevelopmentStorage=true");
            ContainerName = configHelper.GetSetting(CONTAINER_NAME, CONTAINER_NAME_DEFAULT);
        }

        public string StorageAccountConnectionString { get; }

        public string ContainerName { get; }
    }
}
