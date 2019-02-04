using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BackupFolderAzureDurableFunctionDemo.Exceptions;
using BackupFolderAzureDurableFunctionDemo.Services.Logging;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage.Config;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage.Helpers;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.Protocol;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage
{
    public class RepositoryBase<T> : IRepository<T>
        where T : TableEntityBase, new()
    {
        private readonly object _lockObject = new object();
        private readonly TableRequestOptionsHelper _tableRequestOptionsHelper;
        private readonly ValidateStorage _validateStorage;
        private string _connectionString;

        public RepositoryBase(ILog log, ValidateStorage validateStorage,
            TableRequestOptionsHelper tableRequestOptionsHelper, IRepositoryConfig repositoryConfig)
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
            _tableRequestOptionsHelper = tableRequestOptionsHelper;
            Config = repositoryConfig;
        }

        public CloudTable CloudTable { get; set; }

        public CloudTableClient CloudTableClient { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Table Repository configuration
        /// </summary>
        public IRepositoryConfig Config { get; set; }

        public async Task<bool> CreateTableIfNotExistsAsync(TimeSpan? timeout = null)
        {
            ThrowWhenCloudTableDoesNotExist();
            var result = false;
            var success = false;
            var realTimeout = timeout ?? TimeSpan.FromSeconds(30);
            var retryTimeout = DateTime.UtcNow.Add(realTimeout);
            do
            {
                try
                {
                    result = await CloudTable.CreateIfNotExistsAsync(_tableRequestOptionsHelper.Get(), null).ConfigureAwait(false);
                    success = true;
                }
                catch (StorageException ex)
                {
                    if (ex.RequestInformation.HttpStatusCode == 409 &&
                        ex.RequestInformation.ExtendedErrorInformation.ErrorCode.Equals(TableErrorCodeStrings.TableBeingDeleted))
                    {
                        Thread.Sleep(1000); // The table is currently being deleted. Try again until it works or retry timeout
                    }
                    else
                    {
                        throw;
                    }
                }
            } while (DateTime.UtcNow < retryTimeout && !success);
            return result;
        }

        /// <summary>
        /// Delete all table entries
        /// </summary>
        /// <returns></returns>
        public async Task DeleteAllAsync()
        {
            ThrowWhenCloudTableDoesNotExist();
            await ProcessEntitiesAsync(DeleteProcessorAsync).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete single table entry specified with partitionKey and rowKey
        /// </summary>
        /// <param name="partitionKey"></param>
        /// <param name="rowKey"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(string partitionKey, string rowKey)
        {
            ThrowWhenCloudTableDoesNotExist();
            bool result;
            try
            {
                var tableEntity = new TableEntity(partitionKey, rowKey)
                {
                    ETag = "*"
                };
                var tableResult = await CloudTable.ExecuteAsync(TableOperation.Delete(tableEntity), _tableRequestOptionsHelper.Get(), null);
                result = tableResult.HttpStatusCode == (int)HttpStatusCode.NoContent;
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    result = false;
                }
                else
                {
                    throw;
                }
            }
            return result;
        }

        public async Task DeleteAsync(Expression<Func<DynamicTableEntity, bool>> filter)
        {
            ThrowWhenCloudTableDoesNotExist();
            await ProcessEntitiesAsync(DeleteProcessorAsync, filter).ConfigureAwait(false);
        }

        // inspired by: http://www.wintellect.com/devcenter/jlane/deleting-entities-in-windows-azure-table-storage
        public async Task DeleteByPartitionKeyAsync(string partitionKey)
        {
            ThrowWhenCloudTableDoesNotExist();
            Expression<Func<DynamicTableEntity, bool>> filter = entity => entity.PartitionKey == partitionKey;
            await ProcessEntitiesAsync(DeleteProcessorAsync, filter).ConfigureAwait(false);
        }

        public async Task DeleteByRowKeyAsync(string rowKey)
        {
            ThrowWhenCloudTableDoesNotExist();
            Expression<Func<DynamicTableEntity, bool>> filter = entity => entity.RowKey == rowKey;
            await ProcessEntitiesAsync(DeleteProcessorAsync, filter).ConfigureAwait(false);
        }

        public async Task<bool> DeleteTableIfExistsAsync()
        {
            ThrowWhenCloudTableDoesNotExist();
            var result = await CloudTable.DeleteIfExistsAsync(_tableRequestOptionsHelper.Get(), null).ConfigureAwait(false);
            CloudTable = null;
            return result;
        }

        public async Task<bool> ExistsAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowWhenCloudTableDoesNotExist();
            var retrievedResult = await GetAsync(partitionKey, rowKey, cancellationToken).ConfigureAwait(false);
            return retrievedResult != null;
        }

        /// <summary>
        /// Returns all table entries in the Cloud table
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of entities</returns>
        public async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = new TableQuery<TableStorageEntityAdapter<T>>();
            var result = await GetAsync(query, cancellationToken).ConfigureAwait(false);
            return result;
        }

        public async Task<T> GetAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowWhenCloudTableDoesNotExist();
            try
            {
                var tableResult = await CloudTable.ExecuteAsync(TableOperation.Retrieve<TableStorageEntityAdapter<T>>(partitionKey, rowKey), null, null, cancellationToken).ConfigureAwait(false);
                var result = ((TableStorageEntityAdapter<T>)tableResult.Result)?.InnerObject;
                return result;
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return default(T);
                }
                throw;
            }
        }

        /// <summary>
        /// Returns all table entries filtered by table query
        /// </summary>
        /// <param name="query">Defined TableQuery</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of T</returns>
        // Usage: 
        // var filter = TableQuery.GenerateFilterConditionForInt("Year", QueryComparisons.Equal, 2017);
        // var query = new TableQuery<TableStorageEntityAdapter<NavitaireCall>>().Where(filter).Take(100);
        // var entries = navitaireCallStatisticsTableRepository.GetAsync(query).Result;
        public async Task<List<T>> GetAsync(TableQuery<TableStorageEntityAdapter<T>> query, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowWhenCloudTableDoesNotExist();
            var result = new List<T>();
            TableContinuationToken continuationToken = null;
            try
            {
                do
                {
                    var tableEntities = await CloudTable.ExecuteQuerySegmentedAsync(query, continuationToken, null, null, cancellationToken);
                    result.AddRange(tableEntities.Select(tableEntity => tableEntity?.InnerObject));
                    continuationToken = tableEntities.ContinuationToken;
                } while (continuationToken != null && !cancellationToken.IsCancellationRequested);
                return result;
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return result;
                }
                throw;
            }
        }

        /// <summary>
        /// Returns all table entries with specific partition key
        /// </summary>
        /// <param name="partitionKey">Defined partition key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of T</returns>
        public async Task<List<T>> GetByPartitionKeyAsync(string partitionKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowWhenCloudTableDoesNotExist();
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            var query = new TableQuery<TableStorageEntityAdapter<T>>().Where(filter);
            var result = await GetAsync(query, cancellationToken);
            return result;
        }

        /// <summary>
        /// Returns all table entries with specific partition key
        /// </summary>
        /// <param name="rowKey">Defined row key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of T</returns>
        public async Task<List<T>> GetByRowKeyAsync(string rowKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowWhenCloudTableDoesNotExist();
            var filter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey);
            var query = new TableQuery<TableStorageEntityAdapter<T>>().Where(filter);
            var result = await GetAsync(query, cancellationToken);
            return result;
        }

        public void GetOrCreateCloudTableClient()
        {
            lock (_lockObject)
            {
                if (CloudTableClient != null)
                {
                    return;
                }
                if (string.IsNullOrEmpty(_connectionString))
                {
                    _connectionString = Config.StorageAccountConnectionString;
                }
                Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(_connectionString), "StorageAccountConnectionString cannot be null or empty");
                var cloudStorageAccount = CloudStorageAccount.Parse(_connectionString);
                CloudTableClient = cloudStorageAccount.CreateCloudTableClient();
                CloudTableClient.DefaultRequestOptions = _tableRequestOptionsHelper.Get();
            }
        }

        public void GetOrCreateCloudTableReference()
        {
            lock (_lockObject)
            {
                if (string.IsNullOrWhiteSpace(Config.TableName))
                {
                    throw new ApplicationException("Config.TableName is null or empty");
                }
                if (CloudTable != null && CloudTable.Name == Config.TableName)
                {
                    return;
                }
                _validateStorage.ValidateTableName(Config.TableName, nameof(Config.TableName));
                CloudTable = CloudTableClient.GetTableReference(Config.TableName);
            }
        }

        /// <summary>
        /// Base initialization - Client and Table creation
        /// </summary>
        /// <param name="createTableIfNotExists">Flag if table should be created</param>
        public void Initialize(bool createTableIfNotExists = true)
        {
            GetOrCreateCloudTableClient();
            GetOrCreateCloudTableReference();
            if (createTableIfNotExists)
            {
                CreateTableIfNotExistsAsync().Wait();
            }
        }

        public async Task<bool> SetAsync(T instance)
        {
            ThrowWhenCloudTableDoesNotExist();
            bool result;
            try
            {
                var tableEntity = new TableStorageEntityAdapter<T>(instance)
                {
                    ETag = "*"
                };
                var tableResult = await CloudTable.ExecuteAsync(TableOperation.InsertOrReplace(tableEntity));
                result = tableResult.HttpStatusCode == (int)HttpStatusCode.NoContent;
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    result = false;
                }
                else
                {
                    throw;
                }
            }
            return result;
        }

        public async Task SetAsync(IEnumerable<T> instances)
        {
            ThrowWhenCloudTableDoesNotExist();
            var batches = new List<TableBatchOperation>();
            var partitionGroups = instances.GroupBy(arg => arg.PartitionKey).ToArray();
            foreach (var group in partitionGroups)
            {
                var groupList = group.ToArray();
                var offSet = 100;
                var entities = groupList.Take(offSet).ToArray();
                while (entities.Any())
                {
                    var tableBatchOperation = new TableBatchOperation();
                    foreach (var entity in entities)
                    {
                        var tableEntity = new TableStorageEntityAdapter<T>(entity)
                        {
                            ETag = "*"
                        };
                        tableBatchOperation.Add(TableOperation.InsertOrReplace(tableEntity));
                    }
                    batches.Add(tableBatchOperation);
                    entities = groupList.Skip(offSet).Take(100).ToArray();
                    offSet += 100;
                }
            }
            await Task.WhenAll(batches.Select(CloudTable.ExecuteBatchAsync));
        }

        public async Task<bool> TableExistsAsync()
        {
            ThrowWhenCloudTableDoesNotExist();
            return await CloudTable.ExistsAsync(_tableRequestOptionsHelper.Get(), null);
        }

        public async Task<bool> TableIsEmptyAsync()
        {
            ThrowWhenCloudTableDoesNotExist();
            var tableQuery = new TableQuery<TableStorageEntityAdapter<T>>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, string.Empty));
            var result = await GetAsync(tableQuery);
            return !result.Any();
        }

        /// <summary>
        /// Updates table entity with changed properties
        /// </summary>
        /// <param name="partitionKey">Partition key</param>
        /// <param name="rowKey">Row key</param>
        /// <param name="properties">Dictionary with properties</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>TableResult</returns>
        // Example:
        // var updatedProperties = new Dictionary<string, EntityProperty>
        // {
        //     { "Processed", new EntityProperty(processed) },
        //     { "ProcessingTime", new EntityProperty(processingTime) }
        // };
        // await UpdateAsync(exportId, routeKey, updatedProperties);
        public async Task<TableResult> UpdateAsync(string partitionKey, string rowKey, Dictionary<string, EntityProperty> properties, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowWhenCloudTableDoesNotExist();
            var updatedEntity = new DynamicTableEntity(partitionKey, rowKey)
            {
                Properties = properties,
                ETag = "*"
            };
            try
            {
                return await CloudTable.ExecuteAsync(TableOperation.Merge(updatedEntity), null, null, cancellationToken).ConfigureAwait(false);
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return null;
                }
                throw;
            }
        }

        private async Task DeleteProcessorAsync(IEnumerable<DynamicTableEntity> items)
        {
            var list = new List<TableBatchOperation>();
            var partitionGroups = items.GroupBy(arg => arg.PartitionKey).ToArray();
            foreach (var group in partitionGroups)
            {
                var groupList = group.ToArray();
                var offSet = 100;
                var entities = groupList.Take(offSet).ToArray();
                while (entities.Any())
                {
                    var tableBatchOperation = new TableBatchOperation();
                    foreach (var entity in entities)
                    {
                        tableBatchOperation.Add(TableOperation.Delete(entity));
                    }
                    list.Add(tableBatchOperation);
                    entities = groupList.Skip(offSet).Take(100).ToArray();
                    offSet += 100;
                }
            }
            await Task.WhenAll(list.Select(CloudTable.ExecuteBatchAsync));
        }

        private async Task ProcessEntitiesAsync(Func<IEnumerable<DynamicTableEntity>, Task> processor, Expression<Func<DynamicTableEntity, bool>> filter = null)
        {
            ThrowWhenCloudTableDoesNotExist();
            TableQuerySegment<DynamicTableEntity> segment = null;
            while (segment == null || segment.ContinuationToken != null)
            {
                if (filter == null)
                {
                    segment = await CloudTable.ExecuteQuerySegmentedAsync(new TableQuery<DynamicTableEntity>().Take(100), segment?.ContinuationToken).ConfigureAwait(false);
                }
                else
                {
                    var leftValue = "";
                    var rightValue = "";
                    if (filter.Body is BinaryExpression body)
                    {
                        if (body.Left is MemberExpression left)
                        {
                            leftValue = left.Member.Name;
                        }
                        if (body.Right is ConstantExpression right)
                        {
                            rightValue = right.Value.ToString();
                        }
                    }
                    var query = TableQuery.GenerateFilterCondition(leftValue, ConvertOperator(filter.Body), rightValue);
                    segment = await CloudTable.ExecuteQuerySegmentedAsync(new TableQuery<DynamicTableEntity>().Where(query).Take(100), segment?.ContinuationToken).ConfigureAwait(false);
                }
                processor?.Invoke(segment.Results);
            }
        }

        private static string ConvertOperator(Expression binaryExpression)
        {
            switch (binaryExpression.NodeType)
            {
                case ExpressionType.Equal:
                    return "eq";
                case ExpressionType.GreaterThan:
                    return "gt";
                case ExpressionType.GreaterThanOrEqual:
                    return "ge";
                case ExpressionType.LessThan:
                    return "lt";
                case ExpressionType.LessThanOrEqual:
                    return "le";
                default:
                    return "eq";
            }
        }

        private void ThrowWhenCloudTableDoesNotExist([CallerMemberName] string memberName = "")
        {
            // inspired by: http://www.kunal-chowdhury.com/2012/07/whats-new-in-c-50-learn-about.html
            if (CloudTable != null)
            {
                return;
            }
            var exceptionMessage = $"{memberName}: before usage, Initialize must be called to initialize the CloudTable";
            throw new CloudTableDoesNotExistException(exceptionMessage);
        }
    }
}