using LTLaserBlogMvc.Data;
using LTLaserBlogMvc.Models;
using LTLaserBlogMvc.Services;
using LTLaserBlogMvc.ViewModels;
using LTLaserBlogMvc.Routes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LTLaserBlogMvc.Controllers;

// Chỉ tài khoản có vai trò Admin mới được truy cập Controller này.
[Authorize(Roles = "Admin")]
[Route(AppRoutes.Admin.Base)]
public sealed class AdminController : Controller
{
    // Các định dạng ảnh được hệ thống chấp nhận.
    private static readonly string[] ImageTypes =
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    // Đối tượng làm việc với cơ sở dữ liệu.
    private readonly AppDbContext _dbContext;

    // Dịch vụ quản lý tài khoản người dùng.
    private readonly UserManager<ApplicationUser> _userManager;

    // Constructor nhận các dịch vụ thông qua Dependency Injection.
    public AdminController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }


    // =========================================================
    // 1. HIỂN THỊ DANH SÁCH BÀI VIẾT
    // GET: /quan-tri
    // =========================================================

    [HttpGet(AppRoutes.Admin.Dashboard)]
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
    // 1B. TẢI THÊM BÀI VIẾT QUẢN TRỊ (AJAX)
    // GET: /quan-tri/load-more
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

        return PartialView("_AdminItemsPartial", posts);
    }


    // =========================================================
    // 2. HIỂN THỊ TRANG ĐĂNG BÀI
    // =========================================================

    [HttpGet(AppRoutes.Admin.CreatePost)]
    public IActionResult Create()
    {
        // Tạo sẵn một khối đoạn văn khi mở trang đăng bài.
        var postViewModel = new PostVm
        {
            Blocks = new List<PostBlockVm>
            {
                new PostBlockVm
                {
                    BlockType = PostBlockType.Paragraph
                }
            }
        };

        return View(postViewModel);
    }


    // =========================================================
    // 3. XỬ LÝ ĐĂNG BÀI
    // =========================================================

    [HttpPost(AppRoutes.Admin.CreatePost)]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32_000_000)]
    public async Task<IActionResult> Create(PostVm postViewModel)
    {
        ValidatePost(postViewModel);

        if (!ModelState.IsValid)
        {
            return View(postViewModel);
        }

        string slug = await CreateUniqueSlugAsync(postViewModel.Title);

        var newPost = new Post
        {
            Title = postViewModel.Title.Trim(),
            Summary = postViewModel.Summary.Trim(),
            Slug = slug,
            AuthorId = _userManager.GetUserId(User)!
        };

        await ApplyCoverAsync(newPost, postViewModel);
        ApplyBlocks(newPost, postViewModel);

        _dbContext.Posts.Add(newPost);
        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    // =========================================================
    // 4. HIỂN THỊ TRANG SỬA BÀI VIẾT
    // GET: /quan-tri-bi-mat/sua/{id}
    // =========================================================

    [HttpGet(AppRoutes.Admin.EditPost)]
    public async Task<IActionResult> Edit(int id)
    {
        /*
         * Tìm bài viết theo ID.
         *
         * Include giúp tải cả các khối nội dung thuộc bài viết.
         */
        Post? post = await _dbContext.Posts
            .Include(currentPost => currentPost.Blocks)
            .FirstOrDefaultAsync(currentPost => currentPost.Id == id);

        // Trả về lỗi 404 nếu bài viết không tồn tại.
        if (post is null)
        {
            return NotFound();
        }

        /*
         * Chuyển dữ liệu từ Post sang PostVm để hiển thị
         * trong biểu mẫu chỉnh sửa.
         */
        var postViewModel = new PostVm
        {
            Id = post.Id,
            Title = post.Title,
            Summary = post.Summary,

            // Ghi nhận bài viết hiện đã có ảnh bìa hay chưa.
            HasCoverImage = post.ImageData is not null,

            // Sắp xếp các khối theo đúng thứ tự hiển thị.
            Blocks = post.Blocks
                .OrderBy(block => block.DisplayOrder)
                .Select(block => new PostBlockVm
                {
                    Id = block.Id,
                    BlockType = block.BlockType,
                    Content = block.Content,
                    Caption = block.Caption,

                    // Kiểm tra khối này đã có hình ảnh hay chưa.
                    HasExistingImage = block.ImageData is not null,

                    // Mặc định tiếp tục giữ hình ảnh hiện tại.
                    KeepExistingImage = block.ImageData is not null
                })
                .ToList()
        };

        return View(postViewModel);
    }


    // =========================================================
    // 5. XỬ LÝ SỬA BÀI VIẾT
    // POST: /quan-tri-bi-mat/sua/{id}
    // =========================================================

    [HttpPost(AppRoutes.Admin.EditPost)]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32_000_000)]
    public async Task<IActionResult> Edit(
     int id,
     PostVm postViewModel)
    {
        // Kiểm tra ID.
        if (!postViewModel.Id.HasValue ||
            id != postViewModel.Id.Value)
        {
            return BadRequest();
        }

        ValidatePost(postViewModel);

        if (!ModelState.IsValid)
        {
            await RestoreImageStateAsync(id, postViewModel);
            return View(postViewModel);
        }

        Post? post = await _dbContext.Posts
            .Include(currentPost => currentPost.Blocks)
            .FirstOrDefaultAsync(currentPost => currentPost.Id == id);

        if (post is null)
        {
            return NotFound();
        }

        post.Title = postViewModel.Title.Trim();
        post.Summary = postViewModel.Summary.Trim();
        post.UpdatedAtUtc = DateTime.UtcNow;

        // Cập nhật slug theo tiêu đề mới.
        post.Slug = await CreateUniqueSlugAsync(
            postViewModel.Title,
            post.Id
        );

        await ApplyCoverAsync(post, postViewModel);
        await ApplyBlocksForEditAsync(post, postViewModel);

        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }



    // =========================================================
    // 6. XÓA BÀI VIẾT
    // POST: /quan-tri-bi-mat/xoa/{id}
    // =========================================================

    [HttpPost(AppRoutes.Admin.DeletePost)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        // Tìm bài viết theo khóa chính.
        Post? post = await _dbContext.Posts.FindAsync(id);

        // Chỉ thực hiện xóa nếu tìm thấy bài viết.
        if (post is not null)
        {
            _dbContext.Posts.Remove(post);

            await _dbContext.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }


    // =========================================================
    // 7. XEM TRƯỚC BÀI VIẾT
    // GET: /quan-tri-bi-mat/xem-truoc/{id}
    // =========================================================

    [HttpGet(AppRoutes.Admin.PreviewPost)]
    public async Task<IActionResult> Preview(int id)
    {
        /*
         * AsNoTracking được sử dụng vì đây chỉ là thao tác đọc.
         * Entity Framework không cần theo dõi thay đổi dữ liệu.
         */
        Post? post = await _dbContext.Posts
            .Include(currentPost => currentPost.Blocks)
            .AsNoTracking()
            .FirstOrDefaultAsync(currentPost => currentPost.Id == id);

        if (post is null)
        {
            return NotFound();
        }

        // Dùng giao diện chi tiết của Blog để xem trước bài viết.
        return View(
            viewName: "~/Views/Blog/Detail.cshtml",
            model: post
        );
    }


    // =========================================================
    // 8. HIỂN THỊ TRANG CẤP TÀI KHOẢN ADMIN
    // GET: /quan-tri-bi-mat/tai-khoan
    // =========================================================

    [HttpGet(AppRoutes.Admin.Accounts)]
    public IActionResult Accounts()
    {
        var createAdminViewModel = new CreateAdminVm();

        return View(createAdminViewModel);
    }


    // =========================================================
    // 9. XỬ LÝ CẤP TÀI KHOẢN ADMIN
    // POST: /quan-tri-bi-mat/tai-khoan
    // =========================================================

    [HttpPost(AppRoutes.Admin.Accounts)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accounts(
        CreateAdminVm createAdminViewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(createAdminViewModel);
        }

        // Tạo đối tượng tài khoản mới.
        var newAdminUser = new ApplicationUser
        {
            UserName = createAdminViewModel.Email,
            Email = createAdminViewModel.Email,
            EmailConfirmed = true
        };

        // Lưu tài khoản cùng mật khẩu vào cơ sở dữ liệu.
        IdentityResult createUserResult =
            await _userManager.CreateAsync(
                newAdminUser,
                createAdminViewModel.Password
            );

        if (createUserResult.Succeeded)
        {
            // Gán vai trò Admin cho tài khoản vừa được tạo.
            IdentityResult addToRoleResult =
                await _userManager.AddToRoleAsync(
                    newAdminUser,
                    "Admin"
                );

            if (addToRoleResult.Succeeded)
            {
                // TempData tồn tại đến yêu cầu HTTP tiếp theo.
                TempData["Ok"] = "Đã cấp tài khoản admin.";

                return RedirectToAction(nameof(Accounts));
            }

            // Hiển thị lỗi nếu không thể cấp vai trò Admin.
            foreach (IdentityError identityError in addToRoleResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    identityError.Description
                );
            }

            /*
             * Xóa tài khoản vừa tạo nếu không thể cấp vai trò Admin,
             * tránh để lại tài khoản không hoàn chỉnh.
             */
            await _userManager.DeleteAsync(newAdminUser);

            return View(createAdminViewModel);
        }

        // Hiển thị lỗi nếu không thể tạo tài khoản.
        foreach (IdentityError identityError in createUserResult.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                identityError.Description
            );
        }

        return View(createAdminViewModel);
    }


    // =========================================================
    // 10. KIỂM TRA DỮ LIỆU BÀI VIẾT
    // =========================================================

    private void ValidatePost(PostVm postViewModel)
    {
        ValidateImage(
            postViewModel.CoverImage,
            nameof(PostVm.CoverImage)
        );

        postViewModel.Blocks ??= new List<PostBlockVm>();

        if (postViewModel.Blocks.Count == 0)
        {
            ModelState.AddModelError(
                nameof(PostVm.Blocks),
                "Bài viết cần ít nhất một khối nội dung."
            );

            return;
        }

        for (int blockIndex = 0;
             blockIndex < postViewModel.Blocks.Count;
             blockIndex++)
        {
            PostBlockVm blockViewModel =
                postViewModel.Blocks[blockIndex];

            ValidateImage(
                blockViewModel.Image,
                $"Blocks[{blockIndex}].Image"
            );

            bool isImageBlock =
                blockViewModel.BlockType == PostBlockType.Image;

            bool hasNewImage =
                blockViewModel.Image is not null &&
                blockViewModel.Image.Length > 0;

            bool keepsExistingImage =
                blockViewModel.HasExistingImage &&
                blockViewModel.KeepExistingImage;

            if (isImageBlock &&
                !hasNewImage &&
                !keepsExistingImage)
            {
                ModelState.AddModelError(
                    $"Blocks[{blockIndex}].Image",
                    "Khối ảnh cần chọn một ảnh."
                );
            }

            if (!isImageBlock &&
                string.IsNullOrWhiteSpace(blockViewModel.Content))
            {
                ModelState.AddModelError(
                    $"Blocks[{blockIndex}].Content",
                    "Nội dung không được để trống."
                );
            }
        }
    }


    // =========================================================
    // 11. KIỂM TRA FILE HÌNH ẢNH
    // =========================================================

    // Kiểm tra file ảnh.
    // Kiểm tra file ảnh.
    private void ValidateImage(
        IFormFile? imageFile,
        string modelStateKey)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            return;
        }

        if (imageFile.Length > 5_000_000)
        {
            ModelState.AddModelError(
                modelStateKey,
                "Mỗi ảnh tối đa 5 MB."
            );
        }

        if (!ImageTypes.Contains(
                imageFile.ContentType,
                StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                modelStateKey,
                "Chỉ nhận JPG, PNG hoặc WEBP."
            );
        }
    }


    // =========================================================
    // 12. TẠO SLUG KHÔNG BỊ TRÙNG
    // =========================================================

    private async Task<string> CreateUniqueSlugAsync(
        string title,
        int? exceptPostId = null)
    {
        // Chuyển tiêu đề thành slug.
        string slug = SlugService.Create(title);

        // Dùng slug mặc định nếu tiêu đề không tạo được slug.
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "bai-viet";
        }

        // Lưu slug gốc để tạo các slug có hậu tố.
        string rootSlug = slug;

        int suffix = 2;

        /*
         * Kiểm tra slug đã tồn tại hay chưa.
         *
         * exceptPostId dùng để bỏ qua một bài viết cụ thể
         * trong trường hợp cập nhật bài viết.
         */
        while (
            await _dbContext.Posts.AnyAsync(
                post =>
                    post.Slug == slug &&
                    post.Id != exceptPostId))
        {
            slug = $"{rootSlug}-{suffix}";

            suffix++;
        }

        return slug;
    }


    // =========================================================
    // 13. CẬP NHẬT ẢNH BÌA
    // =========================================================

    private async Task ApplyCoverAsync(
    Post post,
    PostVm postViewModel)
    {
        // Có ảnh mới thì thay bằng ảnh mới
        if (postViewModel.CoverImage is not null &&
            postViewModel.CoverImage.Length > 0)
        {
            post.ImageData = await ConvertFileToBytesAsync(
                postViewModel.CoverImage
            );

            post.ImageContentType =
                postViewModel.CoverImage.ContentType;

            return;
        }

        // Khi sửa bài, nếu bỏ chọn giữ ảnh thì xóa ảnh cũ
        if (post.Id > 0 &&
            post.ImageData is not null &&
            !postViewModel.KeepCoverImage)
        {
            post.ImageData = null;
            post.ImageContentType = null;
        }
    }



    // =========================================================
    // 14. TẠO CÁC KHỐI CHO BÀI VIẾT MỚI
    // =========================================================

    // Tạo khối cho bài viết mới.
    private void ApplyBlocks(
        Post post,
        PostVm postViewModel)
    {
        for (int index = 0;
             index < postViewModel.Blocks.Count;
             index++)
        {
            PostBlockVm blockViewModel =
                postViewModel.Blocks[index];

            var newBlock = new PostBlock
            {
                BlockType = blockViewModel.BlockType,
                Content = blockViewModel.Content?.Trim()
                    ?? string.Empty,
                Caption = blockViewModel.Caption?.Trim(),
                DisplayOrder = index
            };

            if (blockViewModel.Image is not null &&
                blockViewModel.Image.Length > 0)
            {
                using var stream = new MemoryStream();

                blockViewModel.Image.CopyTo(stream);

                newBlock.ImageData = stream.ToArray();
                newBlock.ImageContentType =
                    blockViewModel.Image.ContentType;
            }

            post.Blocks.Add(newBlock);
        }
    }


    // =========================================================
    // 15. TẠO LẠI CÁC KHỐI KHI SỬA BÀI VIẾT
    // =========================================================

    private async Task ApplyBlocksForEditAsync(
        Post post,
        PostVm postViewModel)
    {
        HashSet<int> submittedBlockIds = postViewModel.Blocks
            .Where(block => block.Id is int)
            .Select(block => block.Id!.Value)
            .ToHashSet();

        List<PostBlock> removedBlocks = post.Blocks
            .Where(block => !submittedBlockIds.Contains(block.Id))
            .ToList();

        _dbContext.PostBlocks.RemoveRange(removedBlocks);

        for (
            int blockIndex = 0;
            blockIndex < postViewModel.Blocks.Count;
            blockIndex++)
        {
            PostBlockVm blockViewModel =
                postViewModel.Blocks[blockIndex];

            PostBlock? block = blockViewModel.Id is int blockId
                ? post.Blocks.FirstOrDefault(
                    currentBlock => currentBlock.Id == blockId)
                : null;

            if (block is null)
            {
                block = new PostBlock();
                post.Blocks.Add(block);
            }

            block.BlockType = blockViewModel.BlockType;
            block.Content =
                blockViewModel.Content?.Trim()
                ?? string.Empty;
            block.Caption = blockViewModel.Caption?.Trim();
            block.DisplayOrder = blockIndex;

            // Nếu người dùng tải ảnh mới, sử dụng ảnh mới.
            if (blockViewModel.Image is not null)
            {
                block.ImageData =
                    await ConvertFileToBytesAsync(
                        blockViewModel.Image
                    );

                block.ImageContentType =
                    blockViewModel.Image.ContentType;
            }
            else if (!blockViewModel.KeepExistingImage)
            {
                block.ImageData = null;
                block.ImageContentType = null;
            }
        }
    }


    // =========================================================
    // 16. CHUYỂN FILE THÀNH MẢNG BYTE
    // =========================================================

    private static async Task<byte[]> ConvertFileToBytesAsync(
        IFormFile file)
    {
        // MemoryStream tạm thời chứa dữ liệu của file.
        using var memoryStream = new MemoryStream();

        // Sao chép dữ liệu từ file tải lên vào MemoryStream.
        await file.CopyToAsync(memoryStream);

        // Chuyển dữ liệu thành mảng byte để lưu vào cơ sở dữ liệu.
        return memoryStream.ToArray();
    }

    // Khôi phục trạng thái ảnh khi form lỗi.
    private async Task RestoreImageStateAsync(
        int postId,
        PostVm postViewModel)
    {
        Post? existingPost = await _dbContext.Posts
            .Include(post => post.Blocks)
            .AsNoTracking()
            .FirstOrDefaultAsync(post => post.Id == postId);

        if (existingPost is null)
        {
            return;
        }

        postViewModel.HasCoverImage =
            existingPost.ImageData is not null;

        for (int index = 0;
             index < postViewModel.Blocks.Count;
             index++)
        {
            PostBlockVm blockViewModel =
                postViewModel.Blocks[index];

            if (!blockViewModel.Id.HasValue)
            {
                continue;
            }

            PostBlock? existingBlock =
                existingPost.Blocks.FirstOrDefault(
                    block => block.Id == blockViewModel.Id.Value
                );

            blockViewModel.HasExistingImage =
                existingBlock?.ImageData is not null;
        }
    }
}