using System;
using System.Collections.Generic;
using System.IO;
using BackupFolderAzureDurableFunctionDemo.Exceptions;
using BackupFolderAzureDurableFunctionDemo.Services.Helpers;
using Microsoft.WindowsAzure.Storage.Blob;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Helpers
{
    public sealed class CloudBlockBlobMd5Helper
    {
        // The integrity during the entire roundtrip, we need to apply a suplementary header 
        // used to perform the MD5 check
        public const string METADATA_MD5_KEY = "CloudBlockBlobContentMD5";
        private readonly CryptographyHelper _cryptographyHelper;
        private readonly BlobRequestOptionsHelper _blobRequestOptionsHelper;

        public CloudBlockBlobMd5Helper(CryptographyHelper cryptographyHelper, 
            BlobRequestOptionsHelper blobRequestOptionsHelper)
        {
            _cryptographyHelper = cryptographyHelper;
            _blobRequestOptionsHelper = blobRequestOptionsHelper;
        }

        /// <summary>
        /// Apply a content hash to the blob to verify upload and roundtrip consistency.
        /// </summary>
        /// <param name="blob">Cloud Blob</param>
        /// <param name="stream">Stream value</param>
        public void ApplyContentMd5Hash(ICloudBlob blob, Stream stream)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var hash = _cryptographyHelper.ComputeStreamMd5Hash(stream);
            blob.FetchAttributesAsync(null, _blobRequestOptionsHelper.Get(), null).Wait();

            // StorageClient does not provide a way to retrieve MD5 so we add our own MD5 check
            // which let perform our own validation when downloading the blob
            if (blob.Metadata.ContainsKey(METADATA_MD5_KEY))
            {
                blob.Metadata[METADATA_MD5_KEY] = hash;
            }
            else
            {
                blob.Metadata.Add(new KeyValuePair<string, string>(METADATA_MD5_KEY, hash));
            }

            blob.SetMetadataAsync().Wait();
        }

        /// <summary>
        /// Apply a content hash to the blob to verify upload and roundtrip consistency.
        /// </summary>
        /// <param name="blob">Cloud Blob</param>
        /// <param name="byteArray">Byte Array value</param>
        public void ApplyContentMd5Hash(ICloudBlob blob, byte[] byteArray)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }
            if (byteArray == null)
            {
                throw new ArgumentNullException(nameof(byteArray));
            }

            string hash;
            using (var stream = new MemoryStream(byteArray))
            {
                hash = _cryptographyHelper.ComputeStreamMd5Hash(stream);
            }

            blob.FetchAttributesAsync(null, _blobRequestOptionsHelper.Get(), null).Wait();

            // StorageClient does not provide a way to retrieve MD5 so we add our own MD5 check
            // which let perform our own validation when downloading the blob
            if (blob.Metadata.ContainsKey(METADATA_MD5_KEY))
            {
                blob.Metadata[METADATA_MD5_KEY] = hash;
            }
            else
            {
                blob.Metadata.Add(new KeyValuePair<string, string>(METADATA_MD5_KEY, hash));
            }

            blob.SetMetadataAsync().Wait();
        }

        /// <summary>
        /// Throws a DataCorruptionException if the content hash is available but doesn't match.
        /// </summary>
        /// <param name="blob">Cloud Blob</param>
        /// <param name="fileName">File name</param>
        public void VerifyContentMd5Hash(ICloudBlob blob, string fileName)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            var expectedHash = GetBlobHash(blob);
            if (string.IsNullOrWhiteSpace(expectedHash))
            {
                throw new DataCorruptionException($"Blob MD5 content hash is null or empty {blob?.Name}. Data is corrupted!");
            }

            var computedHash = _cryptographyHelper.ComputeFileMd5Hash(fileName);
            if (expectedHash != computedHash)
            {
                throw new DataCorruptionException($"Blob MD5 content hash mismatch {fileName}. Data is corrupted! Expected hash: {expectedHash}, computed: {computedHash}");
            }
        }

        public bool BlobIsDownloaded(ICloudBlob blob, string fileName)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            var computedHash = _cryptographyHelper.ComputeFileMd5Hash(fileName);
            if (string.IsNullOrWhiteSpace(computedHash))
            {
                return false;
            }

            var expectedHash = GetBlobHash(blob);
            if (string.IsNullOrWhiteSpace(expectedHash))
            {
                return false;
            }

            if (expectedHash == computedHash)
            {
                return true;
            }
            return false;
        }

        public string GetBlobHash(ICloudBlob blob)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }

            blob.FetchAttributesAsync(null, _blobRequestOptionsHelper.Get(), null).Wait();
            var result = string.Empty;
            if (blob.Metadata.ContainsKey(METADATA_MD5_KEY))
            {
                result = blob.Metadata[METADATA_MD5_KEY];
            }
            return result;
        }
    }
}
