using System.Collections.Generic;

namespace BackupFolderAzureDurableFunctionDemo.Services.Helpers
{
    public interface IFileHelper
    {
        List<string> GetAllFilesFromDirectory(string root, bool searchSubfolders = true);
    }
}
