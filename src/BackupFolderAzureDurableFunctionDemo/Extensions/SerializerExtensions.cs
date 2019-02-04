using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using SerializationBinder = System.Runtime.Serialization.SerializationBinder;

namespace BackupFolderAzureDurableFunctionDemo.Extensions
{
    public static class SerializerExtensions
    {
        public static T DeserializeWithDecompression<T>(this byte[] buffer, SerializationBinder serializationBinder = null)
        {
            using (var compressedMemoryStream = new MemoryStream(buffer))
            {
                using (var decompressedMemoryStream = new MemoryStream())
                {
                    using (var deCompressor = new GZipStream(compressedMemoryStream, CompressionMode.Decompress))
                    {
                        IFormatter formatter = new BinaryFormatter();
                        using (var buffStream = new BufferedStream(deCompressor, 65536))
                        {
                            buffStream.CopyTo(decompressedMemoryStream);
                            decompressedMemoryStream.Position = 0;
                            // To prevent errors serializing between version number differences (e.g. Version 1 serializes, and Version 2 deserializes)
                            // inspired by: http://spazzarama.com/2009/06/25/binary-deserialize-unable-to-find-assembly/
                            formatter.Binder = serializationBinder;
                            var result = (T)formatter.Deserialize(decompressedMemoryStream);
                            buffStream.Close();
                            return result;
                        }
                    }
                }
            }
        }

        public static T DeserializeWithDecompressionFromMemoryStream<T>(this MemoryStream memoryStream, SerializationBinder serializationBinder = null)
        {
            using (var decompressedMemoryStream = new MemoryStream())
            {
                using (var deCompressor = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    IFormatter formatter = new BinaryFormatter();
                    using (var buffStream = new BufferedStream(deCompressor, 65536))
                    {
                        buffStream.CopyTo(decompressedMemoryStream);
                        decompressedMemoryStream.Position = 0;
                        // To prevent errors serializing between version number differences (e.g. Version 1 serializes, and Version 2 deserializes)
                        // inspired by: http://spazzarama.com/2009/06/25/binary-deserialize-unable-to-find-assembly/
                        formatter.Binder = serializationBinder;
                        var result = (T)formatter.Deserialize(decompressedMemoryStream);
                        buffStream.Close();
                        return result;
                    }
                }
            }
        }

        public static T DeserializeWithDecompressionFromString<T>(this string value)
        {
            var base64EncodedBytes = Convert.FromBase64String(value);
            return base64EncodedBytes.DeserializeWithDecompression<T>();
        }

        public static T DeserializeWithDecompressionJson<T>(this byte[] buffer, JsonSerializerSettings settings = null)
        {
            using (var compressedMemoryStream = new MemoryStream(buffer))
            {
                using (var deCompressor = new GZipStream(compressedMemoryStream, CompressionMode.Decompress))
                {
                    using (StreamReader streamReader = new StreamReader(deCompressor))
                    {
                        using (JsonTextReader jsonReader = new JsonTextReader(streamReader))
                        {
                            var jsonSerializer = JsonSerializer.CreateDefault(settings);
                            var result = jsonSerializer.Deserialize<T>(jsonReader);
                            return result;
                        }
                    }
                }
            }
        }

        public static byte[] SerializeWithCompression<T>(this T value)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var compressor = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    using (var buffStream = new BufferedStream(compressor, 65536))
                    {
                        var binaryFormatter = new BinaryFormatter();
                        binaryFormatter.Serialize(buffStream, value);
                    }
                }
                return memoryStream.ToArray();
            }
        }

        public static byte[] SerializeWithCompressionJson<T>(this T value, JsonSerializerSettings settings = null)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var compressor = new GZipStream(memoryStream, CompressionLevel.Optimal))
                {
                    using (var writer = new StreamWriter(compressor))
                    {
                        var serializer = JsonSerializer.CreateDefault(settings);
                        serializer.Serialize(writer, value);
                    }
                }
                return memoryStream.ToArray();
            }
        }

        public static string SerializeWithCompressionToString<T>(this T value)
        {
            var buffer = value.SerializeWithCompression();
            return Convert.ToBase64String(buffer);
        }
    }
}