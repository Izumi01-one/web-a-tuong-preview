using LTLaserBlogMvc.Routes;
using System.Net;

namespace LTLaserBlogMvc.Services;

// Middleware giới hạn quyền truy cập vào khu vực quản trị theo địa chỉ IP
public sealed class NetworkGateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    // Nhận middleware tiếp theo và cấu hình của ứng dụng
    public NetworkGateMiddleware(
        RequestDelegate next,
        IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    // Phương thức được gọi khi có một HTTP request
    public async Task InvokeAsync(HttpContext context)
    {
        // Nếu không truy cập đường dẫn quản trị bí mật,
        // cho phép request tiếp tục bình thường
        PathString adminPath = $"/{AppRoutes.Admin.Base}";
        if (!context.Request.Path.StartsWithSegments(adminPath))
        {
            await _next(context);
            return;
        }

        // Lấy địa chỉ IP của thiết bị gửi request
        IPAddress? ipAddress = context.Connection.RemoteIpAddress;

        // Lấy danh sách các dải IP được phép từ appsettings.json
        string[] allowedCidrs =
            _configuration
                .GetSection("AdminNetwork:AllowedCidrs")
                .Get<string[]>()
            ?? Array.Empty<string>();

        // Cho phép truy cập nếu là IP nội bộ hoặc thuộc dải IP được cấu hình
        if (ipAddress is not null)
        {
            bool isLoopback = IPAddress.IsLoopback(ipAddress);
            bool isAllowedNetwork = IsAllowed(ipAddress, allowedCidrs);

            if (isLoopback || isAllowedNetwork)
            {
                await _next(context);
                return;
            }
        }

        // Trả về 404 để không tiết lộ sự tồn tại của trang quản trị
        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }

    // Kiểm tra IP có thuộc một trong các dải CIDR được phép hay không
    private static bool IsAllowed(
        IPAddress ipAddress,
        IEnumerable<string> ranges)
    {
        // Chuyển địa chỉ IPv4 được biểu diễn dưới dạng IPv6 về IPv4
        if (ipAddress.IsIPv4MappedToIPv6)
        {
            ipAddress = ipAddress.MapToIPv4();
        }

        // Duyệt qua từng dải CIDR trong danh sách
        foreach (string range in ranges)
        {
            if (InCidr(ipAddress, range))
            {
                return true;
            }
        }

        return false;
    }

    // Kiểm tra một địa chỉ IP có nằm trong một dải CIDR cụ thể hay không
    private static bool InCidr(
        IPAddress ipAddress,
        string cidr)
    {
        // Ví dụ: "192.168.1.0/24" được tách thành
        // địa chỉ mạng "192.168.1.0" và số bit mạng "24"
        string[] parts = cidr.Split('/');

        // CIDR phải có đúng hai thành phần
        if (parts.Length != 2)
        {
            return false;
        }

        // Chuyển phần địa chỉ mạng thành IPAddress
        bool isValidNetworkAddress =
            IPAddress.TryParse(parts[0], out IPAddress? networkAddress);

        // Chuyển phần số bit mạng thành số nguyên
        bool isValidPrefixLength =
            int.TryParse(parts[1], out int prefixLength);

        if (!isValidNetworkAddress ||
            !isValidPrefixLength ||
            networkAddress is null)
        {
            return false;
        }

        // IPv4 chỉ cho phép prefix từ 0 đến 32,
        // IPv6 chỉ cho phép prefix từ 0 đến 128
        int maximumPrefixLength =
            networkAddress.AddressFamily ==
            System.Net.Sockets.AddressFamily.InterNetwork
                ? 32
                : 128;

        if (prefixLength < 0 || prefixLength > maximumPrefixLength)
        {
            return false;
        }

        // IP và địa chỉ mạng phải cùng loại IPv4 hoặc IPv6
        if (ipAddress.AddressFamily != networkAddress.AddressFamily)
        {
            return false;
        }

        byte[] ipBytes = ipAddress.GetAddressBytes();
        byte[] networkBytes = networkAddress.GetAddressBytes();

        // So sánh từng byte theo số bit mạng của CIDR
        for (int index = 0; index < ipBytes.Length; index++)
        {
            // Xác định số bit cần so sánh trong byte hiện tại
            int bitsToCompare = Math.Clamp(
                prefixLength - index * 8,
                0,
                8);

            // Không còn bit nào cần so sánh
            if (bitsToCompare == 0)
            {
                break;
            }

            // Tạo mặt nạ mạng cho byte hiện tại
            byte mask = (byte)(0xFF << (8 - bitsToCompare));

            // So sánh phần mạng của hai địa chỉ IP
            if ((ipBytes[index] & mask) !=
                (networkBytes[index] & mask))
            {
                return false;
            }
        }

        // IP nằm trong dải CIDR
        return true;
    }
}