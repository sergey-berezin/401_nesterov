using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;


namespace FaceEmbeddingsAsync
{

using ImageType = Image<Rgb24>;

public static class Utils
{
    private static int Height = 112;
    private static int Width = 112;

    public static List<NamedOnnxValue> Preprocess(ImageType image)
    {
        image.Mutate(x => x.Resize(Height, Width));

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("data", ImageToTensor(image))
        };

        return inputs;
    }

    public static DenseTensor<float> ImageToTensor(ImageType img)
    {
        var w = img.Width;
        var h = img.Height;
        var t = new DenseTensor<float>(new[] { 1, 3, h, w });

        img.ProcessPixelRows(pa =>
        {
            for (int y = 0; y < h; y++)
            {
                Span<Rgb24> pixelSpan = pa.GetRowSpan(y);
                for (int x = 0; x < w; x++)
                {
                    t[0, 0, y, x] = pixelSpan[x].R;
                    t[0, 1, y, x] = pixelSpan[x].G;
                    t[0, 2, y, x] = pixelSpan[x].B;
                }
            }
        });

        return t;
    }

    public static float Length(float[] v) =>
        (float)Math.Sqrt(v.Select(x => x * x).Sum());

    public static float Distance(float[] v1, float[] v2) =>
        Length(v1.Zip(v2).Select(p => p.First - p.Second).ToArray());

    public static float Similarity(float[] v1, float[] v2) =>
        v1.Zip(v2).Select(p => p.First * p.Second).Sum();

    public static float[] Normalize(float[] v)
    {
        var len = Length(v);
        return v.Select(x => x / len).ToArray();
    }
}

} // FaceEmbeddingsAsync
