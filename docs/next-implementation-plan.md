# Core Guard — Next Implementation Plan

## Handoff status — 2026-09-15

Branch: `feature/core-guard-gameplay-updates` (Unity `6000.3.23f1`). Pull this branch and continue from the first unchecked item below.

### Completed and committed

- [x] Task 1 — shared Player/Core world health bars and live current/max HUD values (`2453294`, `bade440`).
- [x] Task 2 — enemy projectile damage plus visible firing flash/audio feedback (`3829191`, `90f44f8`).
- [x] Task 3 — deterministic random X/Y/Z lifecycle, 5 seconds visible + 5 seconds hidden, pause/reset/session isolation (`c9187b2`, `1c027d2`, `e91ac0d`; focused tests 13/13).
- [x] Task 4 — all six X/Y/Z effects plus procedural animation/VFX, floating text, and SFX (`cdffd02`; interaction tests 20/20 and audio tests 3/3).
- [x] Task 5 — Shield/EMP mechanics, cooldown/reset/session filtering, shield/EMP rings, EMP affected count (`dab68ed`; defense 4/4 and presentation 4/4).
- [x] Task 6 — compact anchored HUD, no large top/bottom surfaces, READY/active/cooldown states, 1280x720 and 1920x1080 bounds (`dab68ed`; layout 1/1 and HUD regression 2/2).
- [x] Task 8 partial — idempotent repair of a missing enemy gate while preserving authored gate references (focused test 1/1; commit immediately after this handoff update).

### Intentionally deferred

- [ ] Task 7 — asset selection/integration, import settings, licenses, and `docs/asset-register.md`. The owner explicitly deferred this work. `D:/Game-T5/Resources` is user-supplied and must not be deleted or committed accidentally.

### Continue here

- [ ] Run the complete EditMode suite, fix any integration failures, and record the final count.
- [ ] Run the complete PlayMode suite, fix any integration failures, and record the final count.
- [ ] Run `Core Guard → Configure Project` twice and confirm the builder remains idempotent in the real Main scene.
- [ ] Perform the manual Editor smoke pass: movement/aim/fire, enemy shots, X/Y/Z cycles and feedback, Shield/EMP, pause/retry, win/loss, and HUD readability at both target resolutions.
- [ ] Update `docs/progress.md` and `docs/acceptance.md` with fresh full-suite and smoke evidence. Leave `docs/asset-register.md` for the deferred asset pass.
- [ ] Review the combined Task 5/6 commit `dab68ed`; the automated reviewer could not complete because its usage quota expired.
- [ ] After the deferred asset pass, rerun full verification and merge this feature branch.

Known local-only paths that must stay uncommitted: `Resources/`. Unity test/configuration runs may also touch `Assets/Settings/InputSystem_Actions.inputactions` and the Enemy/Mine/Projectile prefabs; inspect ownership before committing those files.

## 1. Mục tiêu

Hoàn thiện demo Unity theo hướng người chơi phải vừa di chuyển né đạn enemy, vừa dùng kỹ năng phòng thủ và thu thập các object X/Y/Z trong arena.

Phạm vi chính:

- Thêm và chuẩn hóa thanh máu cho player và Core.
- Hoàn thiện enemy projectile để player phải di chuyển né đạn.
- Cho X/Y/Z xuất hiện ở vị trí ngẫu nhiên theo chu kỳ 5 giây.
- Tạo tối thiểu 6 hiệu ứng khi player va chạm X/Y/Z.
- Hoàn thiện Shield và EMP cho xe tăng.
- Dọn HUD, không để panel biên trên/dưới che arena.
- Thay thế và đồng bộ asset theo phong cách sci-fi.

## 2. Trạng thái hiện tại cần lưu ý

- Project là Unity Editor demo 2D top-down, dùng Input System.
- `PlayerStats` đã có HP, Armor, Coins và event `Changed`, nhưng chưa có world health bar riêng cho player.
- `CoreHealth` đã có HP/event và runtime health bar; cần kiểm tra lại vị trí, kích thước và khả năng cập nhật trong Scene thật.
- `EnemyController` đã có projectile attack; cần kiểm tra khả năng nhìn thấy đạn, tần suất bắn và damage thực tế lên player.
- `DefenseController` đã có Shield và EMP; cần hoàn thiện feedback và kiểm thử trong gameplay.
- `InteractionObject` hiện xử lý X/Y/Z nhưng vị trí còn cố định và vòng đời chưa theo chu kỳ 5 giây.
- `DemoSceneBuilder` đã tắt các panel HUD lớn `Top bar` và `Bottom bar`; cần kiểm tra, dọn object cũ trong Scene và giữ lại HUD nhỏ gọn.

## 3. Task 1 — Health bar cho player và Core

### Player

Tạo component dùng chung, đề xuất tên `WorldHealthBar`, đặt phía trên xe tăng.

Yêu cầu:

- Hiển thị HP hiện tại/tối đa.
- Thanh nền tối, thanh fill màu xanh.
- Đổi sang vàng khi HP dưới 50%.
- Đổi sang đỏ khi HP dưới 25%.
- Cập nhật qua `PlayerStats.Changed`.
- Khi HP bằng 0, thanh hiển thị rỗng hoặc tắt.

Thông tin HUD player:

```text
PLAYER HP 100/100
ARMOR 50/50
COINS 0
```

### Core

Kiểm tra và chuẩn hóa health bar hiện có trong `CoreHealth`:

- Đặt cố định phía trên Core.
- Không chồng lên vòng Forbidden Zone.
- Chiều dài lớn hơn health bar player.
- Fill giảm ngay khi Core nhận damage.
- Màu chuyển từ cyan sang vàng/đỏ.

File dự kiến:

- `Assets/_Game/Scripts/WorldHealthBar.cs`
- `Assets/_Game/Scripts/PlayerStats.cs`
- `Assets/_Game/Scripts/CoreHealth.cs`
- `Assets/_Game/Editor/DemoSceneBuilder.cs`
- `Assets/_Game/Scripts/HudPresenter.cs`

## 4. Task 2 — Enemy bắn player

Hoàn thiện và kiểm tra `EnemyController.TryShoot`.

Thông số đề xuất:

- Attack range: khoảng 7 units.
- Attack interval: khoảng 1.5 giây.
- Enemy projectile damage: 10.
- Enemy projectile speed: 7–8 units/giây.
- Projectile màu đỏ/cam, đủ lớn để dễ nhìn.
- Projectile bay tới vị trí player tại thời điểm bắn, không tự bám theo player.
- Projectile không tự va chạm với enemy tạo ra nó.
- Damage đi qua Armor trước, sau đó mới trừ HP.

Feedback cần thêm:

- Flash nhỏ ở enemy khi bắn.
- Âm thanh bắn enemy.
- Có thể thêm indicator/crosshair đỏ trong thời gian ngắn.

Acceptance:

- Enemy trong tầm sẽ bắn player.
- Player đứng yên sẽ mất Armor/HP.
- Player di chuyển bằng WASD có thể né đạn.
- Shield chặn được projectile.

File liên quan:

- `EnemyController.cs`
- `Projectile.cs`
- `PlayerStats.cs`
- `DefenseController.cs`
- `CombatVfxPresenter.cs`
- `AudioService.cs`

## 5. Task 3 — Chu kỳ xuất hiện ngẫu nhiên cho X/Y/Z

Tạo component quản lý vòng đời, đề xuất tên `InteractionCycleController`.

Vòng đời chuẩn:

```text
Xuất hiện ở vị trí ngẫu nhiên
        ↓ 5 giây
Biến mất
        ↓ 5 giây
Xuất hiện lại ở vị trí ngẫu nhiên khác
```

Nếu player chạm object trước khi hết 5 giây:

1. Kích hoạt hiệu ứng.
2. Object biến mất ngay.
3. Chờ 5 giây.
4. Respawn ở vị trí mới.

Vị trí random phải:

- Nằm trong arena.
- Không nằm trong tường.
- Không quá gần Core.
- Không trùng với X/Y/Z khác.
- Không xuất hiện ngay dưới player.
- Không nằm ngoài vùng camera.

Vùng gợi ý:

```text
X: -6.5 đến 6.5
Y: -3.0 đến 3.0
```

Nên tách random position thành hàm riêng và cho phép dùng seed trong test để tránh test không ổn định.

File liên quan:

- `InteractionObject.cs`
- `InteractionCycleController.cs` — file mới
- `GameSession.cs`
- `DemoSceneBuilder.cs`

## 6. Task 4 — Sáu hiệu ứng X/Y/Z

### X — Damage Trap

- Giảm 20 HP.
- Giảm 10 Armor.
- Phát hiệu ứng nổ.
- Phát âm thanh explosion/damage.
- Hiện floating text `-20 HP / -10 ARMOR`.

### Y — EMP Field

- Giảm tốc độ player còn 50% trong 3 giây.
- Phá Shield hiện tại.
- Hiện hiệu ứng EMP.
- Hiện floating text `SLOWED / SHIELD BROKEN`.

### Z — Supply Boost

- Cộng 10 Coins.
- Tăng tốc độ player 1.5 lần trong 4 giây.
- Hiện hiệu ứng boost.
- Hiện floating text `+10 COINS / SPEED BOOST`.

Sáu hiệu ứng gameplay tối thiểu là:

1. Giảm HP.
2. Giảm Armor.
3. Giảm tốc độ.
4. Phá Shield.
5. Tăng Coins.
6. Tăng tốc độ.

Mỗi object nên có thêm animation ngắn khi được kích hoạt: scale pulse, flash màu và particle tương ứng.

## 7. Task 5 — Cơ chế phòng thủ

### Shield — phím Q

- Thời gian hoạt động: 3 giây.
- Chặn tối đa 3 projectile enemy.
- Cooldown: 8 giây.
- Hiển thị vòng khiên quanh xe tăng.
- Hiển thị `SHIELD 3/3` trên HUD.
- Khi hết lượt chặn hoặc hết thời gian, Shield tắt.

### EMP — phím E

- Bán kính: 3 units.
- Làm enemy dừng di chuyển/bắn trong 2 giây.
- Cooldown: 6 giây.
- Hiển thị vùng EMP và số enemy bị ảnh hưởng.

Kiểm tra thêm:

- Shield không cho projectile trừ HP/Armor.
- EMP chỉ tác động enemy thuộc cùng `GameSession`.
- Cooldown hoạt động đúng khi pause, retry và reset run.

File liên quan:

- `DefenseController.cs`
- `StatusEffects.cs`
- `Projectile.cs`
- `HudPresenter.cs`
- `CombatVfxPresenter.cs`

## 8. Task 6 — Chỉnh sửa giao diện

Không sử dụng panel lớn ở biên trên và biên dưới.

HUD đề xuất:

### Góc trái trên

```text
PLAYER HP 100/100
ARMOR 50/50
COINS 0
```

### Góc phải trên

```text
CORE 100/100
TIME 90.0
```

### Khu vực nhỏ phía dưới hoặc cạnh player

```text
WEAPON: BULLET
SHIELD: READY
EMP: 4.2s
```

Nguyên tắc UI:

- Không che Core, player, enemy hoặc projectile.
- Dùng HUD/Sci-Fi FUI nhất quán.
- Chữ trắng/cyan trên nền tối, bảo đảm tương phản rõ.
- Đỏ chỉ dùng cho damage và warning.
- Nút có trạng thái normal, hover, pressed và disabled.
- Test ở 1280×720 và 1920×1080.
- Không dùng emoji làm icon.

Bảng màu gợi ý:

```text
Background: #0B0B10
Panel:      #1E1E23
Text:       #F8FAFC
Accent:     #3B82F6
Cyan:       #25E6D5
Warning:    #F59E0B
Danger:     #EF4444
```

## 9. Task 7 — Asset/resource

Ưu tiên sử dụng cùng hệ Kenney để tránh trộn phong cách.

Nguồn đề xuất:

- [Top-down Tanks Remastered](https://kenney.nl/assets/top-down-tanks-remastered) — tank, projectile và tile.
- [Particle Pack](https://kenney.nl/assets/particle-pack) — muzzle flash, explosion và particle.
- [Smoke Particles](https://kenney.nl/assets/smoke-particles) — smoke và collision effect.
- [UI Pack - Sci-Fi](https://kenney.nl/assets/ui-pack-sci-fi) — panel, button và HUD.
- [Crosshair Pack](https://kenney.nl/assets/crosshair-pack) — crosshair và aim indicator.
- [Sci-fi Sounds](https://kenney.nl/assets/sci-fi-sounds) — laser, engine và warning.
- [Impact Sounds](https://kenney.nl/assets/impact-sounds) — impact, damage và explosion.

Các asset Kenney có giấy phép CC0 theo trang nguồn. Khi copy asset vào project phải giữ `License.txt` tương ứng.

Cấu trúc thư mục:

```text
Assets/_Game/Art/Kenney/Tanks
Assets/_Game/Art/Kenney/Particles
Assets/_Game/Art/Kenney/UI
Assets/_Game/Art/Kenney/Icons
Assets/_Game/Audio/Kenney
```

Sau khi chọn asset:

1. Cập nhật `docs/asset-register.md`.
2. Ghi rõ file nào dùng cho player, enemy, Core, X/Y/Z và HUD.
3. Cập nhật `DemoSceneBuilder` để cấu hình lặp lại không tạo object trùng.
4. Kiểm tra texture type, pixels per unit, filter mode và sorting order.

## 10. Test cần bổ sung

### Health bar

- Player HP giảm thì player bar giảm.
- Core HP giảm thì Core bar giảm.
- HP bằng 0 thì bar rỗng/tắt.
- Reset run đưa cả hai bar về giá trị đầy đủ.

### Interaction cycle

- X/Y/Z spawn trong vùng hợp lệ.
- Object tự ẩn sau 5 giây.
- Object respawn sau thêm 5 giây.
- Vị trí mới không trùng Core/object khác.
- Collision chỉ kích hoạt một lần trong một chu kỳ.

### Sáu hiệu ứng

- X trừ đúng HP và Armor.
- Y làm chậm player.
- Y phá Shield.
- Z cộng Coins.
- Z tăng tốc player.
- VFX/audio/floating text xuất hiện đúng loại.

### Enemy và defense

- Enemy tạo projectile khi player trong tầm.
- Projectile gây damage qua Armor/HP.
- Player có thể né projectile.
- Shield chặn projectile.
- EMP làm enemy dừng trong đúng thời gian.

### UI

- Không còn panel lớn ở biên trên/dưới.
- HUD không che arena.
- Đọc được ở 1280×720 và 1920×1080.
- Cooldown và trạng thái Q/E/1/2/3 hiển thị đúng.

## 11. Trình tự thực hiện

1. Tạo/chuẩn hóa `WorldHealthBar` cho player và Core.
2. Hoàn thiện enemy projectile và kiểm tra damage player.
3. Hoàn thiện feedback Shield và EMP.
4. Tạo `InteractionCycleController`.
5. Đổi X/Y/Z sang random position và chu kỳ 5/5 giây.
6. Thêm đủ sáu hiệu ứng, VFX, audio và floating text.
7. Dọn HUD, xóa/tắt panel biên trên/dưới.
8. Chọn và tích hợp asset mới.
9. Cập nhật `DemoSceneBuilder` theo hướng idempotent.
10. Chạy EditMode và PlayMode tests.
11. Chạy smoke test thủ công toàn bộ gameplay.
12. Cập nhật `docs/progress.md`, `docs/acceptance.md` và `docs/asset-register.md`.

## 12. Điều kiện hoàn thành

Task được xem là hoàn thành khi:

- Player và Core đều có health bar cập nhật theo damage.
- Enemy bắn player đủ rõ để người chơi phải né.
- X/Y/Z xuất hiện ngẫu nhiên, tồn tại 5 giây, ẩn 5 giây rồi xuất hiện lại.
- Có ít nhất sáu hiệu ứng gameplay khác nhau.
- Shield và EMP hoạt động, có cooldown và feedback.
- Không có panel HUD lớn che khu vực chơi.
- Asset và license được ghi nhận trong asset register.
- Các test chính chạy pass trên Unity 6000.3.23f1.
