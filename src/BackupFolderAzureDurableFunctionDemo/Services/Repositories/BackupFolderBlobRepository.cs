using BackupFolderAzureDurableFunctionDemo.Models;
using BackupFolderAzureDurableFunctionDemo.Services.Logging;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Helpers;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories
{
    public class BackupFolderBlobRepository : RepositoryBase<FileInformation>
    {
        public BackupFolderBlobRepository(ILog log, ValidateStorage validateStorage,
            CloudBlockBlobMd5Helper cloudBlockBlobMd5Helper, BlobRequestOptionsHelper blobRequestOptionsHelper, BackupFolderBlobRepositoryConfig backupFolderBlobRepositoryConfig)
            : base(log, validateStorage, cloudBlockBlobMd5Helper, blobRequestOptionsHelper, backupFolderBlobRepositoryConfig)
        {
        }
    }
}
