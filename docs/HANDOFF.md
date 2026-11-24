# HANDOFF — HanaMart24 → Unity (NM01-GOTY)

> **Đọc file này đầu tiên.** Người bàn giao: Claude (2026-11-25 02:15). Người nhận: Codex (hoặc phiên Claude mới).
> Người dùng: Nguyễn Tiến Mạnh — nói tiếng Việt, hãy trả lời bằng tiếng Việt.

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
- Kế hoạch 2 (ĐANG LÀM): `docs/superpowers/plans/2026-11-25-hanamart24-p2-player.md` — lộ trình kế hoạch 3–7 ở cuối plan 1.
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
- Task 5 đang tới: cầm/đặt Q + đồ rơi. `StoreDoors.defaultInteractions` là tương tác tạm để kiểm cửa; Kế hoạch 4 phải tắt và điều khiển qua logic luật (đặc biệt tủ đông/lò), tránh mở tủ tự do khi đã có kịch bản.

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
