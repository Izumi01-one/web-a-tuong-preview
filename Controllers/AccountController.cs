using LTLaserBlogMvc.Models;
using LTLaserBlogMvc.ViewModels;
using LTLaserBlogMvc.Routes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LTLaserBlogMvc.Controllers;

[Route(AppRoutes.Admin.Base)]
public sealed class AccountController : Controller
{
    // Dịch vụ đăng nhập và đăng xuất.
    private readonly SignInManager<ApplicationUser> _signInManager;

    // Dịch vụ quản lý tài khoản.
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    // =========================================================
    // HIỂN THỊ TRANG ĐĂNG NHẬP
    // GET: /quan-tri-bi-mat/dang-nhap
    // =========================================================

    [HttpGet(AppRoutes.Admin.Login)]
    public IActionResult Login()
    {
        var loginViewModel = new LoginVm();

        return View(loginViewModel);
    }

    // =========================================================
    // XỬ LÝ ĐĂNG NHẬP
    // POST: /quan-tri/dang-nhap
    // =========================================================

    [HttpPost(AppRoutes.Admin.Login)]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginVm loginViewModel)
    {
        // Nếu dữ liệu không hợp lệ, hiển thị lại trang đăng nhập.
        if (!ModelState.IsValid)
        {
            return View(loginViewModel);
        }

        /*
         * Dùng var để tránh lỗi SignInResult bị trùng tên giữa:
         * Microsoft.AspNetCore.Identity.SignInResult
         * Microsoft.AspNetCore.Mvc.SignInResult
         */
        var signInResult =
            await _signInManager.PasswordSignInAsync(
                userName: loginViewModel.Email,
                password: loginViewModel.Password,
                isPersistent: false,
                lockoutOnFailure: true
            );

        // Nếu đăng nhập thành công, chuyển đến trang quản trị.
        if (signInResult.Succeeded)
        {
            return RedirectToAction(
                actionName: "Index",
                controllerName: "Admin"
            );
        }

        // Thông báo chung khi đăng nhập thất bại.
        ModelState.AddModelError(
            key: string.Empty,
            errorMessage:
                "Thông tin đăng nhập không đúng hoặc tài khoản đang bị khóa."
        );

        return View(loginViewModel);
    }

    // =========================================================
    // XỬ LÝ ĐĂNG XUẤT
    // POST: /quan-tri-bi-mat/dang-xuat
    // =========================================================

    [HttpPost(AppRoutes.Admin.Logout)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction(
            actionName: "Index",
            controllerName: "Home"
        );
    }

    // =========================================================
    // HIỂN THỊ TRANG TẠO ADMIN ĐẦU TIÊN
    // GET: /quan-tri-bi-mat/khoi-tao
    // =========================================================

    [HttpGet(AppRoutes.Admin.Bootstrap)]
    public async Task<IActionResult> Bootstrap()
    {
        var adminUsers =
            await _userManager.GetUsersInRoleAsync("Admin");

        // Không cho khởi tạo nếu hệ thống đã có Admin.
        if (adminUsers.Count > 0)
        {
            return NotFound();
        }

        var createAdminViewModel = new CreateAdminVm();

        return View(
            viewName: "CreateAdmin",
            model: createAdminViewModel
        );
    }

    // =========================================================
    // XỬ LÝ TẠO ADMIN ĐẦU TIÊN
    // POST: /quan-tri-bi-mat/khoi-tao
    // =========================================================

    [HttpPost(AppRoutes.Admin.Bootstrap)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bootstrap(
        CreateAdminVm createAdminViewModel)
    {
        var adminUsers =
            await _userManager.GetUsersInRoleAsync("Admin");

        // Kiểm tra lại để tránh tạo thêm Admin bằng yêu cầu POST.
        if (adminUsers.Count > 0)
        {
            return NotFound();
        }

        return await CreateAdminAsync(createAdminViewModel);
    }

    // =========================================================
    // TẠO TÀI KHOẢN ADMIN
    // =========================================================

    private async Task<IActionResult> CreateAdminAsync(
        CreateAdminVm createAdminViewModel)
    {
        // Hiển thị lại biểu mẫu nếu dữ liệu không hợp lệ.
        if (!ModelState.IsValid)
        {
            return View(
                viewName: "CreateAdmin",
                model: createAdminViewModel
            );
        }

        // Tạo tài khoản mới.
        var newAdminUser = new ApplicationUser
        {
            UserName = createAdminViewModel.Email,
            Email = createAdminViewModel.Email,
            EmailConfirmed = true
        };

        // Lưu tài khoản và mật khẩu vào cơ sở dữ liệu.
        var createUserResult =
            await _userManager.CreateAsync(
                user: newAdminUser,
                password: createAdminViewModel.Password
            );

        // Nếu tạo tài khoản thất bại, hiển thị các lỗi.
        if (!createUserResult.Succeeded)
        {
            foreach (var identityError in createUserResult.Errors)
            {
                ModelState.AddModelError(
                    key: string.Empty,
                    errorMessage: identityError.Description
                );
            }

            return View(
                viewName: "CreateAdmin",
                model: createAdminViewModel
            );
        }

        // Gán vai trò Admin cho tài khoản vừa tạo.
        var addToRoleResult =
            await _userManager.AddToRoleAsync(
                user: newAdminUser,
                role: "Admin"
            );

        // Nếu gán vai trò thành công, chuyển tới trang đăng nhập.
        if (addToRoleResult.Succeeded)
        {
            return RedirectToAction(nameof(Login));
        }

        // Hiển thị lỗi nếu không thể gán vai trò Admin.
        foreach (var identityError in addToRoleResult.Errors)
        {
            ModelState.AddModelError(
                key: string.Empty,
                errorMessage: identityError.Description
            );
        }

        // Xóa tài khoản vừa tạo vì chưa được gán vai trò Admin.
        await _userManager.DeleteAsync(newAdminUser);

        // Mọi đường đi của phương thức đều có giá trị trả về.
        return View(
            viewName: "CreateAdmin",
            model: createAdminViewModel
        );
    }
}