using System.Collections.Generic;
using System.IO;
using BackupFolderAzureDurableFunctionDemo.Services.Logging;

namespace BackupFolderAzureDurableFunctionDemo.Services.Helpers
{
    public class FileHelper : IFileHelper
    {
        private readonly ILog _log;

        public FileHelper(ILog log)
        {
            _log = log;
            _log.Debug();
        }

        public List<string> GetAllFilesFromDirectory(string root, bool searchSubDirectories = true)
        {
            _log.Debug();
            var files = new List<string>();
            var directories = new Queue<string>();
            directories.Enqueue(root);
            while (directories.Count != 0)
            {
                var currentDirectory = directories.Dequeue();
                try
                {
                    var filesInCurrentDirectory = Directory.GetFiles(currentDirectory, "*.*", SearchOption.TopDirectoryOnly);
                    files.AddRange(filesInCurrentDirectory);
                }
                catch
                {
                    // Do nothing
                }

                try
                {
                    if (searchSubDirectories)
                    {
                        var directoriesInCurrentDirectory = Directory.GetDirectories(currentDirectory, "*.*", SearchOption.TopDirectoryOnly);
                        foreach (var directory in directoriesInCurrentDirectory)
                        {
                            directories.Enqueue(directory);
                        }
                    }
                }
                catch
                {
                    // Do nothing
                }
            }

            return files;
        }
    }
}
