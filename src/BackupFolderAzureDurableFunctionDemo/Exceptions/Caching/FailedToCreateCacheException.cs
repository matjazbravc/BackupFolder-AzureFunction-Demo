using System;

namespace BackupFolderAzureDurableFunctionDemo.Exceptions.Caching
{
    public class FailedToCreateCacheException : ExceptionBase
    {
        public FailedToCreateCacheException()
        {
        }

        public FailedToCreateCacheException(string message)
            : base(message)
        {
        }

        public FailedToCreateCacheException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}