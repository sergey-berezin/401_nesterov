using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;


namespace WindowApp
{
    public static class Storage
    {
        public static float[][] RetrieveEmbeddings(IEnumerable<Image> query_images)
        {
            var hashes = query_images.Select(image => Utils.Hash(image.Details.Data)).ToArray();

            var retrievedEmbeddings = new List<float[]>();
            using (var db = new ImagesContext())
            {
                foreach (var (hash, image) in hashes.Zip(query_images))
                {
                    var q = db.Images
                        .Where(x => x.Hash == hash)
                        .Include(x => x.Details)
                        .Where(x => Equals(x.Details.Data, image.Details.Data))
                        .FirstOrDefault();

                    retrievedEmbeddings.Add(Utils.ByteToFloat(q?.Embedding));
                }
            }
            return retrievedEmbeddings.ToArray();
        }

        public static void SaveEmbedding(byte[] embedding, Image image)
        {
            using (var db = new ImagesContext())
            {
                var q = db.Images
                    .Where(x => x.Hash == image.Hash)
                    .Include(x => x.Details)
                    .Where(x => Equals(x.Details.Data, image.Details.Data))
                    .FirstOrDefault();

                if (q == null)
                {
                    ImageDetails newDetails = new ImageDetails
                    {
                        Data = image.Details.Data
                    };
                    Image newImage = new Image
                    {
                        Name = image.Name,
                        Embedding = embedding,
                        Details = newDetails,
                        Hash = image.Hash
                    };

                    db.Images.Add(newImage);
                    db.Details.Add(newDetails);
                }
                else if (embedding != null)
                {
                    image.Embedding = embedding;
                }

                db.SaveChanges();
            }
        }
    }
}
