# HanaMart24 → Unity — Kế hoạch 3: Nhân vật

Ngày 2026-11-25. Tiếp Kế hoạch 2 (`cb4a65f`, 76/76 EditMode). Thực thi tuần tự, TDD; không cần subagent. Giữ scene của người dùng, nhánh `manh`, không push, cập nhật HANDOFF và commit từng task.

Nguồn: `source/src/person.js`, `game.js` (Actor), `nav.js` trong bản Three.js. Bốn FBX đã được CharacterSetup chuẩn bị Humanoid. Render thật: Unity 6000.3.8f1 / URP 17.3 Forward, Linear, HDR, DX12; SkinnedMeshRenderer. Áp dụng skill unity-shader, đọc package Lit hiện cài trước khi viết shader.

## Task 1 — Dữ liệu vai, chuyển động thuần, catalog bốn model

Đã hoàn thiện: RED job `31b6506df2e94226aed935d9aacd33b0` → full GREEN 88/88 `80c51531af844726a7993886817db6d8`.

- [ ] Viết test trước: trẻ em .68; scale/tall tùy chọn; reveal nội suy dt×1.6; cổ/đầu/tay và eye emission theo đúng person.js; nhìn đầu yaw ±1.3, pitch .7; cập nhật lặp không tăng dần transform; catalog đủ bốn avatar và clip humanoid.
- [ ] Stub → chạy RED, ghi job; viết ActorOptions/ActorMotion/ActorCatalog, editor tạo catalog, chạy GREEN.
- [ ] Vai mặc định: Ch31 cho nữ tóc dài/Mi-rae/biểu cảm; Ch08 cho nam áo mưa/quản lý; Ch01 cho khách nam/trẻ thu nhỏ; Ch42 cho nữ/khách áo khoác. Cho phép override bằng model index trong dữ liệu vai, không sửa model gốc.
- [ ] Commit và HANDOFF.

## Task 2 — Mặt nạ quần áo, shader nhuộm/lộ dạng, áo mưa

Đã hoàn thiện: nhiều vòng RED cho mask thực, phân loại mesh và phụ kiện → full **103/103 GREEN**, job `be13e51ab0aa487a92d4699c25dd8a65`. Ảnh QA trong `docs/visual/characters/`; Actor animation vẫn thuộc Task 3.

- [ ] Test RED: mỗi body material có mask data linear và diffuse/spec/normal còn nguyên; mask phân biệt da/quần áo; shader compile không lỗi; reveal=0 không đổi base texture; áo mưa không che mặt/tay.
- [ ] Editor sinh mask từ UV + trọng số xương và màu da ở vùng tay, lưu trong HanaMart; kiểm ảnh atlas và mask cho cả bốn model. Kênh R áo, G quần, B da, A mặt. Hair giữ material tóc và alpha.
- [ ] Shader URP dựa trên LitInput/LitForwardPass của package 17.3 đang cài; tint chỉ vùng quần áo. Noise/vein và vertex jitter dịch person.js, tham số reveal 0..1/time giây. Tách include dùng chung cho forward/depth/shadow để không lệch bóng.
- [ ] Áo mưa = bản sao SkinnedMeshRenderer cùng bones, shell phồng có mặt nạ bỏ da/mặt, độ bóng .28/clearcoat giả .8; thêm mũ trùm. Mask trắng/đen/cười, da tái/không mặt theo options.
- [ ] Render cùng camera/đèn trong HanaMart, chụp adult/child/yellow coat và reveal 0/.5/1, Console/logger sạch. Commit + HANDOFF.

## Task 3 — Actor runtime, animation và biến hình xương

- [ ] Test RED: spawn/despawn catalog; vị trí/yaw Three→Unity; scale .68 không cộng dồn; di chuyển phát Female Walk ở mọi avatar; reveal lặp không trôi xương; reset về 0 phục hồi tư thế.
- [ ] Actor MonoBehaviour dùng options/catalog, Animator Humanoid + clip Female Walk retarget. Idle đứng tự nhiên + thở/quay đầu thủ tục. Không sửa controller của scene Animation Test.
- [ ] Giữ hằng số person.js: stride 1.3, blend dt×6; thở sin(t×1.6+phase×.1); reveal dt×1.6; head nâng .34 m; arm length +.55; mắt scale +1.6/emission×6; head roll R×.9×sin(t×.7), snap 1.2..4.4 s, lerp dt×22, jitter .02/.03. Điều chỉnh theo trục xương model thật, không áp tùy tiện vào local Y.
- [ ] Actor API cho Kế hoạch 4: pos Three, yaw, speed, lookTarget, revealTarget, headSpin, armUp, moveTo/path, remove; quản lý material instance và tài nguyên khi despawn.
- [ ] Play bốn avatar đi/đứng, child, biến hình; log sạch. Commit + HANDOFF.

## Task 4 — Navigation và mặt đỡ đặt đồ

- [ ] Test RED: đường ngoài phố→cửa trước→quầy; quầy→cửa kho→cửa sau; không cắt góc kệ; không qua kính/tường; đường không tồn tại trả trạng thái thất bại có giới hạn.
- [ ] Bake NavMesh từ vùng sàn nối và collider, radius .24; bỏ hai collider cửa đúng game.js. Giữ các cổng như nav.js, source positions Three chỉ chuyển ở renderer.
- [ ] Port walkableAt grid C=.15/R=.24/AREAS cho logic đặt đồ, nối Placement thay WalkArea người chơi. Mọi tìm kiếm có giới hạn (nguồn A* 60000 lượt, nearestFree 25 ô), không treo Editor.
- [ ] Test placement cũ và cả assembly GREEN. Commit + HANDOFF.

## Task 5 — Kiểm tích hợp nhân vật

- [ ] QA qua MCP: showroom tạm trong Play (không đổi bố cục scene đã lưu) cho bốn model, các vai và ba mức reveal; chụp bằng chứng.
- [ ] Kiểm đường đi thực tế không xuyên kệ/quầy/kính, tốc độ và animation đồng bộ; 60 giây cập nhật không drift/leak/Console exception. Giới hạn QA theo thời gian và số vòng.
- [ ] Kiểm hai scene bị bảo vệ không có git diff; toàn HanaMart.Tests.EditMode GREEN; cập nhật HANDOFF, ledger và commit. Viết Kế hoạch 4 khi bắt đầu, không báo hoàn thành game ở mốc này.
