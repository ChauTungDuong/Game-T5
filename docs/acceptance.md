# Core Guard Acceptance

> Current scope is Unity Editor demo only. Standalone Windows build checks below are historical T1 evidence and are not required for the current handoff.

T1–T6 register, verified 2026-09-15 with Unity 6000.3.23f1 in headless batchmode. Complete test suite 70/70 passed (58 EditMode, 12 PlayMode).

| Requirement ID | Scenario | Expected | Actual | Status | Evidence |
|---|---|---|---|---|---|
| R01 | A attacks with each weapon | Distinct short SFX for accepted attacks | Verified through AudioService event contracts and playback triggers | PASS (automated) | `Logs/playmode.xml`, `Logs/editmode.xml` |
| R02 | B enters restricted zone | One four-beep warning sequence | FIFO queue plays 4 beeps at 0.45s cadence, no overlaps | PASS (automated) | `AudioContractTests.Alert_FourBeepSequencePlaysInFifoQueue` |
| R03 | Toggle SFX | SoundOff/SoundOn alternate consistently | Sound toggle pairs update mute state and button visibility | PASS (automated) | `AudioContractTests.AudioToggles_SoundButtonsDriveMute` |
| R04 | Toggle music | MusicOn/MusicOff control only music | Music toggle pairs control BGM mute independently | PASS (automated) | `AudioContractTests.AudioToggles_MusicButtonsDriveMute` |
| R05 | Move and aim A | Normalized movement, mouse aim, bounds | Physics travel, clamp, aim retention and action/state gating pass | PASS (automated) | `PhysicsGameplayTests`, `PresentationInputTests` |
| R06 | Bullet, Rocket, Mine | Three damaging attacks/cooldowns | 3 weapons, cooldown persistence, projectile hit-guard and AoE deduplication pass | PASS (automated) | `CombatContractTests` (12/12) |
| R07 | Shield and EMP | Shield blocks projectiles; EMP disables enemies | Shield absorbs 3 shots / 3s, EMP stuns radius, cooldown & reset verified | PASS (automated) | `DefenseContractTests` (7/7), `PresentationInputTests` |
| R08 | X/Y/Z interactions | E1–E6 observable numeric changes | All 6 effects E1–E6, 5s visible/hidden cycle, speed stacking formula verified | PASS (automated) | `InteractionContractTests` (20/20), `InteractionCycleControllerTests` (7/7) |
| R09 | A state changes | Live HP, Armor, Coins, weapon/cooldown HUD | World health bars, compact HUD, READY/cooldown states and session reset pass | PASS (automated) | `HealthBarContractTests` (8/8), `HealthBarPresentationTests` (4/4), `PresentationInputTests` |

| Test Suite / Step | Result | Evidence |
|---|---|---|
| Full EditMode suite | PASS (58/58) | `Logs/editmode.xml` exit code 0 |
| Full PlayMode suite | PASS (12/12) | `Logs/playmode.xml` exit code 0 |
| Idempotent scene builder (`ConfigureProject`) | PASS | `Logs/configure1.log`, `Logs/configure2.log` |
| Armor-first damage, nonnegative HP and change notification | PASS | `GameplayContractTests.Stats_ArmorAbsorbsDamageBeforeHpAndNotifies` |
| Enemy contact applies exactly 20 core damage once and retires | PASS | `GameplayContractTests.Enemy_ContactDamagesCoreExactlyOnceAndRetires` |
| Pause clock; death before timeout; alive timeout wins | PASS | `GameplayContractTests.Session_*`; physics final-tick test |
| Five-second cadence, gate rotation, six-enemy cap without backlog | PASS | `GameplayContractTests.Spawner_CapConsumesMissedIntervalsAndRotatesGates` |
| Central Retry resets 100/50/0, core 100, timer 90, position and spawn state | PASS (automated) | `GameplayContractTests.Retry_CentralResetRestoresStatsCoreClockPositionAndSpawns` |
| Two seconds axial/diagonal Rigidbody travel equals eight units within tolerance | PASS | `PhysicsGameplayTests.Motor_TwoSecondsAxialAndDiagonalTravelBothEightUnits` |
| Player bounds, paused movement/aim, zero-distance aim retention | PASS | `PhysicsGameplayTests.Motor_ClampsBoundsPausesAndRetainsAimAtCenter` |
| Enemy speed 1.2; final-tick contact loses before timeout | PASS | `PhysicsGameplayTests.Enemy_MovesAtOnePointTwoAndFinalTickContactLosesBeforeTimeout` |
| Real Input System keyboard move, pause and result-only Retry | PASS (automated) | `PresentationInputTests.Input_KeyboardMovementPauseAndResultRetryUseActions` |
| Real mouse fire intent suppressed over raycastable UI and outside Playing | PASS | `PresentationInputTests.Input_FireIsSuppressedOverUiAndOutsidePlaying` |
| Live HUD and Start/Resume/Retry Button listeners | PASS (automated) | `PresentationInputTests.Hud_ShowsLiveValuesAndButtonsDriveSingleSceneFlow` |
| Idempotent builder preserves Main/unrelated entries and additive-scene ownership | PASS | `Logs/t1-scene-focused-green.xml` (3/3) |
| Complete gameplay suites | PASS | `Logs/t1-editmode-full.xml` 10/10; `Logs/t1-playmode-full.xml` 6/6, both exit 0 |
| Windows build | PASS | `t1-build.log`: Build Successful, 102,436,248 bytes |
| Visible scene/HUD at 1280×720 and 1920×1080 | PASS | `Logs/t1-ready1280.png` and `Logs/t1-ready1920.png` (filenames retained; final captures show Playing) |
| Live core damage, timer and Lost panel in Windows player | PASS | 1280 capture: core 40 / 69.2 s; later `Logs/t1-playing1280.png`: core 0 / 59.7 s / LOST |
| Clean player closure / runtime error scan | PASS | `CloseMainWindow=True HasExited=True`; neither player log matched exception/error/crash |
| Controlled physical keyboard/button pass in Windows player | NOT RUN | Win32 input injection was unreliable amid active desktop interaction; use steps below |

## Remaining physical input checklist

1. Launch `Builds/Windows/CoreGuard/CoreGuard.exe` and click Start. Confirm the Start panel hides, state is Playing, and the timer decreases from 90.
2. Hold each WASD and arrow-key direction, including diagonals. Compare two-second horizontal and diagonal travel; move into every edge and confirm the body stays inside the arena.
3. Move the pointer around A and onto A's center. Confirm the turret follows the pointer and keeps its last direction at the center.
4. Press Esc, wait at least two seconds, and hold movement keys. Confirm player, enemies and timer stay fixed. Press Esc or Resume to continue.
5. Let enemies deplete the core. Confirm LOST appears. Press R and separately test the Retry button after another loss. Confirm player HP/Armor/Coins 100/50/0, core 100, timer 90, restored player position, no prior enemies, and a fresh five-second spawn schedule.
6. Repeat the panel/button checks at 1920×1080. Layout readability at both resolutions has already been visually verified; this remaining check is specifically physical input.

The T1 prototype has no weapons yet, so unattended runs lose before 90 seconds. The alive-timeout win path is covered by automated component tests; no demo-only controls were added to force it in the player.
