using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage.Config;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage
{
    public interface IRepository<T>
        where T : TableEntityBase, new()
    {
        IRepositoryConfig Config { get; set; }

        Task<bool> CreateTableIfNotExistsAsync(TimeSpan? timeout = null);

        Task DeleteAllAsync();

        Task DeleteAsync(Expression<Func<DynamicTableEntity, bool>> filter);

        Task<bool> DeleteAsync(string partitionKey, string rowKey);

        Task DeleteByPartitionKeyAsync(string partitionKey);

        Task DeleteByRowKeyAsync(string rowKey);

        Task<bool> DeleteTableIfExistsAsync();

        Task<bool> ExistsAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default(CancellationToken));

        Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default(CancellationToken));

        Task<T> GetAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default(CancellationToken));

        Task<List<T>> GetAsync(TableQuery<TableStorageEntityAdapter<T>> query, CancellationToken cancellationToken = default(CancellationToken));

        Task<List<T>> GetByPartitionKeyAsync(string partitionKey, CancellationToken cancellationToken = default(CancellationToken));

        Task<List<T>> GetByRowKeyAsync(string rowKey, CancellationToken cancellationToken = default(CancellationToken));

        void Initialize(bool createTableIfNotExists = true);

        Task<bool> SetAsync(T instance);

        Task SetAsync(IEnumerable<T> instances);

        Task<bool> TableExistsAsync();

        Task<bool> TableIsEmptyAsync();

        Task<TableResult> UpdateAsync(string partitionKey, string rowKey, Dictionary<string, EntityProperty> properties, CancellationToken cancellationToken = default(CancellationToken));
    }
}