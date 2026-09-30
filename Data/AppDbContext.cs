using LTLaserBlogMvc.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LTLaserBlogMvc.Data;

// DbContext quản lý kết nối và thao tác với cơ sở dữ liệu
public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    // Nhận cấu hình kết nối cơ sở dữ liệu từ Program.cs
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Đại diện cho bảng Posts trong cơ sở dữ liệu
    public DbSet<Post> Posts
    {
        get
        {
            return Set<Post>();
        }
    }

    // Đại diện cho bảng PostBlocks trong cơ sở dữ liệu
    public DbSet<PostBlock> PostBlocks
    {
        get
        {
            return Set<PostBlock>();
        }
    }

    // Cấu hình cấu trúc và quan hệ giữa các bảng
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Slug không được trùng.
        modelBuilder.Entity<Post>()
            .HasIndex(post => post.Slug)
            .IsUnique();

        modelBuilder.Entity<Post>()
            .Property(post => post.Content)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<Post>()
            .Property(post => post.ImageData)
            .HasColumnType("varbinary(max)");

        modelBuilder.Entity<PostBlock>()
            .Property(block => block.Content)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<PostBlock>()
            .Property(block => block.ImageData)
            .HasColumnType("varbinary(max)");

        // Một tác giả có nhiều bài viết.
        modelBuilder.Entity<Post>()
            .HasOne(post => post.Author)
            .WithMany()
            .HasForeignKey(post => post.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Một bài viết có nhiều khối.
        modelBuilder.Entity<PostBlock>()
            .HasOne(block => block.Post)
            .WithMany(post => post.Blocks)
            .HasForeignKey(block => block.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}