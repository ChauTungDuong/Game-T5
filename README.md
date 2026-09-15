# Core Guard

A small, single-scene 2D arena prototype built with Unity **6000.3.23f1**, Universal 2D, and the Input System.

After checkout, let Unity finish importing `Assets/_Game/Art/Kenney` and `Assets/_Game/Audio/Kenney`, then run **Core Guard → Configure Project** once. Open `Assets/_Game/Scenes/Main.unity` and press Play. Click **Start**, move with **WASD or arrow keys**, aim with the mouse, select Bullet/Rocket/Mine with **1/2/3**, fire with the left mouse button, activate Shield/EMP with **Q/E**, and interact with X/Y/Z. **Esc** pauses/resumes. The result panel's **Retry** button or **R** resets the run after a win/loss; **F1** opens the repeatable Demo Mode panel.

The player starts with 100 HP, 50 Armor and 0 Coins; the core has 100 HP. Enemies spawn every five seconds, rotate through four gates, and stop at six living enemies. Each enemy reaching the core deals 20 damage once. The timer lasts 90 seconds; player/core death takes priority over timeout. Movement, aiming, spawning and time stop outside Playing.

This checkpoint contains the playable Unity Editor demo: three weapons, equal-sized player/enemy tanks, enemy projectiles, Shield/EMP, deterministic X/Y/Z cycles, audio toggles, four-beep alerts, the restricted zone, compact HUD, a 3–2–1 start countdown, result artwork/audio, animated explosions on tank death and detonation, and repeatable F1 Demo Mode. Configure the project once from `Core Guard → Configure Project`, then open `Assets/_Game/Scenes/Main.unity` and press Play. Standalone player export remains outside the current scope.

The visual/audio pass uses Kenney tank sprites for the player and enemies, Kenney particle sprites for core/mine/combat feedback, Kenney Game Icons for state markers and audio toggles, Kenney sci-fi UI panels/buttons and font, and Kenney Sci-fi Sounds for weapon, defense, alert and ambient-loop audio. Selected user-provided result/countdown/explosion artwork and feedback clips live under `Assets/_Game/Art/Provided/` and `Assets/_Game/Audio/Provided/`. See `docs/asset-register.md` for exact local paths, provenance and license notes.

## Rebuild the scene and player

Use **Core Guard → Configure Project** to add missing arena objects, create the required assets, and wire references. The command preserves existing Main content and unrelated build entries and can be rerun while another additive scene is active. It uses the existing project Input Actions asset in `Assets/Settings/` for both gameplay and UI.

This checkpoint is intended to run as a demo inside the Unity Editor; no standalone player build is required. The Canvas uses 1280×720 Scale With Screen Size and supports 1920×1080.

## Verification

Run the `CoreGuard.Tests` EditMode and PlayMode suites from Unity's Test Runner. They exercise real components, two-second physics travel, damage contracts, spawn cadence, final-tick ordering, reset values, input commands, HUD buttons and scene-builder idempotence.

Batch example (adjust the Editor executable path to your installation):

```powershell
rtk proxy "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\Game-T5" -runTests -testPlatform EditMode -testFilter "CoreGuard.Tests" -testResults "D:\Game-T5\Logs\editmode.xml" -logFile "D:\Game-T5\Logs\editmode.log"
```

Use `-testPlatform PlayMode` for physics, input and HUD tests. Test logs are local ignored artifacts. See `docs/progress.md` and `docs/acceptance.md` for the current verification status and remaining manual checks.
