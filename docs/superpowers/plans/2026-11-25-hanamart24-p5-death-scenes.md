# HanaMart24 → Unity — Kế hoạch 5: Cảnh thua và điện ảnh

Nguồn chính là `deaths.js`, cùng `game.js:cinematic, updateCinematic, apparition` và hậu kỳ trong `main.js`. Khởi động từ sự kiện thua do Kế hoạch 4 phát ra. Giữ hệ tọa độ Three.js trong dữ liệu.

## Task 1 — Điều phối và camera (xong)

- Viết test RED cho chọn cảnh theo `SCENE_OF`, bộ đếm biến thể, thời điểm bỏ qua sau 1.4 giây, bước thời gian tối đa .05 giây, kết thúc và dọn tài nguyên khi retry hoặc đổi scene.
- Port điều khiển vị trí/hướng camera, nội suy mũ, fade/glitch/warp/đỏ/dolly, rung và khóa input. Phục hồi đèn, camera, hàng tạm ẩn và tham số phim khi kết thúc. Chạy test GREEN và smoke trước commit.

215/215 EditMode đạt, smoke `Run Death Camera` kiểm chuyển góc camera, skip và retry trong Play. Các sequence chuyên biệt nối ở Task 2–3.

## Task 2 — Cảnh với khách và người giả

- Dịch `counter`, `childMom`, `storage`, `mirae`. `SCENE_OF` dùng chung trình tự cho một số luật, với biến thể theo mã lỗi và lượt gặp như bản gốc. Giữ thời điểm đổi góc máy, tên âm thanh, vị trí đứng, kính nứt và lộ dạng.
- Test từng ID dẫn đúng nhánh, actor staging/despawn và timeline kết thúc hữu hạn. Chụp khung trước/sau ở các mốc nhận diện.

## Task 3 — Cảnh với thiết bị và địa điểm

- Dịch `phone`, `freezer`, `cctv`, `box13`, `alley`, `blackout`, `red`, `chase`, `fired` và `lunge` dự phòng, cùng mọi biến thể trong nguồn.
- Test lịch lệnh, màu sáng, quy tắc vị trí không xuyên quầy/kệ và khôi phục trạng thái. Kiểm qua MCP, toàn assembly GREEN, ảnh các cảnh tiêu biểu, cập nhật HANDOFF và commit từng task.

## Task 4 — Nhịp phim giữa ca

- Nối các đoạn điện ảnh như slow motion, dolly và nhịp tim khi xảy ra biến cố, bóng rìa mắt, gợi ý âm thanh và im ắng. Giữ giá trị về thời lượng và di chuyển từ source.
- Test đoạn điện ảnh hết đúng hạn, giấy/đối thoại và kết thúc ca không kẹt input. Cập nhật sổ quyết định cùng kết quả Play.
