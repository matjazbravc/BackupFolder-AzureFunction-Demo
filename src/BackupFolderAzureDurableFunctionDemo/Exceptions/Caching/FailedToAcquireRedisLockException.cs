using System;

namespace BackupFolderAzureDurableFunctionDemo.Exceptions.Caching
{
    public class FailedToAcquireRedisLockException : ExceptionBase
    {
        public FailedToAcquireRedisLockException()
        {
        }

        public FailedToAcquireRedisLockException(string message)
            : base(message)
        {
        }

        public FailedToAcquireRedisLockException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}