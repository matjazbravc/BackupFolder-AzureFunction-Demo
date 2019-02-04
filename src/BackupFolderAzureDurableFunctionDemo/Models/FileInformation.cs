using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage.Models;

namespace BackupFolderAzureDurableFunctionDemo.Models
{
    [DataContract, Serializable]
    public class FileInformation : TableEntityBase, IFileInformation
    {
        public FileInformation()
        {
            Id = RowKey;
        }

        [Key]
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "filename")]
        public string FileName { get; set; }

        [DataMember(Name = "modifieddate")]
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }
}