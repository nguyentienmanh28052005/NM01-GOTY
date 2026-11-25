# So sánh hình ảnh Three.js ↔ Unity

## 1. Bảng so sánh 14 góc máy (1280×720, FOV 72)

| Góc | Độ sáng TB Three | Độ sáng TB Unity | Chênh lệch | Vùng tối Three (R,G,B) | Vùng tối Unity (R,G,B) |
|---|---|---|---|---|---|
| entrance | 134 | 128 | -4% | (35, 77, 89) | (47, 59, 79) |
| counter | 122 | 122 | +0% | (27, 42, 81) | (24, 40, 81) |
| aisle1 | 128 | 122 | -4% | (41, 78, 89) | (55, 59, 69) |
| aisle2 | 120 | 126 | +5% | (43, 65, 83) | (37, 62, 81) |
| aisle3 | 113 | 115 | +2% | (36, 58, 76) | (37, 52, 80) |
| fridges | 125 | 99 | -21% | (37, 47, 75) | (34, 47, 78) |
| noodleBar | 109 | 125 | +14% | (33, 36, 52) | (23, 35, 65) |
| storage | 137 | 141 | +3% | (74, 66, 66) | (60, 69, 83) |
| freezer | 132 | 137 | +4% | (75, 69, 71) | (55, 70, 92) |
| alley | 38 | 36 | -4% | (42, 28, 17) | (26, 27, 41) |
| street | 110 | 107 | -3% | (17, 25, 44) | (16, 28, 54) |
| glassFromOutside | 118 | 109 | -7% | (22, 30, 48) | (16, 29, 56) |
| doorRain | 81 | 99 | +22% | (31, 41, 65) | (25, 38, 72) |
| alleyLamp | 52 | 33 | -37% | (47, 32, 24) | (21, 17, 23) |

## 2. Xử lý các mục còn mở từ lần so sánh 1
- **Hẻm sau (`alley`: −48% → −4%)**: đã bổ sung bù suy giảm ánh sáng cho các đèn Three.js dùng `decay < 2` (`alleyLight` decay 1.2, `alleySpot` decay 1.3) và ép `LightRenderMode.ForcePixel` trong `StoreImporter.cs`. Tường gạch và nền hẻm nay nhận rõ ánh cam ấm như bản gốc. Ở góc `alleyLamp` (−37%) hướng thẳng về cửa sắt sau, đèn `alleyBulb` không có halo bloom rộng bằng Three.js nhưng vẫn giữ rõ tương phản vùng tối/sáng.
- **Sàn vinyl trong nhà**: `StoreImporter.MaterialConverter` nay đọc `roughnessMap` từ `store.gltf` để tính `_Smoothness` trung bình thay vì để mặc định nhám hoàn toàn khi `roughnessFactor == 1`; sàn trong nhà (`entrance`, `glassFromOutside`) phản sáng máng đèn trần rõ hơn.
- **Mưa, vệt phản chiếu đường ướt và quầng đèn đường**: đã thêm `StoreWeather` + shader `Universal Render Pipeline/HanaMart/RainStreaks` (mưa GPU theo mức đồ họa), `Universal Render Pipeline/HanaMart/LampCone` (quầng đèn đường `S:cones[0..1]` trên đường), và 12 vệt phản chiếu mặt đường ướt (`wetStreaks` ngoài cửa kính, biển hiệu, neon đối diện, đèn đường, đèn hẻm).
- **Kiểm tra bằng mắt & cảnh động (`dynamic-scenes.jpg`)**: đã kiểm tra ảnh ghép `alley.jpg`, `alleyLamp.jpg`, `street.jpg`, `doorRain.jpg`, `entrance.jpg`, `fridges.jpg` và ảnh Play mode `dynamic-scenes.jpg` (khách áo mưa vàng đứng tại quầy thanh toán; Giờ Đỏ `05:15` với ánh sáng đỏ và tờ `HÓA ĐƠN #0000`).
- **Sắp xếp & hướng nhãn hàng trên kệ (`aisle1.jpg`, `aisle2.jpg`, `aisle3.jpg`, `fridges.jpg`)**: đã sửa hướng dựng thẳng đứng (`ProductPlanogram.ComputeUprightAlignment` căn mặt nhãn `+Z` và chiều đứng `+dP/dv -> +Y` theo hệ UV Unity), trích xuất từ thư viện mẫu đơn lẻ chuẩn ở vùng trên cao (`y > 15`) của `магазин.glb` (139 mẫu across 11 nhóm gồm `candy`, `household`, `household2`), gom thành từng khối mặt hàng đồng nhất theo tầng giống demo (`GameScene.unity` / Three.js) thay vì xếp xen kẽ từng món, và scale đầy chiều cao kệ có giới hạn tỷ lệ để không bị thu nhỏ.

## 3. Khác biệt do model & hàng thay thế đã duyệt
- **Tủ mát (`fridges`: −21%) và kệ hàng**: theo quyết định thiết kế đã duyệt, hàng hóa dùng gói `Assets/Art/Model/super-market-for-free` (hộp đồ ăn lạnh/mì/snack nhỏ và tối màu hơn chai/lon phát sáng vẽ thủ tục của bản Three.js); riêng thuốc lá giữ hộp bản gốc.
- **Nhân vật**: dùng 4 model Mixamo (`Ch31`, `Ch08`, `Ch01`, `Ch42`) theo Hướng A (nhuộm màu áo quần bằng mask, lớp áo mưa vàng phủ ngoài, trẻ em thu nhỏ ×0.68) thay cho người dựng khối của Three.js.

## 4. Hiệu năng URP thực đo (Play mode, `Logs/HanaMart/perf-report.json`)
Đo trên **NVIDIA GeForce RTX 3050 Ti Laptop GPU**, **12th Gen Intel Core i5-12500H**, Unity **6000.3.8f1** Editor Play mode ở độ phân giải **2560×1440**, có **3 NPC đang hoạt động + mưa/vệt đường ướt/quầng đèn (`StoreWeather`) + HUD** (30 khung khởi động + 180 khung đo cho mỗi mức):

| Mức đồ họa | Dynamic Res | Số hạt mưa | FPS TB | 1% Low FPS | Frame TB (ms) | P95 Frame (ms) | Render Scale |
|---|---|---|---|---|---|---|---|
| `low` | Tắt | 1200 | **56.4** | 32.4 | 17.73 | 24.61 | 0.75 |
| `medium` | Tắt | 2400 | **55.5** | 20.9 | 18.01 | 29.09 | 1.00 |
| `medium_dynres` | Bật | 2400 | **67.1** | 46.0 | 14.90 | 18.64 | 1.00 |
| `high` | Tắt | 3600 | **70.2** | 48.0 | 14.25 | 18.81 | 1.00 |
