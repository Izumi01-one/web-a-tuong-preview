using LTLaserBlogMvc.Data;
using LTLaserBlogMvc.Models;
using LTLaserBlogMvc.Routes;
using LTLaserBlogMvc.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Threading.RateLimiting;

// Tạo đối tượng builder để:
// - Đọc cấu hình từ appsettings.json và biến môi trường.
// - Đăng ký các dịch vụ vào Dependency Injection.
// - Chuẩn bị xây dựng ứng dụng ASP.NET Core.
WebApplicationBuilder builder =
    WebApplication.CreateBuilder(args);


// ============================================================
// 1. CẤU HÌNH KẾT NỐI CƠ SỞ DỮ LIỆU
// ============================================================

// Đọc chuỗi kết nối có tên "DefaultConnection".
//
// Chuỗi kết nối có thể được khai báo trong:
// - appsettings.json
// - appsettings.Production.json
// - Biến môi trường ConnectionStrings__DefaultConnection
//
// Nếu không tìm thấy chuỗi kết nối, chương trình sẽ dừng
// và hiển thị thông báo lỗi.
string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Thiếu ConnectionStrings__DefaultConnection"
    );

// Đăng ký AppDbContext vào hệ thống Dependency Injection.
//
// UseSqlServer cho biết ứng dụng sử dụng Microsoft SQL Server
// làm hệ quản trị cơ sở dữ liệu.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        connectionString,
        sqlOptions =>
        {
            // Thử lại khi SQL mất kết nối tạm thời.
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            );
        }
    );
});


// ============================================================
// 2. CẤU HÌNH ASP.NET CORE IDENTITY
// ============================================================

// Đăng ký hệ thống Identity dùng để:
// - Đăng nhập.
// - Đăng xuất.
// - Quản lý tài khoản.
// - Quản lý mật khẩu.
// - Quản lý vai trò như Admin.
//
// ApplicationUser là lớp đại diện cho người dùng.
// IdentityRole là lớp đại diện cho vai trò.
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(
        options =>
        {
            // -----------------------------
            // Cấu hình yêu cầu mật khẩu
            // -----------------------------

            // Mật khẩu phải có ít nhất 12 ký tự.
            options.Password.RequiredLength = 12;

            // Mật khẩu bắt buộc phải có chữ số.
            options.Password.RequireDigit = true;

            // Mật khẩu bắt buộc phải có chữ thường.
            options.Password.RequireLowercase = true;

            // Mật khẩu bắt buộc phải có chữ hoa.
            options.Password.RequireUppercase = true;

            // Mật khẩu bắt buộc phải có ký tự đặc biệt.
            // Ví dụ: !, @, #, $, %
            options.Password.RequireNonAlphanumeric = true;


            // -----------------------------
            // Cấu hình khóa tài khoản
            // -----------------------------

            // Tài khoản sẽ bị khóa sau 5 lần đăng nhập sai.
            options.Lockout.MaxFailedAccessAttempts = 5;

            // Tài khoản bị khóa trong vòng 15 phút.
            options.Lockout.DefaultLockoutTimeSpan =
                TimeSpan.FromMinutes(15);


            // -----------------------------
            // Cấu hình tài khoản người dùng
            // -----------------------------

            // Không cho phép nhiều tài khoản sử dụng cùng một email.
            options.User.RequireUniqueEmail = true;
        }
    )

    // Yêu cầu Identity sử dụng AppDbContext
    // để lưu người dùng, mật khẩu và vai trò vào cơ sở dữ liệu.
    .AddEntityFrameworkStores<AppDbContext>()

    // Đăng ký các token mặc định cho:
    // - Đặt lại mật khẩu.
    // - Xác nhận email.
    // - Xác thực hai bước nếu được sử dụng.
    .AddDefaultTokenProviders();


// ============================================================
// 3. CẤU HÌNH COOKIE ĐĂNG NHẬP
// ============================================================

builder.Services.ConfigureApplicationCookie(
    options =>
    {
        // Đường dẫn đến trang đăng nhập.
        //
        // Khi người dùng chưa đăng nhập nhưng truy cập trang
        // yêu cầu quyền, hệ thống sẽ chuyển về đường dẫn này.
        options.LoginPath = $"/{AppRoutes.Admin.Base}/{AppRoutes.Admin.Login}";



        // Đường dẫn được sử dụng khi người dùng đã đăng nhập
        // nhưng không có đủ quyền truy cập.
        options.AccessDeniedPath = $"/{AppRoutes.Admin.Base}/{AppRoutes.Admin.Login}";

        // Không cho JavaScript phía trình duyệt đọc cookie đăng nhập.
        // Cấu hình này giúp giảm nguy cơ đánh cắp cookie bằng XSS.
        options.Cookie.HttpOnly = true;

        // Cookie được gửi trong các yêu cầu cùng trang web
        // và một số trường hợp điều hướng từ trang bên ngoài.
        options.Cookie.SameSite = SameSiteMode.Lax;

        // Chỉ yêu cầu cookie bảo mật khi kết nối hiện tại dùng HTTPS.
        //
        // Trong môi trường Production, nếu website luôn dùng HTTPS,
        // có thể cân nhắc sử dụng CookieSecurePolicy.Always.
        options.Cookie.SecurePolicy =
            CookieSecurePolicy.SameAsRequest;

        // Phiên đăng nhập có thời hạn 2 giờ.
        options.ExpireTimeSpan =
            TimeSpan.FromHours(2);

        // Nếu người dùng tiếp tục hoạt động,
        // thời hạn cookie có thể được gia hạn.
        options.SlidingExpiration = true;
    }
);


// ============================================================
// 4. ĐĂNG KÝ MVC VÀ ANTIFORGERY
// ============================================================

// Đăng ký các dịch vụ cần thiết cho mô hình MVC,
// bao gồm Controller và Razor View.
builder.Services.AddControllersWithViews();

// Cấu hình cookie chống giả mạo yêu cầu CSRF.
//
// Cookie Antiforgery sẽ sử dụng chính sách bảo mật
// tương ứng với kiểu kết nối hiện tại.
builder.Services.AddAntiforgery(
    options =>
    {
        options.Cookie.SecurePolicy =
            CookieSecurePolicy.SameAsRequest;
    }
);


// ============================================================
// 5. CẤU HÌNH GIỚI HẠN SỐ LƯỢNG YÊU CẦU
// ============================================================

builder.Services.AddRateLimiter(
    options =>
    {
        // Khi vượt quá giới hạn, máy chủ trả về mã HTTP 429.
        //
        // 429 có nghĩa là Too Many Requests.
        options.RejectionStatusCode =
            StatusCodes.Status429TooManyRequests;

        // Tạo chính sách giới hạn yêu cầu có tên "login".
        //
        // Chính sách này cần được áp dụng vào Controller hoặc Action,
        // ví dụ bằng thuộc tính:
        //
        // [EnableRateLimiting("login")]
        options.AddPolicy(
            policyName: "login",
            partitioner: httpContext =>
            {
                // Lấy địa chỉ IP của người gửi yêu cầu.
                //
                // Nếu không lấy được IP, sử dụng chuỗi "unknown".
                string clientIpAddress =
                    httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown";

                // Mỗi địa chỉ IP có một bộ đếm giới hạn riêng.
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIpAddress,
                    factory: partition =>
                    {
                        // Cấu hình giới hạn theo cửa sổ thời gian cố định.
                        return new FixedWindowRateLimiterOptions
                        {
                            // Một địa chỉ IP chỉ được gửi tối đa
                            // 8 yêu cầu trong mỗi cửa sổ thời gian.
                            PermitLimit = 8,

                            // Mỗi cửa sổ thời gian kéo dài 10 phút.
                            Window = TimeSpan.FromMinutes(10),

                            // Không đưa yêu cầu vượt giới hạn vào hàng đợi.
                            // Yêu cầu vượt giới hạn sẽ bị từ chối ngay.
                            QueueLimit = 0
                        };
                    }
                );
            }
        );
    }
);


// ============================================================
// 6. CẤU HÌNH REVERSE PROXY
// ============================================================

// Forwarded Headers được sử dụng khi ứng dụng chạy phía sau:
// - Coolify
// - Nginx
// - Traefik
// - Load balancer
// - Reverse proxy khác
//
// Reverse proxy gửi IP thật và giao thức thật của người dùng
// thông qua các header như X-Forwarded-For và X-Forwarded-Proto.
builder.Services.Configure<ForwardedHeadersOptions>(
    options =>
    {
        // Cho phép ứng dụng đọc địa chỉ IP thật
        // từ header X-Forwarded-For.
        //
        // Đồng thời đọc giao thức HTTP hoặc HTTPS thật
        // từ header X-Forwarded-Proto.
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedProto;

        // Chỉ tin tưởng một lớp reverse proxy gần nhất.
        options.ForwardLimit = 1;

        // Lấy danh sách IP reverse proxy đáng tin cậy
        // từ phần ReverseProxy:KnownProxies trong cấu hình.
        string[] knownProxyAddresses =
            builder.Configuration
                .GetSection("ReverseProxy:KnownProxies")
                .Get<string[]>()
            ?? [];

        // Duyệt qua từng địa chỉ được cấu hình.
        foreach (string proxyAddress in knownProxyAddresses)
        {
            // Thử chuyển chuỗi thành đối tượng IPAddress.
            bool isValidIpAddress =
                IPAddress.TryParse(
                    proxyAddress,
                    out IPAddress? proxyIpAddress
                );

            // Chỉ thêm địa chỉ hợp lệ vào danh sách proxy tin cậy.
            if (isValidIpAddress && proxyIpAddress is not null)
            {
                options.KnownProxies.Add(proxyIpAddress);
            }
        }
    }
);


// ============================================================
// 7. XÂY DỰNG ỨNG DỤNG
// ============================================================

// Tạo ứng dụng ASP.NET Core từ các cấu hình
// và dịch vụ đã đăng ký phía trên.
WebApplication app = builder.Build();


// ============================================================
// 8. CẤU HÌNH MIDDLEWARE PIPELINE
// ============================================================

// Đọc các header do reverse proxy gửi đến.
//
// Middleware này cần được gọi sớm để các middleware phía sau
// nhận đúng địa chỉ IP và giao thức HTTPS của người dùng.
app.UseForwardedHeaders();

// Kiểm tra ứng dụng có đang chạy ở môi trường Development hay không.
if (!app.Environment.IsDevelopment())
{
    // Khi có lỗi chưa được xử lý trong môi trường Production,
    // chuyển yêu cầu đến action /Home/Error.
    app.UseExceptionHandler("/Home/Error");

    // Bật HTTP Strict Transport Security.
    //
    // Trình duyệt sẽ được yêu cầu chỉ truy cập website bằng HTTPS
    // trong khoảng thời gian được cấu hình.
    app.UseHsts();
}

// Chuyển yêu cầu HTTP sang HTTPS.
app.UseHttpsRedirection();

// Cho phép truy cập các file tĩnh trong thư mục wwwroot,
// ví dụ CSS, JavaScript, hình ảnh và font chữ.
app.UseStaticFiles();

// Bật hệ thống định tuyến của ASP.NET Core.
app.UseRouting();

// Bật middleware giới hạn số lượng yêu cầu.
//
// Middleware này cần thiết để chính sách "login"
// có thể hoạt động.
app.UseRateLimiter();

// Chạy NetworkGateMiddleware.
//
// Middleware tùy chỉnh này có thể dùng để kiểm soát
// mạng hoặc địa chỉ IP được phép truy cập.
app.UseMiddleware<NetworkGateMiddleware>();

// Đọc cookie đăng nhập và xác định người dùng hiện tại.
app.UseAuthentication();

// Kiểm tra người dùng có quyền truy cập tài nguyên hay không.
//
// UseAuthorization phải được đặt sau UseAuthentication.
app.UseAuthorization();


// ============================================================
// 9. CẤU HÌNH ROUTE MVC MẶC ĐỊNH
// ============================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// Route mặc định có ý nghĩa:
//
// - Nếu không ghi controller thì dùng HomeController.
// - Nếu không ghi action thì dùng action Index.
// - id là tham số không bắt buộc.
//
// Ví dụ:
// /
//     -> HomeController.Index()
//
// /Posts/Details/5
//     -> PostsController.Details(5)


// ============================================================
// 10. NÂNG CẤP CƠ SỞ DỮ LIỆU VÀ TẠO ROLE ADMIN
// ============================================================

// Tạo một Dependency Injection scope tạm thời.
//
// Scope này được sử dụng để lấy các dịch vụ có vòng đời Scoped,
// chẳng hạn AppDbContext và RoleManager.
using (IServiceScope scope = app.Services.CreateScope())
{
    // Lấy AppDbContext từ hệ thống Dependency Injection.
    AppDbContext databaseContext =
        scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Thực hiện nâng cấp hoặc migration cơ sở dữ liệu.
    //
    // DatabaseUpgrade là lớp tự viết trong project.
    await DatabaseUpgrade.ApplyAsync(databaseContext);

    // Lấy RoleManager để quản lý các vai trò Identity.
    RoleManager<IdentityRole> roleManager =
        scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

    // Kiểm tra vai trò "Admin" đã tồn tại hay chưa.
    bool adminRoleExists =
        await roleManager.RoleExistsAsync("Admin");

    // Nếu vai trò Admin chưa tồn tại thì tạo mới.
    if (!adminRoleExists)
    {
        IdentityRole adminRole =
            new IdentityRole("Admin");

        IdentityResult createRoleResult =
            await roleManager.CreateAsync(adminRole);

        // Nếu không tạo được role Admin thì dừng chương trình
        // và hiển thị danh sách lỗi.
        if (!createRoleResult.Succeeded)
        {
            string errorMessage = string.Join(
                separator: "; ",
                values: createRoleResult.Errors.Select(
                    error => error.Description
                )
            );

            throw new InvalidOperationException(
                $"Không thể tạo vai trò Admin: {errorMessage}"
            );
        }
    }
}


// ============================================================
// 11. KHỞI ĐỘNG ỨNG DỤNG
// ============================================================

// Khởi động web server và bắt đầu nhận yêu cầu.
app.Run();