using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.PixelFormats;

using FaceEmbeddingsAsync;
using Contracts;


namespace Server
{
    public class ImageEmbeddingProcessor
    {
        private readonly ImagesContext context;
        private readonly AsyncInferenceSession session;
        private readonly MetricsList metrics;
        private readonly ReusableToken token = new();

        public ImageEmbeddingProcessor()
        {
            ResetToken();
            context = new();
            session = new();
            metrics = new MetricsList(new List<IMetric>
            {
                new Distance(),
                new Similarity()
            });
        }

        public async Task<List<int>> ProcessImagesAsync(
            List<ImageDetails> query_images,
            IProgress<double>? reporter = null
        )
        {
            var ids = context.AddImages(query_images);
            var tasks = CreateTasks(query_images, token.token);

            try
            {
                double step = 1 / tasks.Length;
                double progress = 0.0;

                foreach (var (task, image) in tasks.Zip(query_images))
                {
                    var embedding = await task;

                    if (token.Cancelled())
                        break;

                    progress += step;
                    if (reporter != null)
                        reporter.Report(progress);

                    context.SaveEmbedding(embedding, image);
                }
            }
            catch (OperationCanceledException)
            {
            }

            return ids;
        }

        public Dictionary<string, float?>? Compare(Tuple<int, int> ids)
        {
            var img1 = context.GetImageById(ids.Item1);
            var img2 = context.GetImageById(ids.Item2);

            if (img1 == null || img2 == null)
                return null;

            var embeddings = context.RetrieveEmbeddings(
                new List<ImageDetails> { img1, img2 }
            );

            var metrics_computed = metrics.Compute(
                new PairVectors(embeddings[0], embeddings[1]
            ));

            return metrics_computed.ToValues();
        }

        public async Task<List<ImageDetails>> GetImages()
        {
            return await context.Details.ToListAsync();
        }

        public void Cancel()
        {
            token.Cancel();
        }

        public async Task<bool> DeleteImageById(int id)
        {
            var deletedImage = context.Images
                        .Where(x => x.Id == id)
                        .Include(x => x.Details)
                        .First();

            if (deletedImage == null)
                return false;

            context.Details.Remove(deletedImage.Details);
            context.Images.Remove(deletedImage);
            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> Clear()
        {
            try
            {
                context.Clear();
                await context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public void ResetToken()
        {
            token.Reset();
        }

        private Task<float[]>[] CreateTasks(
            List<ImageDetails> query_images,
            CancellationToken token
        )
        {
            var retrievedEmbeddings = context.RetrieveEmbeddings(query_images);

            return retrievedEmbeddings.Zip(query_images, (retrieved_embedding, query) =>
            {
                if (retrieved_embedding != null)
                    return Task.FromResult(retrieved_embedding);

                return session.EmbeddingsAsync(
                    SixLabors.ImageSharp.Image.Load<Rgb24>(query.Data),
                    token
                );
            }).ToArray();
        }
    }
}
