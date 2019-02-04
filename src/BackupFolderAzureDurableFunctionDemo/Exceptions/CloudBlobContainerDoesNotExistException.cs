using System;

namespace BackupFolderAzureDurableFunctionDemo.Exceptions
{
    public class CloudBlobContainerDoesNotExistException : ExceptionBase
    {
        public CloudBlobContainerDoesNotExistException()
        {
        }

        public CloudBlobContainerDoesNotExistException(string message)
            : base(message)
        {
        }

		public CloudBlobContainerDoesNotExistException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}