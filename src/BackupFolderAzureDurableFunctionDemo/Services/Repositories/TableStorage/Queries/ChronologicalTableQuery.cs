using System;
using BackupFolderAzureDurableFunctionDemo.Services.Enums;
using Microsoft.WindowsAzure.Storage.Table;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage.Queries
{
    public static class ChronologicalTableQuery
    {
		public static string GenerateFilterCondition(QueryDateChronologicalComparisons comparison, 
            DateTime date, bool useOnlyTicks = false)
        {
            var key = "";
            var queryComparison = QueryComparisons.Equal;
            switch (comparison)
            {
                case QueryDateChronologicalComparisons.After:
					key = RowKeysHelper.CreateChronologicalKeyStart(date.AddTicks(1), useOnlyTicks);
                    queryComparison = QueryComparisons.GreaterThan;
                    break;
                case QueryDateChronologicalComparisons.AfterOrEqual:
					key = RowKeysHelper.CreateChronologicalKeyStart(date, useOnlyTicks);
                    queryComparison = QueryComparisons.GreaterThan;
                    break;
                case QueryDateChronologicalComparisons.Before:
                    queryComparison = QueryComparisons.LessThan;
					key = RowKeysHelper.CreateChronologicalKeyStart(date, useOnlyTicks);
                    break;
                case QueryDateChronologicalComparisons.BeforeOrEqual:
                    queryComparison = QueryComparisons.LessThan;
					key = RowKeysHelper.CreateChronologicalKeyStart(date.AddTicks(1), useOnlyTicks);
                    break;
            }
            return TableQuery.GenerateFilterCondition("RowKey", queryComparison, key);
        }
    }
}