﻿using System.ComponentModel;

namespace BackupFolderAzureDurableFunctionDemo.Services.Enums
{
    public enum ContentType
    {
        [Description("application/xml")]
        ApplicationXml,
        [Description("application/json")]
        ApplicationJson,
        [Description("application/yaml")]
        ApplicationYaml,
        [Description("application/x-yaml")]
        ApplicationXYaml,
        [Description("text/yaml")]
        ApplicationTextYaml
    }
}
