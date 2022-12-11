using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using SixLabors.ImageSharp.PixelFormats;

using FaceEmbeddingsAsync;


namespace WindowApp
{
    public partial class MainWindow : Window
    {
        private List<Image> images = new();
        private readonly ReusableToken token;
        private bool calculations_started = false;
        private readonly AsyncInferenceSession session = new();

        private readonly string files_filter = "Images (*.jpg, *.png)|*.jpg;*.png";
        private readonly string images_dir = Path.GetFullPath("../../../../images");

        private readonly ProgressBarReporter embeddingsBar, pairwiseBar;
        private readonly DynamicImagesGrid dynamicGrid;

        public MainWindow()
        {
            InitializeComponent();

            token = new ReusableToken();
            embeddingsBar = new ProgressBarReporter(ref pbarEmbeddings);
            pairwiseBar = new ProgressBarReporter(ref pbarPairwise);
            dynamicGrid = new DynamicImagesGrid(ref table);
        }

        private void ButtonOpen(object sender, RoutedEventArgs e)
        {
            Clear();

            var ofd = new Microsoft.Win32.OpenFileDialog()
            {
                Multiselect = true,
                Filter = files_filter,
                InitialDirectory = images_dir
            };

            var response = ofd.ShowDialog();
            if (response == null || response == false)
                return;

            var imageDetails = ofd.FileNames.Select(path => new ImageDetails
            {
                Data = File.ReadAllBytes(path)
            });

            images = ofd.FileNames.Zip(imageDetails, (path, details) => new Image { 
                Name = path,
                Details = details,
                Hash = Utils.Hash(details.Data)
            }).ToList();

            dynamicGrid.Draw(images);
        }

        public void Clear()
        {
            token.Reset();
            calculations_started = false;
            embeddingsBar.Reset();
            pairwiseBar.Reset();

            if (!images.Any())
                return;

            dynamicGrid.Clear();

            images.Clear();
        }

        private Task<float[]>[] CreateTasks(List<Image> query_images)
        {
            var retrievedEmbeddings = Storage.RetrieveEmbeddings(query_images);

            return retrievedEmbeddings.Zip(query_images, (retrieved_embedding, query) =>
            {
                if (retrieved_embedding != null)
                    return Task.FromResult(retrieved_embedding);

                return session.EmbeddingsAsync(
                    SixLabors.ImageSharp.Image.Load<Rgb24>(query.Details.Data),
                    token.token
                );
            }).ToArray();
        }

        private async Task ProcessImagesAsync(List<Image> query_images, IProgress<double> reporter)
        {
            var tasks = CreateTasks(query_images);

            try
            {
                double step = 1 / tasks.Length;
                double progress = 0.0;

                foreach (var (task, image) in tasks.Zip(query_images))
                {
                    var res_embedding = await task;

                    if (token.Cancelled())
                        break;

                    progress += step;
                    reporter.Report(progress);

                    var embedding = Utils.FloatToByte(res_embedding);
                    Storage.SaveEmbedding(embedding, image);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async void ButtonStart(object sender, RoutedEventArgs e)
        {
            if (!images.Any()) 
            {
                MessageBox.Show("Choose images to process.");
                return;
            }

            if (calculations_started) 
                return;

            calculations_started = true;

            await ProcessImagesAsync(images, embeddingsBar);
            if (!token.Cancelled())
                embeddingsBar.Complete();

            double step = 1 / (images.Count * images.Count);
            double progress = 0.0;
            for (int i = 0; i < images.Count; ++i) 
            {
                var embedding_i = Storage.RetrieveEmbeddings(new List<Image> { images[i] })[0];

                for (int j = 0; j < images.Count; ++j) 
                {
                    var embedding_j = Storage.RetrieveEmbeddings(new List<Image> { images[j] })[0];

                    bool is_cancelled = token.Cancelled();
                    dynamicGrid.PutLabel(embedding_i, i + 1, embedding_j, j + 1, is_cancelled);

                    if (!is_cancelled)
                    {
                        progress += step;
                        pairwiseBar.Report(progress);
                    }
                }
            }

            if (!token.Cancelled())
                pairwiseBar.Complete();
        }

        private void ButtonClear(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
            token.Cancel();
        }

        private void ButtonOpenDb(object sender, RoutedEventArgs e)
        {
            var storage = new StorageWindow();
            storage.ShowDialog();
        }
    }
}
