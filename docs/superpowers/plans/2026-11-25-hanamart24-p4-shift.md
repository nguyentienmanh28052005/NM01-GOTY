# HanaMart24 → Unity — Kế hoạch 4: Kịch bản ca đêm

Nguồn hành vi là `source/src/game.js` trong bản Three.js. `GAME_DOC.md` giúp tra cứu bối cảnh. Khi hai tài liệu khác nhau, lấy giá trị chạy thật trong JavaScript. Chẳng hạn, mốc mất điện dùng 227±4 phút và số lượt dị thường theo ba giai đoạn là 4, 6, 8 với số lần thử đặt có giới hạn.

Mỗi task bắt đầu bằng test có `[Timeout]` và vòng lặp hữu hạn. Xác nhận RED qua MCP for Unity trước khi viết logic, chạy lại toàn bộ EditMode sau từng nhóm thay đổi, sau đó cập nhật HANDOFF, sổ quyết định và commit trên `manh`.

## Task 1 — Đồng hồ, lịch và checkpoint (xong)

- Tách đồng hồ ca thành cơ chế tăng 1 phút game mỗi 6/7/8 giây theo các mốc 120 và 240, giới hạn tại 360. Đóng băng đồng hồ khi đọc giấy hoặc đối thoại. Đồng hồ giả 04:44 chỉ đổi giá trị hiển thị.
- Tạo lịch có định danh và trạng thái đã chạy cho từng mốc trong `_buildEvents`. Dùng nguồn ngẫu nhiên tiêm được cho test và bot. Giữ thứ tự checkpoint, fax, đổi phase, tủ đông, giao hàng và các bẫy đã đặt lịch.
- Khôi phục giờ, cảnh cáo, nhật ký và cờ lâu dài từ checkpoint. Gỡ các cờ tức thời theo `loadCheckpoint`, tái lập lịch và nhiệm vụ theo điều kiện của bản gốc. Giữ thông tin học được và mức nới lỗi qua lần thử lại trong cùng ca.
- Test mốc cố định, cửa sổ jitter, khoảng cách dị thường, mỗi mốc chạy một lần, checkpoint lúc reload và giảm cảnh cáo đúng điều kiện.

137/137 EditMode đạt. Lịch bản gốc có 29 mốc cốt truyện. 4/6/8 là số dị thường tối đa mỗi phase vì dừng sau 300 lần thử. Đã chạy lại `_buildEvents` JavaScript với hai seed chung để kiểm số lượng và thời điểm. `ShiftRun` gọi checkpoint trước sự kiện cùng giờ và phát lại sự kiện đúng phút checkpoint khi retry.

## Task 2 — Khách, chào hỏi và quầy thu ngân (xong)

- Dịch `updateDirector`, `spawnVisit` và mẫu khách. Giữ trọng số, giới hạn, khoảng lặng, giảm xác suất khi lặp và số khách cùng lúc. Dùng Actor và đường đi của Kế hoạch 3, với cùng tọa độ `NODES`.
- Port trình tự khách đến, chuông 6 giây để chào, xếp hàng, lấy hàng, phục vụ, yêu cầu hâm, xử lý hàng quét và túi. Dùng hàng thay thế và tên hiển thị đã duyệt.
- Test chuông thật/giả, thứ tự xếp hàng, lựa chọn đúng/sai cho áo mưa, trẻ em, khách không bóng và yêu cầu vào kho. Kiểm chế độ 2 khách, bỏ lượt đã rời cửa hàng và cleanup đồ trên quầy.

161/161 EditMode. MCP Play `Run Checkout Visit` xác nhận khách áo mưa hợp lệ bước vào, kích hoạt chào, đặt và quét hai mì ly, trả ₩3.000, rời tiệm và dọn đồ. Điều phối lò và các biến cố nhận từ `SceneAction`/`RingRequested` ở Task 3–4.

## Task 3 — Nhiệm vụ và dị thường

- Nối `Interaction`, `CarryController`, `StoreDoors`, `AudioBank` với trạng thái ca. Tắt tương tác thử nghiệm mặc định khi có điều phối ca. Dùng timer theo thời gian game giống `later`.
- Dịch bổ sung hàng, nhiệt độ tủ mát, lau sàn, rác và hẻm, lò, nhật ký, camera, gọi điện, rơi đồ và dị thường môi trường. Giữ điểm đặt tay, các mốc lò 0.55, 0.95, 1.3 và 6 giây, giữ E đếm 10.
- Test trigger thực, hành động bị chặn theo trạng thái, refresh nhãn khi dùng đồ, phân biệt cửa đang mở và cảnh báo từ quầy. Chạy Play kiểm một vòng nhận việc, hoàn thành và reload checkpoint.

## Task 4 — Luật, bẫy, Giờ Đỏ và kết thúc

- Dịch các lời thoại và nhánh lựa chọn, 13 điều, phụ lục và phần Sổ tay. Giữ điện thoại, số hiện trên màn hình, biến thể giao hàng, giờ cấm tủ đông, người giả và hai kết thúc.
- Dịch từng kiểm tra vi phạm theo thời hạn, khoảng cách, hướng nhìn, hành vi và điều kiện của `update`. Cảnh cáo lần ba kết thúc ca với lý do tương ứng. Gửi sự kiện qua giao diện cho Kế hoạch 5 và 6.
- Dịch lịch tối/sáng và vị trí thực thể từ `redHour/updateRed`, điều kiện di chuyển 0.1 m sau .25 giây và hoàn tác ánh sáng khi xong.
- Test ranh giới 02:00/02:10, 04:44, 06:00, tủ đông và Giờ Đỏ, luồng kết thúc có/không cứu Ji-ho. Kiểm trạng thái camera và input được phục hồi sau retry.

## Task 5 — Tích hợp scene và các mốc kiểm thử

- Chạy các tuyến mốc deterministic qua cùng hệ thống điều khiển game. Log thời điểm các mốc, bẫy, checkpoint và kết thúc. Tách kết quả test logic khỏi kiểm tra hình ảnh/UI ở các kế hoạch sau.
- Xác nhận có thể đi qua cửa, kho, hẻm và đến quầy bằng input bình thường. Kiểm scene cũ không có diff, Console sạch trong smoke Play, toàn assembly GREEN. Ghi những sai lệch có chứng cứ vào HANDOFF.
