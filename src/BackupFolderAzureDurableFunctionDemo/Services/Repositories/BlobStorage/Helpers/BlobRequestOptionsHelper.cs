using System;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Helpers
{
	public sealed class BlobRequestOptionsHelper
	{
		private BlobRequestOptions _blobRequestOptions;

		// More info:
		// https://msdn.microsoft.com/en-us/library/microsoft.windowsazure.storage.blob.blobrequestoptions.aspx
		// http://gauravmantri.com/2012/12/30/storage-client-library-2-0-implementing-retry-policies/
		// http://stackoverflow.com/questions/30203070/azure-blob-downloadtostream-takes-too-long
		public BlobRequestOptions Get(TimeSpan? serverTimeout = null, TimeSpan? maximumExecutionTime = null, bool forceNew = false, bool disableContentMd5Validation = true, bool storeBlobContentMd5 = false)
		{
            if (_blobRequestOptions != null && !forceNew)
			{
				return _blobRequestOptions;
			}
			_blobRequestOptions = new BlobRequestOptions
			{
				ServerTimeout = serverTimeout ?? TimeSpan.FromMinutes(30),
				MaximumExecutionTime = maximumExecutionTime ?? TimeSpan.FromMinutes(60), // After that period all operations will be canceled!
				RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(5), 10),
                DisableContentMD5Validation = disableContentMd5Validation,
                StoreBlobContentMD5 = storeBlobContentMd5,
                SingleBlobUploadThresholdInBytes = 16 * 1048576, // Upload all blobs up to 16MB as a single put blob and will use block upload for blob sizes greater than 16MB. The valid range is 1MB – 64MB.
                ParallelOperationThreadCount = Environment.ProcessorCount
            };
			return _blobRequestOptions;
		}
	}
}