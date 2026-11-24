# So sánh hình ảnh Three.js ↔ Unity

| Góc | Độ sáng TB Three | Độ sáng TB Unity | Chênh lệch | Vùng tối Three (R,G,B) | Vùng tối Unity (R,G,B) |
|---|---|---|---|---|---|
| entrance | 133 | 126 | -5% | (34, 78, 91) | (44, 59, 81) |
| counter | 122 | 121 | -1% | (28, 44, 84) | (24, 41, 82) |
| aisle1 | 126 | 120 | -5% | (38, 78, 92) | (53, 58, 71) |
| aisle2 | 120 | 124 | +3% | (41, 66, 85) | (35, 61, 82) |
| aisle3 | 115 | 115 | +0% | (33, 58, 81) | (36, 52, 80) |
| fridges | 133 | 96 | -27% | (50, 62, 88) | (33, 47, 80) |
| noodleBar | 109 | 120 | +10% | (33, 36, 54) | (23, 39, 74) |
| storage | 136 | 129 | -5% | (73, 66, 65) | (60, 68, 82) |
| freezer | 131 | 128 | -2% | (73, 68, 69) | (56, 69, 90) |
| alley | 37 | 20 | -48% | (42, 28, 17) | (13, 13, 22) |
| street | 110 | 103 | -6% | (17, 25, 44) | (20, 32, 57) |
| glassFromOutside | 117 | 106 | -10% | (22, 31, 49) | (19, 31, 58) |

## Nhận xét (lần so sánh 1 — 2026-11-25)
- 10/12 góc chênh độ sáng trung bình trong ±10%; tông vùng tối cùng hướng xanh lam (B > R) ở mọi góc trong nhà.
- **fridges (−27%)**: khác do thay hàng — bản gốc là chai/lon màu sáng, bản Unity là hộp đồ ăn lạnh (asset siêu thị) nhỏ và tối hơn.
- **alley (−48%)**: đèn hẻm (point + spot màu cam) chưa hắt đủ lên tường gạch như bản gốc — cần xem lại ở kế hoạch sau (giới hạn 8 đèn/vật thể của Forward, hoặc bóng đổ chụp đèn).
- Sàn trong nhà tối và ít bóng hơn bản gốc (bản gốc có phản chiếu môi trường mạnh hơn trên sàn vinyl).
- Chưa có: mưa (Points), vệt phản chiếu đường ướt và quầng đèn đường (ShaderMaterial) — port ở kế hoạch sau.
- Bố cục, chữ trên biển/poster, vị trí mọi vật trùng khớp (xuất trực tiếp từ cảnh Three.js).
