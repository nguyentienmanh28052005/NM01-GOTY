# HanaMart24 → Unity — Kế hoạch 5: Cảnh thua và điện ảnh

Nguồn chính là `deaths.js`, cùng `game.js:cinematic, updateCinematic, apparition` và hậu kỳ trong `main.js`. Khởi động từ sự kiện thua do Kế hoạch 4 phát ra. Giữ hệ tọa độ Three.js trong dữ liệu.

## Task 1 — Điều phối và camera (xong)

- Viết test RED cho chọn cảnh theo `SCENE_OF`, bộ đếm biến thể, thời điểm bỏ qua sau 1.4 giây, bước thời gian tối đa .05 giây, kết thúc và dọn tài nguyên khi retry hoặc đổi scene.
- Port điều khiển vị trí/hướng camera, nội suy mũ, fade/glitch/warp/đỏ/dolly, rung và khóa input. Phục hồi đèn, camera, hàng tạm ẩn và tham số phim khi kết thúc. Chạy test GREEN và smoke trước commit.

215/215 EditMode đạt, smoke `Run Death Camera` kiểm chuyển góc camera, skip và retry trong Play. Các sequence chuyên biệt nối ở Task 2–3.

## Task 2 — Cảnh với khách và người giả (xong)

- Dịch `counter`, `childMom`, `storage`, `mirae`. `SCENE_OF` dùng chung trình tự cho một số luật, với biến thể theo mã lỗi và lượt gặp như bản gốc. Giữ thời điểm đổi góc máy, tên âm thanh, vị trí đứng, kính nứt và lộ dạng.
- Test từng ID dẫn đúng nhánh, actor staging/despawn và timeline kết thúc hữu hạn. Chụp khung trước/sau ở các mốc nhận diện.

216/216 EditMode. MCP chạy trọn 4 cảnh, kiểm vết nứt kính và 6 bóng Mi-rae, cleanup khi retry. Ảnh `docs/visual/deaths/guest-scenes.jpg`.

## Task 3 — Cảnh với thiết bị và địa điểm (xong)

- Dịch `phone`, `freezer`, `cctv`, `box13`, `alley`, `blackout`, `red`, `chase`, `fired` và `lunge` dự phòng, cùng mọi biến thể trong nguồn.
- Test lịch lệnh, màu sáng, quy tắc vị trí không xuyên quầy/kệ và khôi phục trạng thái. Kiểm qua MCP, toàn assembly GREEN, ảnh các cảnh tiêu biểu, cập nhật HANDOFF và commit từng task.

220/220 EditMode (`e49104ecea974fe686eaf89ccf1525d9`). MCP chạy trọn 11 biến thể (`death-objects.json`), sửa bind đèn con, gỡ `BatchingStatic` cho cửa/nắp tủ đông/14 máng đèn trần, nối `HemisphereAmbient` + phát sáng máng đèn, fallback hành lang `DeathSpot`, và guard `Actor` khi thoát Play. Ảnh `docs/visual/deaths/object-scenes.jpg`.

## Task 4 — Nhịp phim giữa ca (xong)

- Nối các đoạn điện ảnh như slow motion, dolly và nhịp tim khi xảy ra biến cố, bóng rìa mắt, gợi ý âm thanh và im ắng. Giữ giá trị về thời lượng và di chuyển từ source.
- Test đoạn điện ảnh hết đúng hạn, giấy/đối thoại và kết thúc ca không kẹt input. Cập nhật sổ quyết định cùng kết quả Play.

225/225 EditMode (`2aa177c9b6cf4e65b1645810f6ea5d33`), `CinematicTests` 5/5. MCP `Run Shift Cinematics`, `Run Story Milestones` (cả A/B), `Run Death Camera` đạt; `play-errors.log` và Console (sau Play stop) 0 lỗi/cảnh báo. Kế hoạch 5 hoàn tất; tiếp theo: Kế hoạch 6.
