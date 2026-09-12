# Core Guard — Game 2D Demo Implementation Plan

> **Cho agent thực thi:** bắt buộc đọc toàn bộ plan này và `02-Tools-Assets-Agent-Guide.md`, sau đó dùng `superpowers:executing-plans` nếu skill có sẵn. Thực hiện tuần tự T0–T8, đánh dấu checkbox ngay sau khi có bằng chứng. Không dùng subagent trừ khi chủ project yêu cầu rõ ràng.

> Bản chốt ngày 12/09/2026; báo cáo thứ Năm 17/09/2026. Đây là plan triển khai, chưa phải bằng chứng game đã hoàn thành hoặc đã kiểm thử.

**Goal:** tạo game 2D một màn chơi offline, thể hiện rõ toàn bộ yêu cầu âm thanh, điều khiển, 3 tấn công, 2 phòng thủ, 3 đối tượng tương tác/6 hiệu ứng và HUD.

**Architecture:** một scene gameplay, các component C# chia theo Player, Combat, Enemy, World, Audio, UI. Player input phát lệnh; combat tạo projectile/effect; collision thay đổi state; HUD và âm thanh phản ứng với sự kiện. Không cần backend, database, networking hoặc AI sinh nội dung trong lúc chơi.

**Tech stack:** Unity `6000.3.23f1` + C#, 2D Physics, Input System, uGUI/TextMeshPro, AudioSource; build Windows x86_64.

**Spec:** các mục 1–7 trong chính tài liệu này là đặc tả; các task bên dưới triển khai đặc tả đó. Các con số cân bằng là giá trị khởi đầu do bản plan đề xuất, không phải yêu cầu của giảng viên.

## Quyết định và ràng buộc đã khóa

- Project mới được tạo trực tiếp tại `D:\Game-T5`; sau T0, chính folder này phải chứa `Assets`, `Packages`, `ProjectSettings` và hai tài liệu Markdown hiện có.
- Chỉ dùng Unity Editor `C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe`, đã xác nhận có trên máy. Không mở project bằng `2022.3.39f1` cũng đang cài.
- Target là Windows x86_64 offline, bàn phím/chuột, màn hình 16:9. Không thêm Android, multiplayer, backend hoặc dịch vụ mạng.
- R01–R09 và các hành vi E1–E6 là phạm vi bắt buộc. Không được chuyển requirement gameplay sang danh sách làm sau chỉ vì thiếu asset đẹp.
- Asset theo thứ tự: nội dung phù hợp có sẵn trong Unity/template → asset miễn phí có nguồn và license rõ ràng mà tool tải được → hình cơ bản/particle/label hoặc âm thanh placeholder tự tạo hợp lệ. Chỉ phần mỹ thuật/âm thanh thay thế chưa kiếm được mới ghi dưới tiêu đề **Should add later** trong README ở T8.
- Không tải asset không rõ license, không phụ thuộc CDN khi báo cáo, không import cả pack nếu chỉ dùng vài file. Mọi asset ngoài project phải có dòng trong `docs/asset-register.md`.
- Một task chỉ hoàn tất khi code compile, scene/prefab đã nối, kiểm tra tự động phù hợp chạy được và kiểm tra thủ công bắt buộc có PASS hoặc NOT RUN kèm lý do. “Code trông đúng” không phải bằng chứng.
- Giữ `Library`, `Temp`, `Logs`, `obj`, `Builds` và artifact dung lượng lớn ngoài Git. Track file `.meta` cùng asset.

---

## 1. Brainstorm và lựa chọn concept

| Hướng | Cách đáp ứng đề | Ưu điểm | Chi phí/rủi ro |
|---|---|---|---|
| **Core Guard — xe tăng robot bảo vệ lõi** | Đạn, tên lửa, mìn; khiên, EMP; kẻ địch vượt vòng an ninh | Một màn nhìn từ trên xuống; asset xe thân/nòng xoay đơn giản; vùng cấm có ý nghĩa | Cần làm rõ nhận diện đạn đồng minh/địch và thứ tự xử lý shield |
| Pháp sư bảo vệ cổng | Cầu lửa, tia sét, bẫy; khiên, đóng băng | Hiệu ứng phép đẹp, khác biệt tấn công rõ | Khó đồng bộ asset nhân vật/phép, animation nhiều hơn |
| Phi thuyền bảo vệ trạm | Laser, tên lửa, bom; khiên, xung vô hiệu hóa | Ít animation, hợp màn hình cố định | Vật phẩm X/Y/Z và vùng cấm cần bố trí khéo để không giống các đối tượng ngẫu nhiên |

**Đề xuất chốt Core Guard.** A là xe tăng robot do người chơi điều khiển, B là xe tăng địch tự tiến về lõi. Người chơi vừa chặn địch, vừa né bẫy và nhặt pin. Đây là game hành động bảo vệ căn cứ; không có hệ thống xây tháp, shop hay nâng cấp giữa các wave.

## 2. Phạm vi, luật chơi và hình ảnh

### 2.1 Điều kiện triển khai đã xác nhận

- Một người làm với AI hỗ trợ; khoảng 18–24 giờ gồm tích hợp, kiểm thử và dự phòng. Project chưa được scaffold và folder hiện chỉ có hai tài liệu kế hoạch.
- Unity `6000.3.23f1` đã cài; T0 phải tạo project ngay tại `D:\Game-T5` và xác minh `ProjectSettings/ProjectVersion.txt` ghi đúng phiên bản.
- Báo cáo bằng laptop Windows, bàn phím/chuột, khung hình ngang 16:9. Đề hiện tại không bắt buộc Android.
- Một scene `Main.unity`, camera orthographic cố định, map nhỏ không cuộn. Menu bắt đầu, pause và kết quả là panel trong cùng scene.
- Bản đầu không có vật cản phức tạp trong đường địch. Enemy đi thẳng về lõi, không cần NavMesh/A*.
- Không làm multiplayer, tài khoản, lưu tiến độ, inventory, boss, procedural map, cutscene, mobile input hay shader phức tạp.
- Nếu giảng viên yêu cầu engine khác hoặc APK, điều chỉnh toolchain/nghiệm thu trước Task 1; không âm thầm thêm nền tảng sát hạn.

### 2.2 Vòng chơi

1. Người chơi bấm Start; A xuất hiện gần lõi, các kỹ năng sẵn sàng.
2. Sống sót và bảo vệ lõi trong 90 giây. Địch xuất hiện mỗi 5 giây từ bốn cổng theo thứ tự cố định, tối đa 6 địch sống cùng lúc; không spawn bù khi đạt giới hạn.
3. B tiến về lõi; khi đi qua vòng đỏ thì phát cảnh báo. A có thể tiêu diệt hoặc làm choáng B.
4. B chạm lõi: lõi mất 20 HP, B bị hủy. A hết HP hoặc lõi hết HP thì thua. Hết thời gian và cả hai còn HP thì thắng; nếu xảy ra cùng tick, xử lý sát thương trước rồi xét kết quả.
5. Retry đặt lại toàn bộ trạng thái trận và các đối tượng; giữ lựa chọn âm thanh trong phiên chạy. Không cần lưu qua lần mở ứng dụng.

### 2.3 Bố cục và phong cách

- Sân nền xám xanh tối, thân xe A xanh cyan, địch đỏ cam; sprite có viền rõ, không trộn pixel art với ảnh tả thực.
- Lõi ở giữa, vòng an ninh đỏ bán kính 3 world units; địch spawn ngoài vòng, không spawn sẵn bên trong.
- A khởi đầu bên trái lõi. Ba khu X/Y/Z có khoảng trống để đi vào, nhãn nhỏ X/Y/Z và biểu tượng riêng.
- HUD phía trên trái: `HP`, `Armor`, `Coins`; trên giữa: `Core HP`, thời gian; trên phải: hai vị trí toggle SFX/BGM.
- Dưới giữa: 3 ô vũ khí, 2 ô kỹ năng có số giây cooldown. Dưới trái: hướng dẫn phím ngắn.
- Canvas reference 1280×720, Scale With Screen Size; kiểm tra thêm 1920×1080. Nút âm thanh 56×56, hai trạng thái cùng anchor/position/size.
- Feedback ngắn: muzzle flash, vệt tên lửa, vòng nổ, nháy đỏ khi trúng đòn, vòng khiên xanh, vòng EMP tím. Tránh rung camera mạnh che mất HUD.

## 3. Ma trận yêu cầu → hành vi → bằng chứng

| ID | Yêu cầu | Triển khai cụ thể | Tiêu chí nghiệm thu | Task |
|---|---|---|---|---|
| R01 | SFX ngắn khi A tấn công | 3 clip riêng: bắn đạn, phóng tên lửa, đặt mìn | Nghe được ngay khi hành động được chấp nhận; không cần chờ trúng đích | T2, T5 |
| R02 | B vào vùng cấm, cảnh báo 3–6 lần | Một lượt vào tạo chuỗi 4 beep, cách nhau 0,45 s | Một B đi vào: nghe đúng 4; đứng yên trong vùng không tạo chuỗi mới | T5 |
| R03 | SoundOff/SoundOn thay nhau, cùng vị trí/kích thước | 2 GameObject button thật, chỉ một active | SoundOff tắt SFX và hiện SoundOn; SoundOn bật SFX và hiện SoundOff | T5 |
| R04 | MusicOn/MusicOff thay nhau | 2 GameObject button thật, chỉ một active | MusicOn bắt đầu nhạc và hiện MusicOff; MusicOff dừng nhạc và hiện MusicOn | T5 |
| R05 | Hướng và tốc độ di chuyển | WASD/mũi tên, vector chuẩn hóa, nòng hướng chuột | 8 hướng; đi chéo không nhanh hơn đi ngang; không vượt biên | T1 |
| R06 | Ít nhất 3 cơ chế tấn công | Đạn trực tiếp, tên lửa nổ lan, mìn đặt tại chỗ | Khác về hành vi; đều gây sát thương thật và có cooldown | T2 |
| R07 | Ít nhất 2 phòng thủ | Khiên chặn đạn; EMP làm choáng địch | Khiên chặn đạn thật; EMP dừng di chuyển lẫn bắn trong 2 s | T3 |
| R08 | A va chạm X/Y/Z, ít nhất 6 hiệu ứng | X: giảm máu/giáp; Y: chậm/mất khiên; Z: tăng coins/tốc độ | Quan sát đủ E1–E6, có số liệu và feedback | T4 |
| R09 | HUD ≥3 thông tin của A | HP, Armor, Coins + weapon/cooldown | Cập nhật ngay khi state đổi, không chỉ text trang trí | T1, T4, T6 |

**Điểm dễ hiểu nhầm:** tên nút biểu thị hành động sắp thực hiện, không phải trạng thái hiện tại. Không tự đảo tên cho “trực giác” rồi làm khác yêu cầu. Cảnh báo thuộc nhóm SFX, vì vậy tắt SFX cũng phải làm cảnh báo im lặng.

## 4. Điều khiển, combat và damage

### 4.1 Điều khiển

| Input | Hành vi |
|---|---|
| WASD / mũi tên | Di chuyển A theo trục màn hình, không dùng tank steering |
| Chuột | Hướng nòng bắn, fallback hướng gần nhất nếu chuột trùng tâm A |
| 1 / 2 / 3 | Chọn Bullet / Rocket / Mine |
| Chuột trái | Bullet: giữ để bắn; Rocket/Mine: một lần mỗi nhấn |
| Q | Bật Shield |
| E | Phát EMP |
| Esc | Pause/Resume; gameplay, cooldown và alert timer dừng theo |
| R | Retry chỉ khi ở màn hình kết quả |
| F1 | Mở bảng Demo Controls được ghi nhãn rõ |

Bấm UI không tạo đạn; đổi vũ khí không reset cooldown. Input gameplay chỉ hoạt động trong trạng thái Playing.

### 4.2 Chỉ số khởi đầu và ba tấn công

A: HP 100, Armor 50, Coins 0, speed 4 units/s. Core: HP 100. B: HP 60, speed 1,2 units/s; khi A trong 6 units, bắn một viên hướng A mỗi 2 s, tiếp tục tiến về lõi. Đạn địch damage 10, speed 5; không gây sát thương lõi.

| Vũ khí | Cách hoạt động | Thông số ban đầu | Phân biệt khi demo |
|---|---|---|---|
| Bullet | Viên đạn đi thẳng, mất khi trúng mục tiêu đầu tiên hoặc hết hạn | Damage 10, speed 14, cooldown 0,2 s, TTL 2 s | Nhanh, đơn mục tiêu |
| Rocket | Tên lửa bay thẳng; nổ khi trúng địch/tường; hết TTL thì biến mất | Damage 35 trong bán kính 1,5, speed 7, cooldown 1,5 s, TTL 3 s | Nổ lan, nhiều địch cùng mất máu; mục tiêu trực tiếp không nhận damage hai lần |
| Mine | Đặt tại vị trí A, arm sau 0,5 s; B chạm thì nổ; không kích bởi A | Damage 50, bán kính 1,8, cooldown 2 s, TTL 12 s; tối đa 3 mìn | Tấn công trì hoãn, có thể rời vị trí sau đặt |

Ba vũ khí sẵn có từ đầu; không giới hạn đạn. Đặt mìn khi đã đủ 3 thì từ chối, hiển thị `Mine limit`, không dùng cooldown và không phát SFX tấn công. A và lõi không chịu sát thương vũ khí của A.

### 4.3 Hai phòng thủ

| Cơ chế | Hành vi | Thông số | Nghiệm thu |
|---|---|---|---|
| Shield — Q | Vòng khiên theo A, chặn và hủy đạn địch trước khi damage được xử lý | 3 s hoặc 3 lần trúng; cooldown 8 s tính từ lúc kích hoạt | HP/Armor không giảm; lần trúng thứ 3 làm vỡ khiên, viên tiếp theo gây damage |
| EMP — E | Một xung quanh A; địch trong bán kính bị stun, dừng di chuyển/bắn | Radius 3, duration 2 s, cooldown 6 s | Địch trong vùng đứng yên và không bắn, địch ngoài vùng vẫn hoạt động |

EMP không gây damage, không xóa đạn đã bay. Khi hết stun, địch tiếp tục; không bắn bù hàng loạt. B không gây body-contact damage lên A, tránh bổ sung một cơ chế khó quan sát. Shield chỉ chặn projectile địch, không chặn bẫy môi trường X/Y.

### 4.4 Quy tắc damage, cooldown và vòng đời

- Damage thường: khiên hợp lệ chặn trước; nếu không, trừ Armor trước, phần dư trừ HP. HP/Armor clamp không âm.
- X dùng damage môi trường riêng như mục 5, cố ý giảm cả HP và Armor để thấy được hai hiệu ứng.
- Mỗi vụ nổ gom mục tiêu theo root damageable ID, không theo số collider. Không gây damage nhiều lần vì thân/nòng có nhiều collider.
- Mỗi projectile chỉ resolve hit một lần. Giới hạn TTL và xóa tất cả khi Retry.
- Cooldown thuộc từng loại vũ khí/kỹ năng; chỉ bắt đầu khi thực thi thành công. Không dùng coroutine mới mỗi frame.
- Chưa cần object pooling; với giới hạn nhỏ, Instantiate/Destroy đủ cho bản demo. Chỉ tối ưu khi profiling chỉ ra vấn đề.

## 5. X/Y/Z và sáu hiệu ứng gameplay

Không tính “phát âm thanh”, “đổi màu” là toàn bộ 6 hiệu ứng. Sáu mục dưới đây đều có thay đổi state, cộng thêm VFX để trình diễn rõ.

| Object | Hiệu ứng gameplay | Giá trị / thời hạn | Feedback |
|---|---|---|---|
| **X — mìn địch** | **E1:** giảm HP | HP −20 trực tiếp, bỏ qua Armor | Flash đỏ và text `−20 HP` |
| X | **E2:** giảm Armor | Armor −10 độc lập E1 | Mảnh giáp và text `−10 Armor` |
| **Y — bẫy điện từ** | **E3:** giảm tốc | Speed ×0,5 trong 3 s từ lúc vào | Icon Slow, quầng tím, HUD phụ hiển thị speed |
| Y | **E4:** mất khiên hiện có | Shield bị hủy tức thì, cooldown Q vẫn giữ | Vòng khiên vỡ, text `Shield broken` |
| **Z — pin tiếp tế** | **E5:** tăng coins | Coins +10 | Particle vàng, text `+10` |
| Z | **E6:** tăng tốc | Speed ×1,5 trong 4 s | Vệt xanh, icon Boost |

X và Z là pickup một lần rồi biến mất. Y là trigger tồn tại: một lần vào áp dụng hai effect; ở lại không áp dụng mỗi frame, ra rồi vào lại mới kích hoạt. Không có khiên thì Y vẫn gây Slow; E4 cần được demo bằng cách bật Q trước khi đi vào.

Slow/Boost có hai timer riêng. Speed thực tế = 4 × (slow ? 0,5 : 1) × (boost ? 1,5 : 1); cùng có thì speed = 3. Nhặt lại cùng loại làm mới thời hạn, không nhân chồng vô hạn. Hết một effect tính lại từ base speed, không cộng/trừ dồn. Retry xóa cả hai.

Sáu hiệu ứng hình ảnh phụ: nổ X, flash bị thương, mảnh giáp, điện tím, khiên vỡ, particle/vệt boost. Các hiệu ứng phụ không thay cho E1–E6.

## 6. Đặc tả âm thanh và nút UI

### 6.1 SFX và BGM độc lập

- Một `AudioService` trong scene, không tạo singleton xuyên scene cho demo một scene.
- MusicSource: một AudioSource loop; SfxSource: one-shot vũ khí/va chạm; AlertSource: beep tuần tự. Tất cả là audio 2D.
- SFX toggle quản lý cả SfxSource và AlertSource; BGM toggle chỉ quản lý MusicSource. Không mute bằng AudioListener.volume vì sẽ tắt cả hai nhóm.
- Mặc định mở ứng dụng: SFX ON, BGM OFF. Hiện SoundOff và MusicOn. Start không tự bật BGM.
- Khi SFX OFF: chặn request mới, dừng clip SFX đang phát, hủy alert đang chạy và hàng đợi. Khi bật lại không phát bù.
- BGM ON: bắt đầu track từ đầu; BGM OFF: Stop. Click nhanh nhiều lần không tạo MusicSource/track trùng.
- Pause: dừng timer gameplay, pause SFX/alert và BGM; resume tiếp tục phần đang phát nếu nhóm tương ứng vẫn enabled. Các nút toggle bị khóa trong pause panel để tránh trạng thái chéo. Game over: hủy SFX/alert, BGM tiếp tục nếu đang bật.
- Clip cảnh báo ngắn khoảng 0,15–0,2 s; BGM để nhỏ hơn SFX bằng nghe thử trên loa laptop. Không cần FMOD/Wwise.

### 6.2 Bảng chuyển trạng thái chính xác

| State trước | Object đang hiển thị / click | State sau | Object thay thế |
|---|---|---|---|
| SFX ON | `SoundOff` — nhãn “Tắt hiệu ứng” | SFX OFF | `SoundOn` — “Bật hiệu ứng” |
| SFX OFF | `SoundOn` | SFX ON | `SoundOff` |
| BGM OFF | `MusicOn` — “Bật nhạc” | BGM ON | `MusicOff` — “Tắt nhạc” |
| BGM ON | `MusicOff` | BGM OFF | `MusicOn` |

Hai cặp đặt tại hai vị trí khác nhau; trong mỗi cặp phải trùng RectTransform, kích thước và vùng click. Dùng 4 GameObject đúng tên đề cho dễ chỉ ra trong Hierarchy, không chỉ đổi sprite của một object.

### 6.3 Cảnh báo vùng cấm

- Vùng cấm là `CircleCollider2D.isTrigger`, center lõi, radius 3; chỉ nhận layer EnemyBody.
- Một B từ ngoài đi vào tạo một event; không dùng OnTriggerStay để phát cảnh báo.
- Đếm theo root enemy ID và số collider đang nằm trong vùng để thân/nòng không tạo nhiều event. Tái vào sau khi ra hoàn toàn tạo event mới.
- Một event khi SFX đang ON tạo một job 4 beep tại t=0; 0,45; 0,90; 1,35 s. Clip ngắn hơn khoảng lặp.
- Một AlertSource xử lý hàng đợi FIFO: nhiều B vào gần nhau tạo các job nối tiếp, không chồng âm. Job đã nhận chạy đủ 4 kể cả B ra khỏi vùng/chết; exception: mute, game over hoặc Retry hủy job.
- SFX OFF lúc vào: vẫn cập nhật occupancy để logic đúng, nhưng bỏ job, không phát bù khi bật lại.
- Visual vòng đỏ nhấp nháy theo sự kiện vào vùng ngay cả khi SFX OFF. Trong Demo Controls có bộ đếm beep đã gửi, chỉ dùng làm bằng chứng phụ; bắt buộc nghe thật.
- Cleanup occupancy khi Enemy bị hủy/disable; spawn demo đặt B ngoài vùng để có chuyển trạng thái vào thật.

## 7. Kiến trúc và file map dự kiến

Các đường dẫn dưới đây dành cho project mới. Nếu mở repo có sẵn, agent phải đối chiếu và ghi mapping reuse trước khi thêm; không xóa hoặc thay cấu trúc của game cũ.

| Đường dẫn dưới `Assets/_Game/` | Trách nhiệm |
|---|---|
| `Scenes/Main.unity` | Scene duy nhất, game objects và Canvas |
| `Scripts/Core/GameSession.cs` | Start, pause, timer, win/lose, retry |
| `Scripts/Player/PlayerInputReader.cs` | Input System, chặn input trên UI |
| `Scripts/Player/PlayerMotor.cs` | Di chuyển theo Rigidbody2D, aim nòng |
| `Scripts/Player/PlayerStats.cs` | HP, armor, coins, damage và event state |
| `Scripts/Player/StatusEffects.cs` | Slow/boost timer và derived speed |
| `Scripts/Combat/WeaponController.cs` | Chọn vũ khí, cooldown và tạo projectile |
| `Scripts/Combat/Projectile.cs` | Bullet/rocket qua cấu hình; hit-once, TTL |
| `Scripts/Combat/Mine.cs` | Arming, limit, trigger và detonate-once |
| `Scripts/Combat/Explosion.cs` | AoE và deduplicate targets |
| `Scripts/Combat/DefenseController.cs` | Shield, EMP, cooldown |
| `Scripts/Combat/IDamageable.cs` | Contract damage chung |
| `Scripts/Enemy/EnemyController.cs` | Đi về lõi, shoot, stun, cleanup |
| `Scripts/Enemy/EnemySpawner.cs` | Nhịp spawn và giới hạn số lượng |
| `Scripts/World/CoreHealth.cs` | Lõi nhận damage từ địch chạm |
| `Scripts/World/ForbiddenZone.cs` | Occupancy và event vào vùng |
| `Scripts/World/InteractionObject.cs` | X/Y/Z, once-on-enter/consume |
| `Scripts/Audio/AudioService.cs` | SFX, BGM, alert queue và toggle state |
| `Scripts/UI/HudPresenter.cs` | Đọc state, hiển thị HUD/skills |
| `Scripts/UI/AudioToggleView.cs` | 4 object On/Off, bind UI |
| `Scripts/UI/DemoDirector.cs` | Các tình huống trình diễn tái lập |
| `Scripts/VFX/FeedbackPresenter.cs` | Flash, popup, particle và lifetime |
| `Editor/DemoSceneBuilder.cs` | Menu build scene/prefab bằng Unity API, chỉ vùng `_Game` |
| `Editor/BuildDemo.cs` | Build Windows, báo lỗi rõ |
| `Tests/EditMode/CoreRulesTests.cs` | Damage, speed, cooldown, state mapping |
| `Tests/PlayMode/DemoAcceptanceTests.cs` | Physics, alert, scene binding, reset |
| `Art/`, `Audio/`, `Prefabs/`, `Settings/` | Asset đã chọn, prefab và cấu hình |

`README.md` nằm ở root project. `docs/progress.md`, `docs/acceptance.md` và `docs/asset-register.md` nằm dưới `docs/`. Hai plan hiện có tiếp tục nằm ở root để không làm mất đường dẫn người dùng đã giao. Giữ cả `.meta` với asset; commit `Assets`, `Packages`, `ProjectSettings`; bỏ `Library`, `Temp`, `Logs`, `obj`, `Builds` và test artifact khỏi git.

**Contract dùng chung:** `IDamageable.ApplyDamage(float amount)`; `PlayerStats.ApplyEnvironmentHit(float hpLoss, float armorLoss)`; `PlayerStats.AddCoins(int amount)`; `StatusEffects.ApplySlow(float multiplier, float duration)` và `ApplyBoost(...)`; `DefenseController.TryActivateShield(): bool`, `TryActivateEmp(): bool`, `BreakShield(): void`, `TryBlockProjectile(): bool`; `EnemyController.Stun(float seconds)`; `AudioService.PlaySfx(AudioClip clip)`, `RequestAlert()`, `SetSfxEnabled(bool)`, `SetMusicEnabled(bool)`. Nếu đổi signature, cập nhật toàn bộ call site và plan trong cùng task.

Physics: PlayerBody và EnemyBody dùng Rigidbody2D gravity 0, freeze rotation; rotation nòng là transform con. Layer PlayerProjectile chỉ trúng EnemyBody/World; EnemyProjectile chỉ trúng PlayerBody/World, đi qua defense check trước damage. ForbiddenZone chỉ nhận EnemyBody; Interaction chỉ nhận PlayerBody. Dùng trigger khi không cần lực vật lý; kiểm thử xuyên đạn ở tốc độ đã chọn, dùng continuous collision hoặc swept cast nếu thực nghiệm có miss.

## 8. Task triển khai có checkpoint

### Quy trình bắt buộc cho mọi task

1. Đọc plan, `docs/progress.md` nếu đã có, `git status --short` và commit gần nhất; không ghi đè thay đổi không thuộc task.
2. Xác định test nhỏ nhất chứng minh hành vi, viết test thất bại trước phần logic tương ứng khi test tự động có ý nghĩa.
3. Thực hiện thay đổi nhỏ, compile, chạy đúng nhóm EditMode/PlayMode, rồi mở scene kiểm tra hành vi quan sát được.
4. Lưu log/XML/screenshot vào `Artifacts/` (không commit file lớn), cập nhật acceptance/progress bằng kết quả thật.
5. Chỉ đánh dấu task hoàn tất khi đạt gate; nếu không chạy được Unity hoặc kiểm tra loa/hình, ghi `NOT RUN`, nguyên nhân và lệnh/thao tác chính xác cho người dùng.
6. Commit đúng file của task. Không push, merge, publish hoặc tạo tag trước T8.

Các lệnh Unity bên dưới dùng executable đã khóa:

```powershell
$unityExe = 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe'
& $unityExe -batchmode -quit -projectPath 'D:\Game-T5' -runTests -testPlatform EditMode -testResults 'D:\Game-T5\Artifacts\Tests\editmode.xml' -logFile 'D:\Game-T5\Artifacts\Logs\editmode.log'
& $unityExe -batchmode -quit -projectPath 'D:\Game-T5' -runTests -testPlatform PlayMode -testResults 'D:\Game-T5\Artifacts\Tests\playmode.xml' -logFile 'D:\Game-T5\Artifacts\Logs\playmode.log'
```

Không chạy batchmode khi Unity Editor đang mở cùng project. Exit code 0 nhưng thiếu XML hoặc log cho thấy không có test chạy thì không được ghi PASS.

### Task 0 (T0) — Bootstrap project và build rỗng có kiểm soát (1–1,5 giờ)

**Files:** tạo `Assets/`, `Packages/`, `ProjectSettings/`, `.gitignore`, `Assets/_Game/Scenes/Main.unity`, `Assets/_Game/Editor/BuildDemo.cs`, `docs/progress.md`, `docs/acceptance.md`, `docs/asset-register.md`.

**Produces:** project Unity `6000.3.23f1` mở/compile được, scene nằm trong build profile, repository có checkpoint đầu tiên và Windows player tối thiểu chạy được.

- [ ] Chụp danh sách file và giữ nguyên hai Markdown hiện có; xác nhận `D:\Game-T5` chưa là Git repo và chưa có project Unity.
- [ ] Từ Unity Hub đang cài, tạo project tại đúng `D:\Game-T5` bằng template **2D** có sẵn. Ưu tiên **Universal 2D** nếu Hub cung cấp; nếu chỉ có **2D Core**, dùng 2D Core và không đổi render pipeline giữa dự án.
- [ ] Nếu tool không điều khiển được Hub, yêu cầu người dùng thực hiện đúng một bước tạo project bằng template; không tự tạo YAML project giả. Sau khi project xuất hiện, agent tiếp tục mà không hỏi lại các quyết định đã khóa.
- [ ] Đọc `ProjectSettings/ProjectVersion.txt`; gate thất bại nếu không phải `6000.3.23f1`. Kiểm tra `Packages/manifest.json` có Input System, uGUI, TextMeshPro và Test Framework phiên bản được Unity resolve; chỉ thêm package còn thiếu qua Package Manager/manifest.
- [ ] Bật Visible Meta Files và Force Text; tạo `.gitignore` Unity trước `git init`, sau đó kiểm tra không có `Library`, `Temp`, `Logs`, `obj`, `Builds` trong staged files.
- [ ] Tạo cấu trúc `Assets/_Game/{Art,Audio,Editor,Prefabs,Scenes,Scripts,Settings,Tests}`; tạo `Main.unity`, một camera, một Canvas/EventSystem và đưa scene vào Windows build profile. Không bắt đầu gameplay ở T0.
- [ ] Tạo `BuildDemo.BuildWindows()` xuất toàn bộ player vào `Builds/Windows/CoreGuard/`; lỗi nếu scene chưa được enable hoặc có compile error.
- [ ] Chạy compile/batchmode và một Windows build rỗng. Mở `.exe`; gate đạt khi không có Console exception và cửa sổ player mở được.
- [ ] Khởi tạo ba tài liệu evidence với trạng thái ban đầu; commit `chore: bootstrap Unity 6000.3.23f1 project`.

### Task 1 (T1) — Một màn chơi và vòng chơi chạy được (3 giờ)

**Files:** Core/GameSession, Player/InputReader/Motor/Stats, Enemy/Controller/Spawner, World/CoreHealth, UI/HudPresenter, Main scene, Editor/DemoSceneBuilder; README và progress.

**Produces:** movement, HP/Armor/Coins state, enemy spawn/move, game session; `IDamageable.ApplyDamage(float)` cho T2.

- [ ] Đọc checkpoint T0; kiểm tra project compile, scene build và git sạch. Nếu T0 chưa đạt, quay lại T0 thay vì sửa gameplay trên project lỗi.
- [ ] Tạo Input System actions Move/Aim/Fire/Weapon1–3/Shield/EMP/Pause/Retry; nối cùng Input System UI module, không trộn legacy input.
- [ ] Dựng map bằng hình cơ bản, A, lõi, biên map, một B; nối reference bằng editor script hoặc inspector có checklist rõ. Builder chạy lại không tạo hai camera/Canvas.
- [ ] Làm movement + normalized input, UI ba chỉ số, timer và điều kiện kết thúc; B chạm lõi mất 20 HP và bị hủy.
- [ ] Kiểm tra: di chuyển chéo/ngang 2 s có khoảng cách tương đương trong sai số physics; không vượt biên; pause ngừng timer; Retry khôi phục 100/50/0 và lõi 100.
- [ ] Build Windows sơ bộ ngay để phát hiện thiếu module/license/scene từ ngày đầu. Chưa có combat không đánh dấu các yêu cầu combat hoàn tất.
- [ ] Commit `feat: add playable arena and session flow`; ghi remaining checks.

### Task 2 (T2) — Ba tấn công và enemy projectile (3 giờ)

**Files:** Combat/IDamageable, WeaponController, Projectile, Mine, Explosion; EnemyController; weapon/projectile prefabs; Tests/EditMode/CoreRulesTests, Tests/PlayMode/DemoAcceptanceTests.

**Consumes:** movement, state/session từ T1. **Produces:** damage, hit events và SFX request hooks cho T5.

- [ ] Viết case kiểm tra damage Armor-first, một explosion chỉ damage một enemy root một lần; xác nhận thất bại trước khi hoàn thiện logic tương ứng.
- [ ] Làm Bullet trước: sinh ở muzzle, aim chuột, TTL, cooldown; bắn hạ B có HP thật.
- [ ] Thêm Rocket AoE; dùng hit guard để direct collision không cộng thêm damage lần hai.
- [ ] Thêm Mine arm/trigger/TTL/limit, địch kích hoạt được, A đi qua an toàn; sinh tại A khi click.
- [ ] Thêm B bắn đạn theo thông số mục 4; damage thường ưu tiên Armor.
- [ ] Nối các hook âm thanh khi action accepted; chưa có AudioService thì hook chưa phát, không tự thêm nhiều AudioSource vào projectile.
- [ ] Kiểm tra: bullet 10 damage; rocket trúng hai B gần nhau mỗi B mất 35; mine chưa arm không nổ, sau arm gây 50; một enemy có hai collider không nhận gấp đôi.
- [ ] Kiểm tra đổi vũ khí liên tục không bypass cooldown; click HUD không bắn; đầy mine không mất cooldown.
- [ ] Commit `feat: add three distinct attacks and enemy fire`.

### Task 3 (T3) — Shield và EMP (2 giờ)

**Files:** Combat/DefenseController, EnemyController, Projectile; defense VFX prefab; tests hiện có.

**Consumes:** projectile damage route T2. **Produces:** defense contracts, shield state/cooldown cho HUD và Y.

- [ ] Test shield hấp thụ 3 hit, hit thứ 4 gây damage; chuẩn bị enemy stun test.
- [ ] Tạo shield theo A; resolve defense một lần trước khi gọi damage; timer/hit count hết thì vỡ.
- [ ] Tạo EMP overlap radius 3, dedupe enemy, gọi Stun(2); không ảnh hưởng địch ngoài radius.
- [ ] Stun dừng movement và shoot, resume không bắn dồn; đạn đã bay không bị xóa.
- [ ] Kiểm tra thực tế Q trước đạn: HP/Armor không đổi; E khi 2 B gần và 1 B xa: chỉ 2 B dừng.
- [ ] Kiểm tra cooldown, pause, Retry; commit `feat: add shield and EMP defense`.

### Task 4 (T4) — X/Y/Z với sáu hiệu ứng (2 giờ)

**Files:** Player/StatusEffects, World/InteractionObject, DefenseController.BreakShield, PlayerStats; X/Y/Z prefabs; tests.

**Consumes:** stats và defense T1/T3. **Produces:** E1–E6, event feedback/HUD.

- [ ] Test state X từ HP100/Armor50 thành HP80/Armor40; slow+boost cùng lúc speed3, hết slow còn speed6, hết cả trở lại4.
- [ ] Tạo X một lần: giảm độc lập HP/Armor, nổ rồi biến mất; không có trigger double hit.
- [ ] Tạo Y on-enter: slow3s và BreakShield; khiên mất nhưng cooldown không reset.
- [ ] Tạo Z một lần: coins+10, boost4s rồi biến mất; lần nhặt thứ hai refresh timer, không stack vô hạn.
- [ ] Tạo visual và text phân biệt cả 6 effect; X/Y/Z có nhãn để báo cáo.
- [ ] Đi vào Y với shield đang bật để chứng minh E4; đứng trong Y không bị kéo dài vô hạn; thử ra/vào lại.
- [ ] Commit `feat: add six collision effects across X Y Z`.

### Task 5 (T5) — Âm thanh đúng đề và vùng cấm (3 giờ)

**Files:** Audio/AudioService, World/ForbiddenZone, UI/AudioToggleView, audio asset, button prefab; tests.

**Consumes:** accepted-action hooks T2, enemy root/lifecycle, session pause/reset. **Produces:** R01–R04 hoàn chỉnh.

- [ ] Kiểm tra asset theo thứ tự đã khóa: Unity/template, rồi nguồn miễn phí có license mà tool tải được. Chỉ import clip thực sự dùng và ghi ngay path/tác giả/URL/license vào asset register.
- [ ] Nếu không tải hoặc tạo được clip phù hợp, dùng các WAV placeholder ngắn, khác nhau và hợp lệ để R01–R04 vẫn kiểm thử được; ghi nhu cầu thay thế chất lượng sản xuất vào danh sách nháp, chưa thêm **Should add later** vào README trước T8.
- [ ] Tạo 3 AudioSource đã mô tả, gán clip thật hoặc placeholder đã đăng ký; audio 2D.
- [ ] Phát SFX ngắn tại fire/drop; thêm hit/explosion/defense feedback vừa đủ.
- [ ] Tạo 4 button đúng tên và bảng trạng thái mục 6; controller nằm ở parent luôn active, không nằm trên nút sẽ bị disable.
- [ ] Làm occupancy + alert queue 4 beep; clock sử dụng gameplay time để pause đúng.
- [ ] Test một B vào → 4 yêu cầu phát ở các mốc; đứng trong vùng không thêm; 2 collider không nhân đôi; ra rồi vào tạo job mới; 2 B vào gần nhau tạo 2 job tuần tự.
- [ ] Test SFX mute giữa chuỗi: clip đang phát dừng, phần còn lại/hàng đợi bị xóa, bật lại không replay; BGM vẫn chạy. Test tắt BGM vẫn nghe bắn.
- [ ] Test nhanh 10 lần bật/tắt mỗi nhóm, luôn đúng một object trong cặp active, đúng cùng rect.
- [ ] Nghe thật trên Editor và bản build để xác nhận 4 tiếng rõ; automated scheduling test không chứng minh loa đã phát.
- [ ] Commit `feat: implement audio controls and four-beep zone alert`.

### Task 6 (T6) — Hoàn thiện HUD, asset và chế độ trình diễn (2 giờ)

**Files:** UI/HudPresenter, UI/DemoDirector, VFX/FeedbackPresenter, Art/Audio/Prefabs, docs/asset-register.md.

**Consumes:** toàn bộ state T1–T5. **Produces:** màn demo dễ thao tác, không phụ thuộc random.

- [ ] Kiểm kê asset Unity/template đang dùng; sau đó mới tải bộ miễn phí có license rõ ràng nếu tool cho phép. Nếu vẫn thiếu, giữ hình cơ bản/particle/label dễ phân biệt; không chặn task chỉ vì chưa có sprite trang trí.
- [ ] Thay hình cơ bản bằng một bộ sprite nhất quán khi có, chỉnh tỷ lệ/collider/pivot; không đổi gameplay trong bước này.
- [ ] HUD đủ HP/Armor/Coins, thêm core/time, selected weapon, cooldown Q/E và weapon hiện tại; có trạng thái READY/giây còn lại.
- [ ] Thêm F1 panel `DEMO MODE`: khi mở tắt auto-spawn; nút Reset Scenario xóa đạn/địch/XZ, reset stats/timer/cooldown nhưng giữ sound choice.
- [ ] Thêm các nút tạo tình huống: Spawn Zone Enemy (ngoài vòng), Spawn Shooter (bắn A), Spawn Enemy Cluster (3 B gần nhau), Restore X/Y/Z. Không thêm nút “giả phát đủ 4 tiếng” thay cho B vào vùng.
- [ ] Mọi kịch bản dùng cùng combat/physics/audio gameplay; debug panel không cập nhật trực tiếp HUD để giả thành tích.
- [ ] Kiểm tra 1280×720 và 1920×1080: UI không che sân, button click không xuyên xuống fire, label tiếng Việt không lỗi glyph nếu dùng.
- [ ] Chốt asset register: tên file thực tế, tác giả, nguồn, license, vai trò, chỉnh sửa.
- [ ] Commit `feat: polish HUD assets and repeatable demo scenarios`.

### Task 7 (T7) — Nghiệm thu và đóng băng tính năng (2–3 giờ + dự phòng sửa lỗi)

**Files:** Editor/BuildDemo, README, docs/acceptance.md, docs/progress.md; build và video ngoài git.

- [ ] Chạy nhóm EditMode/PlayMode liên quan; lưu XML/log với thời điểm và Unity version. Không đặt mục tiêu số lượng test cho đẹp.
- [ ] Chạy tất cả R01–R09 trong build Windows, ghi PASS/FAIL/NOT RUN với bằng chứng. Bất cứ NOT RUN nào cũng phải hiện trong handoff.
- [ ] Chạy Retry 5 lần: không nhân AudioListener/AudioSource/controller, không sót mìn, slow, queue hay occupancy.
- [ ] Test nhanh chơi liên tục 5 phút: không mất reference, không Console exception; đánh giá mục tiêu 60 FPS trên laptop thật ở 1280×720, nếu chưa đo ghi chưa đo.
- [ ] Sửa mọi FAIL chặn R01–R09, chạy lại đúng test và scenario bị ảnh hưởng; sau 20:00 ngày 16/09 chỉ sửa lỗi chặn demo.
- [ ] Khi toàn bộ R01–R09 là PASS hoặc có NOT RUN được nêu trung thực, commit `test: record Core Guard acceptance evidence` và chuyển sang T8.

### Task 8 (T8) — Release, README và diễn tập báo cáo (1–2 giờ)

**Files:** `README.md`, `docs/acceptance.md`, `docs/progress.md`, `docs/asset-register.md`; build ZIP/video/screenshot dưới `Artifacts/` hoặc ngoài Git.

**Consumes:** feature-frozen build từ T7. **Produces:** gói Windows chạy độc lập, tài liệu bàn giao và kịch bản báo cáo đã diễn tập.

- [ ] Build cuối vào `Builds/Windows/CoreGuard/`, zip toàn bộ folder gồm `.exe`, `_Data`, UnityPlayer và thư viện phụ; không chỉ zip file `.exe`.
- [ ] Giải nén ZIP vào một folder khác, mở game không qua Editor; test Start, movement, một vũ khí, Q/E, audio toggles, game over và Retry.
- [ ] Quay video dự phòng 3–5 phút có system audio; lưu ảnh HUD, ba vũ khí, hai phòng thủ và X/Y/Z. Mở lại video để xác nhận có hình lẫn tiếng.
- [ ] Hoàn thiện README: Unity `6000.3.23f1`, cách mở `Main.unity`, controls, build/run, kiến trúc ngắn, asset credits/license, link evidence và known limitations.
- [ ] Chỉ lúc này thêm mục `## Should add later`. Liệt kê từng asset không tạo/tải được bằng vai trò, nơi thay thế trong project và nguồn/loại asset mong muốn. Nếu không thiếu gì, ghi `Không có — mọi asset cần cho bản demo đã có hoặc có placeholder hợp lệ.` Không liệt kê bug hoặc requirement R01–R09 ở mục này.
- [ ] Diễn tập đúng kịch bản mục 10 dưới 5 phút trên build vừa giải nén; ghi thời lượng và lỗi thao tác vào progress, diễn tập lại nếu vượt thời gian.
- [ ] Commit `docs: package Core Guard demo release`. Chỉ tạo tag local `demo-ready` khi acceptance không có FAIL; không push/merge/publish.

## 9. Lịch đến thứ Năm 17/09/2026

| Ngày | Thời lượng mục tiêu | Công việc | Kết quả bắt buộc |
|---|---:|---|---|
| Thứ Bảy 12/09 | 3–4 giờ | T0 + khởi đầu T1 | Project đúng version, build rỗng và vòng chơi cơ bản |
| Chủ Nhật 13/09 | 5 giờ | T2 + T3 | 3 tấn công, 2 phòng thủ chạy thật |
| Thứ Hai 14/09 | 5 giờ | T4 + T5 | Đủ E1–E6 và 4 beep/toggle độc lập |
| Thứ Ba 15/09 | 2–4 giờ | T6, sửa lỗi tích hợp | Toàn bộ yêu cầu trình diễn được |
| Thứ Tư 16/09 | 3–5 giờ | T7 + T8, build, quay dự phòng | Đóng băng tính năng lúc 20:00 giờ Việt Nam; có ZIP và README |
| Thứ Năm 17/09 | 0,5–2 giờ | Kiểm tra loa, mở build, diễn tập T8 | Báo cáo; chỉ sửa lỗi chặn demo nếu thực sự cần |

Ước lượng là kế hoạch, không phải cam kết thời gian của AI. Download/cài lại Unity hoặc yêu cầu APK có thể làm vượt ngân sách. Nếu chỉ có 8–12 giờ, giữ toàn bộ yêu cầu chấm điểm nhưng dùng hình đơn giản, bỏ animation phụ, bỏ auto wave phong phú; ưu tiên demo scenario và build.

**Thứ tự cắt phạm vi khi trễ:** shader/lighting → screen shake → sprite animation nhiều frame → trang trí map → title animation. Không cắt 3 vũ khí, 2 phòng thủ, E1–E6, 4 beep, nút On/Off hoặc HUD ba chỉ số.

## 10. Kịch bản báo cáo 5 phút

| Thời gian | Thao tác | Điều chứng minh |
|---|---|---|
| 0:00–0:30 | Giới thiệu A/B/lõi; Start; WASD và aim | Di chuyển, HUD, mục tiêu |
| 0:30–1:15 | F1 tạo cụm B; dùng lần lượt 1/2/3 | 3 cơ chế khác nhau, SFX lúc phát động |
| 1:15–2:00 | Reset; Spawn Shooter; Q chặn; E làm choáng | 2 phòng thủ thực sự tác động |
| 2:00–2:45 | Reset; đi qua X; Q rồi đi qua Y; nhặt Z | E1–E6 và HUD thay đổi |
| 2:45–3:20 | Reset; Spawn Zone Enemy; để B đi qua vòng | Cảnh báo 4 beep; đứng trong vùng không lặp vô hạn |
| 3:20–4:20 | Bật nhạc; tắt SFX rồi bắn; bật SFX; tắt nhạc rồi bắn | 4 nút thay đúng vị trí, hai nhóm độc lập |
| 4:20–5:00 | Trình bày architecture, test evidence và giới hạn | Hiểu cách triển khai, có build kiểm tra thật |

Chuẩn bị sẵn loa/system volume, đóng ứng dụng đang phát nhạc, thử clip ngoài game nếu mất tiếng. Không bật bất tử để giấu bug; Demo Mode phải hiển thị công khai và chỉ dùng để chuẩn bị tình huống.

## 11. Mẫu checkpoint cho agent

Ghi vào `docs/progress.md` sau mỗi task và trước khi dừng:

```markdown
# Progress
- Project / branch / Unity version: ghi giá trị thực tế
- HEAD: SHA thực tế hoặc chưa commit
- Last completed task: Tn, chỉ điền khi đạt gate
- Current task + completed substeps: ghi các bước đã làm
- Evidence: lệnh/test, kết quả, đường dẫn XML/log/build
- Remaining manual checks: liệt kê điều chưa chạy
- Known issues: triệu chứng và cách tái hiện
- Next smallest action: một hành động cụ thể
- User changes preserved: file không thuộc phạm vi task
```

Không có cơ chế bảo đảm agent luôn biết trước khi hit usage limit. Giảm rủi ro bằng task nhỏ, checkpoint thường xuyên và không bắt đầu thao tác dài khi ngân sách còn thấp. Khi dừng giữa task, ghi rõ partial; không đánh dấu hoàn tất để làm sạch checklist.
