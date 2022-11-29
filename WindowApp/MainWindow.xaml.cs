using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.PixelFormats;

using FaceEmbeddingsAsync;
using static FaceEmbeddingsAsync.Utils;


namespace WindowApp
{
    public partial class MainWindow : Window
    {
        private List<Image> images = new List<Image>();
        private CancellationTokenSource token_src;
        private CancellationToken token;
        private bool calculations_started = false;

        AsyncInferenceSession session = new AsyncInferenceSession();

        private readonly string files_filter = "Images (*.jpg, *.png)|*.jpg;*.png";
        private readonly string images_dir = Path.GetFullPath("../../../../images");

        public MainWindow()
        {
            InitializeComponent();
            ResetToken();
        }

        private void ButtonOpen(object sender, RoutedEventArgs e)
        {
            ClearGrid();

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

            DrawGrid();
        }

        private void DrawGrid()
        {
            AddUnitGrid();
            foreach (var (image, i) in images.Select((x, i) => (x, i)))
            {
                AddUnitGrid();

                var bitmap = Utils.ByteToBitmap(image.Details.Data);

                PutImageOnGrid(bitmap, 0, i + 1);
                PutImageOnGrid(bitmap, i + 1, 0);
            }
        }

        public void AddUnitGrid()
        {
            table.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(1, GridUnitType.Star)
            });

            table.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(1, GridUnitType.Star)
            });
        }

        public void PutImageOnGrid(BitmapImage bitmap, int col, int row)
        {
            var image = new System.Windows.Controls.Image
            {
                Source = bitmap
            };
            Grid.SetColumn(image, col);
            Grid.SetRow(image, row);
            table.Children.Add(image);
        }

        public void ClearGrid()
        {
            ResetToken();
            calculations_started = false;
            ReserPbar();

            if (!images.Any())
                return;

            table.Children.Clear();
            table.RowDefinitions.Clear();
            table.ColumnDefinitions.Clear();

            images.Clear();
        }

        private float[]?[] RetrieveEmbeddingsFromDb(List<Image> query_images)
        {
            var hashes = query_images.Select(image => Utils.Hash(image.Details.Data)).ToArray();

            var retrievedEmbeddings = new List<float[]?>();
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

        private Task<float[]>[] CreateTasks(List<Image> query_images)
        {
            var retrievedEmbeddings = RetrieveEmbeddingsFromDb(query_images);

            return retrievedEmbeddings.Zip(query_images, (retrieved_embedding, query) =>
            {
                if (retrieved_embedding != null)
                    return Task.FromResult(retrieved_embedding);

                return session.EmbeddingsAsync(
                    SixLabors.ImageSharp.Image.Load<Rgb24>(query.Details.Data),
                    token
                );
            }).ToArray();
        }

        private void SaveEmbedding(byte[] embedding, Image image)
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

        private async Task ProcessImagesAsync(List<Image> query_images, double step)
        {
            var tasks = CreateTasks(query_images);

            try
            {
                foreach (var (task, image) in tasks.Zip(query_images))
                {
                    var res_embedding = await task;

                    if (token.IsCancellationRequested)
                        break;

                    UpdatePbar(step);

                    var embedding = Utils.FloatToByte(res_embedding);
                    SaveEmbedding(embedding, image);
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

            double step = pbar.Maximum / (2 * images.Count);
            await ProcessImagesAsync(images, step);

            step = pbar.Maximum / (2 * images.Count * images.Count);
            for (int i = 0; i < images.Count; ++i) 
            {
                for (int j = 0; j < images.Count; ++j) 
                {
                    var label = new Label()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 12
                    };

                    if (token.IsCancellationRequested) 
                    {
                        label.Content = "<Empty>";
                        PutLabelOnGrid(label, i + 1, j + 1);
                        continue;
                    }

                    var img_list = new List<Image> { images[i], images[j] };
                    var embeddings = RetrieveEmbeddingsFromDb(img_list);

                    var dist = Distance(embeddings[0], embeddings[1]);
                    var sim = Similarity(embeddings[0], embeddings[1]);

                    label.Content = $"Distance: {dist:0.00}\nSimilarity: {sim:0.00}";
                    UpdatePbar(step);

                    PutLabelOnGrid(label, i + 1, j + 1);
                }
            }

            if (!token.IsCancellationRequested)
                CompletePbar();
        }

        void PutLabelOnGrid(Label label, int col, int row)
        {
            table.Children.Add(label);
            Grid.SetColumn(label, col);
            Grid.SetRow(label, row);
        }

        private void ButtonClear(object sender, RoutedEventArgs e)
        {
            ClearGrid();
        }

        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
            token_src.Cancel();
        }

        private void ResetToken()
        {
            token_src = new CancellationTokenSource();
            token = token_src.Token;
        }

        private void ButtonOpenDb(object sender, RoutedEventArgs e)
        {
            var storage = new StorageWindow();
            storage.ShowDialog();
        }

        public void UpdatePbar(double step)
        {
            pbar.Value += step;
        }

        public void CompletePbar()
        {
            pbar.Value = pbar.Maximum;
        }

        public void ReserPbar()
        {
            pbar.Value = 0;
        }
    }
}
