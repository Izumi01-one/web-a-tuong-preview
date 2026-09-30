using LTLaserBlogMvc.Data;
using LTLaserBlogMvc.Models;
using LTLaserBlogMvc.Routes;
using LTLaserBlogMvc.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LTLaserBlogMvc.Controllers;

// Đường dẫn chung của BlogController là /bai-viet.
[Route(AppRoutes.Blog.Base)]
public sealed class BlogController : Controller
{
    // Đối tượng dùng để truy cập cơ sở dữ liệu.
    private readonly AppDbContext _dbContext;

    // Nhận AppDbContext thông qua Dependency Injection.
    public BlogController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    // =========================================================
    // 1. HIỂN THỊ DANH SÁCH BÀI VIẾT
    // GET: /bai-viet
    // =========================================================

    [HttpGet(AppRoutes.Blog.List)]
    public async Task<IActionResult> Index(string? q = null, int skip = 0, int take = 6)
    {
        var query = _dbContext.Posts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            string trimmed = q.Trim();
            query = query.Where(post => post.Title.Contains(trimmed) || post.Summary.Contains(trimmed));
        }

        int totalCount = await query.CountAsync();
        List<Post> posts = await query
            .OrderByDescending(post => post.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        bool hasMore = (skip + posts.Count) < totalCount;

        var viewModel = new PostListVm
        {
            Posts = posts,
            SearchQuery = q,
            TotalCount = totalCount,
            HasMore = hasMore,
            Skip = skip + posts.Count
        };

        return View(viewModel);
    }


    // =========================================================
    // 1B. TẢI THÊM BÀI VIẾT (AJAX)
    // GET: /bai-viet/load-more
    // =========================================================

    [HttpGet("load-more")]
    public async Task<IActionResult> LoadMore(string? q = null, int skip = 0, int take = 6)
    {
        var query = _dbContext.Posts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            string trimmed = q.Trim();
            query = query.Where(post => post.Title.Contains(trimmed) || post.Summary.Contains(trimmed));
        }

        int totalCount = await query.CountAsync();
        List<Post> posts = await query
            .OrderByDescending(post => post.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        bool hasMore = (skip + posts.Count) < totalCount;

        Response.Headers.Append("X-Has-More", hasMore ? "true" : "false");
        Response.Headers.Append("X-Total-Count", totalCount.ToString());

        return PartialView("_BlogItemsPartial", posts);
    }



    // =========================================================
    // 2. HIỂN THỊ CHI TIẾT BÀI VIẾT
    // GET: /bai-viet/{slug}
    // Ví dụ: /bai-viet/gioi-thieu-san-pham
    // =========================================================

    [HttpGet(AppRoutes.Blog.Detail)]
    public async Task<IActionResult> Detail(string slug)
    {
        /*
         * Tìm bài viết theo slug.
         *
         * Include giúp lấy cả các khối nội dung thuộc bài viết.
         * Nếu không có Include, danh sách Blocks có thể không
         * được tải từ cơ sở dữ liệu.
         */
        Post? post = await _dbContext.Posts
            .Include(currentPost => currentPost.Blocks)
            .FirstOrDefaultAsync(
                currentPost => currentPost.Slug == slug
            );

        // Trả về lỗi HTTP 404 nếu không tìm thấy bài viết.
        if (post is null)
        {
            return NotFound();
        }

        // Tăng số lượt xem của bài viết.
        post.ViewCount++;

        // Lưu số lượt xem mới vào cơ sở dữ liệu.
        await _dbContext.SaveChangesAsync();

        /*
         * Sắp xếp các khối nội dung theo DisplayOrder
         * để hiển thị đúng thứ tự do Admin đã thiết lập.
         */
        post.Blocks = post.Blocks
            .OrderBy(block => block.DisplayOrder)
            .ToList();

        // Truyền bài viết sang Views/Blog/Detail.cshtml.
        return View(post);
    }


    // =========================================================
    // 3. TRẢ VỀ ẢNH BÌA CỦA BÀI VIẾT
    // GET: /bai-viet/anh/{id}
    // Ví dụ: /bai-viet/anh/5
    // =========================================================

    [HttpGet(AppRoutes.Blog.CoverImage)]

    /*
     * Cho phép trình duyệt và bộ nhớ đệm trung gian lưu ảnh
     * trong 86.400 giây, tương đương 24 giờ.
     */
    [ResponseCache(
        Duration = 86400,
        Location = ResponseCacheLocation.Any
    )]
    public async Task<IActionResult> Image(int id)
    {
        /*
         * Chỉ lấy dữ liệu ảnh và loại nội dung thay vì lấy
         * toàn bộ thông tin của bài viết.
         *
         * Cách này giúp giảm lượng dữ liệu không cần thiết
         * được đọc từ cơ sở dữ liệu.
         */
        var imageInformation = await _dbContext.Posts
            .AsNoTracking()
            .Where(post => post.Id == id)
            .Select(post => new
            {
                post.ImageData,
                post.ImageContentType
            })
            .FirstOrDefaultAsync();

        // Trả về lỗi 404 nếu không tìm thấy bài viết hoặc ảnh.
        if (
            imageInformation is null ||
            imageInformation.ImageData is null ||
            string.IsNullOrWhiteSpace(
                imageInformation.ImageContentType
            ))
        {
            return NotFound();
        }

        /*
         * Trả dữ liệu ảnh về trình duyệt.
         *
         * ImageData là nội dung ảnh dưới dạng mảng byte.
         * ImageContentType là loại ảnh như image/jpeg hoặc image/png.
         */
        return File(
            fileContents: imageInformation.ImageData,
            contentType: imageInformation.ImageContentType
        );
    }


    // =========================================================
    // 4. TRẢ VỀ ẢNH CỦA KHỐI NỘI DUNG
    // GET: /bai-viet/anh-noi-dung/{id}
    // Ví dụ: /bai-viet/anh-noi-dung/12
    // =========================================================

    [HttpGet(AppRoutes.Blog.ContentImage)]

    // Cho phép lưu ảnh trong bộ nhớ đệm trong 24 giờ.
    [ResponseCache(
        Duration = 86400,
        Location = ResponseCacheLocation.Any
    )]
    public async Task<IActionResult> BlockImage(int id)
    {
        /*
         * Tìm ảnh thuộc một khối nội dung theo ID của PostBlock.
         *
         * Chỉ lấy ImageData và ImageContentType để tránh
         * truy vấn những dữ liệu không cần thiết.
         */
        var imageInformation = await _dbContext.PostBlocks
            .AsNoTracking()
            .Where(block => block.Id == id)
            .Select(block => new
            {
                block.ImageData,
                block.ImageContentType
            })
            .FirstOrDefaultAsync();

        // Trả về lỗi 404 nếu khối hoặc dữ liệu ảnh không tồn tại.
        if (
            imageInformation is null ||
            imageInformation.ImageData is null ||
            string.IsNullOrWhiteSpace(
                imageInformation.ImageContentType
            ))
        {
            return NotFound();
        }

        // Trả dữ liệu ảnh của khối nội dung về trình duyệt.
        return File(
            fileContents: imageInformation.ImageData,
            contentType: imageInformation.ImageContentType
        );
    }
}