using System;

namespace BackupFolderAzureDurableFunctionDemo.Exceptions.Caching
{
    public class FailedToSetCacheItemException : ExceptionBase
    {
        public FailedToSetCacheItemException()
        {
        }

        public FailedToSetCacheItemException(string message)
            : base(message)
        {
        }

        public FailedToSetCacheItemException(Exception innerException)
            : base(innerException)
        {
        }

		public FailedToSetCacheItemException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
    }
}