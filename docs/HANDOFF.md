# HANDOFF — HanaMart24 → Unity (NM01-GOTY)

> **Đọc file này đầu tiên.** Người bàn giao: Claude (2026-11-25 02:15). Người nhận: Codex (hoặc phiên Claude mới).
> Người dùng: Nguyễn Tiến Mạnh — nói tiếng Việt, hãy trả lời bằng tiếng Việt.

## Tiếp nhận từ Codex — 25/11/2026

- **Kế hoạch 6 ĐÃ HOÀN THÀNH TOÀN BỘ (Task 1–3) và commit cục bộ** (`a15bb80`, `8d604ed`, `a7eefac`).
- **Kế hoạch 7 — Task 1 XONG** (`cf6999d`): `SimTypes`/`ShiftSimHost`/`SimBot` + `ShiftGame.NewShift(int seed)` + `ShiftSimQA` (`Logs/HanaMart/sim-report.json`: `careful` 5/5 tới 06:00, TB 13.0 chết; `average` 5/5 tới 06:00, TB 20.2 chết; `careless` 4/4 tới 06:00, TB 28.25 chết; 0 kẹt); EditMode 255/255 PASS.
- **Kế hoạch 7 — Task 2 XONG** (so hình & hiệu năng):
  - Thêm `RainStreaks.shader`, `LampCone.shader`, `StoreWeather.cs` (mưa GPU, 12 vệt đường ướt, 2 quầng đèn đường `S:cones[0..1]`), bù decay ánh sáng hẻm + `ForcePixel`, tính `_Smoothness` từ `roughnessMap` trong `StoreImporter.cs`, nối `StoreWeather` vào `ShiftInterface.Settings.cs` và `ShiftGame.Environment.cs`, rebuild `HanaMart24.unity` (`HanaMart/Build Store`).
  - **EditMode:** **260/260 PASS** (`08cc3dac3fe8437292975b70d87c8e5f`, `Logs/HanaMart/tests/p7-t2-full.json`, 17.77 s) trên DLL mới; RED trước đó fail 5/5 (`7a4768c1d3964aed9324218d34cbcc6e`, `p7-t2-red.json`).
  - **So hình (`docs/visual/compare/report.md`):** chụp 14 góc Three.js (`capture_three_cdp.mjs`) ↔ Unity (`HanaMart/Capture Viewpoints`) + 2 cảnh động Play mode (`docs/visual/compare/dynamic-scenes.jpg`). `alley` cải thiện từ **−48% → −4%**, `fridges` **−21%** (do asset thay thế), đa số các góc trong nhà/phố lệch ≤ 7%.
  - **Hiệu năng (`Logs/HanaMart/perf-report.json`, 2560×1440, 3 NPC + mưa/FX + HUD):** `low` **56.4 FPS** (17.73 ms), `medium` **55.5 FPS** (18.01 ms), `medium_dynres` **67.1 FPS** (14.90 ms), `high` **70.2 FPS** (14.25 ms).
- **Tiếp tục từ Kế hoạch 7, Task 3** (`docs/superpowers/plans/2026-11-25-hanamart24-p7-acceptance.md`: chơi một ca thật liên tục 00:00→06:00 qua Unity Play + MCP, kiểm tra retry/bẫy/kết thúc A-B, Console/log sạch và bàn giao).
- Kế hoạch 1–6 đã xong. Kế hoạch 7 chưa triển khai. QA đã kiểm từng mốc, cài đặt và cả hai kết thúc, nhưng **chưa chơi một ca tương tác liên tục 00:00→06:00, chưa làm đối chiếu hình cuối và chưa đủ điều kiện bàn giao game**. Báo cáo nghiệm thu cuối phải ở `docs/visual/compare/report.md`.
- Các thay đổi ngoài phần port cần giữ nguyên gồm `NM01-GOTY.slnx`, `ProjectSettings/QualitySettings.asset`, `docs/visual/store_back.png`. Hai scene được bảo vệ không có diff. Không đưa `Assets/_Recovery`, `Claude outputs` hoặc các file ngoài task vào commit.

## 1. Mục tiêu (yêu cầu gốc của người dùng)
Chuyển nguyên game kinh dị **"QUY TẮC CA ĐÊM — HANA MART 24"** từ bản Three.js v2.2 sang Unity:
- Bản gốc: `E:\Downloads\top\top\HanaMart24` — đọc `GAME_DOC.md` (thiết kế đầy đủ: 13 điều nội quy, phụ lục, điều 14, 3 phase, đạo diễn khách, mốc giờ, dị thường, 9 bẫy, Giờ Đỏ, 15 màn chết, 2 kết thúc…), mã nguồn `source/src/*.js`, bản chơi `HanaMart24.html`.
- **Giữ y nguyên bố cục, màu sắc, kịch bản** của bản Three.js.
- Nhân vật: dùng 4 model trong `Assets/Art/Model/Charactor` (Ch31 = `Female Walk - Lat Mat.fbx`, Ch08 = `character (4)`, Ch01 = `character (5)`, Ch42 = `character (6)`). **Hướng A**: dùng lại 4 model cho mọi vai, đổi màu quần áo, thêm lớp áo mưa vàng phủ ngoài, trẻ em = thu nhỏ model người lớn ×0.68.
- Hàng bán trong cửa hàng: thay bằng asset `Assets/Art/Model/super-market-for-free` (món không có tương đương → chọn món tương tự; chữ trong game đổi theo hình, vd. "Sữa chuối" → "Bánh mandu", "Kimbap" → "Cơm cuộn").
- Kệ/quầy/tủ: tạm dùng như bản Three.js.
- **Trước khi bàn giao cho người dùng: test lại toàn bộ game, sửa hết lỗi, so hình ảnh với bản Three.js.**

## 2. Tài liệu
- Spec (đã duyệt): `docs/superpowers/specs/2026-11-25-hanamart24-unity-port-design.md`
- Kế hoạch 1 (XONG): `docs/superpowers/plans/2026-11-25-hanamart24-p1-foundation.md`
- Kế hoạch 2 (XONG): `docs/superpowers/plans/2026-11-25-hanamart24-p2-player.md`.
- Kế hoạch 3 (XONG): `docs/superpowers/plans/2026-11-25-hanamart24-p3-actors.md`.
- Kế hoạch 4–7 đã viết: `docs/superpowers/plans/2026-11-25-hanamart24-p4-shift.md`, `2026-11-25-hanamart24-p5-death-scenes.md`, `2026-11-25-hanamart24-p6-interface.md`, `2026-11-25-hanamart24-p7-acceptance.md`.
- Sổ tiến độ + mọi quyết định ("Ruling"): `.superpowers/sdd/*/progress.md` (git-ignored, đọc trực tiếp).
- So sánh hình ảnh: `docs/visual/compare/report.md` + ảnh `docs/visual/compare/*.jpg`.

## 3. Quy ước làm việc
- Unity **6000.3.8f1** (editor `E:\Unity\Unity\6000.0.40f1\6000.3.8f1\Editor\Unity.exe`), URP 17.3, project `E:\Unity\My Project\Main\NM01-GOTY`.
- Git: nhánh **`manh`**. Commit sau mỗi task: `git -c user.name="manh2805" -c user.email="tienmanh2805@gmail.com" commit`. **KHÔNG push** khi người dùng chưa yêu cầu. Không commit `Assets/_Recovery/`, `Claude outputs/`.
- TDD: viết test trước (Unity Test Framework, assembly `HanaMart.Tests.EditMode`), xem FAIL rồi mới code. **Mọi vòng lặp trong test phải có giới hạn** (đã từng làm treo Unity vì `while` không giới hạn) — dùng `[Timeout(10000)]`.
- Không sửa `Assets/Scenes/GameScene.unity` và `Assets/Scenes/Animation Test.unity` (của người dùng).
- **Logic gameplay chạy trong hệ tọa độ Three.js** (x, z như bản gốc). Chỉ lớp hiển thị đổi sang Unity bằng `HanaMart.Store.ThreeCoords` (x → −x). Camera: yaw Unity = 180° − yaw Three, pitch Unity = −pitch Three.
- Màu Three.js (r152+) là **tuyến tính** → khi gán cho Unity `_BaseColor`, `Light.color`, `RenderSettings` phải `.gamma`.

## 4. Đã xong (Kế hoạch 1, commit `e542897` → `585e11c`, 33/33 test xanh)
| Việc | Kết quả | Menu/Công cụ |
|---|---|---|
| Khung project | asmdef `HanaMart.Runtime/Editor/Tests.EditMode`, scene `Assets/HanaMart/Scenes/HanaMart24.unity` (scene build #0) | — |
| Nhân vật | texture tách khỏi FBX, material URP Lit (Specular) cho Ch08/Ch01/Ch42, rig **Humanoid** cả 4 | `HanaMart/Setup Characters` |
| Xuất cảnh Three.js | `tools/three-export/export_store.py` + `gltf_writer.js` → `Assets/HanaMart/Art/FromThree/store.gltf` (+bin, 110 PNG, `store_meta.json`). Node: `S:<khóa store>`, `R:<refs>`, `P:<loại>:<biến thể>[:restock]` (hàng), `L:<i>` (đèn), `H:<id>` (vùng tương tác, extras {id,label}) | `python3 tools/three-export/export_store.py` (cần Playwright + Chrome) |
| Dựng cửa hàng | `StoreImporter` (material→URP, đèn, 38 va chạm, probe Custom, sương mù) + `StoreMarker` trên mọi node có khóa | `HanaMart/Build Store` (tự Bake probe) |
| Hàng hóa | `ProductExtractor` + `Assets/HanaMart/Data/ProductCatalog.asset` (8 prefab/nhóm, tên hiển thị); 3629 món thay, thuốc lá giữ | `HanaMart/Extract Products` rồi `Build Store` |
| Hậu kỳ | AgX (r160, exposure 1.02) + shader Film của main.js: `Assets/HanaMart/Art/Shaders/AgxFilm.shader`, Full Screen Pass trên `PC_Renderer`, `FilmSettings` trên camera, Volume Bloom 0.3/0.5/1.05 | `HanaMart/Setup Rendering` |
| So sánh hình | 12 góc (`tools/three-export/viewpoints.json`); 10/12 lệch độ sáng ≤10% | `HanaMart/Capture Viewpoints` + `python3 tools/three-export/capture_three.py` + `compare.py` |

## 5. Đang làm (Kế hoạch 2)
- **Task 6 XONG**: 36 âm thanh + 6 lớp nền xuất từ WebAudio → `Assets/HanaMart/Audio/FromThree/*.wav` (`tools/three-export/export_audio.py`, test `test_audio.py`).
- **Task 1 XONG** (commit `c132192`): `Assets/HanaMart/Scripts/Runtime/Player/{PlayerTypes,WalkArea,PlayerMotor}.cs` + test `Tests/EditMode/PlayerMotorTests.cs`.
  RED đã xác nhận (9 test fail với stub). **Codex đã thoát Play mode và xác nhận GREEN 10/10** qua MCPForUnity (job `395fefe067474194ace10c355da1c43c`); không cần sửa logic.
  Toàn bộ assembly `HanaMart.Tests.EditMode`: **43/43 PASS**, 0 skipped (job `17cbf317269a4974b3f3e2ad0a730096`, 4.96 s).
  Công cụ Codex: `python tools/unity_bridge.py <command> '<JSON>'` kết nối MCPForUnity loopback của đúng project; dùng `run_tests` / `get_test_job`, `manage_editor`, `execute_menu_item`. Không cần khởi động Unity thứ hai.
- **Task 2 XONG**: `ThreeCoords.CameraRotation`, `PlayerController`, `Input/HanaMartControls.inputactions`, menu `HanaMart/Setup Player`. Camera scene HanaMart24 có 38 hộp va chạm Three.js, FOV 72; chuột delta Y đảo về quy ước trình duyệt, pad stick deadzone 0.16/0.12. Rung bằng Input System. Test camera RED 5/5 → GREEN 5/5; EditMode 48/48. Play scene: Console 0 warning/error, ảnh `docs/visual/unity/p2-player-start.png` (chưa thử tay cầm vật lý).
- **Task 3 XONG**: `Interactor` (ray 2.1 m, E/bấm/giữ, `LabelFor`, `HoldSeconds`, grace 0.12 s), `InteractionPrompt` TMP/uGUI; `H:*` luôn active, renderer tắt. RED 5 lỗi/6 test → toàn assembly GREEN **54/54**, job `0094e5eeed4f45588c05024774819c21`. Scene đã Build Store lại + Setup Player; Play snapshot xác nhận 33 hit active, trace/current `rules`, nhãn tiếng Việt đúng. Ảnh `docs/visual/unity/p2-interaction-rules-verified.png`.
  Font Noto Sans (OFL trong `Art/Fonts`); TMP essentials từ package đang cài được đặt tại `Assets/HanaMart/ThirdParty/TextMesh Pro`, GUID giữ nguyên (`python tools/import_tmp_resources.py`). Built-in LegacyRuntime không dùng được với TMP FontEngine.
  QA qua MCP: `HanaMart/QA/Face Rules` (bật runInBackground trong Play để MCP kiểm được frame thật), `Runtime Snapshot` → `Logs/HanaMart/runtime.json`, warning/error/exception từng Play → `Logs/HanaMart/play-errors.log`; `Reload Scripts` yêu cầu nạp assembly mới.
- **Task 4 XONG**: `HingeMotor` + `StoreDoors` gắn khi Build Store; cửa trước +1.35 rad Unity/rate 5, cửa sau −1.6/rate 4, 12 cánh tủ ∓1.45/rate 5, lò +1.75/rate 7, nắp tủ đông +1.2 quanh X. Ghép hit tủ với cánh khi đóng, không ghép lại theo vị trí lúc đang mở. BackDoor collider đồng bộ với target. Giữ front door collider đúng source (người chơi không được rời ca).
  TDD: hinge RED 8/8 → GREEN 9/9 gồm kiểm ghép 12 hit; toàn assembly **63/63**, job `a81e0182e32149658fe99743fc5abc76`. Play `HanaMart/QA/Open Doors`, ảnh `p2-open-doors-verified.png`, play-errors.log rỗng.
  Sửa tiếp: nắp tủ đông phải **−1.2 rad quanh X** (phản chiếu X giữ dấu góc quanh chính X). Test mép trước nắp nâng lên RED (y=−0.932) → GREEN 9/9, job `84d3b964fd6846388a35fba2fea49652`. Mô tả +1.2 ở trên đã được thay bằng giá trị đúng này.
  Chạy kiểm có lưu bằng chứng: `python tools/unity_test.py --label <tên> --compile` (hoặc `--filter HanaMart.Tests.XTests`); kết quả ở `Logs/HanaMart/tests/`. Từ chối lượt 0 test.
- **Task 5 XONG**: `Placement` dịch `surfaceAt/placeTarget/tween` (mặt quầy 1.02, kệ sau quầy .92, bàn 1.02, sàn 0; ray 2.6 m; fallback 1.6→.5 m có giới hạn), `CarryController` đọc Q/RB: giữ xem bóng mờ + vòng sáng, thả đặt, E nhặt. Sáu loại đồ, tay áo, thùng lấy khỏi kho tắt cả collider, cây lau để lại xô; tên Bánh mandu/Cơm cuộn. `ItemPhysics` + `ItemPhysicsBody` dịch trọng lực 9.8, nảy/ma sát/ngủ/wall từ physics.js.
  TDD RED 10 fail/11 → GREEN toàn assembly **75/75**, job `2d388eaec4424776a17e31b6bf344209`. QA `Carry Smoke` sáu loại đạt; `Carry Rapid Smoke` bắt MissingReferenceException khi nhặt ngay .31 s (squash còn truy cập đồ đã hủy) → đã sửa dừng squash khi picking; kiểm lại sáu loại đạt, `Logs/HanaMart/carry-rapid-smoke.json`, play-errors.log rỗng.
  `StoreDoors.defaultInteractions` và `CarryController.defaultInteractions` là tương tác tạm để kiểm; Kế hoạch 4 phải tắt và điều khiển qua logic luật (đặc biệt tủ đông/lò), tránh mở tủ tự do khi đã có kịch bản. Placement fallback hiện dùng WalkArea người chơi; khi có navigation (Kế hoạch 3), nối walkableAt R=.24/grid .15 đúng bản gốc.
  Vật lý đồ rơi dùng bản dịch physics.js, không dùng Rigidbody động vì 38 hộp va chạm chặn người cao 3 m sẽ bắt đồ rơi sai mặt. Tương tác giữa các đồ rơi cũng như source (không có va chạm đồ–đồ).
- **Task 6 hoàn thiện**: `Data/AudioBank.asset` đủ 42 clip bằng id tên file; menu `HanaMart/Build Audio Bank`, `AudioBank.Find(id)`. Bank test RED (thiếu asset) → GREEN; assembly cuối Kế hoạch 2 **76/76**, job `19678b74179847d6ab72147ce873a24d` (6.70 s). `python tools/three-export/test_audio.py Assets/HanaMart/Audio/FromThree` → PASS 42 stereo/44.1 kHz, không câm/vỡ theo ngưỡng gốc. Đã đưa .meta WAV vào git; runtime phát âm thanh nối khi có game director.
- **Tiếp theo: viết/làm Kế hoạch 3** (Actor, nhuộm, áo mưa, trẻ em, humanoid clip, biến hình, navigation). Kế hoạch 4–7 chưa bắt đầu; chưa có ca chơi được 00:00→06:00, chưa so hình cuối và chưa được coi là hoàn thành port.
  Kế hoạch 3: `docs/superpowers/plans/2026-11-25-hanamart24-p3-actors.md`; **Task 1 XONG**, commit `113a174`: ActorOptions/ActorMotion dịch person.js (scale .68, reveal, nhìn đầu, blend/stride/thở/snap), `Data/ActorCatalog.asset` bốn avatar + Female Walk. RED 11 fail/12 → toàn assembly GREEN **88/88**, job `80c51531af844726a7993886817db6d8`. Menu `HanaMart/Build Actor Catalog`. Skill unity-shader đã đọc. Kế hoạch 2 hoàn thiện ở commit `cb4a65f`.
  **Task 2 XONG**: 17 mask vùng da/mặt/áo/quần (R áo, G quần, B da, A đầu/mặt), material body/rain lưu trong `Art/Characters/Variants`; tóc giữ URP alpha. Shader `HanaMart/Character` fork 4 pass Lit URP 17.3 (forward/shadow/depth/normals) với cùng displacement/clip, veins/jitter từ person.js, rain shell .012 m, clearcoat .8, áo mưa vàng `0xf2c200`. Công cụ `python tools/build_character_shader.py` rồi menu `HanaMart/Build Actor Materials`.
  Mesh Body/Shirt/Pants/Shorts phân vùng chính xác hơn chỉ dựa màu (bảo vệ chân trần Ch42 và họa tiết áo); bones dùng xác định phần đầu/mặt. Bake tạm bật mesh readable và khôi phục **nguyên bytes .meta** sau đó; FBX và hai scene được bảo vệ không có diff.
  Mũ áo mưa, mask trắng/đen/cười có mesh/material/texture sở hữu riêng; `ActorAccessoryResources` giải phóng khi despawn. Offset đã chỉnh theo xương Head Mixamo qua kiểm hình (không chép nguyên pivot người khối làm mask che mắt).
  TDD: initial RED mask/material, thêm RED mask rỗng do Read/Write, RED bảo vệ chân trần/họa tiết áo, RED phụ kiện → **103/103 GREEN**, job `be13e51ab0aa487a92d4699c25dd8a65`, 12.64 s; test CompilePass ép đủ bốn pass body/rain trên API hiện tại. QA Play log rỗng; ảnh `docs/visual/characters/materials-reveal.jpg` và `face-masks.jpg`.
  QA qua MCP: `HanaMart/QA/Character Materials`, `Character Masks`, `Capture Character Materials` (chỉ Play, không lưu các model thử vào scene). Shader compilation phải synchronous khi chụp, render warmup trước ảnh; file tên reveal dùng invariant culture (tránh dấu phẩy tiếng Việt).
  **Task 3 XONG**: `Actor` dùng PlayableGraph lấy mẫu `Female Walk` theo bước dài 1.3 m, retarget bốn Humanoid, tư thế nghỉ hạ tay, thở/nhìn/lộ dạng bằng xương và shader. Spawn/despawn tự quản lý graph, mesh tạm và material mắt. Trẻ em cao 68%. API nhận đường waypoint Three.js.
  RED 8/8 với stub (`367134319303457a8abf3a7a3a0086c3`), thêm test đế giày RED (`50b624e05ea54ce6acd265721c043d95`) → Actor GREEN 12/12 (`00ce20b065334b708007719fe8c01ead`). Bù độ cao theo đáy mesh giày skinned, chỉ bake khi blend đi bộ, dùng lại mesh và buffer, đo chi phí ở Kế hoạch 7. Chụp Play phải chờ một frame sau đổi tư thế để skinning cập nhật, dùng bước 1/60 s và y=.012 cho vỉa hè cao .01. Ảnh `docs/visual/characters/actor-motion.jpg`, play-errors.log rỗng.
  Toàn assembly **115/115 GREEN**, job `b73ac9a32d3446c5aae213b6b76fd151`.
  **Task 4 XONG**: `StoreNavigation` port lưới .15 m, bán kính .24 m, A* và các cổng theo `nav.js`, bỏ collider cửa trước/sau khỏi grid lúc tạo theo `game.js`. `Actor.TryMoveTo` dùng lưới, `CarryController` nối `Placement` với cùng `Walkable`. Tuyến bất khả thi dừng như yêu cầu tránh xuyên tường, bỏ fallback đi thẳng của JS. Không thay va chạm người chơi.
  `HanaMart/Build Store Navigation` bake `Data/StoreNavigation.asset` từ các ô đã chừa .24 m. Unity NavMesh dùng thêm inset .01 m, load/unload cùng PlayerController. NPC đi theo lưới để giữ quy tắc ô và điểm đích của JS. Ở cửa hẻm z=−4.6, x=1.88 mở và x=2.14 bị vật cản theo phép chạy trực tiếp `nav.js` với `store_meta.json`.
  RED 7/8 (`dda3e004cec74c1b8006f7c976602cab`) → **124/124 EditMode GREEN** (`a44961fc891b4b6198260524a9b9ba2e`), gồm tuyến NavMesh hoàn chỉnh từ vỉa hè ra hẻm qua cả ba cửa và điểm đặt ngoài phố.
  **Task 5 XONG**: test 60 s cho cả bốn Humanoid bắt một bước cắt ô góc và sót mesh tạm khi hủy trong EditMode. Sửa bằng kiểm từng cạnh ô khi làm mượt đường, đi tới đúng waypoint, giải phóng tài nguyên rõ ràng khi Remove ở EditMode. RED `83ad4efac6b14e4bb591e5324ba0d8ed` → **128/128 EditMode GREEN**, job `beca836aab4f4578aca1ea83dc01a30b`.
  MCP `HanaMart/QA/Run Actor Route` chạy Play 60.001 s, 5704 khung, khách áo mưa vàng đi 19.45 m tới (2.15, −7), không ô bị chặn nào và log 0 cảnh báo/lỗi. Bốn ảnh ở `docs/visual/characters/actor-route.jpg`. QA mở hai cửa trong Play, chưa có đạo diễn tự mở cửa cho khách. Hai scene được bảo vệ không có diff.

### Kế hoạch 4 — Kịch bản ca đêm (xong)

- **Task 1 XONG**: `ShiftClock`, `ShiftTimeline`, `ShiftProgress`, `ShiftRun`. Đồng hồ 6/7/8 s mỗi phút, chặn ở 06:00, dừng khi đọc giấy/đối thoại, 04:44 giả chỉ đổi hiển thị. 29 mốc cốt truyện, lịch dị thường tối đa 4/6/8 theo đúng 300 lần thử mỗi phase. Đối chiếu bằng chạy `_buildEvents` của nguyên bản với hai seed, đạt đúng các mốc/phân bố.
- Checkpoint lưu trước fax/phase/giao hàng/tủ đông, retry chạy lại mốc ngay đầu giờ, bỏ cờ tức thời và giữ Sổ tay/mức nới lỗi. Cảnh cáo giảm khi số cảnh cáo giữ nguyên trọn giờ. RED 7/7 (`f10a50125bfb470f81ddde32dd3e85ef`), test tích hợp checkpoint RED (`7ff8a078fd7249cca88d59267e13035b`) → **137/137 EditMode GREEN** (`4f3934f77b004d17a9f1eaef7fd5ae97`).
- **Task 2 XONG**: `ShiftGame` gắn scene HanaMart24 qua `HanaMart/Setup Shift`, mở ca và chạy `ShiftRun`. `VisitDirector` giữ trọng số/cap, hai khách, khoảng lặng, nới lỗi và giảm lặp. NPC đi qua cửa tự động, phát chuông, có 6 s để chào, chọn quầy theo hàng đợi. `CheckoutRules` giữ giá bản gốc, lời thoại đổi đúng tên các món thay. Quét hàng qua hai cung, tạo túi/hóa đơn, xử lý lựa chọn áo mưa, trẻ em, người không bóng và xin vào kho. `ShiftHud` tạm hiển thị giờ, cảnh cáo, lời thoại và phím 1–3, UI theo bản gốc ở Kế hoạch 6.
- RED luật/mốc chào 21/23 (`63703ac7149849819c16a547eaced0f3`), RED hàng đợi (`42d2f4cc3919446bb82e7f2ecbffc0cd`) → **161/161 EditMode GREEN** (`1b43e0e6b78a427a8f1f219423ea3b78`). `HanaMart/QA/Run Checkout Visit` chạy khách áo mưa qua cửa/chuông, dùng ray E trên hit POS thật, quét 2 mì ly giá ₩3.000, thanh toán rồi rời tiệm. Túi/hóa đơn được dọn, log Play rỗng; QA dùng timeScale ×4.
- **Task 3 XONG**: `ShiftTasks`, `MicrowaveCycle`, `ShiftPhone`. Nhãn và ray E đi qua `ShiftGame`, dùng lại Carry/StoreDoors, kiểm nhiệt, bổ sung hàng có 18 món/kệ, lau (giữ 2/2.5 s), hâm hai lò (.55/.95/1.3 s + 6 s), xử lý rác, giấy/nội quy/nhật ký, fax, điện thoại và dị thường. CCTV 256×160 thay góc mỗi 22 ảnh, giữ E đếm 10 s khi trùng hình và deadline 24/18 s có nới lỗi. Retry phục hồi stock/paper/carry/đạo cụ và camera. Bản gốc không xếp mốc gọi `makeSpill`, Unity giữ cơ chế lau mà không thêm mốc vào lịch.
- RED 10/10 (`2109b89cb168410facfcb724f424e97e`) → **171/171 EditMode GREEN** (`0d2ca7c38fe6476d9fc5960bb65a3abf`). MCP `HanaMart/QA/Run Shift Jobs` dùng ray thật đọc luật/kiểm nhiệt/bê thùng/xếp đủ 18 món, lò đóng/mở và trả món nóng, retry 01:00 giữ stock, dọn đồ và đóng lò. QA checkout áo mưa chạy lại đạt; log Play rỗng. Giấy và HUD vẫn đang dùng giao diện tạm.
- **Task 4 XONG**: `NightRules`, `RedHour`, `ShiftGame.Night` và `StoryGuests` nối giao 12/13 thùng, tiếng gọi trong hẻm, tủ đông/Ji-ho, quản lý và Mi-rae giả, nhân viên giả, đập kính/giao bù, mất điện, nhìn người ngoài kính, đèn đuổi, Giờ Đỏ, luật giả và bàn giao 06:00. Hai kết thúc A/B mang lời thoại, điều kiện và thống kê bản gốc. Cảnh thua và hiệu ứng camera ở Kế hoạch 5, giao diện lưu kết thúc ở Kế hoạch 6.
- RED 10/15 (`46121423cf4d49aab5a4453d0bc8f386`) → **186/186 EditMode GREEN** (`9ee63d89295b477592674916b527192c`). MCP `Run Story Milestones` mở tủ đông 02:00 chết theo luật 6 và retry phục hồi input; chạy toàn bộ 6 chu kỳ Giờ Đỏ đứng yên; mở cả kết thúc A và B bằng Mi-rae tới quầy thật ở 06:00. Kết thúc B kiểm với 5 trang đã nạp để tập trung vào nhánh cứu Ji-ho. Log rỗng, đây là các lượt theo mốc.
- **Task 5 XONG**: 3 seed kiểm clock 00:00→06:00, đủ 29 mốc cốt truyện theo thứ tự, checkpoint 01:00–05:00 đúng một lần. MCP `Run Player Store Route` qua PlayerMotor và ray E thật: cửa trước chặn nhân viên ở z=7.67, cửa sau đóng chặn z=−4.237; mở bằng tương tác, đi tới hẻm (2.148, −6.980). Log rỗng. **190/190 EditMode GREEN** (`bd4385cfaa2c4fc68a6e89b16f106943`), hai scene người dùng không diff.

### Kế hoạch 5 — Cảnh thua và điện ảnh (xong)

- **Task 1 XONG**: `DeathCuts` giữ bảng `SCENE_OF`, variant theo loại cảnh, bước .05 s, bỏ qua sau 1.4 s, giới hạn 20 s. `ShiftGame.Death` runner chuỗi lồng nhau, camera easing, Film, FOV, cảnh thua dự phòng, dọn Actor/đạo cụ/đèn khi retry. `PlayerController.ExternalView` để camera cảnh thua sở hữu trọn khung hình cho tới khi phục hồi. Cảnh riêng theo từng loại ở Task 2–3.
- RED 23/24 (`2ba6400e326343a38f96cfcca61d8ada`) và RED recap khi sequence kết thúc sớm (`bdaf3067cd5c4abf80d883d1d37649c9`) → **215/215 EditMode GREEN** (`5420511b90e341debcd767c7ad28f80a`). MCP `Run Death Camera` đo xoay tới 112°, skip mở recap, retry dọn Actor và phục hồi Film/input; log Play rỗng. `Run Story Milestones` đã chờ recap mới retry.
- **Task 2 XONG**: `ShiftGame.DeathGuests` nối bốn sequence với khách/người giả. `counter` quay đầu theo mã 15 hoặc variant chẵn, còn lại kéo cổ; `childMom` gõ/nứt kính, trẻ và bóng tối; `storage` đi lùi qua kho, tắt từng đèn, áp sát; `mirae` đồng hồ loạn và sáu bóng qua kính. Đạo cụ sinh trong Play được cleanup khi retry.
- RED variant (`2b6b8f9ba10d48d2a4e6fcf8eaee39d9`) → **216/216 EditMode GREEN** (`a064f6261da1420c97d602b3d45dcbfa`). MCP `Capture Guest Death Cuts`: cả bốn cảnh chạy hết không skip (4.30/6.27/11.69/7.66 s), kiểm staging riêng, retry, log Play rỗng. Ảnh `docs/visual/deaths/guest-scenes.jpg`; letterbox HUD và so độ sáng/ánh sáng vật liệu vẫn còn ở các kế hoạch sau.
- **Task 3 XONG**: `ShiftGame.DeathObjects`, `DeathStaging`, `StoreImporter` (gỡ `BatchingStatic` cho lá cửa/nắp tủ đông/14 máng đèn trần), `HemisphereAmbient` (nối `tint` và dimming cùng máng đèn), `Actor` (`pitch`/`roll` + guard `OnDestroy`/`Remove`), `DeathCutTests` 30/30 → toàn assembly **220/220 EditMode GREEN** (`e49104ecea974fe686eaf89ccf1525d9`). MCP `Capture Object Death Cuts` đạt 11/11 cảnh (`12, 16, 6, 7, 8, 21, 5, 10, 14, 17, 0`) + retry phục hồi; `Capture Guest Death Cuts`, `Run Death Camera`, `Run Story Milestones` đều đạt, log & Console rỗng. Ảnh `docs/visual/deaths/object-scenes.jpg`.

## 6. Việc còn lại sau đó (kế hoạch 3–7, viết chi tiết khi bắt đầu)
3. Nhân vật `Actor` (mặt nạ quần áo + nhuộm, áo mưa vỏ phồng, trẻ em 0.68, biến hình xương + material reveal, NavMesh, clip `Female Walk` retarget, đứng/thở thủ tục).
4. Kịch bản `game.js` (1260 dòng): đồng hồ 6/7/8 s mỗi phút game, đạo diễn khách, 13 luật, mốc giờ, dị thường, bẫy, cảnh cáo, checkpoint mỗi giờ, Giờ Đỏ, nhật ký, Sổ tay, 2 kết thúc. Giữ nguyên mọi hằng số.
5. 15 màn chết (`deaths.js`) + khoảnh khắc điện ảnh.
6. UI (menu/cài đặt/tạm dừng/HUD/phụ đề/giấy, tiếng Việt), lưu JSON, mức đồ họa, tay cầm, F3, H.
7. Kiểm thử toàn bộ: port `sim.js` (bot), chạy trọn ca 00:00→06:00, so hình lần cuối, đo FPS, báo cáo cho người dùng.

## 7. Vấn đề đã biết / bẫy kỹ thuật
- **Forward+ của URP 17 lỗi** khi `Camera.Render()` trong Editor (NRE `ReflectionProbeManager`, rồi kẹt `ZBinningJob`) → `PC_Renderer` đang dùng **Forward** (8 đèn/vật). Hẻm sau tối hơn bản gốc ~48% (đèn cam chưa hắt đủ) — có thể do giới hạn này; cân nhắc Forward+ và chụp trong Play mode.
- Tủ mát tối hơn ~27% (hàng thay thế tối màu). Chưa có: mưa (Points), vệt phản chiếu đường ướt + quầng đèn đường (ShaderMaterial, đang tắt renderer, marker `ShaderMesh`).
- glTFast đổi tên node trùng thành `tên_0, tên_1…` — `StoreImporter.SameNode()` đã xử lý.
- Scene `HanaMart24.unity` ~20 MB (dựng lại toàn bộ mỗi lần Build Store). Chưa có Git LFS.
- MCP for Unity (CoplayDev 10.1.2) chạy stdio qua app Claude desktop; Codex có thể dùng Unity CLI/Editor trực tiếp trên máy.
- Khi Codex nhận bàn giao, Editor đang ở scene phục hồi `0` và Play mode. Đã dừng an toàn bằng `manage_editor stop`. Các thay đổi có sẵn `NM01-GOTY.slnx`, `ProjectSettings/QualitySettings.asset`, ảnh `docs/visual/store_back.png` và `Assets/_Recovery*` không thuộc task, không đưa vào commit. Các `.meta` âm thanh do Unity sinh sẽ đưa cùng tài sản âm thanh.
- Người dùng yêu cầu **dùng MCP**, không Computer Use. Đã chuyển MCP sang HTTP local `http://127.0.0.1:8090` (instance `NM01-GOTY@fccbaeec7da3b4aa`). `tools/unity_bridge.py` hỗ trợ HTTP ưu tiên và stdio dự phòng; reconnect sau domain reload trước khi chạy lệnh, không tự phát lại lệnh đã gửi.
- Unity nền có thể trì hoãn compile/reload dù refresh_unity báo idle. Luôn đối chiếu DLL `Library/ScriptAssemblies/HanaMart.*.dll` mới hơn source trước khi chạy test; `HanaMart/QA/Reload Scripts` hoặc vào/ra Play qua MCP ép nạp lại. Lượt test trên assembly cũ không được tính GREEN. `read_console` có thể trả 0 khi bộ lọc cửa sổ Console che log; kiểm thêm play-errors.log và Editor.log.

## 8. Bàn giao cho người dùng (khi mọi kế hoạch xong)
Phải chơi thử trọn ca, sửa hết lỗi Console, so hình với bản Three.js (`docs/visual/compare`), rồi mới báo người dùng.
