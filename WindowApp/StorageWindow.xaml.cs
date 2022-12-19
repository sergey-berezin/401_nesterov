using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

using Contracts;


namespace WindowApp
{
    /// <summary>
    /// Логика взаимодействия для StorageWindow.xaml
    /// </summary>
    public partial class StorageWindow : Window
    {
        public ObservableCollection<ImageDetails> Details { get; private set; }
        private readonly Service service;
        public StorageWindow(Service service)
        {
            this.service = service;
            Details = new();
            LoadImages();

            InitializeComponent();
            DataContext = this;
        }

        private async void LoadImages()
        {
            var images = await service.GetImages();
            if (images == null)
                return;

            foreach (var details in images)
                Details.Add(details);
        }

        private async void ButtonDelete(object sender, RoutedEventArgs e)
        {
            if (!Details.Any())
                return;

            try
            {
                var image = Details[ImagesListBox.SelectedIndex];
                var success = await service.DeleteImageById(image.Id);

                if (success)
                    Details.Remove(image);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void ButtonClear(object sender, RoutedEventArgs e)
        {
            if (!Details.Any())
                return;

            var success = await service.Clear();
            if (success)
                Details.Clear();
        }
    }
}
