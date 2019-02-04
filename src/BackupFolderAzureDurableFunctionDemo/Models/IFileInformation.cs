using System;

namespace BackupFolderAzureDurableFunctionDemo.Models
{
    public interface IFileInformation
    {
        string Id { get; set; }

        string FileName { get; set; }

        DateTime ModifiedDate { get; set; }
    }
}
