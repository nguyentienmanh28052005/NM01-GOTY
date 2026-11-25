# HanaMart24 → Unity — Kế hoạch 6: UI, input và lưu

Dùng `source/src/main.js` và markup/CSS của bản gốc làm mẫu bố cục, tiếng Việt và hành vi. Dùng font và TMP từ Kế hoạch 2. Chỉ thay nhãn hàng khi asset thay thế đã được duyệt.

## Task 1 — Trạng thái UI và lưu JSON (XONG)

- **Đã xong:**
  - `Assets/HanaMart/Scripts/Runtime/UI/SaveData.cs`: `GameSettings` khớp mặc định `hana24_v3` trong `main.js`, `SaveData` (cài đặt, kết thúc A/B, `tutSeen`, bảo toàn trường lạ/tương lai), `SaveStore` (đọc/gộp an toàn khi JSON hỏng hoặc sai kiểu, ghi thay thế nguyên tử `hana24_v3.json` qua file `.tmp`).
  - `Assets/HanaMart/Scripts/Runtime/UI/UiFlow.cs`: máy trạng thái `AppState` (`Boot`/`Menu`/`Play`/`Pause`), `UiScreen`, `UiAction`, `InputDeviceKind`, chặn tạm dừng khi đang mở overlay/kết thúc, khóa con trỏ theo thiết bị.
  - `Assets/HanaMart/Input/HanaMartControls.inputactions`: thêm map `UI` (`UiNavigate`, `UiConfirm`, `UiBack`, `UiTabPrev`, `UiTabNext`) hỗ trợ bàn phím + tay cầm, giữ nguyên map `Player`.
  - `Assets/HanaMart/Scripts/Runtime/UI/ShiftInterface.cs`, `ShiftSetup.cs`, hook `PlayerController`/`ShiftGame`/`ShiftHud`: khởi động ở `Boot`, camera attract ngoài giờ ca, menu/tạm dừng/cài đặt/hướng dẫn vỏ, đồng bộ `Time.timeScale = 0` khi tạm dừng và khôi phục khi tiếp tục/thoát, fallback font `Malgun Gothic` cho chữ Hàn.
  - **Xác nhận:** TDD RED (`p6-t1-red.json`, 12/12 fail) → GREEN (`p6-t1-green.json`, 12/12 pass) → toàn bộ `HanaMart.Tests.EditMode` **237/237 PASS** (`p6-t1-full.json`, job `60be88d5e5f14f74b2fc263efb95b7cb`). Play QA `Run Ui Flow` (`ui-flow-smoke.json`), `Run Story Milestones` (`story-smoke.json`), `Run Death Camera` (`death-camera.json`) đều đạt, `play-errors.log` và Console 0 lỗi/cảnh báo, ảnh `p6-t1-menu.png` và `p6-t1-pause.png` đã kiểm tra trực quan.

## Task 2 — HUD và hội thoại

- Dịch bố cục đồng hồ, nhiệm vụ, tay cầm đồ, tương tác, thanh giữ E, phụ đề, cảnh cáo và chỉ dẫn. Giữ giấy nội quy, phụ lục, nội quy giả, nhật ký và Sổ tay.
- Dịch lựa chọn hội thoại, recap thua/điểm checkpoint, hai màn kết thúc và hướng dẫn. Test mở/đóng từng loại cửa sổ, focus tay cầm, bố cục ở 16:9 và menu đang mở không nhận thao tác vào thế giới.

## Task 3 — Cài đặt và mức đồ họa

- Port tab Đồ họa, Âm thanh và Điều khiển. Giữ preset Thấp/Trung bình/Cao/Siêu cao/Điện ảnh, giá trị mặc định và tùy chọn khi khác phiên bản URP.
- Nối ánh sáng theo preset, độ phân giải động, FOV, độ nhạy, đảo Y, head bob, rung, các bus âm thanh, phụ đề, độ sáng, grain và camera mượt. Nối F3, H và tùy chọn ẩn HUD.
- Test setting đổi trong ca, tái tải giữ nguyên lựa chọn và reset cài đặt. So ảnh HUD/menu cùng độ phân giải Three.js, cập nhật HANDOFF và commit theo từng task.
