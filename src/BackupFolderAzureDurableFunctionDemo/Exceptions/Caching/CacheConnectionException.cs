using System;

namespace BackupFolderAzureDurableFunctionDemo.Exceptions.Caching
{
    public class CacheConnectionException : ExceptionBase
    {
        public CacheConnectionException()
        {
        }

        public CacheConnectionException(string message)
            : base(message)
        {
        }

        public CacheConnectionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}