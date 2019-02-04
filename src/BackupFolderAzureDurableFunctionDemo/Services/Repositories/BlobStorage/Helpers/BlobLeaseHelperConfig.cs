using System;
using System.Diagnostics.Contracts;
using BackupFolderAzureDurableFunctionDemo.Services.Helpers;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Config;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Helpers
{
    public class BlobLeaseHelperConfig : IRepositoryConfig
    {
        private const string STORAGE_ACCOUNT_CONNECTION_STRING = "StorageAccount.ConnectionString";

        public BlobLeaseHelperConfig(ConfigHelper configHelper)
        {
            StorageAccountConnectionString = configHelper.GetSetting(STORAGE_ACCOUNT_CONNECTION_STRING, string.Empty);
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(StorageAccountConnectionString), STORAGE_ACCOUNT_CONNECTION_STRING + " must not be null");
        }

        public string ContainerName { get; } = null;

        public string StorageAccountConnectionString { get; set; }
    }
}
