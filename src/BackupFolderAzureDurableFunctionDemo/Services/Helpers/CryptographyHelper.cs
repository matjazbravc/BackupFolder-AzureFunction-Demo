using System;
using System.IO;
using System.Security.Cryptography;

namespace BackupFolderAzureDurableFunctionDemo.Services.Helpers
{
    public sealed class CryptographyHelper
    {
        public string ComputeStreamMd5Hash(Stream stream)
        {
            string result = null;
            using (var md5 = MD5.Create())
            {
                if (stream != null && stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }
                if (stream != null)
                {
                    result = Convert.ToBase64String(md5.ComputeHash(stream));
                }
            }
            return result;
        }

        public string ComputeFileMd5Hash(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return string.Empty;
            }
            string result;
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                try
                {
                    fileStream.Position = 0;
                    using (var md5 = MD5.Create())
                    {
                        result = Convert.ToBase64String(md5.ComputeHash(fileStream));
                    }
                }
                finally
                {
                    fileStream.Close();
                }
            }
            return result;
        }
    }
}
