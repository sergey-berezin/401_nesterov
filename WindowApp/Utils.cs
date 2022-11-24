using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;


namespace WindowApp
{
    internal static class Utils
    {
        public static string Hash(byte[] data)
        {
            using var sha256 = SHA256.Create();
            return string.Concat(
                sha256
                .ComputeHash(data)
                .Select(x => x.ToString("X2"))
            );
        }

        public static BitmapImage ByteToBitmap(byte[] array)
        {
            using var ms = new MemoryStream(array);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad; // here
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        public static float[]? ByteToFloat(byte[]? array)
        {
            if (array == null)
                return null;
            var len = array.Length;
            var float_array = new float[len / 4];
            Buffer.BlockCopy(array, 0, float_array, 0, len);
            return float_array;
        }

        public static byte[]? FloatToByte(float[]? array)
        {
            if (array == null)
                return null;

            var byte_array = new byte[array.Length * 4];
            Buffer.BlockCopy(array, 0, byte_array, 0, byte_array.Length);
            return byte_array;
        }
    }
}
