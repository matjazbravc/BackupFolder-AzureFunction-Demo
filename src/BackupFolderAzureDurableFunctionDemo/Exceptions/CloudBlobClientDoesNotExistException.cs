using System;

namespace BackupFolderAzureDurableFunctionDemo.Exceptions
{
    [Serializable]
    public class CloudBlobClientDoesNotExistException : ExceptionBase
    {
        public CloudBlobClientDoesNotExistException()
        {
        }

        public CloudBlobClientDoesNotExistException(string message)
            : base(message)
        {
        }

		public CloudBlobClientDoesNotExistException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}