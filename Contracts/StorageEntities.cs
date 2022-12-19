namespace Contracts
{
    public class Image
    {
        public int Id { get; set; }
        public byte[] Embedding { get; set; }
        public string Hash { get; set; }
        public ImageDetails Details { get; set; }
    }

    public class ImageDetails
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] Data { get; set; }
    }
}

