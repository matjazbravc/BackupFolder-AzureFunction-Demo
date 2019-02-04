using System;

namespace BackupFolderAzureDurableFunctionDemo.Exceptions
{
    [Serializable]
    public class CloudTableDoesNotExistException : ExceptionBase
    {
        public CloudTableDoesNotExistException()
        {
        }

        public CloudTableDoesNotExistException(string message)
            : base(message)
        {
        }

		public CloudTableDoesNotExistException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}