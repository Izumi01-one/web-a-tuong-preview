using Microsoft.AspNetCore.Mvc;

namespace LTLaserBlogMvc.Controllers;

// Controller xử lý trang chủ và trang thông báo lỗi.
public sealed class HomeController : Controller
{
    // =========================================================
    // HIỂN THỊ TRANG CHỦ
    // =========================================================

    public IActionResult Index()
    {
        /*
         * Hiển thị file:
         * Views/Home/Index.cshtml
         *
         * ASP.NET Core MVC tự xác định View dựa trên:
         * - Tên Controller: HomeController
         * - Tên Action: Index
         */
        return View();
    }


    // =========================================================
    // HIỂN THỊ TRANG THÔNG BÁO LỖI
    // =========================================================

    public IActionResult Error()
    {
        /*
         * Hiển thị file:
         * Views/Home/Error.cshtml
         */
        return View();
    }
}