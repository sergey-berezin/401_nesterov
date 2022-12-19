using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using SixLabors.ImageSharp.PixelFormats;

using Contracts;


namespace WindowApp
{
    public partial class MainWindow : Window
    {
        private List<ImageDetails> imageDetails = new();
        private readonly ReusableToken token;
        private bool calculations_started = false;
        private readonly Service service = new();

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

            imageDetails = ofd.FileNames.Select(path => new ImageDetails
            {
                Name = path,
                Data = File.ReadAllBytes(path)
            }).ToList();

            dynamicGrid.Draw(imageDetails);
        }

        public void Clear()
        {
            token.Reset();
            calculations_started = false;
            embeddingsBar.Reset();
            pairwiseBar.Reset();

            if (!imageDetails.Any())
                return;

            dynamicGrid.Clear();

            imageDetails.Clear();
        }

        private async void ButtonStart(object sender, RoutedEventArgs e)
        {
            if (!imageDetails.Any()) 
            {
                MessageBox.Show("Choose images to process.");
                return;
            }

            if (calculations_started) 
                return;

            calculations_started = true;

            var ids = await service.ProcessImagesAsync(imageDetails, token.token);
            if (!token.Cancelled())
                embeddingsBar.Complete();

            int n = ids.Count;
            MessageBox.Show($"n: {n}");

            if (n == 0)
                return;

            double progress = 0.0f;
            double step = 1 / (n * n);
            for (int i = 0; i < n; ++i) 
            {
                for (int j = 0; j < n; ++j) 
                {
                    bool is_cancelled = token.Cancelled();
                    var metrics = await service.Compare(ids[i], ids[j]);
                    dynamicGrid.PutLabel(i + 1, j + 1, metrics, is_cancelled);

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
            var storage = new StorageWindow(service);
            storage.ShowDialog();
        }
    }
}
