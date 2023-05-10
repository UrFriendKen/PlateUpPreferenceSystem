using System;
using System.IO;
using System.IO.Compression;

namespace PreferenceSystem.Utils
{
    public static class StringUtils
    {
        public static string ToBase64(string text)
        {
            byte[] compressedBytes;
            using (var memoryStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress, true))
                using (var writer = new StreamWriter(deflateStream))
                {
                    writer.Write(text);
                }

                compressedBytes = memoryStream.ToArray();
            }

            string compressedString = Convert.ToBase64String(compressedBytes);
            return compressedString;
        }
        public static string FromBase64(string base64)
        {
            byte[] compressedBytes = Convert.FromBase64String(base64);

            string decompressedString;
            using (var memoryStream = new MemoryStream(compressedBytes))
            {
                using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
                using (var reader = new StreamReader(deflateStream))
                {
                    decompressedString = reader.ReadToEnd();
                }
            }
            return decompressedString;
        }
    }
}
