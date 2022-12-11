using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using static FaceEmbeddingsAsync.Utils;


namespace WindowApp
{
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

        public void PutLabel(float[] embedding_i, int col, float[] embedding_j, int row, bool is_empty)
        {
            var label = new Label()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                Content = GetLabelContent(embedding_i, embedding_j, is_empty)
            };

            grid.Children.Add(label);
            Grid.SetColumn(label, col);
            Grid.SetRow(label, row);
        }

        private static string GetLabelContent(float[] embedding_first, float[] embedding_second, bool is_empty)
        {
            if (is_empty)
                return "<Empty>";

            var dist = Distance(embedding_first, embedding_second);
            var sim = Similarity(embedding_first, embedding_second);

            return $"Distance: {dist:0.00}\nSimilarity: {sim:0.00}";
        }

        public void Draw(IEnumerable<Image> images)
        {
            AddUnit();
            foreach (var (image, i) in images.Select((x, i) => (x, i)))
            {
                AddUnit();

                var bitmap = Utils.ByteToBitmap(image.Details.Data);

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
