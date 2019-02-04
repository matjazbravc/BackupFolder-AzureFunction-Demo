using BackupFolderAzureDurableFunctionDemo.Services.Helpers;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage.Config;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories
{
    public class BackupFolderTableStorageRepositoryConfig : IRepositoryConfig
    {
        private const string STORAGE_ACCOUNT_CONNECTION_STRING = "StorageAccount.ConnectionString";
        private const string TABLE_NAME = "BackupFolder.TableName";
        private const string TABLE_NAME_DEFAULT = "BackupFolder";
        
        public BackupFolderTableStorageRepositoryConfig(ConfigHelper configHelper)
        {
            StorageAccountConnectionString = configHelper.GetSetting(STORAGE_ACCOUNT_CONNECTION_STRING, "UseDevelopmentStorage=true");
            TableName = configHelper.GetSetting(TABLE_NAME, TABLE_NAME_DEFAULT);
        }

        public string StorageAccountConnectionString { get; set; }

        public string TableName { get; set; }
    }
}
