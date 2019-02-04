using System;
using System.IO;
using System.Threading.Tasks;
using BackupFolderAzureDurableFunctionDemo.Services.Ioc;
using BackupFolderAzureDurableFunctionDemo.Services.Logging;
using Microsoft.Azure.WebJobs;

namespace BackupFolderAzureDurableFunctionDemo.Functions
{
    public static class BackupFolder
    {
        [FunctionName(nameof(BackupFolder))]
        public static async Task<long> Run(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            [Inject] ILog log)
        {
            var rootFolder = context.GetInput<string>()?.Trim();
            if (string.IsNullOrEmpty(rootFolder))
            {
                throw new ApplicationException("Backup folder is empty");
            }

            log.Info($"Starting {rootFolder} backup");
            long totalBytes = 0;
            try
            {            
                var folderInfo = new DirectoryInfo(rootFolder);
                var numFiles = await context.CallActivityAsync<long>("fnGetFileList", folderInfo.FullName);
                log.Info($"Starting to backup {numFiles} files");
                totalBytes = await context.CallActivityAsync<long>("fnCopyFileToBlob", folderInfo);
            }
            catch (Exception ex)
            {
                // Handling errors in Durable Functions
                // https://docs.microsoft.com/en-us/azure/azure-functions/durable-functions-error-handling
                // If an orchestrator function fails with an unhandled exception, the details of the exception 
                // are logged and the instance completes with a Failed status.
                log.Error(ex.Message);
            }
            return totalBytes;
        }
    }
}