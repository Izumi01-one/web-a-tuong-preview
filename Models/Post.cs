using System.ComponentModel.DataAnnotations;
namespace LTLaserBlogMvc.Models;
public sealed class Post {
    public int Id { get; set; }
    [MaxLength(180)] public string Title { get; set; } = "";
    [MaxLength(220)] public string Slug { get; set; } = "";
    [MaxLength(500)] public string Summary { get; set; } = "";
    public string Content { get; set; } = ""; // Giữ tương thích với dữ liệu cũ.
    [MaxLength(100)] public string? ImageContentType { get; set; }
    public byte[]? ImageData { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public int ViewCount { get; set; }
    [MaxLength(450)] public string AuthorId { get; set; } = "";
    public ApplicationUser? Author { get; set; }
    public List<PostBlock> Blocks { get; set; } = [];
}
public enum PostBlockType { Heading = 1, Paragraph = 2, Image = 3, Quote = 4, Notice = 5, List = 6 }
public sealed class PostBlock {
    public int Id { get; set; }
    public int PostId { get; set; }
    public Post? Post { get; set; }
    public PostBlockType BlockType { get; set; }
    public string Content { get; set; } = "";
    [MaxLength(300)] public string? Caption { get; set; }
    [MaxLength(100)] public string? ImageContentType { get; set; }
    public byte[]? ImageData { get; set; }
    public int DisplayOrder { get; set; }
}
