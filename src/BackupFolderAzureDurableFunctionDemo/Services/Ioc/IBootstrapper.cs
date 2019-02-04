using Autofac;

namespace BackupFolderAzureDurableFunctionDemo.Services.Ioc
{
    public interface IBootstrapper
    {
        Module[] CreateModules();
    }
}