using System.ComponentModel;

namespace BackupFolderAzureDurableFunctionDemo.Services.Enums
{
    public enum EncodingType
    {
        [Description("gzip")]
        Gzip,
        [Description("deflate")]
        Deflate
    }
}
