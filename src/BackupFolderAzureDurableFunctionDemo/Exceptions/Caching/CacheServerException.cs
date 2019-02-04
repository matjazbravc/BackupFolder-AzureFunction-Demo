using System;

namespace BackupFolderAzureDurableFunctionDemo.Exceptions.Caching
{
    public class CacheServerException : ExceptionBase
    {
        public CacheServerException()
        {
        }

        public CacheServerException(string message)
            : base(message)
        {
        }

        public CacheServerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}