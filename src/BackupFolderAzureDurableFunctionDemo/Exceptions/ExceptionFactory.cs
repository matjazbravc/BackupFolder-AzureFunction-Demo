using System;
using BackupFolderAzureDurableFunctionDemo.Services.Logging;

namespace BackupFolderAzureDurableFunctionDemo.Exceptions
{
    public class ExceptionFactory
    {
        private readonly ILog _log;

        public ExceptionFactory(ILog log)
        {
          _log = log;
          _log.Debug();  
        }

		public Exception ConfigurationErrorsException(Exception ex, string message = "")
        {
            // Log original exception
            _log.Error(string.IsNullOrWhiteSpace(message) ? ex.Message : message, ex);
            return ex;
        }

		public Exception DeserializationException(Exception ex, string message = "")
		{
			// Log original exception
			_log.Error(string.IsNullOrWhiteSpace(message) ? ex.Message : message, ex);
			return ex;
		}
    }
}
