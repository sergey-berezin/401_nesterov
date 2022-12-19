using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

using Contracts;


namespace Server
{
    internal static class StorageUtils
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

        public static float[] ByteToFloat(byte[] array)
        {
            if (array == null)
                return null;

            var len = array.Length;
            var float_array = new float[len / 4];
            Buffer.BlockCopy(array, 0, float_array, 0, len);
            return float_array;
        }

        public static byte[] FloatToByte(float[] array)
        {
            if (array == null)
                return null;

            var byte_array = new byte[array.Length * 4];
            Buffer.BlockCopy(array, 0, byte_array, 0, byte_array.Length);
            return byte_array;
        }
    }

    public class ImagesContext : DbContext
    {
        public DbSet<Image> Images { get; set; }
        public DbSet<ImageDetails> Details { get; set; }

        public ImagesContext() => Database.EnsureCreated();

        public List<float[]> RetrieveEmbeddings(IEnumerable<ImageDetails> query_images)
        {
            var hashes = query_images
                .Select(image => StorageUtils.Hash(image.Data))
                .ToArray();

            var retrievedEmbeddings = new List<float[]>();
            foreach (var (hash, image) in hashes.Zip(query_images))
            {
                var q = RetrieveImageByHash(image);
                retrievedEmbeddings.Add(StorageUtils.ByteToFloat(q?.Embedding));
            }
            return retrievedEmbeddings.ToList();
        }

        public void SaveEmbedding(float[] embedding, ImageDetails details)
        {
            var embedding_b = StorageUtils.FloatToByte(embedding);
            var image = RetrieveImageByHash(details);
            if (image == null)
            {
                Image newImage = new()
                {
                    Details = details,
                    Embedding = embedding_b,
                    Hash = StorageUtils.Hash(details.Data)
                };

                Images.Add(newImage);
            }
            else if (embedding != null)
            {
                image.Embedding = embedding_b;
            }

            SaveChanges();
        }

        public ImageDetails? GetImageById(int id)
        {
            return Details
                .Where(x => x.Id == id)
                .FirstOrDefault();
        }

        public Image? RetrieveImageByHash(ImageDetails details)
        {
            return Images
                .Where(x => x.Hash == StorageUtils.Hash(details.Data))
                .Include(x => x.Details)
                .Where(x => Equals(x.Details.Data, details.Data))
                .FirstOrDefault();
        }

        public void Clear()
        {
            Images.RemoveRange(Images);
            Details.RemoveRange(Details);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder o)
            => o.UseSqlite("Data Source=images.db");
    }
}
