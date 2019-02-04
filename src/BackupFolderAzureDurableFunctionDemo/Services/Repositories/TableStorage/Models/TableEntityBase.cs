using System;
using System.Runtime.Serialization;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage.Models
{
    // inspired by: http://www.amithegde.com/2015/06/decoupling-tableentity-while-using-azure-storage.html
    [Serializable]
    public class TableEntityBase
    {
        private const string DEFAULT_PARTITION_KEY = "Default";

        public TableEntityBase()
        {
        }

        public TableEntityBase(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        [DataMember(Name = "etag")]
        public string ETag { get; set; }

        [DataMember(Name = "partitionkey")]
        public string PartitionKey { get; set; } = DEFAULT_PARTITION_KEY;

        [DataMember(Name = "rowkey")]
        public string RowKey { get; set; } = $"{(DateTime.MaxValue - DateTime.UtcNow).Ticks:D19}";

        [DataMember(Name = "etag")]
        public DateTimeOffset Timestamp { get; set; }
    }
}