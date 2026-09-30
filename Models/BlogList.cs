namespace LTLaserBlogMvc.Models
{
    public class BlogList
    {
        public int Id { get; init; }

        public string Title { get; init; } = string.Empty;

        public string Slug { get; init; } = string.Empty;

        public string Summary { get; init; } = string.Empty;

        public DateTime CreatedAtUtc { get; init; }

        public int ViewCount { get; init; }

        public bool HasImage { get; init; }
    }
}
