using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using Contracts;


namespace WindowApp
{
    public static class DrawUtils
    {
        public static BitmapImage ByteToBitmap(byte[] array)
        {
            using var ms = new MemoryStream(array);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }
    }

    public class DynamicImagesGrid
    {
        private readonly Grid grid;

        public DynamicImagesGrid(ref Grid grid)
        {
            this.grid = grid;
        }

        public void AddUnit()
        {
            grid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(1, GridUnitType.Star)
            });

            grid.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(1, GridUnitType.Star)
            });
        }

        public void PutImage(BitmapImage bitmap, int col, int row)
        {
            var image = new System.Windows.Controls.Image
            {
                Source = bitmap
            };
            Grid.SetColumn(image, col);
            Grid.SetRow(image, row);
            grid.Children.Add(image);
        }

        public void PutLabel(int col, int row, Dictionary<string, object> metrics, bool is_empty)
        {
            var label = new Label()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                Content = GetLabelContent(metrics, is_empty)
            };

            grid.Children.Add(label);
            Grid.SetColumn(label, col);
            Grid.SetRow(label, row);
        }

        private static string GetLabelContent(Dictionary<string, object> metrics, bool is_empty)
        {
            if (is_empty)
                return "<Empty>";

            var metrics_str = new List<string>();
            foreach (var item in metrics)
                metrics_str.Add($"{item.Key}: " + $"{item.Value:f3}");

            return string.Join("\n", metrics_str);
        }

        public void Draw(IEnumerable<ImageDetails> imagesDetails)
        {
            AddUnit();
            foreach (var (detail, i) in imagesDetails.Select((x, i) => (x, i)))
            {
                AddUnit();

                var bitmap = DrawUtils.ByteToBitmap(detail.Data);

                PutImage(bitmap, 0, i + 1);
                PutImage(bitmap, i + 1, 0);
            }
        }

        public void Clear()
        {
            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();
        }
    }
}
