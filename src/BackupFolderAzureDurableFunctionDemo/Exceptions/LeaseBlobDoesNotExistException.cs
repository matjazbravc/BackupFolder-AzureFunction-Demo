using System;

namespace BackupFolderAzureDurableFunctionDemo.Exceptions
{
    public class LeaseBlobDoesNotExistException : ExceptionBase
    {
        public LeaseBlobDoesNotExistException()
        {
        }

        public LeaseBlobDoesNotExistException(string message)
            : base(message)
        {
        }

		public LeaseBlobDoesNotExistException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}