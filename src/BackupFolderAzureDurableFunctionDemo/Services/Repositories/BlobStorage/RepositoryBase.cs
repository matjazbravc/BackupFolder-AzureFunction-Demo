using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using BackupFolderAzureDurableFunctionDemo.Extensions;
using BackupFolderAzureDurableFunctionDemo.Services.Logging;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Config;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Helpers;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Providers;
using Microsoft.WindowsAzure.Storage.Blob;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage
{
    /// <summary>
    /// Wrap Azure Blob Storage Access
    /// </summary>
    /// inspired by: https://github.com/Azure/Azure-MachineLearning-DataScience/blob/master/Apps/ByodService/Helpers/BlobHelper.cs
    public class RepositoryBase<T> : BlobStorageProviderBase, IRepository<T>
        where T : class
    {
        private readonly BlobRequestOptionsHelper _blobRequestOptionsHelper;
        private readonly ValidateStorage _validateStorage;

        public RepositoryBase(ILog log, ValidateStorage validateStorage,
            CloudBlockBlobMd5Helper cloudBlockBlobMd5Helper, 
            BlobRequestOptionsHelper blobRequestOptionsHelper, 
            IRepositoryConfig repositoryConfig)
            : base(log, validateStorage, cloudBlockBlobMd5Helper, blobRequestOptionsHelper)
        {
            // inspired by: 
            // https://docs.particular.net/nservicebus/azure-storage-persistence/performance-tuning
            // http://blogs.msmvps.com/nunogodinho/2013/11/20/windows-azure-storage-performance-best-practices/
            // https://blogs.msdn.microsoft.com/windowsazurestorage/2010/06/25/nagles-algorithm-is-not-friendly-towards-small-requests/
            // https://alexandrebrisebois.wordpress.com/2013/03/24/why-are-webrequests-throttled-i-want-more-throughput/
            log.Debug();
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 1000;
            _validateStorage = validateStorage;
            _blobRequestOptionsHelper = blobRequestOptionsHelper;
            Config = repositoryConfig;
        }

        public IRepositoryConfig Config { get; set; }

        public async Task<T> GetAsync(string blobName)
        {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(blobName), "blobName cannot be null or empty");
            var stream = await GetBlobAsStreamAsync(blobName);
            var result = stream?.DeserializeWithDecompressionFromMemoryStream<T>();
            return result;
        }

        public void Initialize()
        {
            ConnectionString = Config.StorageAccountConnectionString;
            GetOrCreateCloudBlobClient();
            GetOrCreateContainer(Config.ContainerName);
        }

        public async Task<List<T>> ListAsync(string prefix = "")
        {
            ThrowWhenCloudBlobContainerDoesNotExist();
            BlobContinuationToken continuationToken = null;
            var results = new List<T>();
            do
            {
                var resultSegment = await Container.ListBlobsSegmentedAsync(prefix, true, BlobListingDetails.Metadata, 1000, continuationToken, _blobRequestOptionsHelper.Get(), null);
                foreach (var result in resultSegment.Results)
                {
                    if (!(result is CloudBlockBlob blob))
                    {
                        continue;
                    }
                    var item = await GetAsync(blob.Name);
                    results.Add(item);
                }
                continuationToken = resultSegment.ContinuationToken;
            }
            while (continuationToken != null);
            return results;
        }

        /// <summary>
        /// Put (create or update) a block blob
        /// </summary>
        /// <param name="blobName">blob name</param>
        /// <param name="value">Class of T value</param>
        public async Task<CloudBlockBlob> SetAsync(string blobName, T value)
        {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(blobName), "blobName cannot be null or empty");
            _validateStorage.ValidateBlobName(blobName, BLOBNAME);
            var bytes = value.SerializeWithCompression();
            CloudBlockBlob result;
            using (var memoryStream = new MemoryStream(bytes))
            {
                result = await SetBlobFromStreamAsync(blobName, memoryStream, "gzip");
            }
            return result;
        }
    }
}