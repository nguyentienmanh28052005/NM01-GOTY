# HanaMart24 → Unity — Kế hoạch 7: Ca hoàn chỉnh và so hình

Chỉ đánh dấu hoàn tất khi có bằng chứng cho các mục nghiệm thu trong yêu cầu và design spec. Kết quả test logic trước đó không thay cho kiểm tra một ca thật qua Play mode.

## Task 1 — Mô phỏng nhiều ca **(XONG)**

- Đã port `sim.js` vào `Assets/HanaMart/Scripts/Runtime/Sim/{SimTypes,ShiftSimHost,SimBot}.cs` với đủ 3 mức (`careful` / `average` / `careless`), giữ nguyên hằng số nhận biết, đọc thoại, tốc độ, `SPOTS`, `DEATH2TRAP`, giới hạn bước/thời gian/retry, lưu seed và log sự kiện; bổ sung `ShiftGame.NewShift(int seed)` và menu Play QA `HanaMart/QA/Run Sim Batch` (`ShiftSimQA.cs`).
- TDD: RED 5/5 fail (`32364cb074a34337b8ac2b464d4bb67d`, `p7-t1-red.json`) → `SimBotTests` 5/5 PASS (`ea1fbb88d3bc4f82a9123119b60320a6`, `p7-t1-sim.json`) → toàn assembly **255/255 PASS** (`0309b5c02d824e1bbfa9c79ee2671935`, `p7-t1-full.json`, 15.47 s).
- Play QA `Run Sim Batch` (`Logs/HanaMart/sim-report.json`): `careful` 5/5 tới 06:00 (0 kẹt, TB **13.0** chết [11..16], 90.5 phút; khớp Three.js v2.2 ~13), `average` 5/5 tới 06:00 (0 kẹt, TB **20.2** chết [18..24], 119.7 phút; khớp Three.js v2.2 ~19), `careless` 4/4 tới 06:00 (0 kẹt, TB 28.25 chết); `play-errors.log` và Console 0 lỗi.

## Task 2 — So hình và hiệu năng

- Dùng `tools/three-export` chụp cặp ảnh Three.js và Unity với cùng vị trí, hướng, FOV, thời gian và preset. Giữ đủ các điểm chụp đang có, bổ sung cửa/mưa/đèn hẻm và cảnh có khách hoặc biến cố đại diện.
- Cập nhật `docs/visual/compare/report.md` với chênh lệch độ sáng, màu vùng tối và kiểm mắt. Xử lý chênh lệch tủ mát/hẻm, mưa và phản sáng còn mở trong báo cáo lần một. Ghi riêng khác biệt do bốn model và hàng thay thế đã duyệt.
- Đo thời gian frame ở cùng thiết lập trước/sau tối ưu. Kiểm URP với cảnh nặng có NPC và FX. Chỉ ghi số thực đo được.

## Task 3 — Chơi một ca thật và kiểm tra bàn giao

- Qua Unity Play và MCP, bắt đầu 00:00, đi qua mỗi giai đoạn, thử một retry đúng checkpoint, đi đến 06:00 và xác nhận màn kết thúc. Ghi mốc và thời lượng thực. Kiểm riêng bẫy hoặc kết thúc chưa gặp trong ca bằng lượt seed phù hợp.
- Kiểm Console và log trong suốt ca, sửa lỗi có thể tái hiện, chạy lại toàn assembly. Kiểm hai scene được bảo vệ, nhánh `manh`, các tệp bị loại và commit cuối.
- Cập nhật HANDOFF cùng báo cáo so hình và các bằng chứng ca thử. Báo rõ phần nào đã chơi, phần nào chỉ có test hoặc mô phỏng.
