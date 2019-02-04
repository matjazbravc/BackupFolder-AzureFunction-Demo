using BackupFolderAzureDurableFunctionDemo.Services.Logging.Serilog.Extensions;
using Microsoft.WindowsAzure.Storage;
using Serilog;
using Serilog.Exceptions;

namespace BackupFolderAzureDurableFunctionDemo.Services.Logging.Serilog.Sinks
{
    public sealed class SerilogToAzureTableStorage : LoggerBase
    {
        public SerilogToAzureTableStorage(string name, CloudStorageAccount storageAccount, string storageTableName)
        {
            Logger = new LoggerConfiguration()
                .Enrich.WithExceptionDetails()
                .WriteTo.AzureTableStorage(storageAccount, storageTableName: storageTableName)
                .CreateLogger().ForContext("SourceContext", name);
        }
    }
}