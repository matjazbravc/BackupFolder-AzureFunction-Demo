using System.Threading.Tasks;
using BackupFolderAzureDurableFunctionDemo.Models;
using BackupFolderAzureDurableFunctionDemo.Services.Helpers;
using BackupFolderAzureDurableFunctionDemo.Services.Ioc;
using BackupFolderAzureDurableFunctionDemo.Services.Logging;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories;
using Microsoft.Azure.WebJobs;

namespace BackupFolderAzureDurableFunctionDemo.Functions
{
    public static class GetFileList
    {
        [FunctionName("fnGetFileList")]
        public static async Task<long> GetFileListAsync(
            [ActivityTrigger] string rootFolder,
            [Inject] ILog log,
            [Inject] IFileHelper fileHelper,
            [Inject] BackupFolderTableStorageRepository backupFolderTableStorageRepository)
        {
            log.Info($"Initializing '{backupFolderTableStorageRepository.Config.TableName}'...");
            backupFolderTableStorageRepository.Initialize();

            log.Info($"Deleting all records from '{backupFolderTableStorageRepository.Config.TableName}'...");
            backupFolderTableStorageRepository.DeleteAllAsync().Wait();

            log.Info($"Searching for files under '{rootFolder}'...");
            var files = fileHelper.GetAllFilesFromDirectory(rootFolder);
            log.Info($"Found {files.Count} file(s) under {rootFolder}.");

            log.Info($"Storing {files.Count} file(s) into {backupFolderTableStorageRepository.Config.TableName}.");
            foreach (var file in files)
            {
                var fileInfo = new FileInformation { FileName = file };
                await backupFolderTableStorageRepository.SetAsync(fileInfo).ConfigureAwait(false);
            }

            return files.Count;
        }
    }
}