using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage
{
    public interface IRepository<T>
        where T : class
    {
        Task<bool> DeleteAsync(string blobName);

        Task<T> GetAsync(string blobName);

        Task<List<T>> ListAsync(string prefix = "");

        Task<CloudBlockBlob> SetAsync(string blobName, T value);
    }
}
