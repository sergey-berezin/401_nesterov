using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using FaceEmbeddingsAsync;
using static FaceEmbeddingsAsync.Utils;


using var session = new FaceEmbeddingsAsync.AsyncInferenceSession();
using var face1 = Image.Load<Rgb24>("face1.png");
using var face2 = Image.Load<Rgb24>("face2.png");
using var arnold1 = Image.Load<Rgb24>("arnold1.png");
using var arnold2 = Image.Load<Rgb24>("arnold2.png");

var faces = new Dictionary<string, Image<Rgb24>>()
{
    {"face1", face1},
    {"face2", face2},
    {"arnold1", arnold1},
    {"arnold2", arnold2}
};

var cts = new CancellationTokenSource();
var token = cts.Token;


/*
-------------------------------------------------
        Инференс по очереди (параллельно)
-------------------------------------------------
*/

var embeddings_one_thread = new Dictionary<string, float[]>();

foreach (KeyValuePair <string, Image<Rgb24>> entry in faces)
{
    embeddings_one_thread[entry.Key] = await session.EmbeddingsAsync(entry.Value, token);
}

Console.WriteLine("Similarity from one thread");

var sim1_face = Similarity(embeddings_one_thread["face1"], embeddings_one_thread["face2"]);
Console.WriteLine($"Sim(face1, face2) = {sim1_face}");

var sim_arnold = Similarity(embeddings_one_thread["arnold1"], embeddings_one_thread["arnold2"]);
Console.WriteLine($"Sim(arnold1, arnold2) = {sim_arnold}");

var sim_different = Similarity(embeddings_one_thread["face1"], embeddings_one_thread["arnold2"]);
Console.WriteLine($"Sim(face1, arnold2) = {sim_different}\n");


/*
-----------------------------------
        Инференс асинхронно
-----------------------------------
*/

var embeddings_async_res = new Dictionary<string, System.Threading.Tasks.Task<float[]>>();
var embeddings_async = new Dictionary<string, float[]>();

foreach (KeyValuePair <string, Image<Rgb24>> entry in faces)
{
    embeddings_async_res[entry.Key] = session.EmbeddingsAsync(entry.Value, token);
}

foreach (KeyValuePair <string, Image<Rgb24>> entry in faces)
{
    embeddings_async[entry.Key] = await embeddings_async_res[entry.Key];
}

Console.WriteLine("Similarity Async");

sim1_face = Similarity(embeddings_async["face1"], embeddings_async["face2"]);
Console.WriteLine($"Sim(face1, face2) = {sim1_face}");

sim_arnold = Similarity(embeddings_async["arnold1"], embeddings_async["arnold2"]);
Console.WriteLine($"Sim(arnold1, arnold2) = {sim_arnold}");

sim_different = Similarity(embeddings_async["face1"], embeddings_async["arnold2"]);
Console.WriteLine($"Sim(face1, arnold2) = {sim_different}\n");


/*
---------------------------------------------
        Инференс с отменой вычисления
---------------------------------------------
*/

var embeddings_stopped = session.EmbeddingsAsync(arnold1, token);
cts.Cancel();

Console.WriteLine("Cancellation token passing");

try {
    _ = await embeddings_stopped;
} catch (OperationCanceledException) {
    Console.WriteLine($"Calculations were cancelled\n");
}
