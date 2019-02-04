using System;
using System.ComponentModel;
using BackupFolderAzureDurableFunctionDemo.Services.Logging;
using Microsoft.Azure;

namespace BackupFolderAzureDurableFunctionDemo.Services.Helpers
{
    public sealed class ConfigHelper
    {
        private readonly ILog _log;

        public ConfigHelper(ILog log)
        {
            _log = log;
            _log.Debug();
        }

        /// <summary>
        /// Generic method for reading Cloud/App settings
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">Setting name/key</param>
        /// <param name="defaultValue"></param>
        /// <returns>Result T</returns>
        public T GetSetting<T>(string name, T defaultValue = default(T))
        {
            _log.Debug();
            var result = defaultValue;
            var value = CloudConfigurationManager.GetSetting(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return result;
            }
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                result = (T)converter.ConvertFromInvariantString(value);
            }
            catch (Exception)
            {
                result = defaultValue;
            }
            return result;
        }
    }
}