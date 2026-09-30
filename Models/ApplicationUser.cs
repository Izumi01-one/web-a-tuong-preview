using Microsoft.AspNetCore.Identity;
namespace LTLaserBlogMvc.Models;

// Đại diện cho tài khoản người dùng trong hệ thống
public sealed class ApplicationUser : IdentityUser
{
    // Hiện tại sử dụng các thuộc tính có sẵn của IdentityUser,
    // ví dụ: Id, UserName, Email, PasswordHash và PhoneNumber.
}