using System.Collections.Generic;
using System.Linq;
using Autofac;
using BackupFolderAzureDurableFunctionDemo.Services.Modules;

namespace BackupFolderAzureDurableFunctionDemo.Services.Ioc
{
    public static class InjectConfiguration
    {
        public static void Initialize(List<Module> modules)
        {
            if (modules.All(module => module.GetType().FullName != typeof(CommonCoreModule).FullName))
            {
                modules.Add(new CommonCoreModule());
            }
            ServiceLocator.Initialize(modules);
        }
    }
}
