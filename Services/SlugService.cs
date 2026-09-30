using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LTLaserBlogMvc.Services;

// Lớp hỗ trợ chuyển tiêu đề bài viết thành slug thân thiện với URL.
// Ví dụ: "Hướng dẫn lập trình C#" thành "huong-dan-lap-trinh-c".

// Từ khóa partial được sử dụng vì phương thức Regex bên dưới được tự động tạo mã bởi GeneratedRegex.
public static partial class SlugService
{
    // Chuyển chuỗi value thành slug thân thiện với URL.
    public static string Create(string value)
    {
        // Chuẩn hóa chuỗi sang dạng FormD.
        // FormD tách ký tự có dấu thành chữ cái gốc và phần dấu riêng.
        //
        // Ví dụ: "ế" được tách thành "e" và các thành phần dấu.
        var s = value.Normalize(NormalizationForm.FormD);

        // Tạo đối tượng StringBuilder để xây dựng chuỗi sau khi bỏ dấu.
        var b = new StringBuilder();

        // Duyệt qua từng ký tự trong chuỗi đã chuẩn hóa.
        foreach (var c in s)
        {
            // Kiểm tra loại Unicode của ký tự.
            //
            // NonSpacingMark là các thành phần dấu như:
            // dấu sắc, huyền, hỏi, ngã và nặng.
            //
            // Nếu ký tự không phải thành phần dấu thì thêm vào chuỗi mới.
            if (CharUnicodeInfo.GetUnicodeCategory(c)
                != UnicodeCategory.NonSpacingMark)
            {
                b.Append(c);
            }
        }

        // Chuyển StringBuilder thành chuỗi và chuẩn hóa lại về FormC.
        s = b.ToString().Normalize(NormalizationForm.FormC);

        // Chuyển tất cả ký tự thành chữ thường.
        s = s.ToLowerInvariant();

        // Chữ "đ" không bị loại bỏ bởi quá trình chuẩn hóa Unicode,
        // vì vậy cần chuyển thủ công từ "đ" thành "d".
        s = s.Replace('đ', 'd');

        // Thay một hoặc nhiều ký tự không hợp lệ bằng dấu gạch ngang.
        // Sau đó, Trim('-') xóa dấu gạch ngang thừa ở đầu và cuối.
        //
        // Ví dụ: "lap  trinh c#" thành "lap-trinh-c".
        s = Invalid().Replace(s, "-").Trim('-');

        // Nếu slug dài hơn 180 ký tự:
        // - Chỉ lấy 180 ký tự đầu tiên.
        // - Loại bỏ dấu gạch ngang ở cuối nếu có.
        //
        // Nếu slug không quá 180 ký tự thì trả về nguyên chuỗi.
        return s.Length > 180 ? s[..180].TrimEnd('-') : s;
    }

    // Tạo biểu thức chính quy để tìm các ký tự không hợp lệ.
    // [^a-z0-9] có nghĩa là ký tự không thuộc:
    // - Chữ thường từ a đến z.
    // - Chữ số từ 0 đến 9.
    // Dấu + có nghĩa là một hoặc nhiều ký tự không hợp lệ liên tiếp.

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex Invalid();

}

   