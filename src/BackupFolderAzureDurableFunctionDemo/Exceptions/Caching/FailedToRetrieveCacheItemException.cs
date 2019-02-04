using System;

namespace BackupFolderAzureDurableFunctionDemo.Exceptions.Caching
{
    public class FailedToRetrieveCacheItemException : ExceptionBase
    {
        public FailedToRetrieveCacheItemException()
        {
        }

        public FailedToRetrieveCacheItemException(string message)
            : base(message)
        {
        }

        public FailedToRetrieveCacheItemException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}