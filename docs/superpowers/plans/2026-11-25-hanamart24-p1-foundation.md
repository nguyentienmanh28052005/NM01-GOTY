# HanaMart24 → Unity — Kế hoạch 1: Nền móng (giai đoạn 0–2) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Có scene `HanaMart24.unity` dựng y nguyên cửa hàng/kho/hẻm/phố của bản Three.js v2.2, hàng hóa thay bằng asset siêu thị, 4 nhân vật đủ material + rig Humanoid, màu/hậu kỳ AgX+Film, và bộ ảnh so sánh với bản gốc.

**Architecture:** Chạy bản Three.js (`HanaMart24.html`) trong Chrome không giao diện, tiêm một bộ ghi GLB tự viết để xuất cảnh `__HANA.store.scene` (hình học, material, texture canvas, tên theo khóa của `store`) cùng siêu dữ liệu (đèn, vùng tương tác, va chạm, ô hàng hóa) vào `extras`. Unity nhập GLB bằng glTFast, một script Editor chuyển material sang URP/Lit, dựng đèn/va chạm/probe từ `extras`, thay ô hàng hóa bằng prefab asset siêu thị. Hậu kỳ là renderer feature HLSL port từ `main.js`.

**Tech Stack:** Unity 6000.3.8f1, URP 17.3, glTFast 6.21, Unity Test Framework 1.6; Python 3 + Playwright + Chrome (máy làm việc của Claude) cho công cụ xuất; MCP for Unity để điều khiển Editor.

**Spec:** `docs/superpowers/specs/2026-11-25-hanamart24-unity-port-design.md`

## Global Constraints
- Unity 6000.3.8f1, URP 17.3; không thêm package ngoài những gì đã có trong `Packages/manifest.json`.
- Mọi file mới của game nằm trong `Assets/HanaMart/`; công cụ xuất nằm trong `tools/three-export/`.
- Không sửa `Assets/Scenes/GameScene.unity`, không làm hỏng `Assets/Scenes/Animation Test.unity`.
- Giữ nguyên số liệu bản gốc (vị trí, kích thước, màu, cường độ đèn). Hệ tọa độ: Three.js (Y lên, tay phải) → glTF → glTFast tự chuyển; không tự đổi trục ở chỗ khác.
- Chữ trong game: tiếng Việt như bản gốc; tên món đổi theo bảng Q1 của spec.
- Git: commit sau mỗi task lên nhánh `manh` bằng `git -c user.name="manh2805" -c user.email="tienmanh2805@gmail.com" commit`, cuối message có
  `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>` và `Claude-Session: https://claude.ai/code/session_015KZr8vhrHdpCnvVzazQhr4`. Không push.
- Sau mỗi task: Console Unity không lỗi biên dịch; cập nhật `docs/HANDOFF.md` (đã xong / đang dở / bước tiếp).

## Review Focus
1. Material trong suốt/phát sáng (kính mặt tiền, cửa tủ mát, neon, biển hiệu, màn hình) bị chuyển thành đục hoặc mất phát sáng → test `StoreImportTests.TransparentAndEmissiveSurvive` (Task 4).
2. Chữ trên biển/poster/giấy bị lật gương sau khi đổi hệ tọa độ → kiểm bằng ảnh so sánh góc "quay" và "lối vào" (Task 7, bước so ảnh) + test `StoreImportTests.SignTextNotMirrored` (Task 4: UV.x của mặt biển kệ tăng theo hướng phải của người nhìn).
3. Chạy lại "Build Store" lần hai sinh trùng object → test `StoreImportTests.RebuildIsIdempotent` (Task 4).
4. Đổi rig sang Humanoid làm hỏng scene Animation Test → kiểm Play mode scene đó, Console sạch (Task 2).
5. Hàng hóa thay thế to hơn ô kệ, cắm xuyên kệ/tủ → test `ProductTests.FitsThreeSlot` (Task 5).

---

### Task 1: Khung project

**Files:**
- Create: `Assets/HanaMart/Scripts/Runtime/HanaMart.Runtime.asmdef`, `Assets/HanaMart/Scripts/Editor/HanaMart.Editor.asmdef` (refs: Runtime, glTFast, Unity.RenderPipelines.Universal.Runtime), `Assets/HanaMart/Tests/EditMode/HanaMart.Tests.EditMode.asmdef` (Editor-only, refs Editor+Runtime, nunit)
- Create: `Assets/HanaMart/Scenes/HanaMart24.unity` (trống: Main Camera FOV 72, near 0.05; Global Volume)
- Modify: `ProjectSettings/EditorBuildSettings.asset` — danh sách build = [`Assets/HanaMart/Scenes/HanaMart24.unity`] (bỏ `SampleScene.unity` đã mất)
- Create: `docs/HANDOFF.md`
- Test: `Assets/HanaMart/Tests/EditMode/BuildSettingsTests.cs`

- [ ] **Step 1: Test** `BuildSettingsTests.AllBuildScenesExist()` — mọi `EditorBuildSettings.scenes[i].path` tồn tại (`File.Exists`); `HanaMartSceneIsFirst()` — `scenes[0].path == "Assets/HanaMart/Scenes/HanaMart24.unity"`.
- [ ] **Step 2: Chạy** `run_tests(mode=EditMode, filter=BuildSettingsTests)` → FAIL.
- [ ] **Step 3:** Tạo asmdef, scene, sửa Build Settings (MCP `manage_build scenes`).
- [ ] **Step 4: Chạy lại** → PASS; Console sạch.
- [ ] **Step 5:** Viết `docs/HANDOFF.md` (mục: bối cảnh, cách chạy MCP, spec/plan, trạng thái task). Commit.

### Task 2: Material + rig cho 4 nhân vật

**Files:**
- Create: `Assets/HanaMart/Scripts/Editor/CharacterSetup.cs` — menu `HanaMart/Setup Characters`, `public static void Run()`
- Output: `Assets/HanaMart/Art/Characters/<Ch08|Ch01|Ch42>/` (texture tách ra + material), sửa `.meta` của 4 FBX
- Test: `Assets/HanaMart/Tests/EditMode/CharacterSetupTests.cs`

**Interfaces:** Produces `CharacterSetup.ModelPaths` (string[4]: đường dẫn FBX Ch31/Ch08/Ch01/Ch42) cho Kế hoạch 3 (Actor).

- [ ] **Step 1: Test** `EveryRendererHasUrpLitWithBaseMap` — với mỗi FBX: mọi `Renderer.sharedMaterials` khác null, `shader.name=="Universal Render Pipeline/Lit"`, `_BaseMap` khác null; `HairIsTransparent` — material có "hair" trong tên có `_Surface==1`; `RigIsHumanoid` — `ModelImporter.animationType==Human` và avatar `isHuman && isValid`.
- [ ] **Step 2:** Chạy → FAIL (3 model chưa có material, rig Generic).
- [ ] **Step 3:** `Run()`: `ModelImporter.ExtractTextures(dir)`; tạo material theo mẫu `Ch31_body.mat`/`Ch31_hair.mat` (URP Lit, `_WorkflowMode=0` Specular, BaseMap=`*_Diffuse`, BumpMap=`*_Normal` (đặt texture type NormalMap), SpecGlossMap ghép từ `*_Specular` RGB + `*_Glossiness` A như `Ch31_1001_SpecGloss.png`, `_Smoothness=0.45`; hair: `_Surface=1`, `_Cull=2`); `importer.SearchAndRemapMaterials` hoặc `AddRemap` theo tên slot; `animationType=Human`, `avatarSetup=CreateFromThisModel`; `SaveAndReimport`.
- [ ] **Step 4:** Chạy test → PASS. Mở `Animation Test.unity`, Play 5 s (MCP), Console không lỗi, chụp ảnh; quay lại scene cũ không lưu thay đổi.
- [ ] **Step 5:** Commit.

### Task 3: Công cụ xuất cảnh Three.js → GLB

**Files:**
- Create: `tools/three-export/glb_writer.js` — hàm tiêm vào trang `window.__exportGLB(root, opts) → {glb: base64, stats}`
- Create: `tools/three-export/export_store.py` — chạy `HanaMart24.html?q=ultra&nofade` (Playwright, swiftshader), đợi `__HANA.store`, tiêm writer, ghi `out/store.glb` + `out/store_meta.json`
- Test: `tools/three-export/test_export.py`

**Interfaces:** Produces `store.glb` với quy ước tên node:
- node có khóa store: `S:<key>` hoặc `S:<key>[i]` (khóa của `store`, `store.refs` → `R:<key>`);
- ô hàng hóa: node rỗng `P:<type>:<variant>[:<restockKey>]`, `extras={type,variant,restock,w,h,d}` (w/h/d từ `store._geos[type]`);
- đèn: node rỗng `L:<i>`, `extras={kind:Point|Spot|RectArea|Hemisphere, color, intensity, distance, decay, angle, penumbra, width, height, zone, group}`;
- vùng tương tác: node rỗng `H:<id>`, `extras={label, size:[x,y,z]}`; va chạm: `C:<i>`, `extras={min,max}`;
- material ShaderMaterial (mưa, quầng đèn, vệt đường, mảng mục): xuất hình học + `extras={shader:"<tên>"}`;
- texture canvas → PNG nhúng, `KHR_texture_transform` cho repeat/offset; `extras` cấp material giữ `clearcoat`, `transmission`, `envMapIntensity`.
- Produces `store_meta.json`: `{background, fog, lightGroups, probe:{pos,boxMin,boxMax}, counts}`.

- [ ] **Step 1: Test** `test_export.py`: GLB hợp lệ (magic `glTF`, JSON parse); số node `P:*` == tổng `instance count` các InstancedMesh hàng hóa (đọc trong trang); số `L:*` == 15; số `H:*` == `store.hits.length`; số `C:*` == `store.colliders.length`; ≥ 120 ảnh PNG; không mesh nào có NaN trong POSITION.
- [ ] **Step 2:** Chạy → FAIL (chưa có file).
- [ ] **Step 3:** Viết `glb_writer.js` (duyệt `scene`, gom bufferView theo geometry, ghi TRS cục bộ, material → pbrMetallicRoughness + emissive + alphaMode BLEND khi `transparent`, `doubleSided` khi `side==DoubleSide`) và `export_store.py`.
- [ ] **Step 4:** Chạy test → PASS. Chép `out/store.glb`, `out/store_meta.json` vào `Assets/HanaMart/Art/FromThree/` (device_commit_files).
- [ ] **Step 5:** Commit (kèm `tools/three-export/`).

### Task 4: Dựng cửa hàng trong Unity từ GLB

**Files:**
- Create: `Assets/HanaMart/Scripts/Editor/StoreImporter.cs` — menu `HanaMart/Build Store`, `public static GameObject Build(Scene scene)`
- Create: `Assets/HanaMart/Scripts/Runtime/Store/StoreMarker.cs` — `public enum Kind {Ref, Product, Light, Hit, Collider}`, `public string key; public Kind kind; public string json;` (giữ extras)
- Create: `Assets/HanaMart/Art/Materials/Converted/` (material URP Lit sinh ra)
- Test: `Assets/HanaMart/Tests/EditMode/StoreImportTests.cs`

**Interfaces:**
- Consumes: quy ước node của Task 3.
- Produces: trong scene `HanaMart24` một root `Store` chứa: hình học tĩnh (Static), `StoreMarker` trên mọi node `S:/R:/P:/L:/H:/C:`; `Light` thật cho `L:*`; `BoxCollider` cho `C:*`; `BoxCollider isTrigger` + layer `Interact` cho `H:*`; `ReflectionProbe` "StoreProbe" box (0,0,0)-(12,3,8) tâm (6.2,1.55,4.0) (sau đổi trục).

- [ ] **Step 1: Test** `StoreRootExists`, `CollidersMatchMeta` (số BoxCollider không trigger == `counts.colliders`), `LightsMatchMeta` (số Light == 15, RectArea → Spot góc 120° + cường độ quy đổi), `TransparentAndEmissiveSurvive` (material nguồn BLEND → URP `_Surface==1`; nguồn có emissive ≠ 0 → bật `_EMISSION`), `SignTextNotMirrored`, `RebuildIsIdempotent` (Build 2 lần → số Renderer không đổi), `NoErrorShader`.
- [ ] **Step 2:** Chạy → FAIL.
- [ ] **Step 3:** `Build`: xóa root `Store` cũ nếu có; instantiate prefab glTFast của `store.glb`; chuyển material sang URP Lit (giữ map, màu, `smoothness=1-roughness`, metallic, emissive, alpha, tiling/offset); đặt Static; đọc `extras` → tạo Light/Collider/Hit/Probe; nền camera = `store_meta.background`.
- [ ] **Step 4:** Chạy test → PASS; chụp ảnh từ 3 góc (MCP `manage_camera screenshot`).
- [ ] **Step 5:** Commit.

### Task 5: Thay hàng hóa bằng asset siêu thị

**Files:**
- Create: `Assets/HanaMart/Scripts/Editor/ProductExtractor.cs` — menu `HanaMart/Extract Products`; tạo prefab `Assets/HanaMart/Art/Prefabs/Products/<category>_<n>.prefab` từ các khối của `магазин.glb` theo material
- Create: `Assets/HanaMart/Scripts/Runtime/Store/ProductCatalog.cs` (ScriptableObject `ProductCatalog : ScriptableObject { public Entry[] entries; }`, `Entry { string threeType; GameObject[] prefabs; string displayName; }`) + asset `Assets/HanaMart/Data/ProductCatalog.asset`
- Modify: `StoreImporter.Build` — thay mỗi node `P:*` bằng prefab theo catalog, co theo `w/h/d`
- Test: `Assets/HanaMart/Tests/EditMode/ProductTests.cs`

Bảng catalog (spec 6.5 + Q1): ramenCup, ramenBag → `рамены`; chips → `чипси`; candy (kệ kẹo quầy) → `мармелад`; kimbap, lunch → `суши`; can, bottle, coffee, soju (tủ mát) → `мясо`/`трава`/`фрукты`/`всякое` xoay vòng; banana → `пельмени`; cig → giữ hình học Three.js (không thay).
displayName: `ramenCup:"Mì ly"`, `ramenBag:"Mì gói"`, `chips:"Snack"`, `kimbap:"Cơm cuộn"`, `lunch:"Cơm hộp"`, `banana:"Bánh mandu"`, `can/bottle/coffee/soju`: tên theo món thay thế (`"Thịt"`, `"Rau"`, `"Trái cây"`, `"Đồ tươi"`).

- [ ] **Step 1: Test** `EveryProductNodeReplaced` (không còn node `P:*` chỉ có hình học Three.js, trừ `cig`), `CountsMatchMeta` (theo type), `FitsThreeSlot` (bounds của prefab ≤ `w,h,d` × 1.05), `RestockGroupsKept` (`shelfRamen`, `shelfMilk` còn đủ nhóm để bật/tắt).
- [ ] **Step 2:** Chạy → FAIL.
- [ ] **Step 3:** Viết extractor + catalog + thay thế (dùng `GPU instancing` trên material).
- [ ] **Step 4:** Chạy test → PASS; chụp kệ mì, tủ mát, quầy.
- [ ] **Step 5:** Commit.

### Task 6: Màu & hậu kỳ (AgX + Film)

**Files:**
- Create: `Assets/HanaMart/Art/Shaders/AgxFilm.shader` (HLSL: AgX như `three/src/renderers/shaders/ShaderChunk/tonemapping_pars_fragment` r160, exposure 1.02; Film = port `const Film` trong `main.js` dòng 62+, cùng uniform)
- Create: `Assets/HanaMart/Scripts/Runtime/Render/AgxFilmFeature.cs` (`ScriptableRendererFeature`, inject AfterRenderingPostProcessing), `Assets/HanaMart/Scripts/Runtime/Render/FilmSettings.cs` (`VolumeComponent` với các uniform Film: grain, ca, vignette, wave, glitch, death, grade)
- Create: `Assets/HanaMart/Scripts/Runtime/Render/Agx.cs` (bản C# của AgX để test)
- Modify: `Assets/Settings/PC_RPAsset.asset` (HDR on), `PC_Renderer.asset` (thêm feature, SSAO), Volume profile `Assets/HanaMart/Settings/HanaMartVolume.asset` (Tonemapping None, Bloom intensity 0.3 / scatter 0.5 / threshold 1.05)
- Test: `Assets/HanaMart/Tests/EditMode/AgxTests.cs`

- [ ] **Step 1: Test** `AgxMatchesThreeReference` — `Agx.Tonemap(new Vector3(r,g,b)*1.02)` khớp ±0.002 với 6 cặp mẫu tính bằng công thức three.js (tạo bằng `tools/three-export/agx_ref.js` chạy Node): (0,0,0), (0.18,0.18,0.18), (1,1,1), (4,2,1), (0.05,0.1,0.3), (16,16,16).
- [ ] **Step 2:** Chạy → FAIL.
- [ ] **Step 3:** Viết `Agx.cs`, shader, feature, settings; gán vào renderer/volume.
- [ ] **Step 4:** Chạy test → PASS; Play scene, Console sạch.
- [ ] **Step 5:** Commit.

### Task 7: Ảnh tham chiếu & so sánh hình ảnh lần 1

**Files:**
- Create: `tools/three-export/viewpoints.json` — 12 góc: `entrance, counter, aisle1, aisle2, aisle3, fridges, noodleBar, storage, freezer, alley, street, glassFromOutside`; mỗi góc `{pos:[x,y,z], yaw, pitch, fov:72}` theo tọa độ Three.js
- Create: `tools/three-export/capture_three.py` (đặt `__HANA.camera` theo góc, ẩn HUD, chụp 1280×720 → `out/ref/<name>.png`)
- Create: `Assets/HanaMart/Scripts/Editor/ViewpointCapture.cs` — menu `HanaMart/Capture Viewpoints` (đọc cùng JSON, chuyển tọa độ như glTFast, render 1280×720 → `docs/visual/unity/<name>.png`)
- Create: `tools/three-export/compare.py` — ghép cạnh nhau `docs/visual/compare/<name>.jpg` + bảng `docs/visual/report.md` (độ sáng TB, màu TB vùng tối/sáng, chênh lệch %)

- [ ] **Step 1:** Chụp bản Three.js → 12 ảnh.
- [ ] **Step 2:** Chụp bản Unity → 12 ảnh.
- [ ] **Step 3:** So sánh; chỉnh cường độ đèn/exposure/bloom/probe cho tới khi chênh lệch độ sáng TB ≤ 10% mỗi góc và tông vùng tối cùng hướng xanh lam (B > R); khác biệt còn lại chỉ do hàng hóa/model mới — ghi lý do từng góc trong `report.md`.
- [ ] **Step 4:** Gửi ảnh so sánh cho bạn xem.
- [ ] **Step 5:** Commit (ảnh nén JPG).

---

## Lộ trình các kế hoạch sau (mỗi kế hoạch viết chi tiết khi bắt đầu)
- **Kế hoạch 2 — Người chơi & tương tác:** `PlayerController` theo `player.js` (tốc độ, ngồi C, nhìn kỹ chuột phải, va chạm), Input System + tay cầm/rung; tương tác E/giữ E, cầm/đặt Q (bóng mờ, đường cong, nảy), xếp hàng, lau sàn, lò vi sóng, tủ mát, rác, cửa, CCTV, điện thoại, giấy/nội quy; vật lý Rigidbody; xuất âm thanh WebAudio → WAV.
- **Kế hoạch 3 — Nhân vật:** `Actor` (4 model, mặt nạ quần áo + nhuộm, áo mưa vỏ phồng, trẻ em 0.68, không mặt/da tái/mặt nạ), Animator Humanoid + `Female Walk`, đứng/thở/quay đầu thủ tục, biến hình xương + material `reveal`, NavMesh.
- **Kế hoạch 4 — Kịch bản:** đồng hồ/phase, đạo diễn khách, 13 luật + phụ lục + điều 14, nội quy giả, mốc cố định & lệch, dị thường nhỏ, 9 bẫy, cảnh cáo, checkpoint, Giờ Đỏ, nhật ký, Sổ tay, 2 kết thúc.
- **Kế hoạch 5 — Màn chết & điện ảnh:** 15 cảnh `deaths.js`, khoảnh khắc điện ảnh, nhịp tim, bóng rìa tầm nhìn, im lặng trước biến cố, dọa hụt.
- **Kế hoạch 6 — Giao diện & hệ thống:** menu/cài đặt/tạm dừng/HUD/phụ đề/giấy, lưu JSON, mức đồ họa + độ phân giải động, F3, H, hướng dẫn.
- **Kế hoạch 7 — Kiểm thử toàn bộ & bàn giao:** port `sim.js`, chạy bot, danh sách kiểm kịch bản, so hình ảnh lần cuối, đo FPS, báo cáo.
