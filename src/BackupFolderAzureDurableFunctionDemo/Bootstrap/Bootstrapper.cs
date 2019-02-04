using Autofac;
using BackupFolderAzureDurableFunctionDemo.Services.Ioc;
using BackupFolderAzureDurableFunctionDemo.Services.Modules;

namespace BackupFolderAzureDurableFunctionDemo.Bootstrap
{
    public class Bootstrapper : IBootstrapper
    {
        public Module[] CreateModules()
        {
            return new Module[]
            {
                new CommonCoreModule(),
                new ServicesModule()
            };
        }
    }
}