# Backup folder with usage of Azure Durable Function

[Durable Functions](https://docs.microsoft.com/en-us/azure/azure-functions/durable-functions-overview) is an extension of Azure Functions and Azure WebJobs that lets you write stateful functions in a serverless environment. The primary use case for Durable Functions is simplifying complex, stateful coordination problems in serverless applications.

This application demonstrate following functionalities:
- usage of Azure durable functions,
- dependency injection with [Autofac](https://autofac.org/) (inspired by [Holger Leichsenring Blog Post](http://codingsoul.de/2018/01/19/azure-function-dependency-injection-with-autofac/)),
- logging with [Serilog](https://serilog.net/) sink to Azure Table storage,
- using Table storage as an temporary repository,
- copying files to blobs

Because of cross function communication limitations I've used Table Storage as an temporary repository for list of all files to be backed up. Sure, there is a number of ways to do this but I've choose Table Storage.

## Prerequisites
- [Visual Studio](https://www.visualstudio.com/vs/community) 2017 15.5.5 or greater
- Azure Storage Emulator (the Storage Emulator is available as part of the [Microsoft Azure SDK](https://azure.microsoft.com/en-us/downloads/))

To create and deploy functions, you also need:
- An active Azure subscription. If you don't have an Azure subscription, [free accounts](https://azure.microsoft.com/en-us/free/) are available.
- An Azure Storage account. To create a storage account, see [Create a storage account](https://docs.microsoft.com/en-us/azure/storage/common/storage-create-storage-account#create-a-storage-account).

## Let's get started!
First, you have to enter your Azure storage account connection string (and other settings) into **_local.settings.json_**:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "AzureWebJobsDashboard": "UseDevelopmentStorage=true",
    "Logging.Storage.TableName": "BackupFolderLog",
    "BackupFolder.TableName": "BackupFolder",
    "BackupFolder.ContainerName": "backupfolder",
    "StorageAccount.ConnectionString": "UseDevelopmentStorage=true"
  }
}
```
P.S. More about how to test Azure functions locally you can read [here](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local).

Second, if you use development storage you have to setup Azure Storage Emulator. See [Use the Azure storage emulator for development and testing](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator) and [Configuring and Using the Storage Emulator with Visual Studio](https://docs.microsoft.com/en-us/azure/vs-azure-tools-storage-emulator-using).

Third, build solution and run it with local Azure Functions runtime: 
![](https://github.com/mabravc/BackupFolderAzureDurableFunctionDemo/blob/master/res/function_local_runtime_1.jpg)

And when function is finished, you should receive succeed status:
![](https://github.com/mabravc/BackupFolderAzureDurableFunctionDemo/blob/master/res/function_local_runtime_2.jpg)

Finally test it with [Postman](https://getpostman.com):
![](https://github.com/mabravc/BackupFolderAzureDurableFunctionDemo/blob/master/res/postman_function_test.jpg)

Results you can check easy with excellent Azure Storage Explorer [(you can download it for free)](https://azure.microsoft.com/en-us/features/storage-explorer/):
![](https://github.com/mabravc/BackupFolderAzureDurableFunctionDemo/blob/master/res/ms_storage_emulator_1.jpg)
![](https://github.com/mabravc/BackupFolderAzureDurableFunctionDemo/blob/master/res/ms_storage_emulator_2.jpg)

Enjoy!

## Licence

Licenced under [MIT](http://opensource.org/licenses/mit-license.php).
Developed by [Matjaž Bravc](https://si.linkedin.com/in/matjazbravc)
