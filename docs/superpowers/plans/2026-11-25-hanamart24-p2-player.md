# HanaMart24 → Unity — Kế hoạch 2: Người chơi & tương tác Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Người chơi đi lại, nhìn, ngồi, nhìn kỹ trong scene HanaMart24 với cảm giác y như `player.js`; nhìn vào vùng tương tác thấy nhãn và bấm/giữ E để tương tác; cửa/tủ/lò có bản lề mở-đóng như bản gốc.

**Architecture:** Logic gameplay chạy trong **hệ tọa độ Three.js** (x, z như bản gốc, để dịch `game.js` không phải đổi số); chỉ lớp hiển thị đổi sang Unity qua `ThreeCoords` (x → −x, yaw → 180° − yaw, pitch → −pitch). `PlayerMotor` là lớp C# thuần (test EditMode), `PlayerController` là MonoBehaviour đọc Input System và đặt camera. `Interactor` raycast tâm màn hình 2.1 m vào các vùng `H:*`.

**Tech Stack:** Unity 6000.3.8f1, URP 17.3, Input System 1.18, Unity Test Framework.

**Spec:** `docs/superpowers/specs/2026-11-25-hanamart24-unity-port-design.md`

## Global Constraints
- Giữ nguyên số liệu `player.js`: R 0.28, ZONES `[[0,0,12,8],[0,-4.6,4.6,0],[-1.4,-9.1,8.4,-4.6]]`, bắt đầu (0.85, 0, 4.4), mắt 1.62, ngồi −0.72, đi 1.6 / chạy 3.0 m/s (×(1−0.55·crouch)), gia tốc `min(1, dt·9)`, bước 0.68/0.82 m, pitch ±1.35 rad, độ nhạy chuột 0.0022·S.mouseSens, tay cầm 2.4·padSens·(1−0.55·zoom), lắc đầu 0.035/0.018/0.004/0.003.
- Ray tương tác: tâm màn hình, xa 2.1 m, vào các vùng `H:*` (kể cả vùng bị ẩn).
- Không sửa `GameScene.unity`/`Animation Test.unity`. Commit mỗi task lên nhánh `manh` như Kế hoạch 1. Không push.

## Review Focus
1. Người chơi đi xuyên quầy/kệ khi chạy chéo vào góc → test `PlayerMotorTests.CannotCrossColliderDiagonally`.
2. Đổi trục sai làm camera nhìn ngược/lật trái phải → test `ThreeCoordsTests.YawPitchMatchThreeForward`.
3. Vùng tương tác bị tắt (extras `hidden`) nên không raycast được → test `StoreImportTests.HitsAreActiveTriggers`.
4. Mất khóa chuột/tay cầm không xoay được → kiểm tay trong Play mode (ghi trong HANDOFF).
5. Giữ E cho việc lâu bị ngắt khi rung camera → test `InteractorTests.HoldProgressSurvivesSmallLookJitter`.

---

### Task 1: PlayerMotor (logic thuần, hệ Three.js)
**Files:** Create `Assets/HanaMart/Scripts/Runtime/Player/PlayerMotor.cs`, `PlayerInputState.cs`, `WalkArea.cs`; Test `Assets/HanaMart/Tests/EditMode/PlayerMotorTests.cs`
**Interfaces (Produces):**
- `struct PlayerInputState { float lookDX, lookDY; Vector2 move /*x=phải, y=lùi (+z)*/; Vector2 padLook; bool run, crouch, zoom; }`
- `class PlayerSettings { float mouseSens=1, padSens=1; bool invertY, headBob=true, smoothCam; }`
- `class WalkArea { static readonly float[][] Zones; List<Box2> colliders; bool Walkable(float x, float z); }` với `class Box2 { float x0,z0,x1,z1; bool off; }`
- `class PlayerMotor { Vector2 pos /*x,z Three*/; float yaw, pitch, crouch, zoom, speedMul=1, eye=1.62f; bool frozen; StepResult Update(float dt, PlayerInputState i, PlayerSettings s, WalkArea a, float roll=0); CameraPose Pose; }`; `struct CameraPose { Vector3 threePos; float pitch, yaw, roll; }`; `struct StepResult { bool stepped, run; float speed; }`
- [ ] Step 1: Test `WalkableRespectsZones` ((6,4) đi được; (6,9) không; (2,−2) được; (5,−2) không; (0,−6) được), `WalkableRespectsColliders` (trong hộp+R không; `off=true` thì được), `WalkSpeedReaches16` (giữ W 2 s ở yaw 0 → vận tốc ≈1.6, z giảm), `RunSpeedReaches30`, `CrouchSlowsTo45Percent`, `SlidesAlongWall`, `CannotCrossColliderDiagonally`, `TwoStepsAfter136m`, `PitchClamped`, `EyeHeightWithCrouch` (y = 1.62 − 0.72·crouch − 0.02 khi đứng yên).
- [ ] Step 2: chạy → FAIL. Step 3: viết theo `player.js` dòng 62–146. Step 4: PASS. Step 5: commit.

### Task 2: ThreeCoords cho camera + PlayerController + Input
**Files:** Modify `Assets/HanaMart/Scripts/Runtime/Store/ThreeCoords.cs` (+ `Quaternion CameraRotation(float pitch, float yaw, float roll)`); Create `Assets/HanaMart/Scripts/Runtime/Player/PlayerController.cs`, `Assets/HanaMart/Input/HanaMartControls.inputactions`; Test `Assets/HanaMart/Tests/EditMode/ThreeCoordsTests.cs`
- [ ] Test `YawPitchMatchThreeForward`: với (yaw, pitch) mẫu, `CameraRotation(...) * Vector3.forward` == `ToUnity(threeForward)` trong đó threeForward = (−sin yaw·cos pitch, sin pitch, −cos yaw·cos pitch).
- [ ] PlayerController: đọc chuột/bàn phím/tay cầm (Input System: Move WASD/stick trái, Look chuột/stick phải, Run Shift/L3/LB, Crouch C/Ctrl/B, Zoom chuột phải/LT/Z), khóa chuột khi chơi, gọi `PlayerMotor.Update`, đặt camera (`ThreeCoords.ToUnity(Pose.threePos)`, `CameraRotation`), FOV = 72·(1 − zoom·0.4). Collider lấy từ `store_meta.json` (38 hộp).
- [ ] Đặt PlayerController lên Main Camera trong `HanaMart24.unity`; Play mode: Console sạch, camera ở (−0.85, 1.6, 4.4) nhìn −Z Three → chụp ảnh. Commit.

### Task 3: Interactor (nhìn vào vùng tương tác, nhãn, bấm/giữ E)
**Files:** Fix `StoreImporter` (vùng `H:*` luôn active, chỉ tắt renderer); Create `Assets/HanaMart/Scripts/Runtime/Interaction/Interactor.cs`, `InteractionPrompt.cs` (uGUI + TextMeshPro, chữ giữa dưới màn hình); Tests `StoreImportTests.HitsAreActiveTriggers`, `InteractorTests.*`
**Interfaces:** `Interactor.Current` (`StoreMarker` hoặc null), `event Action<string id> Pressed`, `event Action<string id, float progress> Holding`, `Func<string id, string label> LabelFor` (game logic gắn sau, mặc định trả nhãn gốc từ extras), ray xa 2.1 m, layer `Interact`.
- [ ] Tests: `HitsAreActiveTriggers` (33 vùng active, có BoxCollider trigger, layer Interact); `RayFindsHitInFrontWithin21m`; `RayIgnoresHitBeyond21m`; `HoldProgressSurvivesSmallLookJitter`.
- [ ] Commit.

### Task 4: Cửa, tủ mát, lò vi sóng, cửa sau, tủ đông (bản lề)
Hinge cho `R:door` (cửa tự động bản lề x=2.3, mở ra ngoài), `S:fridgeDoors[i]`, `S:microwaves[i].door`, `R:backDoor` (bản lề x=1.6), `R:freezerLid`; trạng thái 0..1 nội suy như `store.update`; va chạm cửa bật/tắt theo `doorCol/backDoorCol`. Test góc mở theo trạng thái.

### Task 5: Cầm/đặt/nhặt (Q) + vật lý đồ rơi
Theo `game.js` (`setCarry`, `placeItem`, bóng mờ chỗ đặt, đường cong, nảy) + `physics.js`. Test đặt lên mặt quầy/kệ/sàn.

### Task 6: Xuất âm thanh WebAudio → WAV
`tools/three-export/export_audio.py` (OfflineAudioContext, gọi từng hàm của `audio.js`/`audio_base.js`) → `Assets/HanaMart/Audio/FromThree/*.wav`; `AudioBank` ScriptableObject.
