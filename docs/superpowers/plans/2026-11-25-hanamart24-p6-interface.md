# HanaMart24 → Unity — Kế hoạch 6: UI, input và lưu

Dùng `source/src/main.js` và markup/CSS của bản gốc làm mẫu bố cục, tiếng Việt và hành vi. Dùng font và TMP từ Kế hoạch 2. Chỉ thay nhãn hàng khi asset thay thế đã được duyệt.

## Task 1 — Trạng thái UI và lưu JSON

- Test RED cho giá trị mặc định của `hana24_v3`, ghép giữ dữ liệu khi có trường mới, load file hỏng, lưu thay thế an toàn và hai ending.
- Nối khởi động, menu, tạm dừng, xác nhận, quay lại và tiếp tục. Đồng bộ con trỏ, input của PlayerController và thời gian game. Kiểm qua bàn phím, chuột và tay cầm.

## Task 2 — HUD và hội thoại

- Dịch bố cục đồng hồ, nhiệm vụ, tay cầm đồ, tương tác, thanh giữ E, phụ đề, cảnh cáo và chỉ dẫn. Giữ giấy nội quy, phụ lục, nội quy giả, nhật ký và Sổ tay.
- Dịch lựa chọn hội thoại, recap thua/điểm checkpoint, hai màn kết thúc và hướng dẫn. Test mở/đóng từng loại cửa sổ, focus tay cầm, bố cục ở 16:9 và menu đang mở không nhận thao tác vào thế giới.

## Task 3 — Cài đặt và mức đồ họa

- Port tab Đồ họa, Âm thanh và Điều khiển. Giữ preset Thấp/Trung bình/Cao/Siêu cao/Điện ảnh, giá trị mặc định và tùy chọn khi khác phiên bản URP.
- Nối ánh sáng theo preset, độ phân giải động, FOV, độ nhạy, đảo Y, head bob, rung, các bus âm thanh, phụ đề, độ sáng, grain và camera mượt. Nối F3, H và tùy chọn ẩn HUD.
- Test setting đổi trong ca, tái tải giữ nguyên lựa chọn và reset cài đặt. So ảnh HUD/menu cùng độ phân giải Three.js, cập nhật HANDOFF và commit theo từng task.
