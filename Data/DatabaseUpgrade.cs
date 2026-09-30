using Microsoft.EntityFrameworkCore;

namespace LTLaserBlogMvc.Data;

public static class DatabaseUpgrade
{
    // Áp dụng các migration còn thiếu.
    public static async Task ApplyAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();
    }
}