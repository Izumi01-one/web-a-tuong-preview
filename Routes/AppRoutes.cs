namespace LTLaserBlogMvc.Routes;

public static class AppRoutes
{
    public static class Home
    {
        public const string Index = "/";
    }

    public static class Blog
    {
        public const string Base = "bai-viet";

        public const string List = "";
        public const string Detail = "{slug}";
        public const string CoverImage = "anh/{id:int}";
        public const string ContentImage = "anh-noi-dung/{id:int}";
    }

    public static class Admin
    {
        public const string Base = "quan-tri";

        public const string Dashboard = "";
        public const string Login = "dang-nhap";
        public const string Logout = "dang-xuat";
        public const string Bootstrap = "khoi-tao";
        public const string CreatePost = "dang-bai";
        public const string EditPost = "sua/{id:int}";
        public const string DeletePost = "xoa/{id:int}";
        public const string PreviewPost = "xem-truoc/{id:int}";
        public const string Accounts = "tai-khoan";
    }
}