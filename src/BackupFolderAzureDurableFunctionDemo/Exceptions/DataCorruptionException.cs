using System;

namespace BackupFolderAzureDurableFunctionDemo.Exceptions
{
    [Serializable]
    public class DataCorruptionException : ExceptionBase
    {
        public DataCorruptionException()
        {
        }

        public DataCorruptionException(string message)
            : base(message)
        {
        }

		public DataCorruptionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}