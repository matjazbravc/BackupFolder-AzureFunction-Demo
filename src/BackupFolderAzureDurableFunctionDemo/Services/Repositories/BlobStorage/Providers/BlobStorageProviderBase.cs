using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BackupFolderAzureDurableFunctionDemo.Exceptions;
using BackupFolderAzureDurableFunctionDemo.Services.Logging;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Helpers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Providers
{
    /// <summary>
    /// Wrap Azure Blob Storage Access
    /// </summary>
    /// inspired by: https://github.com/Azure/Azure-MachineLearning-DataScience/blob/master/Apps/ByodService/Helpers/BlobHelper.cs
    public class BlobStorageProviderBase
    {
        public const string BLOBNAME = "BlobName";
        private readonly BlobRequestOptionsHelper _blobRequestOptionsHelper;
        private readonly CloudBlockBlobMd5Helper _cloudBlockBlobMd5Helper;
        private readonly ValidateStorage _validateStorage;
        private CloudBlobClient _cloudBlobClient;

        public BlobStorageProviderBase(ILog log, 
            ValidateStorage validateStorage,
            CloudBlockBlobMd5Helper cloudBlockBlobMd5Helper, 
            BlobRequestOptionsHelper blobRequestOptionsHelper)
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
            _cloudBlockBlobMd5Helper = cloudBlockBlobMd5Helper;
            _blobRequestOptionsHelper = blobRequestOptionsHelper;
        }

        /// <summary>
        /// Gets repository configuration.
        /// </summary>
        protected string ConnectionString { get; set; }

        protected CloudBlobContainer Container { get; set; }

        /// <summary>
        /// Queries for a specific blob-name inside a blob container
        /// </summary>
        /// <param name="blobName">Blob name</param>
        /// <returns>True if found, false if not fount and throws on exception</returns>
        public async Task<bool> BlobExistsAsync(string blobName)
        {
            var blobList = await ListBlobsAsync();
            if (blobList == null || !blobList.Any())
            {
                return false;
            }
            return blobList.Any(cloudBlob => ((ICloudBlob)cloudBlob).Name.Equals(blobName));
        }

        /// <summary>
        /// Creates Cloud Append Blob
        /// </summary>
        /// <param name="blobName">The blobId for the Append block blob</param>
        /// <param name="contentType">The content type for the Append block blob</param>
        /// <returns>The created Append block blob</returns>
        public async Task<CloudAppendBlob> CreateAppendBlobAsync(string blobName, string contentType = "text/plain")
        {
            ThrowWhenCloudBlobContainerDoesNotExist();
            _validateStorage.ValidateBlobName(blobName, BLOBNAME);
            var appendBlob = Container.GetAppendBlobReference(blobName);
            appendBlob.Metadata[BLOBNAME] = blobName;
            appendBlob.Properties.ContentType = contentType;
            if (!await appendBlob.ExistsAsync())
            {
                await appendBlob.CreateOrReplaceAsync();
            }
            return appendBlob;
        }

        /// <summary>
        /// Creates Cloud Block Blob
        /// </summary>
        /// <param name="blobName">The blobId for the block blob</param>
        /// <param name="contentType">The content type for the block blob</param>
        /// <returns>The created block blob</returns>
        public CloudBlockBlob CreateBlockBlob(string blobName, string contentType = "text/plain")
        {
            ThrowWhenCloudBlobContainerDoesNotExist();
            _validateStorage.ValidateBlobName(blobName, BLOBNAME);
            var cloudBlockBlob = Container.GetBlockBlobReference(blobName);
            cloudBlockBlob.Metadata[BLOBNAME] = blobName;
            cloudBlockBlob.Properties.ContentType = contentType;
            return cloudBlockBlob;
        }

        /// <summary>
        /// Delete a blob
        /// </summary>
        /// <param name="blobName"></param>
        /// <returns>Return true on success, false if unable to create, throw exception on error</returns>
        public async Task<bool> DeleteAsync(string blobName)
        {
            try
            {
                var blobReferenceFromServer = await Container.GetBlobReferenceFromServerAsync(blobName);
                return await blobReferenceFromServer.DeleteIfExistsAsync();
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 404)
                {
                    return false;
                }
                throw;
            }
        }

        /// <summary>
        /// Delete current blob container
        /// </summary>
        /// <returns>Return true on success, false if not found, throw exception on error.</returns>
        public async Task<bool> DeleteContainerAsync()
        {
            bool result;
            try
            {
                result = await Container.DeleteIfExistsAsync(null, _blobRequestOptionsHelper.Get(), null);
                Container = null;
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 404)
                {
                    return false;
                }
                throw;
            }
            return result;
        }

        /// <summary>
        /// Deletes the blob container
        /// </summary>
        /// <param name="prefix">Prefix</param>
        protected async Task DeleteContainersAsync(string prefix = null)
        {
            var cloudBlobContainers = await ListContainersAsync(prefix);
            foreach (var cloudBlobContainer in cloudBlobContainers)
            {
                await cloudBlobContainer.DeleteIfExistsAsync(null, _blobRequestOptionsHelper.Get(), null);
            }
        }

        /// <summary>
        /// Deletes the blob container according to LastModified property
        /// </summary>
        /// <param name="daysToKeep">Number of days to keep containers</param>
        /// <param name="prefix">Container name prefix</param>
        protected async Task<bool> DeleteContainersAsync(int daysToKeep, string prefix = null)
        {
            var result = true;
            var cloudBlobContainers = await ListContainersAsync(prefix);
            foreach (var cloudBlobContainer in cloudBlobContainers)
            {
                await cloudBlobContainer.FetchAttributesAsync(null, _blobRequestOptionsHelper.Get(), null);
                if (cloudBlobContainer.Properties.LastModified == null ||
                    cloudBlobContainer.Properties.LastModified.Value.UtcDateTime.Date > DateTime.UtcNow.AddDays(-1 * daysToKeep).Date)
                {
                    continue;
                }
                if (!await cloudBlobContainer.DeleteIfExistsAsync(null, _blobRequestOptionsHelper.Get(), null))
                {
                    result = false;
                }
            }
            return result;
        }

        /// <summary>
        /// Generate a shared access signature for a policy
        /// </summary>
        /// <param name="policy"></param>
        /// <returns>Return shared access signature</returns>
        protected string GenerateSharedAccessSignature(SharedAccessBlobPolicy policy)
        {
            ThrowWhenCloudBlobContainerDoesNotExist();
            return Container.GetSharedAccessSignature(policy);
        }

        /// <summary>
        /// Generate a shared access signature for a saved container policy
        /// </summary>
        /// <param name="policyName"></param>
        /// <returns>Return shared access signature</returns>
        protected string GenerateSharedAccessSignature(string policyName)
        {
            ThrowWhenCloudBlobContainerDoesNotExist();
            return Container.GetSharedAccessSignature(new SharedAccessBlobPolicy(), policyName);
        }

        /// <summary>
        /// Generate a shared access signature for a blob and returns the URL
        /// </summary>
        /// <param name="blobName">Blob name</param>
        /// <param name="accessDuration"></param>
        /// <returns>Return blob read url</returns>
        protected string GenerateTemporaryBlobReadUrl(string blobName, TimeSpan accessDuration)
        {
            ThrowWhenCloudBlobContainerDoesNotExist();
            var policy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTimeOffset.Now + accessDuration
            };
            var blob = Container.GetBlockBlobReference(blobName);
            var signature = blob.GetSharedAccessSignature(policy);
            return blob.Uri + signature;
        }

        /// <summary>
        /// Returns as stream with the contents of a block blob
        /// with the given blob name
        /// </summary>
        /// <param name="blobName"></param>
        /// <returns>Stream</returns>
        public async Task<MemoryStream> GetBlobAsStreamAsync(string blobName)
        {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(blobName), "blobName cannot be null or empty");
            ThrowWhenCloudBlobContainerDoesNotExist();
            var stream = new MemoryStream();
            try
            {
                var cloudBlockBlob = await Container.GetBlobReferenceFromServerAsync(blobName).ConfigureAwait(false);
                await cloudBlockBlob.DownloadToStreamAsync(stream, null, _blobRequestOptionsHelper.Get(), null);
                stream.Seek(0, SeekOrigin.Begin);
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == (int) HttpStatusCode.NotFound)
                {
                    stream = null;
                }
                else
                {
                    throw;
                }
            }
            return stream;
        }

        /// <summary>
        /// Returns as string with the contents of a block blob
        /// with the given blob name
        /// </summary>
        /// <param name="blobName"></param>
        /// <returns>string</returns>
        public async Task<string> GetAsStringAsync(string blobName)
        {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(blobName), "blobName cannot be null or empty");
            ThrowWhenCloudBlobContainerDoesNotExist();
            var cloudBlockBlob = Container.GetBlockBlobReference(blobName);
            return await cloudBlockBlob.DownloadTextAsync(Encoding.UTF8, null, _blobRequestOptionsHelper.Get(), null);
        }

        /// <summary>
        /// Get blob metadata
        /// </summary>
        /// <param name="blobName">Blob name</param>
        /// <returns>Return true on success, false if not found, throw exception on error</returns>
        protected async Task<IDictionary<string, string>> GetBlobMetadataAsync(string blobName)
        {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(blobName), "blobName cannot be null or empty");
            ThrowWhenCloudBlobClientDoesNotExist();
            try
            {
                var blob = await Container.GetBlobReferenceFromServerAsync(blobName).ConfigureAwait(false);
                await blob.FetchAttributesAsync().ConfigureAwait(false);
                return blob.Metadata;
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 404)
                {
                    return null;
                }
                throw;
            }
        }

        /// <summary>
        /// returns the blob name from IListBlobItem
        /// </summary>
        /// <param name="blobItem">Blob Item</param>
        protected static string GetBlobName(IListBlobItem blobItem)
        {
            Contract.Requires<ArgumentNullException>(blobItem != null, "blobItem cannot be null");
            var blobName = string.Empty;
            if (blobItem is CloudBlockBlob blob)
            {
                blobName = blob.Name;
            }
            else if (blobItem is CloudPageBlob)
            {
                blobName = ((CloudPageBlob)blobItem).Name;
            }
            else if (blobItem is CloudBlobDirectory)
            {
                blobName = ((CloudBlobDirectory)blobItem).Uri.ToString();
            }
            return blobName;
        }

        /// <summary>
        /// Get blob properties
        /// </summary>
        /// <param name="blobName">Blob name</param>
        /// <returns>Return true on success, false if not found, throw exception on error</returns>
        protected async Task<BlobProperties> GetBlobPropertiesAsync(string blobName)
        {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(blobName), "blobName cannot be null or empty");
            ThrowWhenCloudBlobClientDoesNotExist();
            var blob = await Container.GetBlobReferenceFromServerAsync(blobName);
            blob.FetchAttributesAsync().Wait();
            return blob.Properties;
        }

        /// <summary>
        /// Get the size of a blob
        /// </summary>
        /// <param name="blobName">Blob name</param>
        protected async Task<long> GetBlobSizeAsync(string blobName)
        {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(blobName), "blobName cannot be null or empty");
            var props = await GetBlobPropertiesAsync(blobName);
            return props.Length;
        }

        protected CloudBlockBlob GetBlockBlob(string blobName)
        {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(blobName), "blobName cannot be null or empty");
            var cloudBlockBlob = GetBlockBlobReference(blobName);
            cloudBlockBlob.FetchAttributesAsync().Wait();
            return cloudBlockBlob;
        }

        /// <summary>
        /// Gets a reference to a block blob with the given unique blob name
        /// </summary>
        /// <param name="blobName">The unique block blob identifier</param>
        /// <returns>A reference to the block blob</returns>
        protected CloudBlockBlob GetBlockBlobReference(string blobName)
        {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(blobName), "blobName cannot be null or empty");
            ThrowWhenCloudBlobContainerDoesNotExist();
            return Container.GetBlockBlobReference(blobName);
        }

        /// <summary>
        /// Get container access policies
        /// </summary>
        /// <returns>Return sorted list of SharedAccessBlobPolicies</returns>
        protected async Task<SortedList<string, SharedAccessBlobPolicy>> GetContainerAccessPolicyAsync()
        {
            ThrowWhenCloudBlobContainerDoesNotExist();
            var policies = new SortedList<string, SharedAccessBlobPolicy>();
            var permissions = await Container.GetPermissionsAsync();
            if (permissions.SharedAccessPolicies == null)
            {
                return policies;
            }
            foreach (KeyValuePair<string, SharedAccessBlobPolicy> policy in permissions.SharedAccessPolicies)
            {
                policies.Add(policy.Key, policy.Value);
            }
            return policies;
        }

        /// <summary>
        /// Get container access control
        /// </summary>
        protected async Task<BlobContainerPublicAccessType> GetContainerAclAsync()
        {
            ThrowWhenCloudBlobContainerDoesNotExist();
            var permissions = await Container.GetPermissionsAsync();
            return permissions.PublicAccess;
        }

        /// <summary>
        /// Get container metadata
        /// </summary>
        /// <returns>Return dictionary with metadata</returns>
        protected async Task<IDictionary<string, string>> GetContainerMetadataAsync()
        {
            ThrowWhenCloudBlobContainerDoesNotExist();
            await Container.FetchAttributesAsync(null, _blobRequestOptionsHelper.Get(), null);
            return Container.Metadata;
        }

        /// <summary>
        /// Get container properties
        /// </summary>
        /// <returns>Return BlobContainerProperties</returns>
        protected async Task<BlobContainerProperties> GetContainerPropertiesAsync()
        {
            ThrowWhenCloudBlobContainerDoesNotExist();
            await Container.FetchAttributesAsync(null, _blobRequestOptionsHelper.Get(), null);
            return Container.Properties;
        }

        protected void GetOrCreateCloudBlobClient()
        {
            if (_cloudBlobClient != null)
            {
                return;
            }
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(ConnectionString), "StorageAccountConnectionString cannot be null or empty");
            var cloudStorageAccount = CloudStorageAccount.Parse(ConnectionString);
            _cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            _cloudBlobClient.DefaultRequestOptions = _blobRequestOptionsHelper.Get();
        }

        public void GetOrCreateContainer(string containerName)
        {
            ThrowWhenCloudBlobClientDoesNotExist();
            _validateStorage.ValidateBlobContainerName(containerName, nameof(containerName));
            if (Container != null && Container.Name.Equals(containerName, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }
            Container = _cloudBlobClient.GetContainerReference(containerName);
            Container.CreateIfNotExistsAsync().Wait();
            SetContainerAclAsync(BlobContainerPublicAccessType.Container).Wait();
        }

        public void Initialize(string containerName)
        {
            GetOrCreateCloudBlobClient();
            GetOrCreateContainer(containerName);
        }

        public async Task<List<IListBlobItem>> ListBlobsAsync(string prefix = "")
        {
            ThrowWhenCloudBlobContainerDoesNotExist();
            BlobContinuationToken continuationToken = null;
            var results = new List<IListBlobItem>();
            do
            {
                var resultSegment = await Container.ListBlobsSegmentedAsync(prefix, true, BlobListingDetails.Metadata, 1000, continuationToken, _blobRequestOptionsHelper.Get(), null);
                results.AddRange(resultSegment.Results);
                continuationToken = resultSegment.ContinuationToken;
            }
            while (continuationToken != null);
            return results;
        }

        public async Task<List<IListBlobItem>> ListBlobsAsync(string containerName, string prefix)
        {
            GetOrCreateContainer(containerName);
            var results = await ListBlobsAsync(prefix);
            return results;
        }

        /// <summary>
        /// Enumerate the containers in a storage account
        /// </summary>
        /// <param name="prefix">Container name prefix</param>
        /// <returns>the list of containers</returns>
        protected async Task<List<CloudBlobContainer>> ListContainersAsync(string prefix = "")
        {
            ThrowWhenCloudBlobClientDoesNotExist();
            BlobContinuationToken continuationToken = null;
            var results = new List<CloudBlobContainer>();
            do
            {
                var resultSegment = await _cloudBlobClient.ListContainersSegmentedAsync(prefix, ContainerListingDetails.All, 100, continuationToken, _blobRequestOptionsHelper.Get(), null);
                results.AddRange(resultSegment.Results);
                continuationToken = resultSegment.ContinuationToken;
            }
            while (continuationToken != null);
            return results;
        }

        protected async Task<CopyStatus> RenameContainerAsync(string oldName, string newName)
        {
            var source = (CloudBlockBlob)await Container.GetBlobReferenceFromServerAsync(oldName);
            var target = Container.GetBlockBlobReference(newName);
            await target.StartCopyAsync(source);
            while (target.CopyState.Status == CopyStatus.Pending)
            {
                await Task.Delay(100);
            }
            if (target.CopyState.Status != CopyStatus.Success)
            {
                return target.CopyState.Status;
            }
            await source.DeleteAsync();
            return CopyStatus.Success;
        }

        /// <summary>
        /// Set the properties of for the blob. Using null for a property means that the original property ofthe blob 
        /// remain unaffected, using empty string mean to "erase" the original property value
        /// Note that only 
        /// </summary>
        /// <param name="blobName"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        protected async Task SetBlobPropertiesAsync(string blobName, BlobProperties properties)
        {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(blobName), "blobName cannot be null or empty");
            ThrowWhenCloudBlobClientDoesNotExist();
            var blob = await Container.GetBlobReferenceFromServerAsync(blobName);
            await blob.FetchAttributesAsync();
            // Update the properties with new values if exist or use the original ones.
            blob.Properties.CacheControl = properties.CacheControl ?? blob.Properties.CacheControl;
            blob.Properties.ContentEncoding = properties.ContentEncoding ?? blob.Properties.ContentEncoding;
            blob.Properties.ContentLanguage = properties.ContentLanguage ?? blob.Properties.ContentLanguage;
            blob.Properties.ContentMD5 = properties.ContentMD5 ?? blob.Properties.ContentMD5;
            blob.Properties.ContentType = properties.ContentType ?? blob.Properties.ContentType;
            await blob.SetPropertiesAsync();
        }

        /// <summary>
        /// Set container access policy
        /// </summary>
        /// <param name="policies">container policies</param>
        /// <returns>Return true on success, false if not found, throw exception on error</returns>
        protected async Task SetContainerAccessPolicyAsync(SortedList<string, SharedAccessBlobPolicy> policies)
        {
            ThrowWhenCloudBlobContainerDoesNotExist();
            var permissions = await Container.GetPermissionsAsync();
            permissions.SharedAccessPolicies.Clear();
            if (policies != null)
            {
                foreach (KeyValuePair<string, SharedAccessBlobPolicy> policy in policies)
                {
                    permissions.SharedAccessPolicies.Add(policy.Key, policy.Value);
                }
            }
            await Container.SetPermissionsAsync(permissions);
        }

        /// <summary>
        /// Set container access control to container|blob|private
        /// </summary>
        /// <param name="accessLevel">Set access level to container|blob|private</param>
        /// <returns>Return true on success, false if not found, throw exception on error</returns>
        protected async Task SetContainerAclAsync(BlobContainerPublicAccessType accessLevel)
        {
            ThrowWhenCloudBlobContainerDoesNotExist();
            var permissions = new BlobContainerPermissions
            {
                PublicAccess = accessLevel
            };
            await Container.SetPermissionsAsync(permissions);
        }

        /// <summary>
        /// Set container metadata
        /// </summary>
        /// <param name="metadata">container meta data</param>
        /// <returns>Return true on success, false if not found, throw exception on error</returns>
        protected async Task SetContainerMetadataAsync(IEnumerable<KeyValuePair<string, string>> metadata)
        {
            ThrowWhenCloudBlobClientDoesNotExist();
            Container.Metadata.Clear();
            foreach (var keyValuePair in metadata)
            {
                Container.Metadata.Add(keyValuePair);
            }
            await Container.SetMetadataAsync();
        }

        protected async Task ZipBlobsInContainerAsync(string zipBlobName)
        {
            if (string.IsNullOrWhiteSpace(zipBlobName))
            {
                zipBlobName = Container.Name + ".zip";
            }
            var cloudBlockBlob = CreateBlockBlob(zipBlobName);
            var blobItems = await ListBlobsAsync();
            using (var compressedStream = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(compressedStream, ZipArchiveMode.Create, true))
                {
                    foreach (var listBlobItem in blobItems)
                    {
                        var blob = (CloudBlockBlob)listBlobItem;
                        blob.FetchAttributesAsync().Wait();
                        var blobFileName = Path.GetFileName(blob.Uri.AbsolutePath);
                        var fileInArchive = zipArchive.CreateEntry(blobFileName, CompressionLevel.Optimal);
                        using (var entryStream = fileInArchive.Open())
                        {
                            await blob.DownloadToStreamAsync(entryStream, null, _blobRequestOptionsHelper.Get(), null);
                        }
                    }
                }

                // Reset the stream back to the beginning before trying to upload it
                if (compressedStream.CanSeek)
                {
                    compressedStream.Seek(0, SeekOrigin.Begin);
                }

                cloudBlockBlob.UploadFromStreamAsync(compressedStream, null, _blobRequestOptionsHelper.Get(), null).Wait();
                cloudBlockBlob.Properties.ContentEncoding = "gzip";
                cloudBlockBlob.SetPropertiesAsync().Wait();
            }
        }

        /// <summary>
        /// Creates a new block blob and populates it from a byte array
        /// </summary>
        /// <param name="blobName">The blob Name for the block blob</param>
        /// <param name="value">The data to store in the block blob</param>
        /// <param name="contentType">The content type for the block blob</param>
        /// <returns>The created block blob</returns>
        protected async Task<CloudBlockBlob> SetBlobFromByteArrayAsync(string blobName, byte[] value, string contentType)
        {
            var cloudBlockBlob = CreateBlockBlob(blobName, contentType);
            await cloudBlockBlob.UploadFromByteArrayAsync(value, 0, value.Length, null, _blobRequestOptionsHelper.Get(), null);
            return cloudBlockBlob;
        }

        /// <summary>
        /// Put (create or update) a block blob and populates it from a file
        /// </summary>
        /// <param name="blobName">The blobId for the block blob</param>
        /// <param name="contentType">The content type for the block blob</param>
        /// <param name="filePath"></param>
        /// <returns>The created block blob</returns>
        protected async Task<CloudBlockBlob> SetBlobFromFileAsync(string blobName, string filePath, string contentType)
        {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(blobName), "blobName cannot be null or empty");
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(filePath), "filePath cannot be null or empty");
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(contentType), "contentType cannot be null or empty");
            var cloudBlockBlob = CreateBlockBlob(blobName, contentType);
            await cloudBlockBlob.UploadFromFileAsync(filePath, null, _blobRequestOptionsHelper.Get(), null);
            return cloudBlockBlob;
        }

        /// <summary>
        /// Put (create or update) a block blob and populates it from a stream
        /// </summary>
        /// <param name="blobName">The blob Name for the block blob</param>
        /// <param name="value">The data to store in the block blob</param>
        /// <param name="contentType">The content type for the block blob</param>
        /// <returns>The created block blob</returns>
        protected async Task<CloudBlockBlob> SetBlobFromStreamAsync(string blobName, MemoryStream value, string contentType)
        {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(blobName), "blobName cannot be null or empty");
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(contentType), "contentType cannot be null or empty");
            var cloudBlockBlob = CreateBlockBlob(blobName, contentType);
            // Reset the stream back to the beginning before trying to upload it
            if (value.CanSeek)
            {
                value.Seek(0, SeekOrigin.Begin);
            }
            await cloudBlockBlob.UploadFromStreamAsync(value, null, _blobRequestOptionsHelper.Get(), null);
            _cloudBlockBlobMd5Helper.ApplyContentMd5Hash(cloudBlockBlob, value);
            return cloudBlockBlob;
        }

        /// <summary>
        /// Put (create or update) a block blob and populates it from a string
        /// </summary>
        /// <param name="blobName">The blob Name for the block blob</param>
        /// <param name="value">The data to store in the block blob</param>
        /// <returns>The created block blob</returns>
        protected async Task<CloudBlockBlob> SetBlobFromStringAsync(string blobName, string value)
        {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(value), "data cannot be null or empty");
            var cloudBlockBlob = CreateBlockBlob(blobName);
            await cloudBlockBlob.UploadTextAsync(value, null, null, _blobRequestOptionsHelper.Get(), null);
            return cloudBlockBlob;
        }

        protected void ThrowWhenCloudBlobClientDoesNotExist()
        {
            if (_cloudBlobClient == null)
            {
                throw new CloudBlobClientDoesNotExistException("Before usage, CreateCloudBlobClient must be called to inialize the CloudBlobClient.");
            }
        }

        protected void ThrowWhenCloudBlobContainerDoesNotExist([CallerMemberName] string memberName = "")
        {
            // inspired by: http://www.kunal-chowdhury.com/2012/07/whats-new-in-c-50-learn-about.html
            if (Container != null)
            {
                return;
            }
            var exceptionMessage = $"{memberName}: before usage, Initialize must be called first to initialize the CloudBlobContainer";
            throw new CloudBlobContainerDoesNotExistException(exceptionMessage);
        }
    }
}