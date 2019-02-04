using System;
using Microsoft.Azure.WebJobs.Description;

namespace BackupFolderAzureDurableFunctionDemo.Services.Ioc
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public class InjectAttribute : Attribute
    {
    }
}
