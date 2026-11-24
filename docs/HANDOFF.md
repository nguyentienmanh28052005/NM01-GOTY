# HANDOFF — HanaMart24 → Unity (NM01-GOTY)

> Đọc file này đầu tiên nếu bạn (Codex hoặc một phiên Claude mới) tiếp tục công việc.

## Bối cảnh
- Chuyển game Three.js **HanaMart24 v2.2** (`E:\Downloads\top\top\HanaMart24`, đọc `GAME_DOC.md`, mã nguồn `source/src/`) sang Unity, giữ y nguyên bố cục, màu, kịch bản.
- Spec: `docs/superpowers/specs/2026-11-25-hanamart24-unity-port-design.md` (đã duyệt).
- Kế hoạch đang chạy: `docs/superpowers/plans/2026-11-25-hanamart24-p1-foundation.md` (7 task). Các kế hoạch 2–7 viết khi bắt đầu từng phần (lộ trình ở cuối plan).
- Sổ tiến độ chi tiết: `.superpowers/sdd/2026-11-25-hanamart24-p1-foundation/progress.md` (không commit).
- Git: làm trên nhánh `manh`, commit sau mỗi task (`git -c user.name="manh2805" -c user.email="tienmanh2805@gmail.com" commit`), **không push**.

## Công cụ
- Unity 6000.3.8f1 + URP 17.3, mở project này. MCP for Unity (CoplayDev v10.1.2, stdio) đã cấu hình cho app Claude desktop.
- Test: Unity Test Runner, assembly `HanaMart.Tests.EditMode` (Window > General > Test Runner, hoặc MCP `run_tests`).
- Công cụ xuất từ bản Three.js: `tools/three-export/` (Python + Playwright + Chrome headless chạy `HanaMart24.html`).

## Trạng thái
| Task | Trạng thái |
|---|---|
| 1. Khung project (asmdef, scene `Assets/HanaMart/Scenes/HanaMart24.unity`, Build Settings) | XONG |
| 2. Material + rig Humanoid 4 nhân vật (`HanaMart/Setup Characters`) | XONG |
| 3. Xuất cảnh Three.js → glTF (`tools/three-export/export_store.py` → `Assets/HanaMart/Art/FromThree/store.gltf`) | XONG |
| 4. Dựng cửa hàng từ glTF (`HanaMart/Build Store`) | XONG |
| 5. Thay hàng hóa | chưa |
| 6. Hậu kỳ AgX + Film | chưa |
| 7. So sánh hình ảnh | chưa |

## Bước tiếp theo
Task 5 trong plan.
