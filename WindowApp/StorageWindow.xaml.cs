using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

using Microsoft.EntityFrameworkCore;


namespace WindowApp
{
    /// <summary>
    /// Логика взаимодействия для StorageWindow.xaml
    /// </summary>
    public partial class StorageWindow : Window
    {
        public ObservableCollection<Image> Images { get; private set; }
        public StorageWindow()
        {
            Images = new ObservableCollection<Image>();

            using (var db = new ImagesContext())
            {
                foreach (var image in db.Images)
                    Images.Add(image);
            }

            InitializeComponent();
            DataContext = this;
        }

        private void ButtonDelete(object sender, RoutedEventArgs e)
        {
            if (!Images.Any())
                return;

            try
            {
                var image = Images[ImagesListBox.SelectedIndex];
                using (var db = new ImagesContext())
                {
                    var deletedImage = db.Images
                        .Where(x => x.Id == image.Id)
                        .Include(x => x.Details)
                        .First();

                    if (deletedImage == null)
                        return;

                    db.Details.Remove(deletedImage.Details);
                    db.Images.Remove(deletedImage);
                    db.SaveChanges();
                }
                Images.Remove(image);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ButtonClear(object sender, RoutedEventArgs e)
        {
            if (!Images.Any())
                return;

            using (var db = new ImagesContext())
            {
                db.Clear();
                db.SaveChanges();
            }

            Images.Clear();
        }
    }
}
