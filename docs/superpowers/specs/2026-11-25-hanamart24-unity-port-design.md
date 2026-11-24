# HanaMart24 → Unity (project NM01-GOTY) — Bản thiết kế chuyển đổi

- Ngày: 2026-11-25 · Trạng thái: **Chờ bạn duyệt**
- Bản gốc: `E:\Downloads\top\top\HanaMart24` (Three.js v2.2, tài liệu `GAME_DOC.md`, mã nguồn `source/src/`)
- Đích: `E:\Unity\My Project\Main\NM01-GOTY` (Unity 6000.3.8f1, URP 17.3)

## 1. Mục đích & lý do
Chuyển nguyên game kinh dị quy tắc "QUY TẮC CA ĐÊM — HANA MART 24" (bản Three.js v2.2) sang Unity.
Lý do: bản Three.js dùng người dựng khối và hàng vẽ bằng code nên chưa đạt chất lượng hình ảnh bạn muốn cho Steam;
Unity cho phép dùng nhân vật Mixamo thật, asset thật và hậu kỳ tốt hơn — trong khi **trải nghiệm phải giữ nguyên**.

## 2. Mục tiêu / Không làm / Coi là thất bại
**Mục tiêu**
- Giữ y nguyên: bố cục (cửa hàng 12×8 m, kho, hẻm, phố), màu sắc & ánh sáng (xanh lam kiểu *Dead End Job*), toàn bộ kịch bản v2.2:
  13 điều + phụ lục + điều 14, nội quy giả, 3 phase, đạo diễn khách, mốc cố định & mốc lệch ngẫu nhiên, dị thường nhỏ, 6+3 bẫy,
  Giờ Đỏ, 15 màn chết riêng, 2 kết thúc, nhật ký Ji-ho, Sổ tay sống sót, cảnh cáo, checkpoint mỗi giờ, khoảng lặng/im ắng trước bão,
  menu/cài đặt/tạm dừng, chuột-phím + tay cầm (rung), mức đồ họa, chế độ quay video (H), F3.
- Nhân vật: 4 model trong `Assets/Art/Model/Charactor`, material/texture đúng cho cả 4.
- Hàng bán trong cửa hàng: thay bằng asset `super-market-for-free`; món không có tương đương → chọn món tương tự trong asset đó.
- Kệ, quầy, tủ, kho, hẻm, phố: dựng như bản Three.js.
- Trước khi giao: chơi thử toàn bộ, sửa hết lỗi, so hình ảnh với bản Three.js.

**Không làm (lần này)**
- Nội dung mới (bản 13 đêm, phòng nghỉ…), đổi luật/nhịp game.
- Kệ Sketchfab hay model mua ngoài.
- Âm thanh thu thật (dùng âm thanh tổng hợp xuất từ bản Three.js).
- Đóng gói Steam/Windows build phát hành.

**Coi là thất bại dù "chạy được"**
- Bố cục lệch so với bản gốc; màu "đẹp hơn" nhưng khác không khí bản gốc.
- Thiếu sự kiện/màn chết/kết thúc để kịp tiến độ.
- Không chơi được trọn ca 00:00 → 06:00 (kẹt, chết lặp vô hạn, lỗi Console).
- Làm hỏng scene `Animation Test` hiện có, hoặc commit git khi bạn chưa yêu cầu.

## 3. Ràng buộc
- Unity 6000.3.8f1, URP 17.3, Input System 1.18, Cinemachine 3.1.7, AI Navigation 2.0.10, glTFast 6.21, Timeline 1.8.10, uGUI 2.0, Test Framework 1.6.
- Claude làm việc qua MCP for Unity (điều khiển Editor đang mở) + sửa file trong thư mục project. Unity phải mở project trong suốt quá trình.
- **URP không có đèn vùng (area light) thời gian thực** — bản gốc dùng RectAreaLight bật/tắt liên tục (mất điện, chập, Giờ Đỏ).
- **URP không có tone mapping AgX** — bản gốc dùng AgX (exposure 1.02) + shader phim tự viết.
- Model Mixamo đang nhập kiểu **Generic**; chỉ có **1 clip thân người** (`Female Walk`) + 9 clip mặt của Ch31.
- Asset siêu thị: ~3.480 món đều là **hộp chữ nhật dán ảnh** (atlas ảnh sản phẩm thật, nhiều món Hàn: mì Buldak, snack Hàn, bánh bao mandu), cộng 1 máy tính tiền. Không có lon/chai.
- Git chưa có LFS; FBX 54–87 MB đang chưa commit.

## 4. Các cách đã cân nhắc
| Cách | Mô tả | Được/mất |
|---|---|---|
| **1. Dịch 1:1 có công cụ (ĐÃ CHỌN)** | Script Editor dựng lại cửa hàng từ đúng số liệu `store.js`/`dress.js`; xuất texture canvas + âm thanh từ bản Three.js; dịch logic `game.js` sang C# giữ nguyên số liệu, dùng hạ tầng Unity | Trung thành nhất, làm lại được; tốn công viết công cụ lúc đầu |
| 2. Dựng tay qua MCP | Đặt từng vật bằng lệnh MCP | Hàng nghìn lệnh, chậm, dễ lệch, khó làm lại |
| 3. Dùng tòa nhà siêu thị trong asset, chỉ port gameplay | Nhanh, đẹp sẵn | Bố cục khác bản gốc → trái yêu cầu, loại |

## 5. Quyết định của bạn
- Port y nguyên v2.2 (bố cục, màu, kịch bản).
- Nhân vật: 4 model; `Female Walk - Lat Mat` đã đúng; sửa material cho 3 model còn lại.
- **Hướng A**: dùng lại 4 model cho mọi vai, đổi màu quần áo, thêm áo mưa vàng phủ ngoài, trẻ em = thu nhỏ model người lớn (tạm).
- Hàng hóa: dùng asset siêu thị; món không có → chọn món tương tự trong asset.
- Kệ: tạm dùng kệ như bản Three.js.
- Cách làm: **Cách 1**.
- Phải tự kiểm thử toàn bộ + so hình ảnh trước khi bàn giao.
- Khi gần chạm giới hạn 5 giờ: bàn giao cho Codex (app ChatGPT/Codex) qua computer use.
- Git: commit sau mỗi giai đoạn lên nhánh **`manh`** (nhánh đang dùng). Chỉ commit cục bộ, không push khi chưa được yêu cầu.

## 6. Thiết kế

### 6.1 Thư mục
```
Assets/HanaMart/
  Scenes/HanaMart24.unity            ← scene game mới (không sửa GameScene cũ)
  Scenes/Boot.unity                  ← màn khởi động + menu
  Scripts/Runtime/  (Core, Store, Player, Actors, Director, Rules, Deaths, FX, UI, Audio, Save, Sim)
  Scripts/Editor/   (StoreBuilder, CharacterSetup, ProductExtractor, ReferenceCapture)
  Art/Textures/FromThree/            ← PNG xuất từ canvas bản Three.js
  Art/Materials/  Art/Prefabs/  Art/Shaders/
  Audio/FromThree/                   ← WAV xuất từ WebAudio bản Three.js
tools/three-export/                  ← script Node/Playwright xuất texture, âm thanh, ảnh tham chiếu (ngoài Assets)
docs/HANDOFF.md                      ← trạng thái để Codex/phiên mới làm tiếp
```

### 6.2 Ánh xạ module
| Three.js | Unity |
|---|---|
| `store.js` (753 dòng) dựng cảnh, đèn, cửa, tủ, vật tương tác | `StoreBuilder` (Editor) sinh scene + prefab từ cùng số liệu; runtime `Store` (cửa, tủ, đèn, trạng thái) |
| `dress.js` bày biện | phần của `StoreBuilder` |
| `textures.js` texture canvas | **xuất PNG** từ bản Three.js (giữ chữ Hàn/Việt y hệt) |
| `lighting.js` cube probe, vệt đường ướt, quầng đèn, bóng tiếp xúc | Reflection Probe box-projection tại (6.2,1.55,4.0) hộp (0,0,0)-(12,3,8); shader vệt đường & quầng đèn port HLSL; decal bóng tiếp xúc |
| `main.js` renderer, hậu kỳ, menu, HUD, cài đặt, vòng lặp | URP Volume + renderer feature (AgX & Film); uGUI + TextMeshPro; `Settings`/`Save` (JSON) |
| `player.js` | `PlayerController` (CharacterController, cùng tốc độ/độ cao/ngồi/bob) + Input System (bàn phím, chuột, tay cầm, rung) |
| `person.js` người khối + biến hình | `Actor` = model Mixamo + Animator (Humanoid) + bộ "biến hình" chỉnh xương ở LateUpdate + material corrupt |
| `nav.js` A* | NavMesh (AI Navigation) bake theo đúng vùng đi được |
| `physics.js` | Rigidbody Unity |
| `fx.js` hạt + mảng mục | Particle System (URP) + shader mảng mục port HLSL |
| `game.js` (1260 dòng) | `GameClock`, `Director`, `RuleBook`, `EventTimeline`, `AnomalyDirector`, `Tasks`, `Warnings`, `Checkpoint`, `Endings` — giữ nguyên hằng số, giờ, xác suất, giới hạn số lần |
| `deaths.js` | `DeathDirector` + 15 cảnh (Cinemachine + script cảnh, bước thời gian cố định, bỏ qua được) |
| `audio.js` WebAudio | AudioSource 3D (spatialize) phát WAV đã xuất; nhạc nền/mưa lặp |
| `sim.js` bot chơi thử | `SimBot` C# chạy trong Play mode tua nhanh, 3 mức kỹ năng |

### 6.3 Dựng cửa hàng
- `StoreBuilder` đọc các hằng số/vị trí từ `store.js`/`dress.js` (chép số liệu sang C#), sinh: sàn, tường, trần, mặt tiền kính, cửa tự động, quầy, POS,
  tủ sau quầy (thuốc lá, máy cà phê, điện thoại, CCTV, bảng nội quy, công tắc, ngăn kéo, sổ việc, thùng rác), 3 dãy kệ + biển treo + đầu kệ,
  6 tủ mát (cửa bản lề, bảng nhiệt độ, LED), tủ kimbap, quầy mì (bình nước nóng, 2 lò vi sóng cửa bản lề), bàn ăn sát kính,
  kho (kệ sắt, thùng mì, thùng sữa, cây lau + xô, tủ đông, tủ đồ Ji-ho, cầu dao, cửa sau), hẻm, phố (đèn đường, trạm xe buýt, neon, biển hiệu, mưa).
- Kết quả lưu thành scene + prefab thật → bạn sửa tay được. Chạy lại builder được khi cần.
- Đèn: máng huỳnh quang = mesh phát sáng + **đèn spot/point realtime (Forward+)** đặt theo cụm như bản gốc (2/4/9/tất cả theo mức đồ họa); bật/tắt/chập = đổi cường độ (không bật tắt object).
  Reflection Probe chụp lúc đủ đèn, độ sáng môi trường giảm theo số đèn đang sáng (như v2.2).

### 6.4 Texture & âm thanh lấy từ bản Three.js
- Chạy `HanaMart24.html` bằng Chrome không giao diện (Playwright) trong máy làm việc của Claude; gọi các hàm trong `textures.js` → lưu PNG
  (sàn, tường, trần, poster, biển kệ, thẻ giá, giấy nội quy, màn hình, mặt tiền, cửa cuốn, neon, gạch, lưới máy lạnh, két soju, bẩn, ố, biển sàn ướt…).
  Nếu font chữ Hàn trong máy làm việc thiếu → chạy xuất trên máy bạn bằng trình duyệt tích hợp.
- Âm thanh WebAudio: thu từng âm (chuông cửa, POS, điện thoại, gõ, thì thầm, mưa, máy in…) bằng OfflineAudioContext → WAV.

### 6.5 Hàng hóa (đề xuất thay thế)
| Bản Three.js | Asset siêu thị |
|---|---|
| Mì ly (kệ 1 tầng 2, thùng bổ sung, khách áo mưa vàng) | hộp `рамены` (mì Buldak) |
| Mì gói | `рамены` |
| Snack | `чипси` (snack Hàn) |
| Kẹo trên quầy | `мармелад` (kẹo dẻo) |
| Kimbap tam giác, cơm hộp (tủ mát mở, lò vi sóng) | `суши` (hộp cơm cuộn) |
| Đồ uống trong 6 tủ mát (nước chai, lon, cà phê lon, soju) | **chưa có đồ uống** → xem câu hỏi Q1 |
| Sữa chuối (việc 01:50, kệ 3 tầng 3, thùng trong kho) | **xem Q1** |
| Lon rơi (dị thường) | gói `чипси` rơi |
| Thuốc lá (tủ sau quầy) | không có món tương tự → giữ hộp kiểu Three.js (texture xuất) |
| (Không có trong bản gốc) đồ gia dụng `быт`, `быт2`, rau `трава`, thịt `мясо`, trái cây `фрукты`, `всякое`, mandu `пельмени` | dùng lấp đầy kệ/tủ ở những chỗ bản gốc là hàng trang trí |
- `ProductExtractor` (Editor): nhập `магазин.glb`, gom các khối theo material → tạo prefab từng loại (giữ UV atlas), chuẩn hóa kích thước theo ô kệ bản gốc.
- POS/quầy: giữ như bản gốc (không phải hàng bán).

### 6.6 Nhân vật
- 4 model: Ch31 (`Female Walk - Lat Mat`, đã đúng), Ch08 (`character (4)`), Ch01 (`character (5)`), Ch42 (`character (6)`).
- `CharacterSetup` (Editor): tách texture nhúng trong FBX → tạo material URP Lit (Specular workflow, giống `Ch31_body`/`Ch31_hair`: BaseMap, Normal, SpecGloss; tóc Transparent),
  chuyển rig sang **Humanoid** để dùng chung clip `Female Walk`. Không làm hỏng `Animation Test` (kiểm tra lại scene đó sau khi đổi rig).
- Đổi màu quần áo: tạo **mặt nạ quần áo** cho từng model (tách vùng da/tóc khỏi vùng áo-quần trên atlas) → shader nhuộm vùng quần áo theo màu vai (giữ chi tiết vải).
- Áo mưa vàng (và áo mưa đen): lớp vỏ thứ hai của cùng mesh, phồng ra theo pháp tuyến, chất liệu bóng (clearcoat giả), có mũ trùm; mặt/tay lộ ra.
- Trẻ em: thu nhỏ 0.68 (đúng tỉ lệ bản gốc).
- "Không mặt" (face: none), da tái (pale), mặt nạ trắng/đen, mũ: phủ bằng shader/mesh phụ.
- Biến hình (cổ dài, tay dài, đầu nghiêng/bẻ 180°, giật, rung, mắt đỏ phát sáng, gân đen lan, da xám): chỉnh xương ở LateUpdate + tham số `reveal` trên material — đúng thang 0→1 như `person.js`.
- Chuyển động: đi = clip `Female Walk` (retarget); đứng/thở/quay đầu/cử động "không phải người" = thủ tục trên xương như bản gốc. Bạn thêm clip Mixamo khác lúc nào thì thay vào.
- Vai → model: chọn theo dáng của vai trong bản gốc (nữ/nam, tóc dài/ngắn); vai cần biểu cảm kinh dị trên mặt (Mi-rae giả…) dùng Ch31 vì chỉ Ch31 có blend shape mặt.

### 6.7 Màu & hậu kỳ
- URP: HDR bật, Tonemapping = None; **renderer feature tự viết** chạy sau hậu kỳ: AgX (port công thức three.js, exposure 1.02) + shader Film của `main.js`
  (grain, quang sai màu, vignette, méo sóng, glitch, đỏ khi chết, chỉnh màu xanh lam) — port GLSL → HLSL, cùng tham số.
- Bloom URP chỉnh theo UnrealBloom (0.3, 0.5, 1.05); SSAO thay GTAO; SMAA; DOF/anamorphic/halation/rung khung cho mức "Điện ảnh".
- Nếu AgX ở vị trí sau hậu kỳ không đúng thứ tự bloom → phương án dự phòng: LUT 3D nướng từ bản Three.js.

### 6.8 Gameplay & giao diện
- Dịch nguyên `RULES`, `ADDENDUM`, `FAKES`, `LEARN`, `RULE14`, `DIARY`, `PRICE`, `NAME`, `GUIDE`, lịch mốc, trọng số khách, giới hạn số lần, thời gian khoảng lặng, mốc lệch ±,
  biến thể, dọa hụt, "nhẹ tay khi chết lặp" — **giữ nguyên số liệu**.
- Đồng hồ: 1 phút game = 6/7/8 s theo phase; dừng khi hội thoại/đọc giấy.
- Checkpoint mỗi giờ (00–05), không lưu trạng thái tức thời qua checkpoint (bài học v2.2).
- UI tiếng Việt như bản gốc (menu, cài đặt 3 tab, tạm dừng, HUD, phụ đề, giấy, màn chết, màn kết thúc). Font: Noto Sans / Noto Sans KR (giấy phép OFL) cho tiếng Việt + Hàn.
- Lưu: JSON trong `persistentDataPath` (cài đặt, kết thúc đã mở, Sổ tay).

### 6.9 Kiểm thử trước khi giao
1. **Không lỗi**: Console sạch (0 lỗi biên dịch/0 exception) qua trọn ca.
2. **Bot**: port `sim.js` → chạy ≥4 lượt mỗi mức (cẩn thận/trung bình) tới 06:00; so với bảng v2.2 (cẩn thận ~11 lần chết, trung bình ~19). Mọi lần kẹt/chết lặp vô hạn phải sửa.
3. **Kịch bản**: danh sách kiểm từng mốc giờ, từng luật, 15 màn chết, 2 kết thúc — kích hoạt bằng lệnh debug và xác nhận bằng ảnh chụp.
4. **Hình ảnh**: ~12 góc máy cố định (lối vào, quầy, 3 dãy kệ, tủ mát, quầy mì, kho, tủ đông, hẻm, phố, mất điện, Giờ Đỏ). Chụp bản Three.js (Playwright) và Unity (MCP) cùng vị trí/hướng/FOV 72 → ghép cạnh nhau + so độ sáng/màu trung bình; chỉnh tới khi khác biệt chỉ do model/asset mới.
5. **Hiệu năng**: đo FPS trên máy bạn ở mức Trung bình (mục tiêu ≥ 60).
6. Báo cáo bàn giao: đã kiểm gì, kết quả, ảnh so sánh, việc còn tồn.

## 7. Giai đoạn
0. Chuẩn bị: Build Settings, thư mục, `docs/HANDOFF.md`.
1. Tài sản: material 3 model, rig Humanoid, mặt nạ quần áo, áo mưa; xuất texture/âm thanh/ảnh tham chiếu; prefab hàng hóa.
2. Cửa hàng: `StoreBuilder`, đèn, probe, hậu kỳ AgX+Film → **so hình ảnh lần 1** (cảnh tĩnh).
3. Người chơi & tương tác: di chuyển, ngồi, nhìn kỹ, cầm/đặt/nhặt, xếp hàng, lau sàn, lò vi sóng, tủ mát, rác, CCTV, điện thoại, giấy/nội quy.
4. Nhân vật: `Actor`, NavMesh, đổi màu, biến hình, các vai.
5. Kịch bản: đồng hồ, đạo diễn, luật, mốc, dị thường, bẫy, cảnh cáo, checkpoint, nội quy giả, Giờ Đỏ, nhật ký, kết thúc.
6. Màn chết & điện ảnh: 15 cảnh, khoảnh khắc điện ảnh, nhịp tim, bóng rìa tầm nhìn, im lặng trước biến cố.
7. UI/menu/cài đặt/lưu/tay cầm/mức đồ họa/độ phân giải động/F3/H.
8. Kiểm thử toàn bộ (mục 6.9), sửa lỗi, báo cáo bàn giao.
Mỗi giai đoạn kết thúc bằng: Play mode chạy được + Console sạch + cập nhật `docs/HANDOFF.md`.

## 8. Để lại cho người làm (Claude tự quyết, ghi rõ trong báo cáo)
- Cách chép số liệu `store.js` sang C# (bảng dữ liệu hay code dựng trực tiếp).
- Bộ điều khiển người chơi: tự viết theo `player.js` (không dùng Starter Assets) để đúng cảm giác.
- Vai → model cụ thể; màu quần áo lấy từ mã màu bản gốc.
- Số lượng/khoảng cách đèn realtime theo mức đồ họa.
- Cách tạo mặt nạ quần áo (tự động theo màu da + chỉnh tay).
- Chi tiết dựng 15 màn chết bằng Cinemachine/Timeline hay script.

## 9. Rủi ro
- AgX + Film qua renderer feature có thể lệch thứ tự với bloom → dự phòng LUT.
- Đèn realtime nhiều + bóng đổ có thể nặng hơn bản Three.js → giới hạn theo mức đồ họa.
- Mặt nạ quần áo tự động có thể dính vùng da → chỉnh tay.
- Một clip đi bộ nữ cho mọi vai → dáng đi nam chưa tự nhiên (tạm chấp nhận theo hướng A).
- Font tiếng Hàn khi xuất texture trong máy làm việc của Claude.
- Dự án dài, có thể vượt một phiên làm việc → `docs/HANDOFF.md` luôn cập nhật để Codex/phiên mới làm tiếp.

## 10. Câu hỏi đã chốt
- **Q1 — Món không có tương đương (Claude tự quyết, bạn có thể đổi khi duyệt):** 6 tủ mát chứa đồ ăn lạnh có trong asset (sushi, thịt, rau, trái cây, mandu);
  việc "bổ sung sữa chuối" → bổ sung bánh mandu (`пельмени`); kimbap/cơm hộp → hộp cơm cuộn (`суши`). **Chữ trong game đổi theo cho khớp hình**
  (vd. "Sữa chuối" → "Bánh mandu", "Kimbap tam giác" → "Cơm cuộn", "Nước lon"/"Lon rơi" → món tương ứng). Luật, giờ, cơ chế giữ nguyên.
- **Q2 — Git:** commit sau mỗi giai đoạn lên nhánh `manh`, không push. Máy làm việc của Claude không có git-lfs nên **chưa bật LFS**;
  các file lớn nhất (FBX 87 MB) vẫn dưới giới hạn 100 MB của GitHub. Thư mục `Claude outputs/` (chứa file cấu hình app Claude có mã tài khoản) được đưa vào `.gitignore`.
