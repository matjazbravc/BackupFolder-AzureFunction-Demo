using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BackupFolderAzureDurableFunctionDemo.Extensions;
using BackupFolderAzureDurableFunctionDemo.Services.Ioc;
using BackupFolderAzureDurableFunctionDemo.Services.Logging;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Helpers;
using Microsoft.Azure.WebJobs;

namespace BackupFolderAzureDurableFunctionDemo.Functions
{
    public static class CopyFileToBlob
    {
        [FunctionName("fnCopyFileToBlob")]
        public static async Task<long> CopyFileToBlobAsync(
            [ActivityTrigger] DirectoryInfo folderInfo,
            [Inject] ILog log,
            [Inject] BlobRequestOptionsHelper blobRequestOptionsHelper,
            [Inject] BackupFolderBlobRepository backupFolderBlobRepository,
            [Inject] BackupFolderTableStorageRepository backupFolderTableStorageRepository)
        {            
            log.Info($"Start copying files from '{folderInfo.FullName}'");

            log.Info("Initializing repositories");
            backupFolderBlobRepository.Initialize();
            backupFolderTableStorageRepository.Initialize();

            log.Info($"Deleting all records from '{backupFolderBlobRepository.Config.ContainerName}'");
            await backupFolderBlobRepository.DeleteContainerAsync();
            backupFolderBlobRepository.Initialize();

            var folderFiles = await backupFolderTableStorageRepository.GetAllAsync();
            log.Info($"Retrieved {folderFiles.Count} records from '{backupFolderTableStorageRepository.Config.TableName}'");
            
            var tasks = new List<Task<long>>();
            foreach (var file in folderFiles)
            {
                tasks.Add(CopyFileAsync(file.FileName, log, backupFolderBlobRepository, blobRequestOptionsHelper));
            }
            await Task.WhenAll(tasks);
            var totalBytes = tasks.Sum(t => t.Result);
            
            return totalBytes;
        }

        private static async Task<long> CopyFileAsync(string filePath, ILog log, BackupFolderBlobRepository backupFolderBlobRepository, BlobRequestOptionsHelper blobRequestOptionsHelper)
        {
            var fileBytes = new FileInfo(filePath).Length;
            var blobOutputLocation = GetBlobName(filePath);

            log.Info($"Copying {filePath} to {blobOutputLocation}, size: {fileBytes.ToPrettySize()}");
            var cloudBlockBlob = backupFolderBlobRepository.CreateBlockBlob(blobOutputLocation, "image/jpg");
            try
            {
                using (Stream source = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    source.Seek(0, SeekOrigin.Begin);
                    await cloudBlockBlob.UploadFromStreamAsync(source, null, blobRequestOptionsHelper.Get(), null);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }

            return fileBytes;
        }

        /// <summary>
        ///  Build sup-directory in Container - strip the drive letter prefix and convert back slashes to forward slashes
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>Blob name</returns>
        private static string GetBlobName(string filePath)
        {
            return filePath.Substring(Path.GetPathRoot(filePath).Length).Replace('\\', '/');
        }
    }
}