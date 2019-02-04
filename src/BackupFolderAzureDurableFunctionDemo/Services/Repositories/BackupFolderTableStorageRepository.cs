using System.Threading.Tasks;
using BackupFolderAzureDurableFunctionDemo.Models;
using BackupFolderAzureDurableFunctionDemo.Services.Logging;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage.Helpers;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories
{
    public class BackupFolderTableStorageRepository : RepositoryBase<FileInformation>
    {
        private const string DEFAULT_PARTITION_KEY = "Default";

        public BackupFolderTableStorageRepository(ILog log, ValidateStorage validateStorage, 
            TableRequestOptionsHelper tableRequestOptionsHelper, BackupFolderTableStorageRepositoryConfig backupFolderTableRepositoryConfig) 
            : base(log, validateStorage, tableRequestOptionsHelper, backupFolderTableRepositoryConfig)
        {
        }

        public async Task<bool> DeleteAsync(string id)
        {
            return await DeleteAsync(DEFAULT_PARTITION_KEY, id).ConfigureAwait(false);
        }

        public async Task<FileInformation> GetAsync(string id)
        {
            return await GetAsync(DEFAULT_PARTITION_KEY, id).ConfigureAwait(false);
        }
    }
}
