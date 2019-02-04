using System;
using System.Collections.Generic;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage
{
    public class TableStorageEntityAdapter<T> : ITableEntity 
        where T : TableEntityBase, new()
    {
        /// <summary>
        ///     Gets or sets the entity's partition key
        /// </summary>
        public string PartitionKey
        {
            get => InnerObject.PartitionKey;
            set => InnerObject.PartitionKey = value;
        }

        /// <summary>
        ///     Gets or sets the entity's row key.
        /// </summary>
        public string RowKey
        {
            get => InnerObject.RowKey;
            set => InnerObject.RowKey = value;
        }

        /// <summary>
        ///     Gets or sets the entity's Timestamp.
        /// </summary>
        public DateTimeOffset Timestamp
        {
            get => InnerObject.Timestamp;
            set => InnerObject.Timestamp = value;
        }

        /// <summary>
        ///     Gets or sets the entity's current ETag.
        ///     Set this value to '*' in order to blindly overwrite an entity as part of an update operation.
        /// </summary>
        public string ETag
        {
            get => InnerObject.ETag;
            set => InnerObject.ETag = value;
        }

        /// <summary>
        ///     Place holder for the original entity
        /// </summary>
        public T InnerObject { get; set; }

        public TableStorageEntityAdapter()
        {
            //InnerObject = new T();
            InnerObject = (T)Activator.CreateInstance(typeof(T));
        }

        public TableStorageEntityAdapter(T innerObject)
        {
            InnerObject = innerObject;
        }

        public virtual void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            TableEntity.ReadUserObject(InnerObject, properties, operationContext);
        }

        public virtual IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return TableEntity.WriteUserObject(InnerObject, operationContext);
        }
    }
}