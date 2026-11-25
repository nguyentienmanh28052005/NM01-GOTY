# HanaMart24 → Unity — Kế hoạch 6: UI, input và lưu

Dùng `source/src/main.js` và markup/CSS của bản gốc làm mẫu bố cục, tiếng Việt và hành vi. Dùng font và TMP từ Kế hoạch 2. Chỉ thay nhãn hàng khi asset thay thế đã được duyệt.

## Task 1 — Trạng thái UI và lưu JSON (XONG)

- **Đã xong:**
  - `Assets/HanaMart/Scripts/Runtime/UI/SaveData.cs`: `GameSettings` khớp mặc định `hana24_v3` trong `main.js`, `SaveData` (cài đặt, kết thúc A/B, `tutSeen`, bảo toàn trường lạ/tương lai), `SaveStore` (đọc/gộp an toàn khi JSON hỏng hoặc sai kiểu, ghi thay thế nguyên tử `hana24_v3.json` qua file `.tmp`).
  - `Assets/HanaMart/Scripts/Runtime/UI/UiFlow.cs`: máy trạng thái `AppState` (`Boot`/`Menu`/`Play`/`Pause`), `UiScreen`, `UiAction`, `InputDeviceKind`, chặn tạm dừng khi đang mở overlay/kết thúc, khóa con trỏ theo thiết bị.
  - `Assets/HanaMart/Input/HanaMartControls.inputactions`: thêm map `UI` (`UiNavigate`, `UiConfirm`, `UiBack`, `UiTabPrev`, `UiTabNext`) hỗ trợ bàn phím + tay cầm, giữ nguyên map `Player`.
  - `Assets/HanaMart/Scripts/Runtime/UI/ShiftInterface.cs`, `ShiftSetup.cs`, hook `PlayerController`/`ShiftGame`/`ShiftHud`: khởi động ở `Boot`, camera attract ngoài giờ ca, menu/tạm dừng/cài đặt/hướng dẫn vỏ, đồng bộ `Time.timeScale = 0` khi tạm dừng và khôi phục khi tiếp tục/thoát, fallback font `Malgun Gothic` cho chữ Hàn.
  - **Xác nhận:** TDD RED (`p6-t1-red.json`, 12/12 fail) → GREEN (`p6-t1-green.json`, 12/12 pass) → toàn bộ `HanaMart.Tests.EditMode` **237/237 PASS** (`p6-t1-full.json`, job `60be88d5e5f14f74b2fc263efb95b7cb`). Play QA `Run Ui Flow` (`ui-flow-smoke.json`), `Run Story Milestones` (`story-smoke.json`), `Run Death Camera` (`death-camera.json`) đều đạt, `play-errors.log` và Console 0 lỗi/cảnh báo, ảnh `p6-t1-menu.png` và `p6-t1-pause.png` đã kiểm tra trực quan.

## Task 2 — HUD và hội thoại (XONG)

- **Đã xong:**
  - `Assets/HanaMart/Scripts/Runtime/Shift/RuleBook.cs`: `RuleEntry`, `RuleView`, `NoteFor`, `BuildView`.
  - `Assets/HanaMart/Scripts/Runtime/UI/HudModel.cs`: `RulePaperFormatter`, `GuideDirector`, `DialogFocus`, `DeathRecapModel`, `EndingModel`, `TaskListModel`, `HudLayoutSpec`.
  - `ShiftGame` partials & `UiFlow.ShowScreen`: sự kiện `ToastRequested`, `BannerRequested`, `WarnRequested`, `TasksPanelToggled`, `DeathRecapShown`, hỗ trợ `ExternalDialogInput`, `ExternalTasksPanel`, `PaperIsRules`/`PaperIsHandwritten`.
  - `ShiftInterface.cs`, `ShiftInterface.Hud.cs`, `ShiftInterface.Screens.cs`: tắt `ShiftHud`/`InteractionPrompt` tạm, HUD đầy đủ (letterbox, flash, đồng hồ + 3 vạch cảnh cáo, chỉ dẫn `◆` + mũi tên rìa, 4 việc chờ + bảng Tab, prompt + thanh giữ E, chào/bỏ qua, đồ cầm + gợi ý theo thiết bị, phụ đề, toast/banner/cảnh cáo đỏ), đối thoại 1–3 + tay cầm, tờ giấy `enableAutoSizing` dấu đỏ `[HANA]` + điều giả + phụ lục + Sổ tay sống sót + nhật ký, màn chết, 2 kết thúc A/B, màn Hướng dẫn 2 cột và giấy hướng dẫn ca đầu tiên (sau 2.6 s khi bấm Nhận ca).
  - **Xác nhận:** TDD RED (`p6-t2-red.json`) → GREEN (`p6-t2-green.json`, 19/19 pass) → toàn bộ `HanaMart.Tests.EditMode` **244/244 PASS** (`p6-t2-full.json`, job `04c83b4ef9d6462a978cfb2ee86ec0ef`). Play QA `Run Ui Flow`, `Run Hud And Dialog`, `Run Story Milestones`, `Run Death Camera` đều đạt; `play-errors.log` và Console 0 lỗi/cảnh báo; 5 ảnh `p6-t2-{hud,rules,dialog,death,ending}.png` đã kiểm tra trực quan.

## Task 3 — Cài đặt và mức đồ họa (XONG)

- **Đã xong:**
  - `Assets/HanaMart/Scripts/Runtime/UI/GraphicsSettingsModel.cs`: 5 preset `low`/`medium`/`high`/`ultra`/`cine`, `DynamicResolutionController` (hạ khi <50 FPS về tối thiểu 0.55, phục hồi khi >58 FPS), `SettingsModel` (3 tab Đồ họa/Âm thanh/Điều khiển, Đặt lại mặc định, Quay lại), `PerfOverlayModel`.
  - Hook runtime trên `PlayerController` (`BaseFov`, `RumbleEnabled`), `ShiftGame` (`AmbientVolume`, `SfxVolume`), `ShiftGame.Cinematics`, `ShiftGame.Environment` (`ApplyLightPreset` giữ đèn trần > 0 với `PresetScale` + `LightRenderMode`).
  - `Assets/HanaMart/Scripts/Runtime/UI/ShiftInterface.Settings.cs` + nối `ShiftInterface.cs`/`ShiftInterface.Hud.cs`: giao diện Cài đặt 3 tab bằng chuột/phím/tay cầm (`Q`/`E`/`LB`/`RB`), áp dụng trực tiếp độ sáng/hạt phim/FOV/độ nhạy/đảo Y/lắc đầu/rung/âm thanh/renderScale/SMAA/aniso, lưu và khôi phục mặc định trong `hana24_v3.json`, phím `F3` (FPS + độ phân giải + preset), phím `H` (ẩn HUD giữ toast), `F11` toàn màn hình. Khôi phục URP/Quality/AudioListener trong `OnDestroy` để không làm bẩn asset.
  - **Xác nhận:** TDD RED (`p6-t3-red.json`, 6/6 fail) → GREEN (`p6-t3-green.json`, 25/25 pass) → toàn bộ `HanaMart.Tests.EditMode` **250/250 PASS** (`p6-t3-full.json`, job `206774e7b6de4d5799b3ce2b5b6fabf4`). Play QA `Run Ui Flow`, `Run Hud And Dialog`, `Run Settings And Presets`, `Run Story Milestones`, `Run Death Camera` đều đạt; `play-errors.log` và Console 0 lỗi/cảnh báo; ảnh tổng hợp `docs/visual/ui/interface-compare.jpg` đã kiểm tra trực quan.
