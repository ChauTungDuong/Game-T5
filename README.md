# Core Guard

A 2D top-down arena defense prototype built with Unity **6000.3.23f1**, Universal 2D, and the new Input System. The player defends a central Core against incoming enemies while dodging projectiles, utilizing active defense abilities, and collecting dynamic power-up traps.

## Quick Start (Unity Editor Demo)

1. Open the project in Unity **6000.3.23f1**.
2. Run **Core Guard → Configure Project** from the top menu once to verify scene setup and wire all references.
3. Open `Assets/_Game/Scenes/Main.unity` and press **Play**.
4. Click **Start** to begin the match.

## Controls

| Action | Input Binding | Description |
|---|---|---|
| **Move** | `W, A, S, D` or Arrow Keys | Normalized 8-directional Rigidbody2D movement (base speed: 4.0 units/s) |
| **Aim** | Mouse Pointer | Smooth turret rotation tracking the cursor |
| **Fire** | Left Mouse Button | Fires active weapon towards cursor (suppressed over UI) |
| **Select Weapon** | `1`, `2`, `3` | `1`: Bullet (rapid single-target, cooldown 0.2s)<br>`2`: Rocket (high damage AoE radius 1.5, cooldown 1.5s)<br>`3`: Mine (deployable trap, 0.5s arm delay, 50 AoE damage, max 3) |
| **Shield Defense** | `Q` | Absorbs up to 3 enemy projectiles or lasts 3.0s (cooldown 8.0s) |
| **EMP Blast** | `E` | Emits shockwave in radius 3.0, stunning enemies for 2.0s (cooldown 6.0s) |
| **Demo Director** | `F1` | Toggles repeatable scenario controls (Spawn Shooter, Zone Enemy, Cluster, Reset) |
| **Pause / Resume** | `Esc` | Freezes/resumes clock, movement, aiming, and weapon timers |
| **Retry** | `R` or UI Button | Centrally resets stats, Core HP, clock, position, and clears all projectiles/mines |

## Gameplay Mechanics

- **Player Stats:** 100 HP, 50 Armor, 0 Coins. Incoming damage follows an Armor-first contract.
- **Core:** 100 HP located at center `(0, 0)`. Enemies contacting the Core deal 20 damage once and self-destruct.
- **Enemies:** Spawn every 5.0s rotating through 4 gates (max 6 active). When within 7.0 units, enemies shoot projectiles at the player (speed 7.5, damage 10).
- **X/Y/Z Interaction Objects:** Randomly spawn in arena bounds, visible for 5s, then hidden for 5s.
  - `X (Damage Trap)`: Deals -20 HP and -10 Armor.
  - `Y (EMP Field)`: Slows player to 50% for 3s and breaks active Shield.
  - `Z (Supply Boost)`: Grants +10 Coins and boosts movement speed to 150% for 4s.
- **Audio & Alerts:** Independent SFX and BGM toggle buttons. Enemies entering the 3-unit restricted zone trigger a deterministic 4-beep warning sequence via a FIFO queue.

## Architecture

- **`GameSession`**: Central coordinator for match lifecycle (Ready, Playing, Paused, Won, Lost), timing, and centralized clean reset.
- **`PlayerMotor`**: 2D physics movement with bound clamping, analog input normalization, and speed modifiers from `StatusEffects`.
- **`WeaponController` & `DefenseController`**: Modular combat and defensive abilities with cooldown tracking and event triggers.
- **`InteractionCycleController`**: Deterministic lifecycle and placement generator for dynamic arena items.
- **`AudioService` & `ForbiddenZone`**: Dedicated audio source management and FIFO queue alert sequencer.
- **`HudPresenter` & `DemoDirector`**: Non-intrusive sci-fi HUD layout (1280×720 & 1920×1080 responsive) and director debug tools.
- **`DemoSceneBuilder`**: Completely idempotent builder ensuring consistent scene configuration without manual linking.

## Verification & Testing

The project is backed by comprehensive automated test suites covering all contracts, physics travel, input gating, UI flows, and idempotence.

- **EditMode Suite:** **59/59 passed** (`Logs/editmode.xml`)
- **PlayMode Suite:** **13/13 passed** (`Logs/playmode.xml`)
- **Total:** **72/72 tests passed 100%** on Unity `6000.3.23f1`.
- **Idempotence:** `BuildDemo.ConfigureProject` verified twice with 0 diff on re-run.

Batch command example:
```bash
/home/tuananh/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
/home/tuananh/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/playmode.log
```

## Asset Credits & Licenses

All visual and audio assets are CC0 Public Domain assets created by Kenney ([kenney.nl](https://kenney.nl)):
- **Tanks:** Top-down Tanks Remastered (CC0)
- **Particles:** Kenney Particle Pack (CC0)
- **Icons:** Kenney Game Icons (CC0)
- **UI & Fonts:** Kenney UI Pack - Space Expansion & Kenney Future Fonts (CC0)
- **Audio:** Kenney Sci-fi Sounds (CC0)

Detailed mappings and individual license files are preserved in `docs/asset-register.md`.

## Known Limitations

- Physical keyboard and mouse feel, audio speaker levels, and visual inspection of animations require interactive GUI playback in the Unity Editor.
- Standalone executable export is intentionally omitted per project scope (Editor-only demo).

## Should add later

Không có — mọi asset cần cho bản demo đã có hoặc có placeholder hợp lệ.

