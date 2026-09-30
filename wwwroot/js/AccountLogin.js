// ẩn hiện mk đăng nhập
document.addEventListener("DOMContentLoaded", () => {
    const passwordInput =
        document.getElementById("password-input");

    const toggleButton =
        document.getElementById("password-toggle");

    const toggleText =
        document.getElementById("password-toggle-text");

    if (!passwordInput || !toggleButton || !toggleText) {
        return;
    }

    toggleButton.addEventListener("click", () => {
        const passwordIsHidden =
            passwordInput.type === "password";

        passwordInput.type =
            passwordIsHidden ? "text" : "password";

        toggleText.textContent =
            passwordIsHidden ? "Ẩn" : "Hiện";

        toggleButton.setAttribute(
            "aria-label",
            passwordIsHidden
                ? "Ẩn mật khẩu"
                : "Hiện mật khẩu"
        );

        toggleButton.setAttribute(
            "aria-pressed",
            String(passwordIsHidden)
        );

        passwordInput.focus();

        // Đặt con trỏ về cuối mật khẩu.
        const passwordLength = passwordInput.value.length;

        passwordInput.setSelectionRange(
            passwordLength,
            passwordLength
        );
    });
});