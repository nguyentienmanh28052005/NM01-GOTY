# HanaMart24 → Unity — Kế hoạch 7: Ca hoàn chỉnh và so hình

Chỉ đánh dấu hoàn tất khi có bằng chứng cho các mục nghiệm thu trong yêu cầu và design spec. Kết quả test logic trước đó không thay cho kiểm tra một ca thật qua Play mode.

## Task 1 — Mô phỏng nhiều ca **(XONG)**

- Đã port `sim.js` vào `Assets/HanaMart/Scripts/Runtime/Sim/{SimTypes,ShiftSimHost,SimBot}.cs` với đủ 3 mức (`careful` / `average` / `careless`), giữ nguyên hằng số nhận biết, đọc thoại, tốc độ, `SPOTS`, `DEATH2TRAP`, giới hạn bước/thời gian/retry, lưu seed và log sự kiện; bổ sung `ShiftGame.NewShift(int seed)` và menu Play QA `HanaMart/QA/Run Sim Batch` (`ShiftSimQA.cs`).
- TDD: RED 5/5 fail (`32364cb074a34337b8ac2b464d4bb67d`, `p7-t1-red.json`) → `SimBotTests` 5/5 PASS (`ea1fbb88d3bc4f82a9123119b60320a6`, `p7-t1-sim.json`) → toàn assembly **255/255 PASS** (`0309b5c02d824e1bbfa9c79ee2671935`, `p7-t1-full.json`, 15.47 s).
- Play QA `Run Sim Batch` (`Logs/HanaMart/sim-report.json`): `careful` 5/5 tới 06:00 (0 kẹt, TB **13.0** chết [11..16], 90.5 phút; khớp Three.js v2.2 ~13), `average` 5/5 tới 06:00 (0 kẹt, TB **20.2** chết [18..24], 119.7 phút; khớp Three.js v2.2 ~19), `careless` 4/4 tới 06:00 (0 kẹt, TB 28.25 chết); `play-errors.log` và Console 0 lỗi.

## Task 2 — So hình và hiệu năng **(XONG)**

- Đã bổ sung `RainStreaks.shader`, `LampCone.shader`, `StoreWeather.cs` (mưa GPU, 12 vệt đường ướt, 2 quầng đèn đường `S:cones[0..1]`), bù decay ánh sáng hẻm + `ForcePixel` và tính `_Smoothness` từ `roughnessMap` trong `StoreImporter.cs`.
- TDD: `StoreWeatherTests` RED 5/5 (`7a4768c1d3964aed9324218d34cbcc6e`, `p7-t2-red.json`) → GREEN 5/5 (`1017c8329c4e42de91087e9b0b607cea`, `p7-t2-weather.json`) → toàn assembly **260/260 PASS** (`08cc3dac3fe8437292975b70d87c8e5f`, `p7-t2-full.json`, 17.77 s).
- Đã chụp 14 góc Three.js (`capture_three_cdp.mjs`) ↔ Unity (`HanaMart/Capture Viewpoints`) + 2 cảnh động Play (`dynamic-scenes.jpg`); cập nhật `docs/visual/compare/report.md` (`alley` từ −48% → −4%, `fridges` −21% do hàng thay thế, ghi rõ khác biệt 4 model & asset đã duyệt).
- Thực đo URP Play mode 2560×1440 (`Logs/HanaMart/perf-report.json`, 3 NPC + FX + HUD): `low` 56.4 FPS (17.73 ms), `medium` 55.5 FPS (18.01 ms), `medium_dynres` 67.1 FPS (14.90 ms), `high` 70.2 FPS (14.25 ms).

## Task 3 — Chơi một ca thật và kiểm tra bàn giao **(XONG)**

- Đã thêm `Assets/HanaMart/Scripts/Editor/ShiftFullPlayQA.cs` (`HanaMart/QA/Run Full Shift Acceptance`) và `ShiftGame.ChaseSafeThreePos` (`ShiftGame.Night.cs`).
- Qua Unity Play + MCP (`Logs/HanaMart/full-shift-play.json`): chạy liên tục `00:00→06:00` (seed `7001`, `timeScale = 30`, **99.54 s** thực, **0 chết ngoài ý muốn**), đi qua Phase 1/2/3, kiểm chứng retry đúng checkpoint `02:00` (tủ đông luật 6), qua Giờ Đỏ `05:15` và đạt Kết thúc A lúc `06:00` (`94.34 s`); kiểm thêm Kết thúc B (5 nhật ký + Ji-ho `05:00→06:00`) và bẫy luật 12 (`04:44`) + luật 10 (mất điện) với retry phục hồi sạch.
- `Logs/HanaMart/play-errors.log` rỗng, Unity Console 0 lỗi/cảnh báo, toàn bộ `HanaMart.Tests.EditMode` đạt **260/260 PASS** (`p7-final-full`), hai scene bảo vệ không có diff, nhánh `manh` sạch.
