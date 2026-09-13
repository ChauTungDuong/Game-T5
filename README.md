# Core Guard

A small, single-scene 2D arena prototype built with Unity **6000.3.23f1**, Universal 2D, and the Input System.

Open `Assets/_Game/Scenes/Main.unity` and press Play. Click **Start**, move with **WASD or arrow keys**, and aim with the mouse. **Esc** pauses/resumes. The result panel's **Retry** button or **R** resets the run after a win/loss.

The player starts with 100 HP, 50 Armor and 0 Coins; the core has 100 HP. Enemies spawn every five seconds, rotate through four gates, and stop at six living enemies. Each enemy reaching the core deals 20 damage once. The timer lasts 90 seconds; player/core death takes priority over timeout. Movement, aiming, spawning and time stop outside Playing.

This T1 checkpoint contains movement, stats, enemies, the core, session flow and a live HUD. Weapons, shield/EMP behavior, interactions and audio are later tasks. Their input action names are reserved, but no combat is implemented, so ordinary unattended runs lose when enough enemies reach the core.

## Rebuild the scene and player

Use **Core Guard → Configure Project** to add missing arena objects, create the required assets, and wire references. The command preserves existing Main content and unrelated build entries and can be rerun while another additive scene is active. It uses the existing project Input Actions asset in `Assets/Settings/` for both gameplay and UI.

Use **Core Guard → Build Windows** to build the complete player folder at `Builds/Windows/CoreGuard/`. Keep the `.exe`, its `_Data` folder, and companion DLLs together. The Canvas uses 1280×720 Scale With Screen Size and supports 1920×1080.

## Verification

Run the `CoreGuard.Tests` EditMode and PlayMode suites from Unity's Test Runner. They exercise real components, two-second physics travel, damage contracts, spawn cadence, final-tick ordering, reset values, input commands, HUD buttons and scene-builder idempotence.

Batch example (adjust the Editor executable path to your installation):

```powershell
rtk proxy "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\Game-T5" -runTests -testPlatform EditMode -testFilter "CoreGuard.Tests" -testResults "D:\Game-T5\Logs\editmode.xml" -logFile "D:\Game-T5\Logs\editmode.log"
```

Use `-testPlatform PlayMode` for physics, input and HUD tests. Test/build logs and generated builds are local ignored artifacts. See `docs/progress.md` and `docs/acceptance.md` for the current verification status and remaining manual checks.
