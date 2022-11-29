using Microsoft.EntityFrameworkCore;

namespace WindowApp
{
    public class Image
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Hash { get; set; }
        public byte[] Embedding { get; set; }
        public ImageDetails Details { get; set; }
    }

    public class ImageDetails
    {
        public int Id { get; set; }
        public byte[] Data { get; set; }
    }

    public class ImagesContext : DbContext
    {
        public DbSet<Image> Images { get; set; }
        public DbSet<ImageDetails> Details { get; set; }

        public ImagesContext() => Database.EnsureCreated();

        public void Clear()
        {
            Images.RemoveRange(Images);
            Details.RemoveRange(Details);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder o)
        {
            o.UseSqlite("Data Source=images.db");
        }
    }
}
