# Core Guard — Công cụ, asset và hướng dẫn triển khai với AI agent

Ngày lập: 12/09/2026. Đọc kèm `01-Game-2D-Implementation-Plan.md`.

Đề xuất mặc định: **Unity + C# → Windows offline**, một agent thực hiện chính, người dùng kiểm tra Editor và chơi thử. Đây là lựa chọn cho bản plan, chưa phải xác nhận máy đã có đúng engine/module. Chưa có asset nào được tải hoặc import trong bước lập tài liệu này.

## 1. Chọn engine: cài một bộ đủ dùng

| Công cụ | Ưu điểm cho bài này | Nhược điểm / chi phí | Khi nên chọn |
|---|---|---|---|
| **Unity + C# — mặc định** | Editor trực quan, physics/audio/UI tích hợp, tiện chỉ ra GameObject SoundOn/Off khi báo cáo | Cài nặng; code đúng vẫn có thể sai scene/prefab/Inspector; version và package phải khớp | Đã quen Unity, môn học dùng Unity hoặc muốn Windows player |
| **Godot + GDScript** | Engine có hệ thống 2D, license MIT; scene/script dạng text thuận tiện diff | Phải học Node/Signal; agent dễ lẫn API Godot 3 và 4; cần export template đúng bản | Bắt đầu mới, thích công cụ nhẹ và engine không ràng buộc phí |
| **Phaser + TypeScript** | Dùng JS/TS, thuận nền tảng web; agent sửa code/bundled asset thuận tiện | Thiên về web, tự tổ chức nhiều UI; trình duyệt có audio unlock; native package cần lớp bổ sung | Giảng viên chấp nhận browser demo và muốn tận dụng TypeScript |

Đánh giá độ tiện, thời gian và độ khó là nhận định cho phạm vi demo này. Tài liệu gốc: [Unity release support](https://unity.com/releases/unity-6/support), [Godot features](https://godotengine.org/features/), [Godot license](https://godotengine.org/license/), [Phaser overview](https://docs.phaser.io/).

**Quy tắc chọn:** nếu có project Unity đang dùng cho môn này, giữ engine đó và bổ sung một scene/module sau khi agent kiểm tra repo. Nếu hoàn toàn mới, dùng Unity 6.3 LTS đã cài ổn định; đóng băng exact patch sau preflight. Đừng mở project cũ bằng engine khác major chỉ để “lên mới nhất”. Trang Unity hiện liệt kê 6.3 LTS; không cần cài thêm một patch khác nếu máy đã có bản phù hợp và build tốt. [Nguồn Unity](https://unity.com/releases/unity-6/support).

Nếu đổi sang Godot/Phaser, giữ nguyên luật chơi và R01–R09, nhưng phải chuyển file map/task kỹ thuật trước khi code. Plan hiện tại không phải một plan Godot/Phaser đã được triển khai chi tiết.

## 2. Bộ công cụ tối thiểu và phần tùy chọn

| Công cụ | Bắt buộc? | Vai trò / ưu điểm | Nhược điểm / lưu ý |
|---|---|---|---|
| Unity Hub + một Unity Editor | Có, nếu chọn Unity | Quản lý phiên bản và mở project | Không mở nhiều Editor cùng project |
| Hỗ trợ build Windows phù hợp trong Hub | Có cho output Windows | Có bản chạy độc lập để báo cáo | Cài đúng module theo backend; chưa cần IL2CPP cho demo này |
| VS Code + Unity extension của Microsoft | Có một IDE; đây là mặc định | Viết/debug C#, dễ dùng cùng agent | Cần cấu hình package/external editor cho IntelliSense |
| Visual Studio Community | Thay thế VS Code | Debug C# và Unity tích hợp thuận tiện | Cài lớn hơn; chỉ cài nếu thích IDE này |
| Git | Nên có ngay | Checkpoint, diff, quay về commit tốt | Phải track `.meta`; không commit thư mục cache Unity |
| Một coding agent đang sử dụng được | Có nếu muốn AI triển khai | Viết code, test, docs, đọc log | Cần đủ quyền truy cập folder; khả năng điều khiển Editor tùy môi trường |
| Audacity | Tùy chọn | Cắt SFX, chỉnh âm lượng, tạo beep, cắt loop | Thêm công đoạn nếu asset đã phù hợp |
| Piskel | Tùy chọn, chạy web | Chỉnh sprite/pixel animation đơn giản | Không cần nếu dùng PNG có sẵn; không hợp xử lý ảnh tả thực |
| Công cụ quay màn hình có system audio | Nên có | Video demo dự phòng | Kiểm tra file quay có tiếng game, không chỉ mic |

Audacity hỗ trợ chỉnh/sửa audio và xuất các định dạng phổ biến; Piskel là sprite editor online có xuất PNG/spritesheet. [Audacity](https://www.audacityteam.org/), [Piskel](https://www.piskelapp.com/).

**Không cần cho phạm vi này:** Blender, Photoshop trả phí, FMOD/Wwise, Docker, database, backend, máy chủ, Android Studio/emulator, nhiều AI agent đồng thời. Nếu công cụ đã có, không cài lại chỉ vì xuất hiện trong checklist.

### 2.1 Cài Unity và tạo project

1. Mở Unity Hub; nếu chưa có, theo [hướng dẫn cài Hub](https://docs.unity.com/en-us/hub/install-hub).
2. Trong Installs kiểm tra Editor phù hợp, thêm module build cần dùng qua Hub. Với project cũ đọc `ProjectSettings/ProjectVersion.txt` trước khi mở. [Quản lý Editor](https://docs.unity.com/en-us/hub/add-editor).
3. Project mới: tạo `CoreGuardDemo` bằng template 2D Core/Built-in nếu có; nếu Hub chỉ cung cấp Universal 2D phù hợp thì giữ URP đã sinh, không tự đổi render pipeline sau đó. Không bật ánh sáng phức tạp.
4. Lưu ở đường dẫn đơn giản như `D:\Game2D\CoreGuardDemo`, tránh thư mục sync đang khóa asset. Đây là ví dụ đường dẫn, không phải folder đã tồn tại.
5. Trong Package Manager kiểm tra Input System, uGUI/TMP, Test Framework; cài bản tương thích Editor, ghi lại manifest/lockfile. Dùng Input System thống nhất cho game và UI, không trộn handler legacy nếu không cần.
6. Thiết lập Asset Serialization = Force Text và Version Control = Visible Meta Files để dễ quản lý git.
7. Tạo scene `Assets/_Game/Scenes/Main.unity`; đưa scene vào danh sách build. Với Unity 6, kiểm tra Build Profiles; các bản cũ có thể dùng Build Settings.
8. Build Windows sớm bằng backend mặc định phù hợp; nếu chọn Mono thì không thêm gánh nặng IL2CPP/toolchain khi chưa cần.

### 2.2 Kết nối VS Code

Cài extension **Unity** do Microsoft phát hành. Tài liệu Microsoft ghi extension này cài các dependency như C# Dev Kit; trong Unity dùng package **Visual Studio Editor** phiên bản tương thích (tài liệu yêu cầu ít nhất 2.0.20). Package tên **Visual Studio Code Editor** cũ đã ngừng được duy trì. Chọn VS Code tại Preferences → External Tools → External Script Editor. [Hướng dẫn chính thức](https://code.visualstudio.com/docs/other/unity).

Kiểm tra bằng cách mở một script từ Unity, xem có gợi ý kiểu `MonoBehaviour` và thử attach debugger. Không coi dấu gạch đỏ ở IDE là kết luận compile lỗi nếu Unity Console chưa được đối chiếu.

### 2.3 Kiểm tra công cụ trên Windows

Chạy các lệnh độc lập trong PowerShell ở root project:

```powershell
git --version
Get-Content .\ProjectSettings\ProjectVersion.txt
Get-Content .\Packages\manifest.json
git status --short
```

Lệnh đọc ProjectVersion/manifest chỉ chạy sau khi project đã được tạo. Nếu không dùng Git repo sẵn có, khởi tạo Git trong project mới sau khi thêm `.gitignore` Unity. Không chạy `git init` trong thư mục cha chứa nhiều project.

### 2.4 Nếu chọn engine khác

**Godot:** tải bản stable Standard từ [Godot](https://godotengine.org/), dùng GDScript để không cần .NET; tải export templates đúng phiên bản. Chuyển `GameObject` thành Node/Scene, `Collider2D trigger` thành Area2D, C# component thành GDScript, AudioSource thành AudioStreamPlayer. Giữ hai cặp node button On/Off khác nhau để bám đề. Không cài Unity song song chỉ để phục vụ game này.

**Phaser:** dùng Node/npm và template TypeScript từ [hướng dẫn cài chính thức](https://docs.phaser.io/phaser/getting-started/installation). Khóa version dependency và commit lockfile sau khi scaffold. Bundle asset tại local, không phụ thuộc CDN trong buổi báo cáo. Có Start button để unlock audio từ tương tác người dùng; chạy qua local HTTP server, không mở `index.html` bằng `file://`. Dùng docs đúng major thực tế đã cài; không đưa code Phaser 3 vào project major khác mà không đối chiếu API.

## 3. Asset đề xuất: một style chính, số lượng nhỏ

### 3.1 Nguồn cụ thể đã kiểm tra trang vào 12/09/2026

| Nguồn | Dùng cho gì | Cách chọn trong project |
|---|---|---|
| [Kenney — Top-down Tanks Redux](https://kenney-assets.itch.io/top-down-tanks-redux) | Thân/nòng A và B, đạn/tên lửa, thùng/vật thể nền, vụ nổ | Một xe xanh, một xe đỏ, một nhóm nền cùng bộ; ưu tiên PNG riêng |
| [Kenney — Top-Down Tanks](https://kenney.nl/assets/top-down-tanks) | Phương án asset xe tăng thay thế | Chọn một bộ nhất quán, không import cả hai bộ chỉ để thử |
| [Kenney — UI Pack](https://kenney.nl/assets/ui-pack) | Panel, nền nút/ô kỹ năng | Dùng panel/nút cơ bản; nếu thiếu icon loa, làm icon đơn giản hoặc label rõ |
| [Kenney — Sci-fi Sounds](https://kenney.nl/assets/sci-fi-sounds) | Bullet, rocket, mine, shield/EMP | Nghe và chọn vài clip ngắn khác nhau; không giả định pack có đúng filename game cần |
| [Kenney — Interface Sounds](https://kenney.nl/assets/interface-sounds) | Click/pickup/beep tham khảo | Chọn beep đủ ngắn; nếu không phù hợp, tự tạo beep bằng Audacity |
| [Abstraction / Tallbeard — FREE Music Loop Bundle](https://tallbeard.itch.io/music-loop-bundle) | Một loop BGM | Chọn đoạn electronic/chiptune ít dày, nghe được SFX phía trên |

Kenney xác nhận asset trên trang asset dùng CC0; các trang UI/Sci-fi/Interface cũng ghi CC0. Lưu license kèm bản tải; attribution không bắt buộc theo thông tin Kenney nhưng nên ghi credit trong README cho báo cáo. [Kenney FAQ](https://kenney.nl/support).

Trang Tallbeard ghi license CC0 cho music pack và gợi ý credit tên nghệ sĩ **Abstraction**; đây là nguồn BGM, không dùng một jingle thắng/thua làm toàn bộ nhạc nền. Chưa chốt track/filename trước khi nghe và tải. [Trang tác giả](https://tallbeard.itch.io/music-loop-bundle).

### 3.2 Danh sách tải tối thiểu

| Nhóm | Số lượng mục tiêu | Logical name sau khi chọn |
|---|---:|---|
| Xe A, B | 4 sprite nếu tách thân/nòng | `player_body`, `player_turret`, `enemy_body`, `enemy_turret` |
| Đạn và tên lửa | 2 sprite | `bullet`, `rocket` |
| Mìn A, mìn X | 2 sprite hoặc một sprite tint khác nhau | `player_mine`, `hazard_mine` |
| X/Y/Z và lõi | 3–4 sprite/hình cơ bản | `emp_trap`, `supply_cell`, `core` |
| Nền/viền map | 2–4 sprite | `floor`, `border` |
| UI | 1 panel, 1 button base, icon/label các trạng thái | Không cần tải đủ hàng trăm icon |
| SFX | 7–9 clip | `bullet_fire`, `rocket_launch`, `mine_drop`, `explosion`, `shield`, `emp`, `pickup`, `alert`, tùy chọn `hit` |
| BGM | 1 loop | `arena_loop` |

Tên trên là **tên logic dự kiến**, không khẳng định tên file gốc trong pack. Agent phải inspect thư mục và ghi tên file thực tế vào asset register trước khi gán reference.

### 3.3 Import và xử lý

1. Download từ trang tác giả; chỉ giải nén những file sẽ dùng vào `Assets/_Game/Art` và `Assets/_Game/Audio`. Giữ license và tên pack trong register.
2. PNG riêng: import Sprite (2D and UI), Sprite Mode Single; pivot thân/nòng phải phù hợp xoay, không để nòng quay quanh mép ảnh.
3. Chọn scale nhất quán theo kích thước asset thực tế. Ví dụ thân xe rộng 64 px có thể đặt 64 PPU cho khoảng 1 world unit; đây là ví dụ, không áp dụng cứng nếu file là 128/256 px.
4. Pixel art dùng Point nếu chọn style pixel; bộ sprite mượt dùng Bilinear phù hợp. Không chọn Point cho mọi asset chỉ vì game 2D.
5. Collider bao thân, không bao cả bóng đổ/nòng dài; rocket/mine hitbox kiểm tra trong Scene view.
6. Shield/EMP/vùng cấm dùng sprite vòng tròn và particle đơn giản sinh trong engine; không cần asset pack phép thuật lớn.
7. SFX nên cắt ngắn, bỏ khoảng im lặng đầu; cảnh báo khoảng 0,15–0,2 s để tách được 4 beep. Dùng WAV cho SFX ngắn, BGM OGG nếu phù hợp pipeline.
8. Audacity có thể tạo tone beep; fade ngắn đầu/cuối tránh tiếng click. Khi dùng nhạc, nghe điểm loop; không chọn đoạn có cú dừng hoặc fade-out quá dài.
9. Nghe bản build với BGM bật: tiếng bắn/cảnh báo vẫn rõ; hạ gain chứ không đẩy mọi clip lên cực đại.
10. Khi đổi tên/move asset đã import, làm trong Unity hoặc di chuyển kèm `.meta` và kiểm tra reference. Không tái tạo GUID thủ công.

### 3.4 Mẫu asset register

Mỗi file thực tế dùng điền một dòng tại `docs/asset-register.md`:

| Local path | Original filename | Pack / author | Source URL | License | Vai trò | Chỉnh sửa |
|---|---|---|---|---|---|---|
| Ghi đường dẫn thực tế sau tải | Giữ tên gốc | Tác giả thật | Trang tác giả | Theo license tải kèm | Bullet / BGM / UI | Trim / tint / none |

Không cần AI image generation cho bản đầu: sprite có sẵn giúp giữ silhouette, pivot và màu nhất quán. Nếu muốn hình title bằng AI, chỉ làm sau khi đủ R01–R09; title không được thay cho gameplay asset kiểm thử được.

## 4. Cách chia việc giữa bạn và AI agent

| Công việc | Agent làm | Bạn kiểm tra |
|---|---|---|
| Preflight | Đọc repo/version/packages, báo reusable components | Đúng project/engine đang dùng cho môn |
| Code | Gameplay, event/state, editor builder, test | Quy tắc game và cảm giác điều khiển |
| Scene/prefab | Tạo bằng Unity Editor API hoặc sửa trong editor khi có khả năng | Inspector reference, layer, scene view, Console |
| Asset | Liệt kê file, import/config/reference | Asset đúng style, audio dễ nghe |
| Tests/build | Chạy được thì lưu log/XML; không chạy được phải ghi rõ | Chạy Editor/player thật, xác nhận âm thanh/hình |
| Báo cáo | README, acceptance matrix, kịch bản demo | Trình bày hiểu được lý do và chỉ ra component |

**Một agent chính sửa repo tại một thời điểm.** Có thể dùng Claude/Gemini/Codex làm reviewer ở lượt riêng với diff/log, nhưng không cho nhiều bên cùng sửa `Main.unity`, package hoặc prefab. Nếu chuyển agent, gửi cùng plan, progress, git status và test evidence.

AI agent có file access không đồng nghĩa điều khiển được Unity Editor. Khi thiếu tích hợp editor, dùng menu `Tools/Core Guard/Build Demo Scene` do agent tạo, rồi người dùng bấm menu và Play. Agent nên tạo scene/prefab qua Unity API để tránh lỗi GUID/YAML thủ công.

Không cần thêm plugin/MCP Unity trước khi bắt đầu. Nếu đã có kết nối editor hoạt động, có thể tận dụng; việc cài tích hợp mới không phải điều kiện bắt buộc của demo.

## 5. Luồng triển khai với agent

### Bước A — Chuẩn bị folder và context

1. Tạo/mở đúng project Unity, kiểm tra phiên bản.
2. Copy hai file Markdown này vào `docs/` trong root project.
3. Mở **root project** trong IDE/agent, nơi có `Assets`, `Packages`, `ProjectSettings`; không chỉ mở folder `Scripts`.
4. Nếu dùng agent trong khung chat không có quyền đọc folder, đính kèm plan và các file liên quan task đang làm; không giả định nó đã đọc máy của bạn.
5. Bắt đầu bằng preflight. Engine/project mới hay mở rộng project cũ là quyết định cần chốt trước khi agent tạo/sửa scene.

### Bước B — Prompt đầu tiên: xác nhận môi trường

Dùng cho Codex, Claude hoặc Gemini; không cần cú pháp đặc thù của một CLI:

```text
Read docs/01-Game-2D-Implementation-Plan.md and docs/02-Tools-Assets-Agent-Guide.md, then inspect this repository and its applicable instructions. Confirm the Unity version, packages, build target, existing reusable gameplay and uncommitted changes. Map R01–R09 to the planned tasks. Report only concrete blockers and the proposed Task 1 scope; do not modify gameplay yet. Explain findings concisely in Vietnamese.
```

Bạn xem kết quả: nếu agent đang ở nhầm repo/version, sửa ngay ở bước này. Nếu project đã có game, yêu cầu nó chỉ rõ phần reuse và nơi tạo scene mới; không chấp nhận “rebuild toàn bộ cho sạch”.

### Bước C — Prompt thực thi sau khi chốt Unity và project

```text
Approved: implement Core Guard using the two documents in docs/. Work sequentially from T1 to T7 in this repository, preserving unrelated changes and the confirmed Unity version. Do not use subagents, push, merge, or change platforms. Create real scene/prefab wiring through Unity Editor APIs where practical. Complete each task's acceptance checks, update docs/progress.md with evidence and the next action, and commit only relevant files before proceeding. If Unity execution or a manual audio/visual check is unavailable, mark it NOT RUN and give exact steps; never claim it passed. Keep Vietnamese progress updates concise and persist a recoverable checkpoint before stopping or nearing limits.
```

Prompt này là lựa chọn để **bạn đưa cho agent khi muốn bắt đầu**, không phải xác nhận hiện tại rằng game đã được triển khai. Không cần hỏi lại từng lựa chọn màu/tên file; chỉ cần dừng khi có mâu thuẫn engine, phạm vi, nguy cơ mất thay đổi hoặc requirement giảng viên chưa rõ.

### Bước D — Sau mỗi task

1. Đọc báo cáo ngắn: files changed, behavior, tests run, manual checks còn thiếu.
2. Mở Unity Console, đảm bảo không có compile error; chạy scene và thử hành vi mới.
3. Nếu cần batchmode trên cùng project, đóng Unity Editor trước; không mở hai process tranh project lock.
4. Chỉ chuyển milestone khi phần phụ thuộc đã chạy được. Nếu agent chỉ viết code mà chưa nối scene, task chưa hoàn tất.
5. Sau T1 có build sơ bộ; sau T5 đủ logic đề; sau T7 có bản báo cáo được.

### Bước E — Prompt resume khi hết phiên/limit

```text
Read both project plans in docs/, docs/progress.md, git status and the latest relevant commits. Verify the saved checkpoint, then resume from the next incomplete step without redoing completed work or overwriting user changes. Keep the confirmed engine and scope. Record fresh test evidence and a recoverable checkpoint before stopping. Reply concisely in Vietnamese.
```

Checkpoint phải là file trong repo, không chỉ lời hứa trong chat. Nếu phiên bị cắt đột ngột, agent sau phải đọc diff chưa commit để xác định việc thực tế, không tin tuyệt đối vào checklist cũ.

### Bước F — Prompt reviewer

```text
Review the current implementation against R01–R09 in docs/01-Game-2D-Implementation-Plan.md. Focus on observable compliance, the four-button state mapping, exactly four alerts per zone-entry event, damage deduplication, the six X/Y/Z effects, shield/EMP behavior, pause/retry cleanup and standalone build evidence. Do not edit files. Report concrete defects with file references, reproduction steps and severity in Vietnamese; separate verified failures from untested risks.
```

### Bước G — Prompt chốt báo cáo

```text
Freeze features and perform T7 acceptance for the Windows demo. Verify every R01–R09 in the actual player where available, record PASS/FAIL/NOT RUN honestly, update README and docs/acceptance.md, and package the complete player folder. Prepare the five-minute demonstration script and list any checks I must perform on the laptop. Do not push, merge, publish, or add features.
```

## 6. Bằng chứng kiểm thử nên yêu cầu

| Loại | Kiểm tra phù hợp | Không thể chứng minh bằng loại này |
|---|---|---|
| EditMode | Damage, speed modifiers, cooldown, state transition, alert scheduler | Sprite đã gán đúng, loa có phát tiếng |
| PlayMode | Trigger vào vùng, prefab binding, damage-once, shield hit, reset | Bản Windows có đủ file, âm lượng có dễ nghe |
| Chơi build Windows | Bấm nút/di chuyển, nghe đúng 4 beep, độc lập nhạc/SFX, HUD | Không thay thế mọi edge case logic |
| Video/screenshot | Bằng chứng trình diễn và dự phòng | Không chứng minh code mới hơn video đã được test |

Khi agent đưa lệnh Unity test, nó phải dùng đường dẫn Editor thực tế, project tuyệt đối và ghi log/XML ra file. Không copy mù đường dẫn Editor từ máy khác. Không thêm `-nographics` vào mọi lượt kiểm tra: test hình ảnh/render phải có graphics phù hợp. Exit code thành công mà thiếu XML hoặc test không chạy không được gọi là pass.

Mẫu `docs/acceptance.md` cần các cột: Requirement ID, scenario, expected, actual, status, evidence path, thời điểm, Unity version/build commit. Các số test pass phải đọc từ kết quả thật, không phải số test đã viết.

## 7. Các lỗi đáng chặn sớm

| Lỗi | Dấu hiệu | Cách phòng trong plan |
|---|---|---|
| Nút On/Off bị hiểu ngược | Click SoundOff lại bật tiếng | Dùng bảng trạng thái, 4 object đúng tên và label hành động |
| Alert phát mỗi frame | Tiếng dồn dập hoặc vô hạn | On-enter + root occupancy + 4-beep job |
| Mute SFX làm tắt nhạc | BGM biến mất cùng tiếng bắn | Hai state độc lập, không mute AudioListener toàn cục |
| Rocket/mine double damage | B chết nhanh bất thường | Resolve-once và dedupe root damageable |
| Shield chỉ có hình | Đạn vẫn trừ máu khi khiên đang hiện | Defense check trước damage; test số hit |
| E4 không nhìn thấy | Đi qua Y khi chưa có shield | Bật Q trước khi đi qua trong kịch bản demo |
| Slow/boost lệch sau reset | Speed không về 4 | Tính từ base + timer riêng, reset tập trung |
| UI click gây bắn | Vừa tắt nhạc vừa phóng rocket | Chặn input khi pointer trên UI |
| Agent làm xong script nhưng scene trống | Play không có gì | Gate mỗi task yêu cầu chạy scene/prefab thật |
| Chỉ có exe, thiếu data | Sang máy/thư mục khác không chạy | Zip toàn bộ player folder, thử bản giải nén |
| Tới tối thứ Tư vẫn thêm feature | Không còn thời gian nghe/build | Feature freeze; chỉ sửa lỗi chặn nghiệm thu |

## 8. Checklist trước buổi báo cáo

- [ ] Biết đúng deadline/engine/nền tảng giảng viên chấp nhận; mặc định của tài liệu là 17/09/2026, Unity/Windows.
- [ ] Build mới nhất mở offline được từ folder đã giải nén.
- [ ] Loa và system audio hoạt động; nhạc nền không lấn cảnh báo.
- [ ] Trình diễn được 3 vũ khí, Q/E và E1–E6 trong tối đa vài phút.
- [ ] Đếm được 4 beep với một B đi vào vòng; không chạy giả bằng nút phát âm trực tiếp.
- [ ] SoundOff/SoundOn và MusicOn/MusicOff thay đúng vị trí/kích thước.
- [ ] Có README, asset register/credits, acceptance và video có tiếng.
- [ ] Bạn giải thích được luồng Input → action → collision/state → HUD/audio và vì sao alert không dùng Update/OnTriggerStay.

Bộ tài liệu này giúp triển khai và nghiệm thu game. Nguồn asset đã được kiểm tra ở cấp trang giới thiệu/license; filename cụ thể, chất lượng âm thanh, tương thích import và build thực tế chỉ xác nhận sau khi tải và chạy project.
