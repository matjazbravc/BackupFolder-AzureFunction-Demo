using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage.Queries
{
    // Some Basic Azure Table Storage Abstractions
    // inspired by: http://odetocode.com/blogs/scott/archive/2014/02/27/some-basic-azure-table-storage-abstractions.aspx
    public class TableStorageQuery<T> 
        where T : TableEntityBase, new()
	{
		protected TableQuery<TableStorageEntityAdapter<T>> Query;

		public TableStorageQuery()
		{
			Query = new TableQuery<TableStorageEntityAdapter<T>>();
		}

	    public virtual async Task<List<T>> ExecuteOnAsync(CloudTable cloudTable)
	    {
	        var results = new List<T>();
            var token = new TableContinuationToken();
	        var tableEntities = await cloudTable.ExecuteQuerySegmentedAsync(Query, token);
	        while (token != null)
	        {
	            results.AddRange(tableEntities.Select(tableEntity => tableEntity?.InnerObject));
	            token = tableEntities.ContinuationToken;
	            tableEntities = await cloudTable.ExecuteQuerySegmentedAsync(Query, token);
	        }
	        return results;
	    }
    }
}