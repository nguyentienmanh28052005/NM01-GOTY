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

## Task 3 — Chơi một ca thật và kiểm tra bàn giao

- Qua Unity Play và MCP, bắt đầu 00:00, đi qua mỗi giai đoạn, thử một retry đúng checkpoint, đi đến 06:00 và xác nhận màn kết thúc. Ghi mốc và thời lượng thực. Kiểm riêng bẫy hoặc kết thúc chưa gặp trong ca bằng lượt seed phù hợp.
- Kiểm Console và log trong suốt ca, sửa lỗi có thể tái hiện, chạy lại toàn assembly. Kiểm hai scene được bảo vệ, nhánh `manh`, các tệp bị loại và commit cuối.
- Cập nhật HANDOFF cùng báo cáo so hình và các bằng chứng ca thử. Báo rõ phần nào đã chơi, phần nào chỉ có test hoặc mô phỏng.
