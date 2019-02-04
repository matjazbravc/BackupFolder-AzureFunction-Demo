using System;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage.Helpers
{
	public sealed class TableRequestOptionsHelper
	{
		private TableRequestOptions _tableRequestOptions;

		public TableRequestOptions Get(bool forceNew = false)
		{
			if (_tableRequestOptions != null && !forceNew)
			{
				return _tableRequestOptions;
			}
			_tableRequestOptions = new TableRequestOptions
			{
				ServerTimeout = TimeSpan.FromMinutes(5),
				MaximumExecutionTime = TimeSpan.FromMinutes(15),
				RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(10), 5)
			};
			return _tableRequestOptions;
		}
	}
}
