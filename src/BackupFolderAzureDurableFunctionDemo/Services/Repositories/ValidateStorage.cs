using System;
using System.Text.RegularExpressions;
using BackupFolderAzureDurableFunctionDemo.Services.Logging;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories
{
	//inspired by: https://github.com/cmatskas/Azure.Storage/blob/master/Azure.Storage/Validate.cs
	public sealed class ValidateStorage
	{
	    private readonly ILog _log;

	    public ValidateStorage(ILog log)
	    {
	        _log = log;
            _log.Debug();
	    }

        // source: https://msdn.microsoft.com/en-us/library/azure/dd135715.aspx
        public void ValidateBlobContainerName(string paramValue, string paramName)
		{
		    _log.Debug();
			var regex = new Regex("^(?-i)(?:[a-z0-9]|(?<=[0-9a-z])-(?=[0-9a-z])){3,63}$", RegexOptions.Compiled);
			if (!regex.IsMatch(paramValue))
			{
				throw new ArgumentException("Blob container names must conform to these rules: " +
											"Must start with a letter or number, and can contain only letters, numbers, and the dash (-) character. " +
											"Every dash (-) character must be immediately preceded and followed by a letter or number; consecutive dashes are not permitted in container names. " +
											"All letters in a container name must be lowercase. " +
											"Must be from 3 to 63 characters long.", paramName ?? "");
			}
		}

		// source: https://msdn.microsoft.com/en-us/library/azure/dd135715.aspx
		public void ValidateBlobName(string paramValue, string paramName)
		{
		    _log.Debug();
			if (paramValue.Length > 1024)
			{
				throw new ArgumentException("Blob names must conform to these rules: " +
											"Must be from 1 to 1024 characters long.", paramName ?? "");
			}
		}

		public void ValidateQueueName(string paramValue, string paramName)
		{
		    _log.Debug();
            var regex = new Regex("^(?-i)(?:[a-z0-9]|(?<=[0-9a-z])-(?=[0-9a-z])){3,63}$", RegexOptions.Compiled);
			if (!regex.IsMatch(paramValue))
			{
				throw new ArgumentException("Queue names must conform to these rules: " +
											"Must start with a letter or number, and can contain only letters, numbers, and the dash (-) character. " +
											"The first and last letters in the queue name must be alphanumeric. The dash (-) character cannot be the first or last character. Consecutive dash characters are not permitted in the queue name. " +
											"All letters in a queue name must be lowercase. " +
											"Must be from 3 to 63 characters long.", paramName ?? "");
			}
		}

		public void ValidateTableName(string paramValue, string paramName)
		{
		    _log.Debug();
            var regex = new Regex("^[A-Za-z][A-Za-z0-9]{2,62}$", RegexOptions.Compiled);
			if (!regex.IsMatch(paramValue))
			{
				throw new ArgumentException("Table names must conform to these rules: " +
											"May contain only alphanumeric characters. " +
											"Cannot begin with a numeric character. " +
											"Are case-insensitive. " +
											"Must be from 3 to 63 characters long.", paramName ?? "");
			}
		}

		public void ValidateTablePropertyValue(string paramValue, string paramName)
		{
		    _log.Debug();
            var regex = new Regex(@"^[^/\\#?]{0,1024}$", RegexOptions.Compiled);
			if (!regex.IsMatch(paramValue))
			{
				throw new ArgumentException("Table property values must conform to these rules: " +
											"Must not contain the forward slash (/), backslash (\\), number sign (#), or question mark (?) characters. " +
											"Must be from 1 to 1024 characters long.", paramName ?? "");
			}
		}
	}
}