// =========================================================
// 1. ĐIỀU KHIỂN MENU TRÊN THIẾT BỊ DI ĐỘNG
// =========================================================

// Lấy nút dùng để mở hoặc đóng menu.
const mobileMenuButton =
    document.querySelector(".menu-toggle");

// Lấy thanh điều hướng của trang web.
const navigationMenu =
    document.querySelector(".nav");

/*
 * Chỉ xử lý menu nếu cả nút menu và thanh điều hướng
 * đều tồn tại trong tài liệu HTML.
 */
if (
    mobileMenuButton !== null &&
    navigationMenu !== null
) {
    // -----------------------------------------------------
    // Mở hoặc đóng menu khi người dùng bấm nút
    // -----------------------------------------------------

    mobileMenuButton.addEventListener("click", () => {
        /*
         * classList.toggle("open") hoạt động như sau:
         *
         * - Nếu navigationMenu chưa có class "open":
         *   class "open" sẽ được thêm vào.
         *
         * - Nếu navigationMenu đã có class "open":
         *   class "open" sẽ bị xóa.
         *
         * Phương thức toggle trả về:
         * - true nếu class vừa được thêm.
         * - false nếu class vừa bị xóa.
         */
        const isMenuOpen =
            navigationMenu.classList.toggle("open");

        /*
         * Cập nhật thuộc tính aria-expanded để hỗ trợ
         * trình đọc màn hình và khả năng truy cập.
         *
         * setAttribute yêu cầu giá trị là chuỗi nên cần
         * chuyển boolean true hoặc false thành chuỗi.
         */
        mobileMenuButton.setAttribute(
            "aria-expanded",
            String(isMenuOpen)
        );
    });


    // -----------------------------------------------------
    // Đóng menu khi người dùng chọn một liên kết
    // -----------------------------------------------------

    // Lấy tất cả thẻ <a> nằm bên trong phần tử có class .nav.
    const navigationLinks =
        document.querySelectorAll(".nav a");

    navigationLinks.forEach((navigationLink) => {
        navigationLink.addEventListener("click", () => {
            // Xóa class open để đóng menu.
            navigationMenu.classList.remove("open");

            // Thông báo rằng menu đang ở trạng thái đóng.
            mobileMenuButton.setAttribute(
                "aria-expanded",
                "false"
            );
        });
    });
}


// =========================================================
// 2. SAO CHÉP THÔNG TIN LIÊN HỆ
// =========================================================

/*
 * Lấy phần tử thông báo ngắn.
 *
 * Ví dụ HTML:
 * <div class="toast">Đã sao chép</div>
 */
const toastNotification =
    document.querySelector(".toast");

/*
 * Biến này lưu ID của bộ hẹn giờ đang chạy.
 *
 * Nó được sử dụng để hủy bộ hẹn giờ cũ nếu người dùng
 * bấm nút sao chép nhiều lần liên tiếp.
 */
let toastTimerId;

// Lấy tất cả các nút có class .copy-btn.
const copyButtons =
    document.querySelectorAll(".copy-btn");

copyButtons.forEach((copyButton) => {
    /*
     * Hàm xử lý được khai báo với từ khóa async vì bên trong
     * có sử dụng await khi gọi Clipboard API.
     */
    copyButton.addEventListener("click", async () => {
        /*
         * Lấy nội dung cần sao chép từ thuộc tính data-copy.
         *
         * Ví dụ:
         * <button
         *     class="copy-btn"
         *     data-copy="example@gmail.com">
         *     Sao chép
         * </button>
         *
         * Nếu data-copy không tồn tại thì dùng chuỗi rỗng.
         */
        const textToCopy =
            copyButton.dataset.copy || "";

        try {
            /*
             * Sử dụng Clipboard API hiện đại để ghi nội dung
             * vào bộ nhớ tạm của thiết bị.
             *
             * Clipboard API thường chỉ hoạt động khi:
             * - Trang web sử dụng HTTPS.
             * - Hoặc đang chạy trên localhost.
             */
            await navigator.clipboard.writeText(textToCopy);
        } catch (error) {
            /*
             * Nếu Clipboard API không hoạt động, sử dụng
             * phương án dự phòng dành cho trình duyệt cũ
             * hoặc trang web không chạy bằng HTTPS.
             */

            // Tạo tạm một phần tử textarea.
            const temporaryTextArea =
                document.createElement("textarea");

            // Đưa nội dung cần sao chép vào textarea.
            temporaryTextArea.value = textToCopy;

            /*
             * Thêm textarea vào body.
             *
             * Phần tử phải được thêm vào tài liệu thì phương thức
             * select và lệnh copy mới hoạt động ổn định.
             */
            document.body.appendChild(temporaryTextArea);

            // Chọn toàn bộ nội dung trong textarea.
            temporaryTextArea.select();

            /*
             * Thực hiện lệnh sao chép.
             *
             * document.execCommand("copy") là phương pháp cũ,
             * chỉ được dùng làm phương án dự phòng.
             */
            document.execCommand("copy");

            // Xóa textarea tạm thời khỏi trang.
            temporaryTextArea.remove();
        }

        // Lưu lại nội dung ban đầu của nút.
        const originalButtonText =
            copyButton.textContent;

        // Đổi nội dung nút để thông báo sao chép thành công.
        copyButton.textContent = "Đã chép";


        // -------------------------------------------------
        // Hiển thị thông báo toast
        // -------------------------------------------------

        if (toastNotification !== null) {
            // Thêm class show để hiển thị thông báo.
            toastNotification.classList.add("show");

            /*
             * Hủy bộ hẹn giờ trước đó nếu người dùng bấm
             * nút sao chép nhiều lần liên tiếp.
             *
             * Việc này giúp thời gian hiển thị được tính lại
             * từ lần bấm gần nhất.
             */
            clearTimeout(toastTimerId);

            /*
             * Sau 1.800 mili giây, xóa class show
             * để ẩn thông báo.
             */
            toastTimerId = setTimeout(() => {
                toastNotification.classList.remove("show");
            }, 1800);
        }


        // -------------------------------------------------
        // Khôi phục nội dung ban đầu của nút
        // -------------------------------------------------

        setTimeout(() => {
            copyButton.textContent = originalButtonText;
        }, 1800);
    });
});


// =========================================================
// 3. HIỆU ỨNG HIỂN THỊ KHI CUỘN TRANG
// =========================================================

/*
 * IntersectionObserver theo dõi các phần tử trên trang
 * và phát hiện khi chúng xuất hiện trong vùng nhìn thấy
 * của trình duyệt.
 *
 * Khi một phần tử có class .reveal xuất hiện, JavaScript
 * sẽ thêm class .visible để kích hoạt hiệu ứng CSS.
 */
const revealObserver = new IntersectionObserver(
    /*
     * entries là danh sách các phần tử vừa thay đổi
     * trạng thái giao nhau với vùng nhìn thấy.
     */
    (observerEntries) => {
        observerEntries.forEach((observerEntry) => {
            /*
             * isIntersecting bằng true khi phần tử đã đi vào
             * vùng nhìn thấy của trình duyệt.
             */
            if (observerEntry.isIntersecting) {
                // Thêm class visible để kích hoạt hiệu ứng CSS.
                observerEntry.target.classList.add("visible");

                /*
                 * Ngừng theo dõi phần tử sau khi nó đã xuất hiện.
                 *
                 * Nhờ đó:
                 * - Hiệu ứng chỉ chạy một lần.
                 * - Trình duyệt không cần tiếp tục theo dõi.
                 */
                revealObserver.unobserve(observerEntry.target);
            }
        });
    },

    {
        /*
         * threshold bằng 0.12 nghĩa là callback được gọi
         * khi khoảng 12% diện tích của phần tử xuất hiện
         * trong vùng nhìn thấy.
         */
        threshold: 0.12
    }
);


// ---------------------------------------------------------
// Đăng ký theo dõi các phần tử có hiệu ứng xuất hiện
// ---------------------------------------------------------

// Lấy tất cả phần tử có class .reveal.
const revealElements =
    document.querySelectorAll(".reveal");

revealElements.forEach((revealElement) => {
    // Yêu cầu IntersectionObserver theo dõi phần tử này.
    revealObserver.observe(revealElement);
});


// =========================================================
// 4. TỰ ĐỘNG CẬP NHẬT NĂM Ở CHÂN TRANG
// =========================================================

/*
 * Lấy phần tử có id="year".
 *
 * Ví dụ HTML:
 * <span id="year"></span>
 */
const footerYearElement =
    document.getElementById("year");

// Chỉ cập nhật nếu phần tử tồn tại.
if (footerYearElement !== null) {
    /*
     * new Date() tạo đối tượng chứa ngày và giờ hiện tại.
     *
     * getFullYear() lấy năm gồm bốn chữ số,
     * ví dụ: 2026.
     */
    const currentYear =
        new Date().getFullYear();

    // Hiển thị năm hiện tại trong phần tử chân trang.
    footerYearElement.textContent =
        currentYear.toString();
}