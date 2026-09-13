# CORE GUARD — KẾ HOẠCH PHÂN CHIA NHÓM VÀ HƯỚNG DẪN CHI TIẾT
> **Dành cho:** Duy, An, Tuấn Anh, Huy  
> **Dự án:** Game 2D "Core Guard" (Bảo vệ Lõi) — Unity `6000.3.23f1` / Windows x86_64  
> **Thời hạn báo cáo:** Thứ Năm, ngày 17/09/2026  
> **Tài liệu tham chiếu gốc:** `01-Game-2D-Implementation-Plan.md` và `02-Tools-Assets-Agent-Guide.md`

---

## 1. TỔNG QUAN PHÂN VAI & CỤM TASK

| Thành viên | Vai trò phụ trách | Cụm Task được giao | Nhánh Git riêng (Branch) |
|---|---|---|---|
| **Duy** | **Combat & Defense Lead** (Chiến đấu & Phòng thủ) | **Task 2 + Task 3** | `feature/combat-defense` |
| **An** | **Mechanics & World Dev** (Môi trường & Hiệu ứng) | **Task 4** | `feature/xyz-interactions` |
| **Tuấn Anh** | **Audio Specialist & Asset Lead 1** (Âm thanh) | **Task 5 + Asset Audio** | `feature/audio-system` |
| **Huy** | **Art, UI & Integrator** (Mỹ thuật, Demo F1 & Tích hợp) | **Task 6 + Asset Art + T7/T8** | `feature/art-ui-demo` |

> [!TIP]
> **Quy tắc Greyboxing (Code ngay không cần chờ asset):**
> Duy và An bắt đầu code và test ngay lập tức bằng các hình khối cơ bản (Square, Circle, Capsule) có sẵn của Unity. Tuấn Anh và Huy tìm asset song song, sau này chỉ mất 30 giây để thay ảnh/tiếng vào Prefab.

---

## 2. NHIỆM VỤ CHI TIẾT TỪNG THÀNH VIÊN

---

### 👤 1. DUY — COMBAT & DEFENSE LEAD (Task 2 + Task 3)

* **Mục tiêu:** Xây dựng toàn bộ hệ thống tấn công của người chơi, cơ chế quái bắn đạn, và 2 kỹ năng phòng thủ (Khiên Q, EMP E).
* **Nhánh Git:** `git checkout -b feature/combat-defense`
* **Hình khối tạm để code ngay:** 
  * Đạn (Bullet): Hình tròn nhỏ màu vàng.
  * Tên lửa (Rocket): Hình con nhộng/chữ nhật màu đỏ.
  * Mìn (Mine): Hình tròn xám có viền.
  * Đạn quái: Hình tròn đỏ bay về phía A.

#### Chi tiết công việc:
1. **Task 2: 3 Vũ khí & Quái bắn đạn**
   * Tạo `IDamageable.cs` (hàm `ApplyDamage(float amount)`).
   * Tạo `WeaponController.cs`, `Projectile.cs`, `Mine.cs`, `Explosion.cs`.
   * **Bullet (phím 1):** Tốc độ 14, dmg 10, cooldown 0.2s, TTL 2s. Giữ chuột trái để bắn liên tục.
   * **Rocket (phím 2):** Tốc độ 7, dmg 35 trong bán kính nổ 1.5, cooldown 1.5s, TTL 3s. Click 1 lần mỗi viên. Đảm bảo mục tiêu trực tiếp không nhận sát thương 2 lần (direct hit + AoE).
   * **Mine (phím 3):** Đặt tại chân A, đếm 0.5s mới arm. Quái chạm thì nổ gây 50 dmg (bán kính 1.8), cooldown 2.0s, TTL 12s. Tối đa 3 mìn trên sân (đầy 3 thì từ chối, không mất cooldown).
   * **Quái B bắn:** Khi người chơi A ở trong bán kính 6 units, bắn 1 viên đạn (speed 5, dmg 10) về phía A mỗi 2 giây. Đạn quái không gây hại cho Lõi.
   * Đổi vũ khí 1/2/3 không reset cooldown; click trên UI không làm bắn đạn.

2. **Task 3: Hai cơ chế phòng thủ (Nối tiếp T2)**
   * Tạo `DefenseController.cs` gắn vào người chơi A.
   * **Khiên — Shield (phím Q):** Vòng khiên bọc quanh A, chặn và hủy tối đa 3 viên đạn địch hoặc kéo dài 3 giây, cooldown 8s. Lần trúng thứ 4 sẽ xuyên qua gây sát thương bình thường.
   * **Xung điện — EMP (phím E):** Tạo xung điện bán kính 3 quanh A. Quái địch trong bán kính bị choáng (Stun) trong 2s: dừng di chuyển và ngừng bắn. Hết 2s quái tiếp tục, không bắn dồn. Cooldown 6s.

#### Prompt mẫu Duy gửi cho AI Agent của mình:
```text
Bạn là AI lập trình C# Unity 6000.3.23f1 trên branch feature/combat-defense.
Hãy đọc 01-Game-2D-Implementation-Plan.md và triển khai toàn bộ Task 2 (3 vũ khí Bullet/Rocket/Mine, quái bắn đạn) và Task 3 (Khiên Q chặn 3 đạn, EMP E choáng 2s).
Tạo các Prefab dùng tạm Sprite hình học 2D cơ bản. Tuân thủ TDD: viết test EditMode/PlayMode chứng minh fail trước, sau đó hoàn thiện logic. Không sửa Main.unity.
```

---

### 👤 2. AN — MECHANICS & WORLD SPECIALIST (Task 4)

* **Mục tiêu:** Xây dựng 3 vật phẩm môi trường X, Y, Z và lập trình 6 hiệu ứng gameplay (E1 – E6).
* **Nhánh Git:** `git checkout -b feature/xyz-interactions`
* **Hình khối tạm để code ngay:**
  * Vật thể X: Ô vuông màu đỏ, gắn chữ "X".
  * Vật thể Y: Vòng tròn màu tím, gắn chữ "Y".
  * Vật thể Z: Hình thoi màu vàng/xanh, gắn chữ "Z".

#### Chi tiết công việc:
1. **Tạo mã nguồn:**
   * Tạo `Assets/_Game/Scripts/World/InteractionObject.cs`
   * Tạo `Assets/_Game/Scripts/Player/StatusEffects.cs`
2. **Lập trình 6 Hiệu ứng (E1 – E6):**
   * **Vật thể X (Mìn nguy hiểm):** Pickup một lần rồi biến mất.
     * **E1:** Giảm 20 HP trực tiếp (bỏ qua giáp).
     * **E2:** Giảm 10 Armor độc lập với E1.
   * **Vật thể Y (Bẫy điện từ):** Trigger vùng tồn tại (ra rồi vào lại mới kích hoạt tiếp).
     * **E3:** Giảm 50% tốc độ di chuyển của A trong 3s từ lúc bước vào.
     * **E4:** Hủy Khiên hiện có của A (gọi `BreakShield()`), giữ nguyên cooldown Q.
   * **Vật thể Z (Pin tiếp tế):** Pickup một lần rồi biến mất.
     * **E5:** Cộng 10 Coins (`PlayerStats.AddCoins(10)`).
     * **E6:** Tăng 150% tốc độ di chuyển trong 4s.
3. **Logic tốc độ chuẩn xác:**
   * Tốc độ thực tế = `4.0 * (slow ? 0.5 : 1.0) * (boost ? 1.5 : 1.0)`. Nếu cùng dính cả Slow và Boost thì tốc độ = `3.0 units/s`.
   * Nhặt lại cùng loại sẽ làm mới (refresh) bộ đếm giờ, không cộng dồn tốc độ vô hạn.
4. **Đóng gói Prefab:** Tạo `X_Hazard.prefab`, `Y_Trap.prefab`, `Z_Supply.prefab` trong `Assets/_Game/Prefabs/`.

#### Prompt mẫu An gửi cho AI Agent của mình:
```text
Bạn là AI lập trình C# Unity 6000.3.23f1 trên branch feature/xyz-interactions.
Hãy đọc 01-Game-2D-Implementation-Plan.md phần mục 5 và Task 4. Triển khai InteractionObject.cs, StatusEffects.cs và 3 Prefab X/Y/Z với 6 hiệu ứng E1-E6.
Viết Unit Test kiểm tra tính toán tốc độ độc lập và va chạm một lần. Dùng Sprite khối màu tạm có Text phân biệt. Không sửa Main.unity.
```

---

### 👤 3. TUẤN ANH — AUDIO SPECIALIST & ASSET LEAD 1 (Task 5 & Asset Audio)

* **Mục tiêu:** Phụ trách toàn bộ âm thanh dự án (SFX, BGM), 4 nút bấm On/Off độc lập và cảnh báo 4 tiếng beep khi quái vào vùng cấm.
* **Nhánh Git:** `git checkout -b feature/audio-system`

#### Chi tiết công việc:
1. **Tìm và đăng ký Asset Audio (Làm trước):**
   * Tải từ [Kenney Sci-Fi Sounds](https://kenney.nl/assets/sci-fi-sounds), [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds), [Tallbeard Music](https://tallbeard.itch.io/music-loop-bundle) (tất cả là CC0):
     * `sfx_bullet.wav`: tiếng bắn đạn cắc bụp ngắn.
     * `sfx_rocket.wav`: tiếng phóng tên lửa vút.
     * `sfx_mine_drop.wav`: tiếng đặt mìn cạch kim loại.
     * `sfx_alert_beep.wav`: tiếng beep điện tử ngắn **0.15s – 0.2s** (bắt buộc ngắn để phát chuỗi 4 tiếng không dính âm).
     * `bgm_loop.wav`: 1 bản nhạc nền điện tử/chiptune lặp êm dịu.
   * Lưu vào `Assets/_Game/Audio/SFX/` và `Assets/_Game/Audio/Music/`.
   * Mở `docs/asset-register.md` điền thông tin nguồn, tác giả, license CC0.
2. **Lập trình Task 5:**
   * Tạo `AudioService.cs`: Quản lý 3 `AudioSource` riêng biệt (MusicSource loop, SfxSource one-shot, AlertSource phát chuỗi).
   * Tạo `AudioToggleView.cs` quản lý **4 GameObject Button riêng biệt**:
     * `SoundOff` (hiện khi SFX ON, bấm vào -> tắt SFX, hiện `SoundOn`).
     * `SoundOn` (hiện khi SFX OFF, bấm vào -> bật SFX, hiện `SoundOff`).
     * `MusicOn` (hiện khi BGM OFF, bấm vào -> bật BGM, hiện `MusicOff`).
     * `MusicOff` (hiện khi BGM ON, bấm vào -> tắt BGM, hiện `MusicOn`).
     * Hai nút trong mỗi cặp phải trùng khớp vị trí, kích thước RectTransform.
   * Tạo `ForbiddenZone.cs`: Vòng tròn bán kính 3 quanh Lõi. Khi quái B bước vào:
     * Kích hoạt chuỗi đúng 4 tiếng beep tại các thời điểm `t = 0s, 0.45s, 0.90s, 1.35s`.
     * Quái đứng yên trong vùng không phát thêm; 2 quái vào gần nhau thì phát nối tiếp (FIFO), không đè âm.
     * Tắt SFX thì hủy toàn bộ hàng đợi beep.

#### Prompt mẫu Tuấn Anh gửi cho AI Agent của mình:
```text
Bạn là AI lập trình C# Unity 6000.3.23f1 trên branch feature/audio-system.
Hãy đọc mục 6 và Task 5 trong 01-Game-2D-Implementation-Plan.md. Triển khai AudioService.cs, AudioToggleView.cs với 4 nút On/Off độc lập, và ForbiddenZone.cs phát chuỗi 4-beep bằng hàng đợi FIFO.
Sử dụng các file WAV đã có trong Assets/_Game/Audio/. Viết test PlayMode/EditMode chứng minh logic hàng đợi và trạng thái nút.
```

---

### 👤 4. HUY — ART, UI & INTEGRATOR (Asset Art, Task 6 & Điều phối T7/T8)

* **Mục tiêu:** Tìm và chuẩn bị toàn bộ Sprite đồ họa, hoàn thiện giao diện HUD Cooldown, dựng bảng F1 Demo Director và chủ trì ráp nối game.
* **Nhánh Git:** `feature/art-ui-demo`

#### Chi tiết công việc:
1. **Tìm và phân phối Asset Đồ họa (Làm trong 24h đầu):**
   * Tải bộ [Kenney Top-Down Tanks Redux](https://kenney.nl/assets/top-down-tanks-redux) (CC0):
     * Xe A: `player_body.png` (xanh cyan), `player_turret.png`.
     * Xe B: `enemy_body.png` (đỏ cam), `enemy_turret.png`.
     * Đạn: `bullet.png`, `rocket.png`, `player_mine.png`.
     * Vật thể: `hazard_mine.png` (X), `emp_trap.png` (Y), `supply_cell.png` (Z), `core.png` (Lõi).
   * Lưu vào `Assets/_Game/Art/Sprites/`, điền `docs/asset-register.md` và push lên Git sớm.
2. **Lập trình Task 6: HUD Cooldown & F1 Demo Director:**
   * Nâng cấp `HudPresenter.cs`: Thêm 3 ô vũ khí (1/2/3) và 2 ô kỹ năng (Q/E) hiển thị số giây Cooldown còn lại hoặc chữ `READY`.
   * Tạo `DemoDirector.cs` và Panel phím **F1 (Demo Controls)**:
     * Nút `Spawn Zone Enemy`: Gọi 1 quái ngoài vòng cấm đi vào (test 4 tiếng beep).
     * Nút `Spawn Shooter`: Gọi 1 quái đứng bắn đạn vào A (test Khiên Q).
     * Nút `Spawn Cluster`: Gọi 3 quái gần nhau (test Rocket nổ lan và EMP E choáng).
     * Nút `Reset Scenario`: Xóa đạn, xóa quái, reset máu/cooldown để biểu diễn lại mà không cần tải lại game.
3. **Chủ trì Tích hợp (T7 & T8 - ngày 16 & 17/09):**
   * Review và merge các PR của Duy, An, Tuấn Anh vào `main`.
   * Chạy `DemoSceneBuilder.cs` để nối các Prefab hoàn thiện vào `Main.unity`.
   * Cùng cả nhóm kiểm thử 9 yêu cầu R01–R09 ghi vào `docs/acceptance.md`.
   * Build Windows xuất file `.exe`, nén `.zip`, quay video màn hình 3–5 phút dự phòng và tập kịch bản demo 5 phút theo Mục 10.

#### Prompt mẫu Huy gửi cho AI Agent của mình:
```text
Bạn là AI lập trình C# Unity 6000.3.23f1 trên branch feature/art-ui-demo.
Hãy đọc Task 6 trong 01-Game-2D-Implementation-Plan.md. Nâng cấp HudPresenter.cs hiển thị cooldown vũ khí và Q/E. Xây dựng DemoDirector.cs cùng UI panel mở bằng phím F1 có các nút tạo tình huống demo (Shooter, Zone Enemy, Cluster).
Đảm bảo Canvas co giãn chuẩn ở cả 1280x720 và 1920x1080.
```

---

## 3. QUY TRÌNH PHỐI HỢP GIT & UNITY (CHỐNG CONFLICT)

Để không bao giờ bị vỡ scene hay lỗi liên kết, cả 4 thành viên phải tuân thủ nghiêm 4 điều luật:

1. **Tuyệt đối không sửa trực tiếp file `Main.unity` trên nhánh cá nhân:**
   * Mọi đối tượng tạo mới phải lưu thành file **Prefab** trong `Assets/_Game/Prefabs/`.
   * Chỉ có Huy (Integrator) mới là người kéo các Prefab này vào `Main.unity` khi merge vào `main`.
2. **Luôn `git add` kèm file `.meta`:**
   * Khi thêm `Script.cs` hay `Image.png`, Unity luôn sinh ra `Script.cs.meta` và `Image.png.meta`. Bắt buộc phải commit cả hai.
3. **Không commit file rác:**
   * Không bao giờ add `Library/`, `Logs/`, `Builds/`, `Artifacts/`.
   * Nếu thấy 2 file `UniversalRP.asset` và `ShaderGraphSettings.asset` bị thay đổi, gõ:  
     `git checkout -- Assets/Settings/UniversalRP.asset ProjectSettings/ShaderGraphSettings.asset` để hủy.
4. **Thứ tự Merge Pull Request vào `main`:**
   1. Merge nhánh Asset của Tuấn Anh & Huy (để có sẵn ảnh và âm thanh).
   2. Merge nhánh `feature/xyz-interactions` của An (T4).
   3. Merge nhánh `feature/audio-system` của Tuấn Anh (T5).
   4. Merge nhánh `feature/combat-defense` của Duy (T2 + T3).
   5. Huy tích hợp cuối cùng trên nhánh `feature/art-ui-demo` (T6), chạy nghiệm thu và build.

---

## 4. LỊCH TRÌNH THEO TỪNG GIỜ (13/09 – 17/09)

| Mốc thời gian | Duy (Combat/Defense) | An (Môi trường X/Y/Z) | Tuấn Anh (Audio) | Huy (Art / UI / Demo) |
|---|---|---|---|---|
| **Tối 13/09** | Nhận nhánh, bắt đầu T2 (Bullet/Rocket/Mine) | Nhận nhánh, bắt đầu T4 (Code hiệu ứng E1–E6) | Tìm và tải bộ 5 file Audio CC0 | Đẩy code hiện tại lên GitHub, tải bộ Sprite Kenney |
| **Sáng 14/09** | Xong T2, chuyển sang T3 (Khiên Q, EMP E) | Hoàn thiện 3 Prefab X/Y/Z, test tốc độ | Code AudioService và 4 nút toggle | Push bộ Sprite lên Git; bắt đầu dựng UI HUD Cooldown |
| **Tối 14/09** | Hoàn thành T3, test Khiên chặn đạn | **Merge PR T4 vào main** | Hoàn thiện vòng cấm 4 beep; **Merge PR T5** | Hoàn thiện HUD Cooldowns, bắt đầu làm F1 Demo Panel |
| **Ngày 15/09** | **Merge PR T2+T3 vào main** | Hỗ trợ Huy test gameplay | Kiểm tra âm lượng BGM/SFX trên loa | Ráp Prefab vào Scene; hoàn thiện F1 Demo Director (T6) |
| **Ngày 16/09** | Cùng test phím vật lý trên `.exe` | Cùng test phím vật lý trên `.exe` | Nghe thử 4 tiếng beep trên `.exe` | **Đóng băng tính năng lúc 20:00 (T7)**; ghi nhận `acceptance.md` |
| **Sáng 17/09** | Diễn tập demo 5 phút | Diễn tập demo 5 phút | Diễn tập demo 5 phút | **Build Windows cuối (T8)**, nén ZIP, quay video 3-5 phút |

---

## 5. ENGLISH PROMPTS FOR AI AGENTS (PROMPT TIẾNG ANH DÀNH CHO AI AGENT)

Copy the prompt corresponding to your assigned role and paste it directly into your AI coding agent (Codex, Claude, Cursor, Copilot, ChatGPT).

---

### 📋 PROMPT FOR DUY (COMBAT & DEFENSE LEAD — TASK 2 & TASK 3)

```markdown
You are an expert Unity C# developer pair-programming on the "Core Guard" 2D top-down game project.
Your assigned Git branch is: `feature/combat-defense`
Your task is to implement **Task 2 (3 Player Weapons & Enemy Projectiles)** and **Task 3 (Shield & EMP Defenses)** strictly following `01-Game-2D-Implementation-Plan.md`.

### 1. ENVIRONMENT & CONSTRAINTS:
- Unity: `6000.3.23f1`, C#, 2D Physics, Input System. Target: Windows x86_64, 16:9 offline.
- TDD is strictly required: Write failing EditMode/PlayMode tests first (RED), then implement minimal code to pass (GREEN).
- Do NOT modify `Assets/_Game/Scenes/Main.unity` directly. Package all combat objects as prefabs in `Assets/_Game/Prefabs/`.
- Use primitive 2D geometric shapes (colored Circle, Square, Capsule sprites) as temporary greybox placeholders. Assets will be wired later.
- Never commit `Library/`, `Temp/`, `Logs/`, `Builds/`, or incidental URP/ShaderGraph schema files.

### 2. SPECIFICATIONS TO IMPLEMENT:
#### Part A: Task 2 — Combat System
1. Interface & Damage Contract:
   - Create `Assets/_Game/Scripts/Combat/IDamageable.cs` with `void ApplyDamage(float amount)`.
   - Ensure damage follows the Armor-first rule: armor absorbs damage first; excess damage is subtracted from HP.
2. Three Player Attacks:
   - **Bullet (Key 1 / Left Click Hold):** Direct projectile, speed 14, damage 10, cooldown 0.2s, TTL 2.0s. Spawns at player muzzle in aiming direction. Single target.
   - **Rocket (Key 2 / Left Click Once):** Direct projectile, speed 7, damage 35 in AoE radius 1.5, cooldown 1.5s, TTL 3.0s. Explodes on impact with enemy/wall or TTL expiry. Must use hit-guard deduplication so direct-hit targets do not take damage twice.
   - **Mine (Key 3 / Left Click Once):** Dropped at player position. Arms after 0.5s delay. Detonates when enemy touches, dealing 50 damage in AoE radius 1.8. Cooldown 2.0s, TTL 12.0s. Max 3 active mines on field. If 3 already exist, reject placement, display "Mine limit", do not consume cooldown.
   - Weapon switching (Keys 1/2/3) must NOT reset ongoing cooldowns. Clicking on UI elements must suppress firing.
3. Enemy Fire:
   - When player A is within 6.0 units of enemy B, B shoots a projectile at A every 2.0s (speed 5, damage 10).
   - Enemy projectiles only damage player A, NOT the Core.

#### Part B: Task 3 — Defense Mechanisms
1. **Shield (Key Q):**
   - Spawns a protective barrier attached to player A.
   - Intercepts and destroys incoming enemy projectiles before damage is resolved.
   - Durability: absorbs up to 3 projectile hits or expires after 3.0 seconds. The 4th projectile deals normal damage.
   - Cooldown: 8.0s starting from activation.
2. **EMP Blast (Key E):**
   - Triggers an instant shockwave in radius 3.0 centered on player A.
   - Applies `Stun(2.0f)` to all enemy root IDs within radius: freezes movement and shooting for 2 seconds.
   - Enemies outside radius are unaffected. Does NOT clear already flying projectiles. Cooldown 6.0s.
3. Centralized Reset & Pause:
   - Pause (Esc) freezes cooldowns, projectile velocities, and mine arming.
   - Session Retry clears all active projectiles and mines.

### 3. ACTIONS TO TAKE:
1. Verify git branch is `feature/combat-defense`.
2. Write unit tests in `Assets/_Game/Tests/EditMode/` and `PlayMode/` covering:
   - Armor-first damage calculation.
   - Explosion AoE deduplication on multi-collider enemies.
   - Mine arming delay and 3-mine limit enforcement.
   - Shield 3-hit absorption and EMP 2s stun radius.
3. Implement `IDamageable.cs`, `WeaponController.cs`, `Projectile.cs`, `Mine.cs`, `Explosion.cs`, `DefenseController.cs`, and update `EnemyController.cs`.
4. Run all test suites to confirm GREEN. Update `docs/progress.md` and commit with:
   `feat: implement three distinct weapons, enemy fire, shield, and EMP`
```

---

### 📋 PROMPT FOR AN (MECHANICS & ENVIRONMENT SPECIALIST — TASK 4)

```markdown
You are an expert Unity C# developer pair-programming on the "Core Guard" 2D top-down game project.
Your assigned Git branch is: `feature/xyz-interactions`
Your task is to implement **Task 4 (Interactive Objects X, Y, Z and 6 Gameplay Effects E1–E6)** strictly following `01-Game-2D-Implementation-Plan.md`.

### 1. ENVIRONMENT & CONSTRAINTS:
- Unity: `6000.3.23f1`, C#, 2D Physics, Input System. Target: Windows x86_64, 16:9 offline.
- TDD is strictly required: Write failing tests first (RED), then implement minimal code to pass (GREEN).
- Do NOT edit `Assets/_Game/Scenes/Main.unity` directly. Package objects as prefabs in `Assets/_Game/Prefabs/`.
- Use primitive 2D geometric shapes with clear color tints and label text as greybox placeholders (e.g. Red square for X, Purple circle for Y, Yellow diamond for Z).
- Never commit `Library/`, `Temp/`, `Logs/`, `Builds/`, or incidental URP/ShaderGraph schema files.

### 2. SPECIFICATIONS TO IMPLEMENT:
#### Six Gameplay Effects Across Objects X, Y, Z:
1. **Object X — Hazard Mine (One-time consumable pickup):**
   - Explodes on player contact, deals effects, and disappears immediately (single-trigger guard).
   - **E1:** Decreases Player HP by 20 directly, bypassing Armor.
   - **E2:** Decreases Player Armor by 10 independently of E1.
   - Trigger visual feedback (red flash) and floating indicator text.
2. **Object Y — Electromagnetic Trap (Persistent trigger zone):**
   - Applies effects once upon entry (`OnTriggerEnter2D`). Staying inside does NOT trigger every frame; exiting and re-entering triggers again.
   - **E3 (Slow):** Multiplies player movement speed by 0.5 for 3.0 seconds from entry.
   - **E4 (Break Shield):** Instantly breaks active player shield (calls `BreakShield()` on player DefenseController if present), keeping the Q cooldown active.
3. **Object Z — Supply Cell (One-time consumable pickup):**
   - Picked up on player contact and disappears immediately.
   - **E5:** Increases Coins by +10 (`PlayerStats.AddCoins(10)`).
   - **E6 (Boost):** Multiplies player movement speed by 1.5 for 4.0 seconds. Re-collecting Z refreshes duration, does NOT stack multiplier infinitely.

#### Speed Calculation Rules:
- Base speed = 4.0 units/s.
- `ActualSpeed = 4.0 * (isSlowed ? 0.5 : 1.0) * (isBoosted ? 1.5 : 1.0)`.
- When both Slow and Boost are active simultaneously: `ActualSpeed = 4.0 * 0.5 * 1.5 = 3.0 units/s`.
- When Slow expires while Boost is still active: `ActualSpeed = 6.0 units/s`.
- When both expire: returns to base speed `4.0 units/s`.
- Session Retry clears both timers and restores base speed.

### 3. ACTIONS TO TAKE:
1. Verify git branch is `feature/xyz-interactions`.
2. Write unit tests in `Assets/_Game/Tests/EditMode/` and `PlayMode/` covering:
   - Object X reduces both HP by 20 and Armor by 10.
   - Object Y slow + Z boost speed math (speed = 3.0 when combined, 6.0 with boost only).
   - Refreshing boost timer on second pickup without infinite speed multiplication.
   - Single-trigger consumption for X and Z.
3. Implement `Assets/_Game/Scripts/World/InteractionObject.cs` and `Assets/_Game/Scripts/Player/StatusEffects.cs`.
4. Create prefabs `HazardMine_X.prefab`, `EmpTrap_Y.prefab`, `SupplyCell_Z.prefab` in `Assets/_Game/Prefabs/`.
5. Run all tests to confirm GREEN. Update `docs/progress.md` and commit with:
   `feat: implement X Y Z interaction objects and six gameplay effects`
```

---

### 📋 PROMPT FOR TUẤN ANH (AUDIO SPECIALIST & ASSET LEAD 1 — TASK 5)

```markdown
You are an expert Unity C# and audio developer pair-programming on the "Core Guard" 2D top-down game project.
Your assigned Git branch is: `feature/audio-system`
Your task is to acquire **Audio Assets (CC0)** and implement **Task 5 (Audio Service, 4 Toggle Buttons, and 4-Beep Security Zone Alert)** strictly following `01-Game-2D-Implementation-Plan.md`.

### 1. ENVIRONMENT & CONSTRAINTS:
- Unity: `6000.3.23f1`, C#, 2D Audio, uGUI. Target: Windows x86_64, 16:9 offline.
- TDD required for audio state transitions, queueing, and occupancy counting.
- Do NOT modify `Assets/_Game/Scenes/Main.unity` directly. Package audio system and UI toggles as prefabs in `Assets/_Game/Prefabs/`.
- All acquired audio assets MUST be CC0 Public Domain. Register every file in `docs/asset-register.md`.

### 2. SPECIFICATIONS TO IMPLEMENT:
#### Part A: Audio Asset Acquisition
Download CC0 audio files from Kenney (Sci-Fi Sounds / Interface Sounds) or Tallbeard (Free Music Loop Bundle):
- `Assets/_Game/Audio/SFX/sfx_bullet.wav`: Short, crisp gun shot.
- `Assets/_Game/Audio/SFX/sfx_rocket.wav`: Rocket launch whoosh.
- `Assets/_Game/Audio/SFX/sfx_mine_drop.wav`: Mechanical click/thud.
- `Assets/_Game/Audio/SFX/sfx_alert_beep.wav`: Electronic beep, duration MUST be between 0.15s and 0.20s (crucial so 4 consecutive beeps at 0.45s intervals do not overlap).
- `Assets/_Game/Audio/Music/bgm_loop.wav`: Pleasant looping chiptune/electronic background music.

#### Part B: Task 5 — Audio Architecture & Controls
1. `AudioService.cs`:
   - Contains 3 distinct 2D AudioSources: `MusicSource` (looping), `SfxSource` (one-shot), `AlertSource` (sequential queue).
   - Independent controls: SFX toggle controls SfxSource AND AlertSource. BGM toggle controls MusicSource only. Do NOT use `AudioListener.volume = 0` as it mutes both.
   - Initial application state: SFX ON, BGM OFF.
2. Four Independent UI Toggle Buttons (`AudioToggleView.cs`):
   - Pair 1 (SFX): `SoundOff` ("Mute SFX") and `SoundOn` ("Enable SFX") occupying the EXACT SAME RectTransform position (56x56 size).
     - When SFX is ON: `SoundOff` button is active. Clicking it mutes SFX, hides `SoundOff`, and shows `SoundOn`.
     - When SFX is OFF: `SoundOn` button is active. Clicking it unmutes SFX, hides `SoundOn`, and shows `SoundOff`.
   - Pair 2 (BGM): `MusicOn` ("Play Music") and `MusicOff` ("Stop Music") occupying identical RectTransform position.
     - When BGM is OFF: `MusicOn` button is active. Clicking starts music loop, hides `MusicOn`, and shows `MusicOff`.
     - When BGM is ON: `MusicOff` button is active. Clicking stops music, hides `MusicOff`, and shows `MusicOn`.
3. Forbidden Zone 4-Beep Alert (`ForbiddenZone.cs`):
   - Trigger zone around the Core (CircleCollider2D, radius 3.0 units, reacts only to Enemy root ID).
   - When an enemy enters from outside: enqueues an alert job of EXACTLY 4 beeps at `t = 0.0s, 0.45s, 0.90s, 1.35s`.
   - Standing inside the zone does NOT re-trigger beeps.
   - Multiple enemies entering in quick succession create sequential FIFO jobs on `AlertSource` (no overlapping audio cacophony).
   - When SFX is OFF: discard alert jobs immediately, do NOT replay backlogged beeps when unmuted.
   - Game Pause freezes alert timers; Retry/Game Over cancels active alerts.

### 3. ACTIONS TO TAKE:
1. Verify git branch is `feature/audio-system`.
2. Add audio files to `Assets/_Game/Audio/` and register them in `docs/asset-register.md`.
3. Write unit tests in `Assets/_Game/Tests/EditMode/` covering button state alternation and FIFO alert scheduling.
4. Implement `AudioService.cs`, `AudioToggleView.cs`, and `ForbiddenZone.cs`.
5. Run tests to confirm GREEN. Update `docs/progress.md` and commit with:
   `feat: implement audio service, independent toggles, and four-beep zone alert`
```

---

### 📋 PROMPT FOR HUY (ART, UI & INTEGRATOR — TASK 6, T7, T8)

```markdown
You are an expert Unity C# and UI/UX developer pair-programming on the "Core Guard" 2D top-down game project.
Your assigned Git branch is: `feature/art-ui-demo`
Your task is to acquire **2D Sprite Assets (CC0)**, implement **Task 6 (HUD Cooldowns & F1 Demo Director)**, and coordinate final integration, acceptance, and packaging (Tasks 7 and 8) strictly following `01-Game-2D-Implementation-Plan.md`.

### 1. ENVIRONMENT & CONSTRAINTS:
- Unity: `6000.3.23f1`, C#, uGUI / TextMeshPro. Target: Windows x86_64, 16:9 offline.
- Canvas must scale with screen size (Reference resolution 1280x720, tested up to 1920x1080).
- All acquired sprite assets MUST be CC0 Public Domain (Kenney Top-Down Tanks Redux). Register every file in `docs/asset-register.md`.
- Never commit `Library/`, `Temp/`, `Logs/`, `Builds/`, or incidental URP/ShaderGraph schema files.

### 2. SPECIFICATIONS TO IMPLEMENT:
#### Part A: Sprite Asset Acquisition (Push early for the team)
Download CC0 sprites from Kenney Top-Down Tanks Redux into `Assets/_Game/Art/Sprites/`:
- `player_body.png` (Cyan tank chassis), `player_turret.png` (Cyan rotating turret).
- `enemy_body.png` (Red/orange tank chassis), `enemy_turret.png`.
- `bullet.png`, `rocket.png`, `player_mine.png`.
- `hazard_mine.png` (X), `emp_trap.png` (Y), `supply_cell.png` (Z), `core.png` (Core station), `floor.png`, `border.png`.
- Register all sprites in `docs/asset-register.md`.

#### Part B: Task 6 — HUD Polish & F1 Demo Director
1. Polish `HudPresenter.cs`:
   - Add weapon slots (1: Bullet, 2: Rocket, 3: Mine) displaying selected highlight and remaining cooldown seconds (or "READY").
   - Add defense slots (Q: Shield, E: EMP) displaying remaining cooldown seconds (or "READY").
   - Ensure clean visual layout at both 1280x720 and 1920x1080 without obscuring gameplay.
2. Build F1 Demo Director (`DemoDirector.cs` + uGUI Panel):
   - Pressing **F1** toggles the "DEMO CONTROLS" panel overlay.
   - When Demo panel is open: disable auto-wave spawning.
   - Button **"Spawn Zone Enemy"**: spawns 1 enemy outside radius 3.0 moving towards Core (tests 4-beep alert).
   - Button **"Spawn Shooter"**: spawns 1 enemy at distance 5.0 shooting at player A (tests Shield Q).
   - Button **"Spawn Cluster"**: spawns 3 enemies grouped together (tests Rocket AoE and EMP E stun).
   - Button **"Restore X/Y/Z"**: respawns items X, Y, Z if already consumed.
   - Button **"Reset Scenario"**: clears active bullets, enemies, and mines; resets player stats and cooldowns without reloading scene or altering sound toggles.
   - Important: Demo buttons MUST execute real gameplay combat/physics paths; do NOT forge raw text or fake values.

#### Part C: Tasks 7 & 8 — Integration, Acceptance, and Packaging
1. Pull and merge feature branches (`feature/xyz-interactions`, `feature/audio-system`, `feature/combat-defense`) into `main`.
2. Update `DemoSceneBuilder.cs` to wire all real prefabs and sprites into `Main.unity`.
3. Run complete test suites (`editmode.xml`, `playmode.xml`).
4. Execute `BuildDemo.BuildWindows()` to produce `Builds/Windows/CoreGuard/CoreGuard.exe`.
5. Run manual verification of requirements R01–R09 in the Windows build, record PASS in `docs/acceptance.md`, package `.zip`, and verify the 5-minute demo script.

### 3. ACTIONS TO TAKE:
1. Verify git branch is `feature/art-ui-demo`.
2. Add sprites to `Assets/_Game/Art/Sprites/` and register in `docs/asset-register.md`.
3. Implement `HudPresenter.cs` cooldown display and `DemoDirector.cs`.
4. Update `docs/progress.md` and commit with:
   `feat: polish HUD cooldowns and implement repeatable F1 demo scenarios`
```

