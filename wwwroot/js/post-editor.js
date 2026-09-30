(() => {
    // =========================================================
    // 1. LẤY CÁC PHẦN TỬ CHÍNH TRONG TRANG
    // =========================================================

    // Phần tử chứa danh sách các khối nội dung.
    const blocksContainer = document.querySelector("#blocks");

    // Template dùng để tạo một khối nội dung mới.
    const blockTemplate = document.querySelector("#block-template");

    // Nếu không tìm thấy khu vực chứa khối hoặc template
    // thì dừng chạy JavaScript để tránh lỗi.
    if (blocksContainer === null || blockTemplate === null) {
        return;
    }


    // =========================================================
    // 2. ẨN HOẶC HIỆN TRƯỜNG NHẬP THEO LOẠI KHỐI
    // =========================================================

    /**
     * Hiển thị trường nhập phù hợp với loại khối.
     *
     * Loại 3 là khối hình ảnh:
     * - Ẩn khu vực nhập văn bản.
     * - Hiện khu vực chọn hình ảnh.
     *
     * Các loại còn lại:
     * - Hiện khu vực nhập văn bản.
     * - Ẩn khu vực chọn hình ảnh.
     *
     * @param {HTMLElement} blockElement Khối nội dung cần xử lý.
     */
    function toggleBlockFields(blockElement) {
        // Lấy ô chọn loại khối.
        const blockTypeSelect =
            blockElement.querySelector(".block-type");

        // Nếu không tìm thấy ô chọn loại khối thì dừng xử lý.
        if (blockTypeSelect === null) {
            return;
        }

        // Kiểm tra xem khối hiện tại có phải khối hình ảnh không.
        const isImageBlock = blockTypeSelect.value === "3";

        // Lấy khu vực chứa các trường nhập văn bản.
        const textFields =
            blockElement.querySelector(".text-fields");

        // Lấy khu vực chứa các trường nhập hình ảnh.
        const imageFields =
            blockElement.querySelector(".image-fields");

        // Nếu là khối hình ảnh thì ẩn trường văn bản.
        if (textFields !== null) {
            textFields.hidden = isImageBlock;
        }

        // Nếu không phải khối hình ảnh thì ẩn trường hình ảnh.
        if (imageFields !== null) {
            imageFields.hidden = !isImageBlock;
        }
    }


    // =========================================================
    // 3. CẬP NHẬT THỨ TỰ VÀ TÊN CÁC INPUT
    // =========================================================

    /**
     * Cập nhật lại chỉ số của tất cả các khối.
     *
     * Ví dụ:
     * Blocks[0].Content
     * Blocks[1].Content
     * Blocks[2].Content
     *
     * Việc này giúp ASP.NET Core MVC nhận đúng danh sách Blocks
     * khi biểu mẫu được gửi lên Controller.
     */
    function refreshBlocks() {
        // Chuyển HTMLCollection thành mảng để có thể dùng forEach.
        const blockElements = Array.from(blocksContainer.children);

        blockElements.forEach((blockElement, blockIndex) => {
            // Lưu vị trí hiện tại của khối trong thuộc tính data-index.
            blockElement.dataset.index = blockIndex.toString();

            // Lấy phần tử hiển thị tên của khối.
            const blockLabel =
                blockElement.querySelector(".block-label");

            // Cập nhật nhãn hiển thị, ví dụ: Khối 1, Khối 2...
            if (blockLabel !== null) {
                blockLabel.textContent = `Khối ${blockIndex + 1}`;
            }

            // Lấy tất cả input, textarea hoặc select
            // có thuộc tính data-field trong khối.
            const fields =
                blockElement.querySelectorAll("[data-field]");

            fields.forEach((fieldElement) => {
                // Lấy tên thuộc tính từ data-field.
                // Ví dụ: Content, Image hoặc Caption.
                const fieldName = fieldElement.dataset.field;

                // Nếu data-field tồn tại thì cập nhật thuộc tính name.
                if (fieldName !== undefined) {
                    fieldElement.name =
                        `Blocks[${blockIndex}].${fieldName}`;
                }
            });

            // Cập nhật phần nhập liệu phù hợp với loại khối.
            toggleBlockFields(blockElement);
        });
    }


    // =========================================================
    // 4. THÊM KHỐI NỘI DUNG MỚI
    // =========================================================

    // Lấy tất cả nút có thuộc tính data-add.
    // Ví dụ:
    // <button type="button" data-add="1">Thêm tiêu đề</button>
    // <button type="button" data-add="3">Thêm hình ảnh</button>
    const addBlockButtons =
        document.querySelectorAll("[data-add]");

    addBlockButtons.forEach((addButton) => {
        addButton.addEventListener("click", () => {
            // Lấy phần tử đầu tiên bên trong template.
            const templateBlock =
                blockTemplate.content.firstElementChild;

            // Nếu template không có nội dung thì không thể tạo khối mới.
            if (templateBlock === null) {
                return;
            }

            // Sao chép toàn bộ khối mẫu.
            // true nghĩa là sao chép cả các phần tử con bên trong.
            const newBlock = templateBlock.cloneNode(true);

            // Lấy ô chọn loại khối trong khối vừa tạo.
            const blockTypeSelect =
                newBlock.querySelector(".block-type");

            // data-add chứa loại khối mà người dùng muốn thêm.
            // Ví dụ data-add="3" tương ứng với khối hình ảnh.
            const selectedBlockType = addButton.dataset.add;

            if (
                blockTypeSelect !== null &&
                selectedBlockType !== undefined
            ) {
                blockTypeSelect.value = selectedBlockType;
            }

            // Thêm khối mới vào cuối danh sách.
            blocksContainer.appendChild(newBlock);

            // Cập nhật lại chỉ số và tên các trường nhập.
            refreshBlocks();

            // Cuộn màn hình tới khối vừa được tạo.
            newBlock.scrollIntoView({
                behavior: "smooth",
                block: "center"
            });
        });
    });


    // =========================================================
    // 5. XÓA HOẶC DI CHUYỂN KHỐI
    // =========================================================

    /*
     * Chỉ gắn một sự kiện click vào phần tử cha #blocks.
     * Kỹ thuật này được gọi là Event Delegation.
     *
     * Nhờ đó, các khối được thêm sau này vẫn có thể sử dụng
     * các nút xóa, di chuyển lên và di chuyển xuống.
     */
    blocksContainer.addEventListener("click", (event) => {
        // event.target là phần tử thực sự được người dùng bấm vào.
        const clickedElement = event.target;

        // Đảm bảo phần tử được bấm là một Element.
        if (!(clickedElement instanceof Element)) {
            return;
        }

        // Tìm khối .editor-block gần nhất chứa phần tử được bấm.
        const currentBlock =
            clickedElement.closest(".editor-block");

        // Nếu người dùng không bấm bên trong một khối thì dừng xử lý.
        if (currentBlock === null) {
            return;
        }

        // -----------------------------------------------------
        // Xóa khối
        // -----------------------------------------------------

        const removeButton =
            clickedElement.closest(".remove");

        if (removeButton !== null) {
            currentBlock.remove();

            // Sau khi xóa phải cập nhật lại thứ tự các khối.
            refreshBlocks();

            return;
        }

        // -----------------------------------------------------
        // Di chuyển khối lên trên
        // -----------------------------------------------------

        const moveUpButton =
            clickedElement.closest(".up");

        const previousBlock =
            currentBlock.previousElementSibling;

        if (
            moveUpButton !== null &&
            previousBlock !== null
        ) {
            /*
             * Chèn currentBlock vào trước previousBlock.
             *
             * Ví dụ ban đầu:
             * Khối A
             * Khối B
             *
             * Nếu currentBlock là B thì kết quả:
             * Khối B
             * Khối A
             */
            blocksContainer.insertBefore(
                currentBlock,
                previousBlock
            );

            refreshBlocks();

            return;
        }

        // -----------------------------------------------------
        // Di chuyển khối xuống dưới
        // -----------------------------------------------------

        const moveDownButton =
            clickedElement.closest(".down");

        const nextBlock =
            currentBlock.nextElementSibling;

        if (
            moveDownButton !== null &&
            nextBlock !== null
        ) {
            /*
             * Chèn khối tiếp theo lên trước khối hiện tại.
             *
             * Ví dụ ban đầu:
             * Khối A
             * Khối B
             *
             * Nếu currentBlock là A thì kết quả:
             * Khối B
             * Khối A
             */
            blocksContainer.insertBefore(
                nextBlock,
                currentBlock
            );

            refreshBlocks();
        }
    });


    // =========================================================
    // 6. XỬ LÝ KHI NGƯỜI DÙNG ĐỔI LOẠI KHỐI
    // =========================================================

    blocksContainer.addEventListener("change", (event) => {
        const changedElement = event.target;

        if (!(changedElement instanceof Element)) {
            return;
        }

        // Chỉ xử lý khi phần tử thay đổi có class block-type.
        const isBlockTypeSelect =
            changedElement.classList.contains("block-type");

        if (!isBlockTypeSelect) {
            return;
        }

        // Tìm khối chứa ô chọn loại vừa thay đổi.
        const currentBlock =
            changedElement.closest(".editor-block");

        if (currentBlock !== null) {
            toggleBlockFields(currentBlock);
        }
    });


    // =========================================================
    // 7. LẤY CÁC PHẦN TỬ DÙNG CHO CHỨC NĂNG XEM TRƯỚC
    // =========================================================

    // Hộp thoại xem trước bài viết.
    const previewDialog =
        document.querySelector("#preview-dialog");

    // Khu vực chứa nội dung xem trước.
    const previewContent =
        document.querySelector("#preview-content");

    // Nút mở phần xem trước.
    const previewButton =
        document.querySelector("#preview-button");

    // Nút đóng phần xem trước.
    const previewCloseButton =
        document.querySelector("#preview-close");


    // =========================================================
    // 8. HÀM TẠO HÌNH ẢNH XEM TRƯỚC TỪ FILE
    // =========================================================

    /**
     * Tạo thẻ img từ file người dùng đã chọn.
     *
     * @param {File} imageFile File hình ảnh.
     * @returns {HTMLImageElement} Thẻ img được tạo.
     */
    function createPreviewImage(imageFile) {
        const imageElement = document.createElement("img");

        /*
         * URL.createObjectURL tạo một URL tạm thời cho file.
         * Nhờ đó trình duyệt có thể hiển thị file trước khi
         * file được tải lên server.
         */
        imageElement.src = URL.createObjectURL(imageFile);

        return imageElement;
    }


    // =========================================================
    // 9. HÀM TẠO NỘI DUNG XEM TRƯỚC CHO MỘT KHỐI
    // =========================================================

    /**
     * Tạo phần tử HTML xem trước cho một khối nội dung.
     *
     * Các giá trị loại khối:
     * 1: Tiêu đề
     * 2: Đoạn văn
     * 3: Hình ảnh
     * 4: Trích dẫn
     * 5: Thông báo
     * 6: Danh sách
     *
     * @param {Element} blockElement Khối nội dung cần xem trước.
     * @returns {HTMLElement} Phần tử HTML được tạo.
     */
    function createBlockPreview(blockElement) {
        // Lấy ô chọn loại khối.
        const blockTypeSelect =
            blockElement.querySelector(".block-type");

        // Lấy ô nhập nội dung.
        const contentField =
            blockElement.querySelector('[data-field="Content"]');

        // Lấy ô chọn hình ảnh.
        const imageField =
            blockElement.querySelector('[data-field="Image"]');

        // Lấy ô nhập chú thích ảnh.
        const captionField =
            blockElement.querySelector('[data-field="Caption"]');

        // Lấy loại khối. Nếu không tồn tại thì dùng chuỗi rỗng.
        let blockType = "";

        if (blockTypeSelect !== null) {
            blockType = blockTypeSelect.value;
        }

        // Lấy nội dung văn bản. Nếu không có thì dùng chuỗi rỗng.
        let textContent = "";

        if (contentField !== null) {
            textContent = contentField.value;
        }

        // -----------------------------------------------------
        // Loại 1: Tiêu đề cấp 2
        // -----------------------------------------------------

        if (blockType === "1") {
            const headingElement =
                document.createElement("h2");

            headingElement.textContent = textContent;

            return headingElement;
        }

        // -----------------------------------------------------
        // Loại 3: Hình ảnh
        // -----------------------------------------------------

        if (blockType === "3") {
            const figureElement =
                document.createElement("figure");

            // Kiểm tra xem người dùng đã chọn file hay chưa.
            if (
                imageField !== null &&
                imageField.files !== null &&
                imageField.files.length > 0
            ) {
                const selectedImage = imageField.files[0];

                const imageElement =
                    createPreviewImage(selectedImage);

                figureElement.appendChild(imageElement);
            }

            // Lấy nội dung chú thích ảnh.
            let captionText = "";

            if (captionField !== null) {
                captionText = captionField.value;
            }

            // Chỉ tạo figcaption nếu có nội dung chú thích.
            if (captionText.trim() !== "") {
                const captionElement =
                    document.createElement("figcaption");

                captionElement.textContent = captionText;

                figureElement.appendChild(captionElement);
            }

            return figureElement;
        }

        // -----------------------------------------------------
        // Loại 4: Trích dẫn
        // -----------------------------------------------------

        if (blockType === "4") {
            const quoteElement =
                document.createElement("blockquote");

            quoteElement.textContent = textContent;

            return quoteElement;
        }

        // -----------------------------------------------------
        // Loại 5: Thông báo
        // -----------------------------------------------------

        if (blockType === "5") {
            const noticeElement =
                document.createElement("aside");

            noticeElement.className = "notice";
            noticeElement.textContent = textContent;

            return noticeElement;
        }

        // -----------------------------------------------------
        // Loại 6: Danh sách
        // -----------------------------------------------------

        if (blockType === "6") {
            const listElement =
                document.createElement("ul");

            /*
             * Mỗi dòng trong ô nhập sẽ trở thành một thẻ <li>.
             *
             * Ví dụ người dùng nhập:
             * Java
             * C#
             * JavaScript
             *
             * Kết quả:
             * <ul>
             *     <li>Java</li>
             *     <li>C#</li>
             *     <li>JavaScript</li>
             * </ul>
             */
            const lines = textContent.split("\n");

            lines.forEach((line) => {
                // Xóa khoảng trắng ở đầu và cuối dòng.
                const cleanedLine = line.trim();

                // Không tạo thẻ li cho dòng trống.
                if (cleanedLine === "") {
                    return;
                }

                const listItem =
                    document.createElement("li");

                listItem.textContent = cleanedLine;

                listElement.appendChild(listItem);
            });

            return listElement;
        }

        // -----------------------------------------------------
        // Mặc định: Đoạn văn
        // -----------------------------------------------------

        const paragraphElement =
            document.createElement("p");

        paragraphElement.textContent = textContent;

        return paragraphElement;
    }


    // =========================================================
    // 10. MỞ HỘP THOẠI XEM TRƯỚC
    // =========================================================

    if (
        previewButton !== null &&
        previewDialog !== null &&
        previewContent !== null
    ) {
        previewButton.addEventListener("click", () => {
            /*
             * Xóa toàn bộ nội dung xem trước cũ.
             * Điều này tránh việc nội dung bị lặp lại mỗi lần
             * người dùng bấm nút xem trước.
             */
            previewContent.replaceChildren();

            // Lấy ô nhập tiêu đề.
            const titleInput =
                document.querySelector('[name="Title"]');

            // Lấy ô nhập mô tả tóm tắt.
            const summaryInput =
                document.querySelector('[name="Summary"]');

            // Lấy ô chọn ảnh bìa.
            const coverImageInput =
                document.querySelector('[name="CoverImage"]');

            // Lấy giá trị tiêu đề.
            let title = "";

            if (titleInput !== null) {
                title = titleInput.value;
            }

            // Lấy giá trị mô tả tóm tắt.
            let summary = "";

            if (summaryInput !== null) {
                summary = summaryInput.value;
            }

            // -------------------------------------------------
            // Tạo tiêu đề bài viết
            // -------------------------------------------------

            const titleElement =
                document.createElement("h1");

            titleElement.textContent = title;

            previewContent.appendChild(titleElement);

            // -------------------------------------------------
            // Tạo phần mô tả tóm tắt
            // -------------------------------------------------

            const summaryElement =
                document.createElement("p");

            summaryElement.className = "lead";
            summaryElement.textContent = summary;

            previewContent.appendChild(summaryElement);

            // -------------------------------------------------
            // Hiển thị ảnh bìa nếu người dùng đã chọn file
            // -------------------------------------------------

            if (
                coverImageInput !== null &&
                coverImageInput.files !== null &&
                coverImageInput.files.length > 0
            ) {
                const coverImageFile =
                    coverImageInput.files[0];

                const coverImageElement =
                    createPreviewImage(coverImageFile);

                previewContent.appendChild(coverImageElement);
            }

            // -------------------------------------------------
            // Tạo nội dung xem trước cho từng khối
            // -------------------------------------------------

            const blockElements =
                Array.from(blocksContainer.children);

            blockElements.forEach((blockElement) => {
                const previewElement =
                    createBlockPreview(blockElement);

                previewContent.appendChild(previewElement);
            });

            // Mở hộp thoại xem trước.
            previewDialog.showModal();
        });
    }


    // =========================================================
    // 11. ĐÓNG HỘP THOẠI XEM TRƯỚC
    // =========================================================

    if (
        previewCloseButton !== null &&
        previewDialog !== null
    ) {
        previewCloseButton.addEventListener("click", () => {
            previewDialog.close();
        });
    }


    // =========================================================
    // 12. KHỞI TẠO LẦN ĐẦU KHI TRANG ĐƯỢC TẢI
    // =========================================================

    // Cập nhật thứ tự, tên input và trạng thái hiển thị
    // của các khối đã có sẵn trong HTML.
    refreshBlocks();
})();