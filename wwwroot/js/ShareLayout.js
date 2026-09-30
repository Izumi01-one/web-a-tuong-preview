//  Điều khiển menu trên thiết bị di động
const menuButton = document.querySelector('.menu-toggle');
const nav = document.querySelector('.nav');

if (menuButton && nav) {
    menuButton.addEventListener('click', () => {
        const isOpen = nav.classList.toggle('open');
        menuButton.setAttribute('aria-expanded', String(isOpen));
    });

    // Đóng menu sau khi người dùng chọn liên kết.
    document.querySelectorAll('.nav a').forEach((link) => {
        link.addEventListener('click', () => {
            nav.classList.remove('open');
            menuButton.setAttribute('aria-expanded', 'false');
        });
    });
}

// Hiển thị thêm bài viết theo từng nhóm.
const postList = document.querySelector('#postList');
const loadMoreButton = document.querySelector('#loadMorePosts');

if (postList && loadMoreButton) {
    const posts = Array.from(
        postList.querySelectorAll('.post-row')
    );

    // Số bài hiển thị ban đầu.
    const initialPostCount = 4;

    // Số bài được mở thêm mỗi lần nhấn nút.
    const postsPerLoad = 4;

    let visiblePostCount = initialPostCount;

    function updatePostVisibility() {
        posts.forEach((post, index) => {
            const shouldHide = index >= visiblePostCount;

            post.classList.toggle('is-hidden', shouldHide);
        });

        // Ẩn nút khi đã hiển thị hết bài viết.
        if (visiblePostCount >= posts.length) {
            loadMoreButton.hidden = true;
        } else {
            loadMoreButton.hidden = false;
        }
    }

    loadMoreButton.addEventListener('click', () => {
        visiblePostCount += postsPerLoad;
        updatePostVisibility();
    });

    updatePostVisibility();
}

// Sao chép thông tin liên hệ và hiển thị thông báo toast
const toastNotification = document.querySelector(".toast");
let toastTimerId;
const copyButtons = document.querySelectorAll(".copy-btn");

copyButtons.forEach((copyButton) => {
    copyButton.addEventListener("click", async () => {
        const textToCopy = copyButton.dataset.copy || "";
        try {
            await navigator.clipboard.writeText(textToCopy);
        } catch (error) {
            const temporaryTextArea = document.createElement("textarea");
            temporaryTextArea.value = textToCopy;
            document.body.appendChild(temporaryTextArea);
            temporaryTextArea.select();
            document.execCommand("copy");
            temporaryTextArea.remove();
        }

        const originalButtonText = copyButton.textContent;
        copyButton.textContent = "Đã chép";

        if (toastNotification !== null) {
            toastNotification.classList.add("show");
            clearTimeout(toastTimerId);
            toastTimerId = setTimeout(() => {
                toastNotification.classList.remove("show");
            }, 1800);
        }

        setTimeout(() => {
            copyButton.textContent = originalButtonText;
        }, 1800);
    });
});

// Tự động cập nhật năm ở chân trang
const footerYearElement = document.getElementById("year");
if (footerYearElement !== null) {
    footerYearElement.textContent = new Date().getFullYear().toString();
}


