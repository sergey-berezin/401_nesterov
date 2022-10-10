using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;


using static FaceEmbeddingsAsync.Utils;

namespace FaceEmbeddingsAsync
{

using ImageType = Image<Rgb24>;

public class AsyncInferenceSession: IDisposable
{
    private InferenceSession session;
    private bool disposed = false;

    public AsyncInferenceSession()
    {
        using var modelStream = typeof(AsyncInferenceSession)
            .Assembly
            .GetManifestResourceStream("FaceEmbeddingsAsync.arcfaceresnet100-8.onnx");

        if (modelStream == null)
            throw new ModelNotFoundException("Model can't be loaded.");

        using var memoryStream = new MemoryStream();
        modelStream!.CopyTo(memoryStream);
        session = new InferenceSession(memoryStream.ToArray());
    }

    public async Task<float[]> EmbeddingsAsync(ImageType image, CancellationToken token)
    {
        return await Task<float[]>.Factory.StartNew(() =>
        {
            var inputs = Preprocess(image);

            if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();

            lock (session)
            {
                using var outputs = session.Run(inputs);

                return Normalize(
                    outputs.First(v => v.Name == "fc1")
                    .AsEnumerable<float>()
                    .ToArray()
                );
            }
        }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return;

        if (disposing)
            session.Dispose();

        disposed = true;
    }

    ~AsyncInferenceSession()
    {
        Dispose(disposing: false);
    }
}

} // FaceEmbeddingsAsync
