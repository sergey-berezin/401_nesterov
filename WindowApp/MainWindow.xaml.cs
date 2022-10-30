using System.Collections.Generic;
using System.Threading;
using System.Windows;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using FaceEmbeddingsAsync;
using static FaceEmbeddingsAsync.Utils;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Linq;


namespace WindowApp
{
    public partial class MainWindow : Window
    {
        private List<string> paths = new List<string>();
        private List<Image<Rgb24>> images = new List<Image<Rgb24>>();
        private CancellationTokenSource token_src;
        private CancellationToken token;
        private bool calculations_status;

        AsyncInferenceSession session = new AsyncInferenceSession();

        private string files_filter = "Images (*.jpg, *.png)|*.jpg;*.png";
        private string images_dir = Path.GetFullPath("../../../../images");

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

            foreach (var path in ofd.FileNames) 
            {
                var face = SixLabors.ImageSharp.Image.Load<Rgb24>(path);
                images.Add(face);
                paths.Add(path);
            }

            DrawGrid();
        }

        private void DrawGrid()
        {
            AddUnitGrid();
            foreach (var (path, i) in paths.Select((x, i) => (x, i))) 
            {
                AddUnitGrid();

                var uri = new Uri(path);
                var bitmap = new BitmapImage(uri);

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
            var image = new System.Windows.Controls.Image();
            image.Source = bitmap;
            Grid.SetColumn(image, col);
            Grid.SetRow(image, row);
            table.Children.Add(image);
        }

        public void ClearGrid()
        {
            ResetToken();
            ResetPbar();

            if (!images.Any())
                return;

            table.Children.Clear();
            table.RowDefinitions.Clear();
            table.ColumnDefinitions.Clear();

            paths.Clear();
            images.Clear();
        }

        private async void ButtonStart(object sender, RoutedEventArgs e)
        {
            if (!images.Any()) 
            {
                MessageBox.Show("Choose images to process.");
                return;
            }

            if (calculations_status) 
                return;

            calculations_status = true;

            var tasks = new List<Task>();
            foreach (var image in images)
                tasks.Add(session.EmbeddingsAsync(image, token));

            double step = pbar.Maximum / (2 * images.Count);
            while (tasks.Any()) 
            {
                var task = await Task.WhenAny(tasks);
                if (!token.IsCancellationRequested)
                    UpdatePbar(step);
                tasks.Remove(task);
            }

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

                    var task1 = session.EmbeddingsAsync(images[i], token);
                    var task2 = session.EmbeddingsAsync(images[j], token);

                    var embeddings1 = await task1;
                    var embeddings2 = await task2;

                    var dist = Distance(embeddings1, embeddings2);
                    var sim = Similarity(embeddings1, embeddings2);

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
            calculations_status = false;
        }

        private void UpdatePbar(double step)
        {
            pbar.Value += step;
        }

        private void CompletePbar()
        {
            pbar.Value = pbar.Maximum;
        }

        private void ResetPbar()
        {
            pbar.Value = 0;
        }
    }
}
