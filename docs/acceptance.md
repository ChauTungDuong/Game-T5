# Core Guard Acceptance

> Current scope is the Unity Editor demo. Standalone Windows build/export is not required for this handoff.

Verification date: 2026-09-15. Unity: `6000.3.23f1`. Automated results are from the complete suites after the final gameplay patch.

| Requirement ID | Scenario | Expected | Current evidence | Status |
|---|---|---|---|---|
| R01 | A attacks with each weapon | Distinct short SFX for accepted attacks | Weapon/audio hooks are wired and imported clips are assigned by `DemoSceneBuilder`; listening check remains for the owner | PARTIAL — manual listening |
| R02 | B enters restricted zone | One four-beep warning sequence | Alert contract passes; occupancy dedupe and FIFO four-beep scheduling are covered by PlayMode tests; listening check remains | PARTIAL — manual listening |
| R03 | Toggle SFX | SoundOff/SoundOn alternate consistently | Toggle pair contract passes in PlayMode; physical click/audio check remains | PARTIAL — manual UI/audio |
| R04 | Toggle music | MusicOn/MusicOff controls only music | Toggle pair contract passes in PlayMode; physical click/audio check remains | PARTIAL — manual UI/audio |
| R05 | Move and aim A | Normalized movement, mouse aim, bounds | Physics and Input System tests pass; owner should confirm physical keyboard/mouse feel | PARTIAL — manual input |
| R06 | Bullet, Rocket, Mine | Three damaging attacks and cooldowns | Combat contracts pass, including projectile hit-once behavior, rocket AoE, mine arming and mine-limit feedback | PASS automated |
| R07 | Shield and EMP | Shield blocks projectiles; EMP disables enemies | Defense and presentation tests pass, including shield ring and EMP affected count | PASS automated |
| R08 | X/Y/Z interactions | Six observable numeric/effect changes | Interaction contracts pass; Y is persistent per-entry, with leave/re-enter reset implemented | PASS automated |
| R09 | A state changes | Live HP, Armor, Coins, weapon/cooldown HUD | HUD test passes; live speed and keyboard weapon-selection refresh are also covered by the final code | PASS automated |
| R10 | Start/result feedback | Show 3–2–1 before Playing; show the correct win/loss artwork and play the matching clip | `DemoSceneBuilder` imports and serializes all countdown/result references; PlayMode HUD flow passes; owner visual/audio smoke check remains | PASS automated / PARTIAL manual |
| R11 | Detonation feedback | Rocket/mine detonation plays supplied explosion audio and a nine-frame animation | `CombatVfxPresenter.ExplosionFrames` is configured and verified at length 9; owner visual/audio smoke check remains | PASS automated / PARTIAL manual |
| R12 | Tank scale/death feedback | Enemy tank is visually the same size as the player tank; both tanks show a one-shot explosion on death | Enemy prefab and builder use 1.6 visual scale / 0.6 collider; PlayMode death-feedback regression covers player and enemy paths | PASS code / pending final runtime smoke |

| Verification | Result | Evidence |
|---|---|---|
| Complete EditMode suite | PASS — 59/59 | `Logs/coreguard-editmode-provided.xml` |
| Complete PlayMode suite | PASS — 12/12 | `Logs/coreguard-playmode-provided.xml` |
| Configure Project on real Main scene | PASS — repeated configuration is idempotent | `BuildDemoConfigurationTests`; final configured `Assets/_Game/Scenes/Main.unity` |
| Scene wiring | PASS | Session owns player/core/motor/weapon/defense/effects/audio/demo/spawner/input/interaction cycle; HUD owns feedback label; zone owns warning ring |
| Static hygiene | PASS | `git diff --check` |

## Owner smoke pass in Unity

1. Run `Core Guard → Configure Project`, open `Assets/_Game/Scenes/Main.unity`, press Play and click Start; confirm the centered 3–2–1 image countdown before gameplay begins.
2. Move with WASD/arrows and aim with the mouse. Press 1/2/3 and confirm the HUD weapon label changes; hold left mouse for Bullet and click for Rocket/Mine.
3. Place three mines, wait for the mine cooldown, try a fourth and confirm `MINE LIMIT 3/3` appears without creating another mine.
4. Press Q before an enemy shot and confirm Shield absorbs up to three hits. Press E near enemies and confirm only nearby enemies stun for about two seconds.
5. Use Demo Mode with F1: test Spawn Zone Enemy for the blinking zone ring and four alert beeps, Spawn Shooter, Spawn Enemy Cluster, and Restore X/Y/Z.
6. Walk into Y with Shield active, confirm the shield breaks and speed drops; remain inside to confirm it does not retrigger continuously, then leave and enter again.
7. Toggle SFX and BGM independently. Confirm the visual zone warning still blinks when SFX is off.
8. Test Esc pause/resume, R/button Retry, win/loss banners/audio, explosion animation/audio on both tank deaths, and readability at 1280×720 and 1920×1080.

These are the remaining manual checks because batchmode tests cannot prove physical device feel or that a speaker emits the intended distinct clips.
