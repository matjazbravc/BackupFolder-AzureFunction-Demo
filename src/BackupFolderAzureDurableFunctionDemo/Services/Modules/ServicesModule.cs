using Autofac;
using BackupFolderAzureDurableFunctionDemo.Services.Logging;
using BackupFolderAzureDurableFunctionDemo.Services.Logging.Serilog.Sinks;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;

namespace BackupFolderAzureDurableFunctionDemo.Services.Modules
{
    public class ServicesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var loggingStorageTableName = CloudConfigurationManager.GetSetting("Logging.Storage.TableName");
            var storageConnectingString = CloudConfigurationManager.GetSetting("StorageAccount.ConnectionString");
            var storageAccount = CloudStorageAccount.Parse(storageConnectingString);
            builder.Register(c => new SerilogToAzureTableStorage(nameof(ServicesModule), storageAccount, loggingStorageTableName)).As<ILog>();
        }
    }
}
