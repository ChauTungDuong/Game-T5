# Core Guard

A small, single-scene 2D arena prototype built with Unity **6000.3.23f1**, Universal 2D, and the Input System.

After checkout, let Unity finish importing `Assets/_Game/Art/Kenney` and `Assets/_Game/Audio/Kenney`, then run **Core Guard → Configure Project** once. Open `Assets/_Game/Scenes/Main.unity` and press Play. Click **Start**, move with **WASD or arrow keys**, aim with the mouse, select Bullet/Rocket/Mine with **1/2/3**, fire with the left mouse button, activate Shield/EMP with **Q/E**, and interact with X/Y/Z. **Esc** pauses/resumes. The result panel's **Retry** button or **R** resets the run after a win/loss; **F1** opens the repeatable Demo Mode panel.

The player starts with 100 HP, 50 Armor and 0 Coins; the core has 100 HP. Enemies spawn every five seconds, rotate through four gates, and stop at six living enemies. Each enemy reaching the core deals 20 damage once. The timer lasts 90 seconds; player/core death takes priority over timeout. Movement, aiming, spawning and time stop outside Playing.

This checkpoint contains the T1 arena/session flow plus source implementations for T2–T5: three weapons, enemy projectiles, Shield/EMP, X/Y/Z interactions, audio toggles, four-beep alerts and the restricted zone. T6 now adds weapon/cooldown HUD fields and an F1 Demo Mode with repeatable scenarios. Unity scene import, full test execution and build verification after these changes are still pending, so the feature set is not yet acceptance-complete.

The visual/audio pass uses Kenney tank sprites for the player and enemies, Kenney particle sprites for core/mine/combat feedback, Kenney Game Icons for state markers and audio toggles, Kenney sci-fi UI panels/buttons and font, and Kenney Sci-fi Sounds for weapon, defense, alert and ambient-loop audio. See `docs/asset-register.md` for the exact local paths and licenses.

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
