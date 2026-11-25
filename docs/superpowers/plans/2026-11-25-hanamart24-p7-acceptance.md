# HanaMart24 → Unity — Kế hoạch 7: Ca hoàn chỉnh và so hình

Chỉ đánh dấu hoàn tất khi có bằng chứng cho các mục nghiệm thu trong yêu cầu và design spec. Kết quả test logic trước đó không thay cho kiểm tra một ca thật qua Play mode.

## Task 1 — Mô phỏng nhiều ca

- Dịch `sim.js` vào bộ chạy ca thử với các mức cẩn thận, trung bình và bất cẩn, cùng hằng số nhận biết, đọc thoại, tốc độ và ngẫu nhiên. Giới hạn tối đa thời gian/số bước, lưu seed và log sự kiện cho mỗi ca.
- Chạy ca mô phỏng qua hành vi cùng bộ luật như gameplay, so phân phối chết/cảnh cáo/retry/ending với Three.js khi chạy được nguồn tham chiếu. Tái hiện mọi kẹt với đúng seed, thêm test RED trước khi sửa và chạy lại.

## Task 2 — So hình và hiệu năng

- Dùng `tools/three-export` chụp cặp ảnh Three.js và Unity với cùng vị trí, hướng, FOV, thời gian và preset. Giữ đủ các điểm chụp đang có, bổ sung cửa/mưa/đèn hẻm và cảnh có khách hoặc biến cố đại diện.
- Cập nhật `docs/visual/compare/report.md` với chênh lệch độ sáng, màu vùng tối và kiểm mắt. Xử lý chênh lệch tủ mát/hẻm, mưa và phản sáng còn mở trong báo cáo lần một. Ghi riêng khác biệt do bốn model và hàng thay thế đã duyệt.
- Đo thời gian frame ở cùng thiết lập trước/sau tối ưu. Kiểm URP với cảnh nặng có NPC và FX. Chỉ ghi số thực đo được.

## Task 3 — Chơi một ca thật và kiểm tra bàn giao

- Qua Unity Play và MCP, bắt đầu 00:00, đi qua mỗi giai đoạn, thử một retry đúng checkpoint, đi đến 06:00 và xác nhận màn kết thúc. Ghi mốc và thời lượng thực. Kiểm riêng bẫy hoặc kết thúc chưa gặp trong ca bằng lượt seed phù hợp.
- Kiểm Console và log trong suốt ca, sửa lỗi có thể tái hiện, chạy lại toàn assembly. Kiểm hai scene được bảo vệ, nhánh `manh`, các tệp bị loại và commit cuối.
- Cập nhật HANDOFF cùng báo cáo so hình và các bằng chứng ca thử. Báo rõ phần nào đã chơi, phần nào chỉ có test hoặc mô phỏng.
