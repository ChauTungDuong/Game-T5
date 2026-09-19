# Core Guard Campaign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the existing Core Guard game with three distinct missions, persistent player checkpoints, complete win/loss navigation, and three intelligent enemy NPC roles.

**Architecture:** Keep `Main.unity`, `GameSession`, existing combat, input, HUD, assets, and scene repair pipeline. Add small campaign data/progress components, deterministic enemy decisions, and dedicated menu panels; do not introduce another game, scene-loading framework, or third-party AI/save package. Preserve the existing standalone 90-second session when no campaign is assigned, so existing fixtures and F1 demonstrations remain useful.

**Tech Stack:** Unity 6000.3.23f1, C#, Universal 2D 17.3.0, Input System 1.20.0, uGUI 2.0.0, Unity Test Framework 1.6.0, NUnit, JSON files under `Application.persistentDataPath`.

**Spec:** `next-requirements.md` (read alongside this plan). Existing implementation, not old planning documents, is the behavioral baseline.

## Global Constraints

The following are copied from the requirement document:

- “Cài đặt trạng thái ‘game over’”
- “Hiển thị thông báo/hiệu ứng hình ảnh/hiệu ứng âm thanh xxx trên màn hình chơi game.”
- “Hiển thị ít nhất 3 nút giúp người chơi điều hướng (Replay/Home Screen/Settings/Historical Progress/Item Warehouse/Help/…); cài đặt đầy đủ chức năng cho 3 nút đó -> chuyển tiếp sang các màn hình riêng biệt.”
- “Xây dựng ít nhất 3 cấp độ chơi/nhiệm vụ/bản đồ khác nhau (đồ họa, cơ chế game, thế giới game) cho game.”
- “Duy trì/Lưu trạng thái của người chơi sau khi hoàn thành từng cấp độ chơi/nhiệm vụ/bản đồ.”
- “Cài đặt trạng thái ‘win’ cho game”
- “Có mục tiêu cụ thể để ‘win’ trò chơi.”
- “Hiển thị thông báo/hiệu ứng hình ảnh/hiệu ứng âm thanh yyy trên màn hình chơi game.”
- “Hiển thị ít nhất 3 nút giúp người chơi điều hướng (Replay/Home Screen/High Achievements/Historical Progress/…); cài đặt đầy đủ chức năng cho 3 nút đó -> chuyển tiếp sang các màn hình riêng biệt.”
- “Yêu cầu bổ sung: Xây dựng tính năng/hành vi thông minh cho ít nhất 3 NPC trong game.”

Project/user constraints: improve this project; keep shell commands prefixed with `rtk`; preserve pre-existing edits. No engine/package upgrade, asset download, external service, or platform port is needed. Distinct full-screen Canvas panels count as separate screens; the specification does not require separate Unity scene files. Show both text/artwork and the existing distinct victory/defeat audio, interpreting `xxx`/`yyy` as unspecified feedback rather than literal UI copy.

## Review Focus

- Death on the objective-completion tick must lose and must not save a success (Tasks 1, 3, 5).
- Repeated result callbacks, double-clicks, and replay must not duplicate completion or destroy the last checkpoint (Tasks 2, 3, 7).
- Missing, malformed, incompatible, or unwritable saves must leave the game usable and preserve the last valid save (Task 2).
- NPC overlap, missing targets, EMP, pause, and retry must produce finite movement and no off-session damage (Tasks 4, 6).
- Rapid navigation during loading/countdown and repeated UI binding must not launch a hidden run, replay audio, or accumulate button handlers (Task 7).

---

## Verified starting point and scope decisions

Read on 2026-09-19:

| Existing area | Evidence | Action |
|---|---|---|
| Session | `GameSession.cs`: Ready/Playing/Paused/Won/Lost, death before timeout, central reset, 90-second timer | Extend, retain legacy fallback |
| Results | `HudPresenter.cs`: result panel, artwork, Retry; `AudioService.HandleSessionChanged`: distinct audio on state transition | Retain; add Home and Progress |
| Entry | Start, Settings, loading, countdown already implemented | Reuse; add mission selection/Continue |
| Combat | Bullet/Rocket/Laser, energy-based Shield/EMP, six interaction kinds | Preserve current mechanics |
| Player | HP starts 100, can heal to 200; Energy starts 100, regenerates 2/s; Armor aliases Energy | Save HP/Energy/Coins; do not restore obsolete 50-armor behavior |
| Enemy | One tank prefab, core seeking, player shooting, stun, health bars | Three policies using the same component/prefab |
| World | One arena, four gates, six live enemy cap, interactions cycling | Three mission configurations and visible world props |
| Tooling | `DemoSceneBuilder.Configure(Scene)`, `BuildDemo.ConfigureProject()`, existing EditMode/PlayMode suites | Extend idempotently, test actual Main wiring |

Existing dirty files: `Assets/Settings/InputSystem_Actions.inputactions`, Enemy/Mine/Projectile prefabs, `Main.unity`; untracked `Resources/` and `next-requirements.md`. Do not reset or stage unrelated edits. Inspect scene/prefab diffs before each commit. Keep `Resources/` local; use already imported assets. This planning task changes documentation only. Historical suite totals in `docs/progress.md` are not current test evidence.

One plan is intentional: save, navigation, and missions share session transitions. Tasks are separate reviewable slices; splitting them into independent subsystem projects would duplicate these contracts. Estimated implementation is roughly 6–10 focused hours plus hands-on balance/visual checks; each checkbox is a small action, and the task gate must pass before moving on.

### Concrete campaign design (proposed implementation choices)

| ID | World and graphics | Objective/mechanic | Enemy mix | Duration |
|---|---|---|---|---|
| 0 `Outpost` | Existing sand arena, amber tint, four perimeter beacon props | Keep player and core alive until time expires | Assault | 90 s |
| 1 `Supply Relay` | Teal floor overlay, three visible relay rings | Collect three successful green Z healing supplies, then survive until time expires | Assault, Skirmisher | 75 s |
| 2 `Reactor Siege` | Purple floor overlay, reactor rings and warning pylons | Destroy six enemy tanks and keep player/core alive until time expires | Assault, Skirmisher, Saboteur | 90 s |

All missions use current arena dimensions, central core, controls and interaction cycle. The visible props have no collision: no pathfinding rewrite. Gates stay inside the playable arena. Supply objective counts the existing `InteractionKind.Z`, including a valid pickup at full health; yellow `Z_Speed` does not count. Reaching the timer without the quota loses with a specific explanation. Campaign victory means all three missions completed in order; intermediate results say `MISSION COMPLETE`, final success says `CAMPAIGN COMPLETE — YOU WIN`. Each mission loss says `GAME OVER` plus Player destroyed/Core destroyed/Objective incomplete.

Checkpoint policy: record post-success HP, Energy, Coins and core HP, plus the successful run's entry snapshot, weapon selection and completed mission flags. Continue starts the next mission with the successful player/core snapshot. Fresh campaign starts at current defaults. Retry reuses that mission's entry snapshot, not the defeated or victorious end state. Replaying a completed mission uses its recorded entry snapshot and cannot change the forward campaign checkpoint. Temporary boosts, stuns, shields, projectiles and enemy state reset between missions. F1 marks the whole attempt as practice even if toggled off again; practice never writes progress. A completed campaign remains replayable.

### File responsibility map

All new `.cs` files below also receive Unity-generated `.meta` files.

| Paths | Responsibility |
|---|---|
| `Assets/_Game/Scripts/MissionDefinition.cs` | Three immutable built-in configurations and objective predicate |
| `Assets/_Game/Scripts/CampaignProgress.cs` | Serializable schema and snapshot values |
| `Assets/_Game/Scripts/CampaignSaveStore.cs` | Validation, load, safe replacement, backup recovery |
| `Assets/_Game/Scripts/CampaignController.cs` | Mission selection, counters, checkpoint transitions |
| `Assets/_Game/Scripts/EnemyTactics.cs` | Pure role-to-movement/shoot decision |
| `Assets/_Game/Scripts/MissionWorldPresenter.cs` | Theme, non-colliding props, mission labels |
| `Assets/_Game/Scripts/CampaignScreenPresenter.cs` | Progress screen and mission selection, no combat simulation |
| `GameSession.cs`, `PlayerStats.cs`, `CoreHealth.cs` in Scripts | Integrate mission lifecycle and snapshot restore |
| `EnemyController.cs`, `EnemySpawner.cs` in Scripts | Execute tactical decisions and report genuine kills |
| `InteractionObject.cs`, `DemoDirector.cs`, `Projectile.cs` in Scripts | Session-safe objective events, practice isolation, terminal damage guard |
| `HudPresenter.cs` in Scripts | Existing presentation plus navigation/transition cancellation |
| `Assets/_Game/Editor/CampaignSceneBuilder.cs` | Idempotent construction of campaign UI/world objects |
| `Assets/_Game/Editor/DemoSceneBuilder.cs`, `Assets/_Game/Scenes/Main.unity` | Wire extensions into existing scene |
| EditMode `CampaignTestBase.cs` | Small isolated fixture used by new EditMode tests |
| EditMode `CampaignLifecycleTests.cs`, `CampaignSaveTests.cs`, `MissionObjectiveTests.cs`, `EnemyTacticsTests.cs`, `CampaignConfigurationTests.cs` | Rules, serialization, integration and builder coverage |
| PlayMode `CampaignFlowTests.cs`, `EnemyTacticsPlayTests.cs` | Real buttons, physics and rendered scene flow |
| `README.md`, `docs/progress.md`, `docs/acceptance.md`, `docs/asset-register.md` | Actual behavior, reproducible evidence and reused asset provenance |

## Test/commit conventions

Use Unity Test Runner in the already open project for fastest iteration. Filter by the class named in each task. When no Editor owns this project, batch equivalent (replace filter/platform/results name per task):

```powershell
rtk proxy "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\Game-T5" -runTests -testPlatform EditMode -testFilter "CoreGuard.Tests.Editor.CampaignLifecycleTests" -testResults "D:\Game-T5\Logs\campaign-lifecycle.xml" -logFile "D:\Game-T5\Logs\campaign-lifecycle.log"
```

Do not add `-quit` to test runs. Confirm XML failures and failed assertions, not merely process exit code. For visual tests use the Editor with graphics. Don't close the user's Editor or overwrite unsaved scenes. Focused gates run after every task; the full suites run at integration. A new API may initially produce a compile failure: add its signature with a neutral body, then require an assertion failure for the intended behavior before implementation. Tests must never use the actual player save path. Commit only the task's named files and their `.meta` files; `rtk proxy git diff --check` before each commit. `rtk proxy` is used because filtered `rtk git`/`rtk rg` currently fail to locate their config.

### Task 1: Make mission rules testable through the existing session

**Files:** Create `MissionDefinition.cs`, EditMode `CampaignTestBase.cs`, `MissionObjectiveTests.cs`; modify `GameSession.cs` (`ResetRun`, `Advance`). Paths are rooted as in the map above.

**Interfaces:** `MissionDefinition.Get(int id): MissionDefinition` (reject IDs outside 0..2); readonly `Id`, `Name`, `Duration`, `RequiredSupplies`, `RequiredKills`; `ObjectiveMet(int supplies, int kills): bool`. Add nullable `GameSession.Mission`, integer `SuppliesCollected`/`EnemiesDestroyed`, and string `ResultReason`; default null retains 90-second legacy rules.

- [ ] **Step 1: Add the isolated fixture.** Put this complete helper in `CampaignTestBase.cs`, namespace `CoreGuard.Tests.Editor`, importing `System.Collections.Generic`, `NUnit.Framework`, `UnityEngine`. Derive new component tests from it; pure value tests need not derive.

```csharp
public abstract class CampaignTestBase
{
    readonly List<GameObject> owned = new();
    protected T Make<T>(string name) where T : Component
    {
        var go = new GameObject(name); owned.Add(go);
        return go.GetComponent<T>() ?? go.AddComponent<T>();
    }
    protected GameSession MakeSession()
    {
        var s = Make<GameSession>("Test session");
        s.Player = Make<PlayerStats>("Player");
        s.Core = Make<CoreHealth>("Core");
        s.Motor = s.Player.gameObject.AddComponent<PlayerMotor>();
        s.Motor.Session = s;
        s.Spawner = Make<EnemySpawner>("Spawner");
        s.Spawner.Session = s; s.Spawner.Core = s.Core;
        s.Spawner.enabled = false;
        s.Initialize(); return s;
    }
    [TearDown] public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; --i)
            if (owned[i]) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }
}
```

- [ ] **Step 2: Add rule tests.** In `MissionObjectiveTests : CampaignTestBase`:

```csharp
[TestCase(0, 0, 0, true)]
[TestCase(1, 2, 99, false)]
[TestCase(1, 3, 0, true)]
[TestCase(2, 99, 5, false)]
[TestCase(2, 0, 6, true)]
public void QuotasAreDistinct(int id, int supplies, int kills, bool expected)
{
    Assert.That(MissionDefinition.Get(id).ObjectiveMet(supplies, kills), Is.EqualTo(expected));
}
[Test] public void DeathBeatsSuccessfulTimeout()
{
    var s = MakeSession(); s.Mission = MissionDefinition.Get(0);
    s.StartMatch(); s.Core.ApplyDamage(100); s.Advance(90);
    Assert.That(s.State, Is.EqualTo(MatchState.Lost));
    Assert.That(s.ResultReason, Is.EqualTo("Core destroyed"));
}
[Test] public void MissingQuotaLosesAtDeadline()
{
    var s = MakeSession(); s.Mission = MissionDefinition.Get(1);
    s.StartMatch(); s.Advance(75);
    Assert.That(s.State, Is.EqualTo(MatchState.Lost));
    Assert.That(s.ResultReason, Is.EqualTo("Objective incomplete"));
}
```

- [ ] **Step 3: Run `MissionObjectiveTests`; verify the intended failures.** Expected: absent rules or wrong mission timeout result. Existing `GameplayContractTests` must continue to compile.
- [ ] **Step 4: Implement definitions.** Use a sealed class with a private constructor assigning its five readonly fields and static array of `(0,"Outpost",90f,0,0)`, `(1,"Supply Relay",75f,3,0)`, `(2,"Reactor Siege",90f,0,6)`.

```csharp
public bool ObjectiveMet(int supplies, int kills) =>
    supplies >= RequiredSupplies && kills >= RequiredKills;
```

- [ ] **Step 5: Integrate existing reset and final evaluation.** Reset both counters and reason in `ResetRun`; set `Remaining = Mission?.Duration ?? 90f`. Keep all existing combat/reset calls. Ignore non-finite `delta`. Evaluate after existing contact processing:

```csharp
if (Player.HP <= 0) { State = MatchState.Lost; ResultReason = "Player destroyed"; }
else if (Core.HP <= 0) { State = MatchState.Lost; ResultReason = "Core destroyed"; }
else if (Remaining <= 0)
{
    bool met = Mission == null || Mission.ObjectiveMet(SuppliesCollected, EnemiesDestroyed);
    State = met ? MatchState.Won : MatchState.Lost;
    ResultReason = met ? "Mission complete" : "Objective incomplete";
}
```

- [ ] **Step 6: Run `MissionObjectiveTests` and `GameplayContractTests`.** Expected: green; existing Main still plays its original run. Inspect pause/timer behavior manually once.
- [ ] **Step 7: Commit named task files.** `rtk proxy git commit -m "feat: define campaign mission objectives in existing session"` after explicit staging.

### Task 2: Persist and recover player checkpoints

**Files:** Create Scripts `CampaignProgress.cs`, `CampaignSaveStore.cs`; create EditMode `CampaignSaveTests.cs`.

**Interfaces:** Serializable `RunSnapshot` is a struct with public fields `float Hp, Energy, CoreHp; int Coins; WeaponKind Weapon`; serializable `MissionRecord` is a class with fields `bool Completed; RunSnapshot Entry, Exit`. Serializable `CampaignProgress` fields `int Version = 1, NextMission; bool Completed; RunSnapshot Checkpoint; MissionRecord[] Missions` (exactly 3 initialized records). `CampaignProgress.Fresh(): CampaignProgress` returns 100 HP/100 Energy/100 Core HP/0 coins/Bullet. `CampaignSaveStore(string path)`, `Load(): CampaignProgress`, `TrySave(CampaignProgress progress): bool`, `LastError: string`.

- [ ] **Step 1: Add save tests with a temporary directory per test.** Set `dir = Path.Combine(Path.GetTempPath(), "CoreGuardTests-" + Guid.NewGuid())`, create it, and set `path = Path.Combine(dir,"campaign.json")`; delete only that test directory in teardown.

```csharp
[Test] public void RoundTripPreservesStateAndMissionHistory()
{
    var p = CampaignProgress.Fresh(); p.NextMission = 1;
    p.Missions[0].Completed = true;
    p.Missions[0].Entry = CampaignProgress.Fresh().Checkpoint;
    p.Checkpoint = new RunSnapshot { Hp = 140, Energy = 23, CoreHp = 80,
        Coins = 12, Weapon = WeaponKind.Laser };
    p.Missions[0].Exit = p.Checkpoint;
    Assert.That(new CampaignSaveStore(path).TrySave(p), Is.True);
    var loaded = new CampaignSaveStore(path).Load();
    Assert.That(loaded.NextMission, Is.EqualTo(1));
    Assert.That(loaded.Checkpoint.Hp, Is.EqualTo(140));
    Assert.That(loaded.Checkpoint.Energy, Is.EqualTo(23));
    Assert.That(loaded.Checkpoint.CoreHp, Is.EqualTo(80));
    Assert.That(loaded.Checkpoint.Coins, Is.EqualTo(12));
    Assert.That(loaded.Checkpoint.Weapon, Is.EqualTo(WeaponKind.Laser));
}
[TestCase("{")]
[TestCase("null")]
[TestCase("{\"Version\":99}")]
public void BadSaveDoesNotCrashOrGetOverwritten(string json)
{
    File.WriteAllText(path, json);
    var store = new CampaignSaveStore(path);
    Assert.That(store.Load().NextMission, Is.Zero);
    Assert.That(File.ReadAllText(path), Is.EqualTo(json));
    Assert.That(store.LastError, Is.Not.Empty);
}
[Test] public void BlockedPathReportsFailure()
{
    var blocker = Path.Combine(dir, "file"); File.WriteAllText(blocker, "keep");
    var store = new CampaignSaveStore(Path.Combine(blocker, "campaign.json"));
    Assert.That(store.TrySave(CampaignProgress.Fresh()), Is.False);
    Assert.That(File.ReadAllText(blocker), Is.EqualTo("keep"));
}
```

- [ ] **Step 2: Run `CampaignSaveTests` red.** Expected: missing persistence or incorrect recovery behavior.
- [ ] **Step 3: Implement schema and validation.** Use `JsonUtility`. Require Version 1; three records; contiguous completed prefix consistent with `NextMission` (0..2, or 3 iff completed); finite HP in (0,200], Energy in [0,100], CoreHP in (0,100], nonnegative Coins, valid current `WeaponKind`. Validate snapshots in completed records. Reject malformed records rather than granting unlocks. Missing file returns Fresh without an error; invalid file returns a valid backup or Fresh with `LastError`.
- [ ] **Step 4: Implement safe save.** Validate before touching disk; write/close a same-directory `.tmp` then replace with backup. Catch `IOException`, `UnauthorizedAccessException`, `ArgumentException` and JSON parse failures at the appropriate boundary; set a readable error, return false, keep old file. Never overwrite a future-version file automatically. Use a load-time write-protection flag for incompatible schema, and display that error later.

```csharp
// Inside TrySave's guarded I/O section, after validation:
Directory.CreateDirectory(Path.GetDirectoryName(path));
File.WriteAllText(path + ".tmp", JsonUtility.ToJson(progress, true));
if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak");
else File.Move(path + ".tmp", path);
// Return true only after replacement succeeds. A leftover .tmp is not a save.
```

- [ ] **Step 5: Add recovery and validation tests.** Mutate `Fresh().Checkpoint.Hp` to `float.NaN` and assert `TrySave` false; set `NextMission=2` with no completed records and assert false. Save two valid versions, corrupt primary with `"{"`, and assert Load returns the first backup. Leave only `.tmp` and assert Fresh. Attempt save after loading Version 99 and assert original bytes unchanged. Run all `CampaignSaveTests`; expected green.
- [ ] **Step 6: Commit.** `rtk proxy git commit -m "feat: persist validated campaign checkpoints with backup recovery"`.

### Task 3: Connect campaign, retry, Continue and practice lifecycle

**Files:** Create `CampaignController.cs`, EditMode `CampaignLifecycleTests.cs`; modify `GameSession.cs`, `PlayerStats.cs`, `CoreHealth.cs`, `DemoDirector.cs`.

**Interfaces:** `CampaignController.Session`, `Progress { get; private set; }`, `ActiveMission { get; private set; }`, `IsPractice { get; private set; }`, `SaveError { get; private set; }`; `Initialize(CampaignSaveStore store): void`, `SelectMission(int id): bool`, `ContinueCampaign(): bool`, `MarkPractice(): void`, `CompleteMission(): void`. Add `GameSession.Campaign`, `PrepareMission(MissionDefinition mission, RunSnapshot entry): void`, `ReturnHome(): void`, `CaptureSnapshot(): RunSnapshot`. Add `PlayerStats.Restore(float hp,float energy,int coins): void`, `CoreHealth.Restore(float hp): void`. Neither Restore accepts invalid values; throw `ArgumentOutOfRangeException` before changing any field. Notify existing health/UI listeners once.

- [ ] **Step 1: Add lifecycle tests.** In a `CampaignTestBase` subclass use a per-test temporary store as Task 2; make a campaign component, assign `Session`, set `s.Campaign = c`, call Initialize. The following test bodies pin retry/save behavior:

```csharp
[Test] public void VictoryCarriesPlayerStateAndRetryUsesEntry()
{
    var s = MakeSession();
    var c = Make<CampaignController>("Campaign"); c.Session = s; s.Campaign = c;
    c.Initialize(new CampaignSaveStore(path)); c.SelectMission(0); s.StartMatch();
    s.Player.ApplyDamage(20); s.Player.AddCoins(7); s.Advance(90);
    Assert.That(c.Progress.NextMission, Is.EqualTo(1));
    Assert.That(c.Progress.Checkpoint.Hp, Is.EqualTo(80));
    Assert.That(c.Progress.Checkpoint.Coins, Is.EqualTo(7));
    s.Retry(); Assert.That(s.Player.HP, Is.EqualTo(100));
    Assert.That(s.Player.Coins, Is.Zero);
    Assert.That(c.Progress.Checkpoint.Hp, Is.EqualTo(80));
}
[Test] public void PracticeNeverUnlocksEvenAfterItsPanelCloses()
{
    var s = MakeSession();
    var c = Make<CampaignController>("Campaign"); c.Session = s; s.Campaign = c;
    c.Initialize(new CampaignSaveStore(path)); c.SelectMission(0); s.StartMatch();
    c.MarkPractice(); s.Advance(90); c.CompleteMission();
    Assert.That(c.Progress.NextMission, Is.Zero);
    Assert.That(File.Exists(path), Is.False);
}
```

- [ ] **Step 2: Run `CampaignLifecycleTests` red.** Assert missing carryover/retry/practice behavior, not an unrelated fixture exception.
- [ ] **Step 3: Add snapshot restoration to central reset.** `PrepareMission` is allowed only outside Playing/Paused; set Mission and a value-copy entry snapshot and call existing reset with Ready. `ResetRun` restores that entry after defaults and before `Reset/Changed`. Restore weapon through existing `Weapon.Select`; clamp nothing silently. Retry stays current mission and starts Playing, preserving existing R behavior. ReturnHome clears transients through reset to Ready and emits Changed. Capture uses current HP/Energy/Coins/Core HP and selected weapon (Bullet without a weapon component).

```csharp
// After defaults in ResetRun, when a mission entry exists:
Player.Restore(entry.Hp, entry.Energy, entry.Coins);
Core.Restore(entry.CoreHp);
if (Weapon) Weapon.Select(entry.Weapon);
```

- [ ] **Step 4: Implement campaign orchestration.** Load only in Initialize; select only unlocked IDs; use history entry for completed mission replay and Checkpoint for the frontier mission. `ContinueCampaign` returns false if campaign completed, otherwise selects `NextMission`. Call CompleteMission from GameSession only on transition Playing -> Won, after setting State and before Changed. Copy progress before mutation; advance only when `ActiveMission == Progress.NextMission && !IsPractice`. Always retain a valid success in memory; set SaveError on disk failure so the user can see that persistence failed. Add a `RetrySave(): bool` that retries the current Progress, for the later Progress screen. Set NextMission to 3 and Completed true after mission 2; never call `Get(3)`.

Initialize is explicitly injectable and sets an `initialized` flag. Runtime Start initializes only when not already initialized, using `Path.Combine(Application.persistentDataPath, "core-guard-campaign-v1.json")`; editor scene construction never reads/writes progress. Loading a scene may read the real save, but tests must inject their temporary store before any success or save action. On successful initialization prepare the frontier mission (mission 0 when the campaign is completed) without starting combat. Retry keeps the practice flag for that attempt; selecting a mission begins a new non-practice attempt. Deep-copy the progress object via its validated JSON representation before changes; RunSnapshot itself has value semantics.

```csharp
// CompleteMission guard, before modifying records:
if (Session.State != MatchState.Won || IsPractice || Progress.Completed ||
    ActiveMission != Progress.NextMission) return;
```

- [ ] **Step 5: Isolate F1.** `DemoDirector.Toggle` calls `Campaign.MarkPractice()` when entering demonstration. MarkPractice latches until a new mission attempt is prepared; toggling off does not clear it. `ResetForDemo` preserves practice; normal selection resets it. Add `DemoDirector.ExitPractice(): void` to turn off demo mode, restore auto-spawning, and hide the panel when returning Home/selecting a mission.
- [ ] **Step 6: Add remaining regression tests and run.** Call CompleteMission twice and assert one unlocked level; reload a new controller and assert Continue restores the same snapshot; kill core on timeout and assert no file; select locked ID 2 and assert false/unchanged; win a replay and assert checkpoint unchanged; test Restore at HP 200/Energy 0 and NaN rejection. Run Lifecycle, Objective and existing Gameplay/DemoDirector tests. Expected green.
- [ ] **Step 7: Commit.** `rtk proxy git commit -m "feat: add campaign continuation and isolated mission retries"`.

### Task 4: Give three enemy NPC roles observable tactical decisions

**Files:** Create `EnemyTactics.cs`, EditMode `EnemyTacticsTests.cs`; modify `EnemyController.cs`, `EnemySpawner.cs`.

**Interfaces:** `EnemyRole { Assault, Skirmisher, Saboteur }`; readonly struct `EnemyDecision` fields `Vector2 Target; bool Shoot` and constructor `EnemyDecision(Vector2 target, bool shoot)` assigning them; `EnemyTactics.Decide(EnemyRole role, Vector2 self, Vector2 core, Vector2 player, bool hasPlayer, float hpRatio, float flankSign): EnemyDecision`. Add `EnemyController.Role` (default Assault), `FlankSign = 1f`, `TacticsEnabled` (default false for legacy fixtures), `MovementSpeed = 1.2f`. Add spawner `EnemyRole[] RoleSequence`, `float SpawnInterval = 5f`; retain cap 6.

- [ ] **Step 1: Add pure decision tests.** `EnemyTacticsTests` imports NUnit and UnityEngine.

```csharp
[Test] public void SkirmisherRetreatsFromNearbyPlayer()
{
    var d = EnemyTactics.Decide(EnemyRole.Skirmisher, new Vector2(2,0),
        Vector2.zero, new Vector2(3,0), true, 1, 1);
    Assert.That(d.Target.x, Is.LessThan(2)); Assert.That(d.Shoot, Is.True);
}
[Test] public void AssaultInterceptsOnlyNearbyThreat()
{
    var near = EnemyTactics.Decide(EnemyRole.Assault, new Vector2(5,0),
        Vector2.zero, new Vector2(5,1), true, 1, 1);
    var far = EnemyTactics.Decide(EnemyRole.Assault, new Vector2(5,0),
        Vector2.zero, new Vector2(-5,1), true, 1, 1);
    Assert.That(near.Target, Is.EqualTo(new Vector2(5,1)));
    Assert.That(far.Target, Is.EqualTo(Vector2.zero));
}
[Test] public void SaboteurFlanksThreatButCommitsWhenWeak()
{
    var a = EnemyTactics.Decide(EnemyRole.Saboteur, new Vector2(5,0),
        Vector2.zero, new Vector2(5,1), true, 1, 1);
    var b = EnemyTactics.Decide(EnemyRole.Saboteur, new Vector2(5,0),
        Vector2.zero, new Vector2(5,1), true, .2f, 1);
    Assert.That(a.Target, Is.Not.EqualTo(Vector2.zero));
    Assert.That(b.Target, Is.EqualTo(Vector2.zero)); Assert.That(b.Shoot, Is.False);
}
[TestCase(EnemyRole.Assault)]
[TestCase(EnemyRole.Skirmisher)]
[TestCase(EnemyRole.Saboteur)]
public void CoincidentOrMissingTargetsRemainFinite(EnemyRole role)
{
    foreach (bool present in new[] {true, false})
    {
        var d = EnemyTactics.Decide(role, Vector2.zero, Vector2.zero,
            Vector2.zero, present, 1, 1);
        Assert.That(float.IsNaN(d.Target.x) || float.IsInfinity(d.Target.x), Is.False);
        Assert.That(float.IsNaN(d.Target.y) || float.IsInfinity(d.Target.y), Is.False);
        if (!present) Assert.That(d.Shoot, Is.False);
    }
}
```

- [ ] **Step 2: Run `EnemyTacticsTests` red.** Expected absent/wrong role decisions.
- [ ] **Step 3: Implement the pure policy.** Assault intercepts within 2.5 units else approaches core. Skirmisher keeps 4–6 units from player, orbiting tangentially in that band. Saboteur approaches core; within 3 units of a player and above 30% HP it offsets toward a flank; at low HP it commits directly. Never shoot without a living player; existing AttackRange/cadence still applies.

```csharp
var away = self - player;
var safeAway = away.sqrMagnitude > .000001f ? away.normalized : Vector2.right;
var tangent = new Vector2(-safeAway.y, safeAway.x) * (flankSign < 0 ? -1 : 1);
if (!hasPlayer) return new EnemyDecision(core, false);
if (role == EnemyRole.Assault)
    return new EnemyDecision(away.magnitude <= 2.5f ? player : core, true);
if (role == EnemyRole.Skirmisher)
    return new EnemyDecision(away.magnitude < 4 ? self + safeAway * 2 :
        away.magnitude > 6 ? player : self + tangent * 2, true);
return new EnemyDecision(hpRatio <= .3f || away.magnitude >= 3 ? core :
    core + tangent * 2, false);
```

- [ ] **Step 4: Integrate movement carefully.** In EnemyController.Step retain state/alive/delta/stun guards before decisions. For tactics compute decision, clamp target to current `Session.Motor` arena bounds, move at MovementSpeed, and use TryShoot only when Shoot. Preserve legacy movement when TacticsEnabled false. Core contact must test the actual proposed movement segment against the combined core/enemy radius, not distance to the tactical target. Use a closest-point-on-segment test; call TouchCore once on intersection. Prevent null Core/Session from throwing. Tint existing body amber/cyan/purple by role and update cached bodyColor so hit flashes restore that tint.

```csharp
Vector2 segment = next - body.position;
float t = segment.sqrMagnitude > .000001f
    ? Mathf.Clamp01(Vector2.Dot(corePosition - body.position, segment) / segment.sqrMagnitude) : 0;
if (Vector2.Distance(body.position + segment * t, corePosition) <= contactRadius)
{ TouchCore(); return; }
```

- [ ] **Step 5: Configure deterministic spawning.** Rotate RoleSequence by successful spawn count and alternate flank sign; reset counter in ResetSpawns but retain configured sequence and interval. Assign Role/TacticsEnabled/FlankSign before activation and refresh the role tint after Initialize. An empty/null sequence preserves legacy behavior. Use `SpawnInterval` with valid positive finite fallback 5; missing gates/prefab means skip, never index an empty array. Mission 0 sequence Assault; 1 Assault/Skirmisher; 2 all three. Keep six-enemy cap and consume missed intervals as today.
- [ ] **Step 6: Run tactical and existing Combat/Defense/Gameplay tests.** Expected green; existing legacy spawn tests unchanged. Spawn three role variants in Editor and observe interception, spacing and flanking; do not label color-only differences as intelligent behavior.
- [ ] **Step 7: Commit.** `rtk proxy git commit -m "feat: add assault skirmisher and saboteur enemy tactics"`.

### Task 5: Connect genuine supply pickups and kills to mission objectives

**Files:** Modify `GameSession.cs`, `InteractionObject.cs`, `EnemyController.cs`, `Projectile.cs`; extend `MissionObjectiveTests.cs`.

**Interfaces:** `GameSession.RecordSupply(InteractionObject source): void`, `RecordEnemyDestroyed(EnemyController enemy): void`. Both reject wrong-session events and non-Playing state. Per-attempt enemy identity set prevents repeated kills. Supply call occurs once after successful Z consumption; don't keep a lifetime identity set for supplies, because cycled respawns must count again.

- [ ] **Step 1: Add concrete event tests.** Fixture creates source objects with Make; use real ApplyTo/ApplyDamage entry points.

```csharp
[Test] public void GreenSupplyCountsOncePerSpawnAndNotYellow()
{
    var s = MakeSession(); s.Mission = MissionDefinition.Get(1); s.StartMatch();
    var z = Make<InteractionObject>("Supply"); z.Session = s; z.Player = s.Player;
    z.Kind = InteractionKind.Z; z.ApplyTo(s.Player); z.ApplyTo(s.Player);
    Assert.That(s.SuppliesCollected, Is.EqualTo(1));
    z.ResetObject(); z.ApplyTo(s.Player);
    Assert.That(s.SuppliesCollected, Is.EqualTo(2));
    z.ResetObject(); z.Kind = InteractionKind.Z_Speed; z.ApplyTo(s.Player);
    Assert.That(s.SuppliesCollected, Is.EqualTo(2));
}
[Test] public void CoreContactIsNotAKillAndDamageKillCountsOnce()
{
    var s = MakeSession(); s.Mission = MissionDefinition.Get(2); s.StartMatch();
    var contact = Make<EnemyController>("Contact"); contact.Initialize(s, s.Core);
    contact.TouchCore(); Assert.That(s.EnemiesDestroyed, Is.Zero);
    var kill = Make<EnemyController>("Kill"); kill.Initialize(s, s.Core);
    kill.ApplyDamage(60); kill.ApplyDamage(60);
    Assert.That(s.EnemiesDestroyed, Is.EqualTo(1));
}
```

- [ ] **Step 2: Run these tests red.** Expected counters remain zero or count duplicate/wrong events.
- [ ] **Step 3: Add safe objective hooks.** In ApplyTo reject a mismatched session player and non-Playing session before applying effects; retain standalone no-session test behavior. On successful Z, Consume before notifying session so trigger repeats cannot count. Enemy ApplyDamage records only transition HP > 0 -> HP 0, before Retire; TouchCore/cleanup never records a kill. Clear kill identity set during ResetRun. Increment counters and notify Changed without evaluating success early.

```csharp
// Near start of InteractionObject.ApplyTo:
if (Session && (Session.State != MatchState.Playing || target != Session.Player)) return false;
// After a successful Z has been consumed:
if (Kind == InteractionKind.Z && Session) Session.RecordSupply(this);
// In EnemyController.ApplyDamage, in the lethal branch:
if (HP <= 0) { if (Session) Session.RecordEnemyDestroyed(this); Retire(); }
```

- [ ] **Step 4: Freeze terminal damage.** In Projectile.ResolveAgainst return false when its assigned session is not Playing. In EnemyController.ApplyDamage likewise ignore damage outside its assigned Playing session. This prevents physics callbacks after a result changing the checkpoint or result display.
- [ ] **Step 5: Add boundary tests.** For each record API pass a source belonging to another session and assert counters unchanged; invoke ApplyTo while Paused and assert HP/counter unchanged. For mission 1 collect three supplies, kill core, Advance(75), assert Lost and unchanged saved progress. For mission 2 kill six distinct enemies, Advance(90), assert Won; call Advance again and assert counters stable. Run MissionObjective, Interaction and Combat suites; expected green.
- [ ] **Step 6: Commit.** `rtk proxy git commit -m "feat: connect mission quotas to real pickups and enemy defeats"`.

### Task 6: Make all three missions visually distinct in the existing arena

**Files:** Create `MissionWorldPresenter.cs`, `Assets/_Game/Editor/CampaignSceneBuilder.cs`, PlayMode `EnemyTacticsPlayTests.cs`; modify `DemoSceneBuilder.cs`, `CampaignController.cs`, extend EditMode `CampaignConfigurationTests.cs` (new here).

**Interfaces:** `MissionWorldPresenter.Apply(int missionId): void`; serialized `SpriteRenderer FloorOverlay`, `GameObject[] Themes` of length 3. `CampaignSceneBuilder.Configure(Scene scene, GameSession session, HudPresenter hud): void` constructs/reuses campaign objects in the specified scene. `CampaignController.World` reference; SelectMission calls World.Apply and assigns spawner RoleSequence after reset. Existing world fitting and interactions keep their current owners.

- [ ] **Step 1: Add configuration test using the existing builder test's scene-save/restore pattern.** Find `CampaignSceneBuilder` through reflection as current EditMode assembly does for BuildDemo (it cannot directly reference Assembly-CSharp-Editor). Configure twice in a temporary scene with required camera/canvas/session, then assert exactly one CampaignController, one MissionWorldPresenter and three distinct theme roots. Test actual Main integration at Task 8.

```csharp
// Assertions after two configurations; world is the sole presenter in that scene:
Assert.That(world.Themes.Length, Is.EqualTo(3));
for (int id = 0; id < 3; id++)
{
    world.Apply(id);
    for (int other = 0; other < 3; other++)
        Assert.That(world.Themes[other].activeSelf, Is.EqualTo(other == id));
    Assert.That(world.Themes[id].GetComponentsInChildren<Collider2D>(true), Is.Empty);
}
```

- [ ] **Step 2: Run `CampaignConfigurationTests` red.** Expected missing campaign/theme objects.
- [ ] **Step 3: Build the three themes.** Reuse imported `Assets/_Game/Art/ArenaWhite.asset`, `Kenney/Particles/circle_04.png`, and `Kenney/Icons/warning.png`. Name roots `Campaign world/Outpost`, `Campaign world/Supply Relay`, `Campaign world/Reactor Siege`. Overlay floor tint amber `(0.35,.23,.10,.22)`, teal `(.05,.4,.4,.22)`, purple `(.35,.1,.45,.22)` behind characters, above floor. Add non-colliding outpost beacons at (+/-6,+/-2.5), relay rings at (-4,2),(4,2),(0,-2.5), reactor rings at origin with scale 2/3/4 and pylons at (+/-5,+/-2). Use existing sprite units and camera to verify actual size; do not cover bullets, health bars or supply icons. Do not add a second background fitter or mutate the existing player's visual scale.

```csharp
public void Apply(int missionId)
{
    var colors = new[] { new Color(.35f,.23f,.10f,.22f),
        new Color(.05f,.4f,.4f,.22f), new Color(.35f,.1f,.45f,.22f) };
    MissionDefinition.Get(missionId); // reject invalid IDs before changing visibility
    for (int i = 0; i < Themes.Length; ++i) Themes[i].SetActive(i == missionId);
    FloorOverlay.color = colors[missionId];
}
```

- [ ] **Step 4: Add live tactical physics test.** In a PlayMode fixture create a session with spawner disabled, living player/core, and an initialized Skirmisher at (2,0), player at (3,0); enable tactics. Record Rigidbody position; call Step(.1f), yield `new WaitForFixedUpdate()`, assert x decreased. Call Stun(2), Step(.1f), yield; assert position unchanged. Pause, call Step(.1f), yield; assert unchanged. Repeat for every role's stun/pause guard. Destroy fixture objects and restore time scale on teardown; don't share EditMode helpers across assemblies.
- [ ] **Step 5: Wire and verify.** Invoke CampaignSceneBuilder at the end of existing DemoSceneBuilder.Configure after HUD exists. Assign role sequences 1/2/3 roles, SpawnInterval 5 for all. Run configuration/tactical PlayMode tests. Manually select each mission via inspector/controller in Editor and verify theme, role mix and objective feel at 1280x720; these are functional missions before final menus.
- [ ] **Step 6: Commit.** `rtk proxy git commit -m "feat: theme three campaign missions using existing arena assets"`.

### Task 7: Finish result navigation and visible campaign progress

**Files:** Create `CampaignScreenPresenter.cs`, PlayMode `CampaignFlowTests.cs`; modify `HudPresenter.cs`, `CampaignSceneBuilder.cs`, `CampaignController.cs`; retain existing `AudioService.cs` unless an integration test demonstrates a needed change.

**Interfaces:** `CampaignScreenPresenter.Campaign`, `Hud`, `ProgressPanel`, `ProgressText`, `MissionButtons[3]`, `ContinueButton`, `BackButton`, `RetrySaveButton`; `Bind(): void`, `OpenProgress(): void`, `CloseProgress(): void`, `Refresh(): void`. Add Hud `HomeButton`, `ProgressButton`, `CampaignScreens`, `MissionText`, `ObjectiveText`; `GoHome(): void`, `CancelStartTransition(): void`, `IsTransitioning: bool`, `BeginSelectedMission(): void`. One bind owns each button; bind/unbind symmetrically.

- [ ] **Step 1: Add real button-flow tests in Main.** Use `SceneManager.LoadSceneAsync("Main")` and find the configured Hud/Campaign, but replace store with a temporary store before starting gameplay. Use the existing PlayMode scene setup pattern; restore loaded scenes afterward. Drive `Button.onClick.Invoke()` instead of bypassing UI. Test both terminal states with the same navigation assertions:

```csharp
// Inside a UnityTest, after loading Main and selecting mission 0:
campaign.SelectMission(0); session.StartMatch();
session.Player.ApplyDamage(100); session.Advance(.02f);
Assert.That(hud.ResultPanel.activeSelf, Is.True);
Assert.That(hud.RetryButton.interactable && hud.HomeButton.interactable &&
    hud.ProgressButton.interactable, Is.True);
hud.ProgressButton.onClick.Invoke(); yield return null;
Assert.That(hud.CampaignScreens.ProgressPanel.activeSelf, Is.True);
Assert.That(hud.ResultPanel.activeSelf, Is.False);
hud.CampaignScreens.BackButton.onClick.Invoke(); yield return null;
Assert.That(hud.ResultPanel.activeSelf, Is.True);
hud.HomeButton.onClick.Invoke(); yield return null;
Assert.That(hud.StartPanel.activeSelf, Is.True);
Assert.That(session.State, Is.EqualTo(MatchState.Ready));
```

- [ ] **Step 2: Run `CampaignFlowTests` red.** Expected missing destinations/visibility or duplicate behavior.
- [ ] **Step 3: Extend HUD result presentation without reimplementing audio.** Keep result overlay over the arena; use existing win/loss sprites and sounds. Text distinguishes loss reason, intermediate success and campaign completion. Add three buttons in a 3-column row within panel bounds: Replay -> existing Session.Retry, Home -> GoHome, Progress -> OpenProgress. Replay is the separate gameplay destination. Add optional Next Mission only on nonfinal frontier success; route through ContinueCampaign then BeginSelectedMission. A completed replay's Next action uses current frontier, not replay ID+1.

```csharp
public void CancelStartTransition()
{
    loadingActive = countdownActive = false;
    loadingRemaining = countdownRemaining = 0;
    if (CountdownImage) CountdownImage.gameObject.SetActive(false);
    if (LoadingPanel) LoadingPanel.SetActive(false);
}
public void GoHome()
{
    CancelStartTransition();
    CampaignScreens.CloseProgress();
    Session.ReturnHome();
}
```

- [ ] **Step 4: Build actual Home/Progress destinations.** Home keeps logo/settings and adds three mission buttons with locked/completed/current labels plus Continue. Mission button selects and begins existing loading/countdown. Continue selects saved frontier. Progress is a dedicated full-screen panel listing all three missions, completion, recorded exit HP/Energy/Coins/Core HP and campaign completion. Back restores the originating Home or result screen; do not reset session just to view history. No-save case reads `No missions completed`. SaveError displays `Progress is in memory; saving failed` and details; Retry Save invokes RetrySave then Refresh. Save never occurs just from viewing Progress.
- [ ] **Step 5: Make visibility single-owner.** Add `CampaignScreenPresenter.IsOpen { get; private set; }`; OpenProgress/CloseProgress set it and request a HUD refresh through a new public `HudPresenter.RefreshPresentation(): void` wrapper around Refresh. Hud.Refresh respects IsOpen before enabling Start/Result/Settings panels. OpenProgress allowed only Ready/Won/Lost; cancel any pending loading/countdown before opening. Hide gameplay HUD/mobile controls outside Playing as today. BeginSelectedMission delegates to existing BeginStartCountdown; do not keep a second timer in campaign UI. Objective line stays visible during Playing even though existing compact HUD hides verbose labels; show mission title, quota count and survival timer. Settings/audio toggles retain existing behavior. CampaignScreenPresenter subscribes to Session.Changed and refreshes labels/buttons there; unsubscribe in OnDisable and before rebind. Refresh must not call RefreshPresentation, preventing mutual recursion.
- [ ] **Step 6: Add race/audio tests.** Bind HUD and CampaignScreens twice, click Replay and count Session.Reset: exactly one. BeginSelectedMission twice, GoHome during loading, wait longer than loading+countdown and assert Ready/no spawns; repeat during countdown. Subscribe to `AudioService.SfxPlayed`, complete mission, open/close Progress twice and assert Victory count 1; with SFX disabled count 0 but result text/art remains. Run both Won/Lost button tests, existing PresentationInput/Audio/MobileControls suites and new flow tests; expected green.
- [ ] **Step 7: Commit.** `rtk proxy git commit -m "feat: add complete result navigation and campaign progress screens"`.

### Task 8: Verify real scene wiring, reload and full campaign completion

**Files:** Modify `CampaignConfigurationTests.cs`, `CampaignFlowTests.cs`, `Main.unity`; modify `CampaignSceneBuilder.cs`/`DemoSceneBuilder.cs` only for integration defects found here.

**Interfaces:** Existing `BuildDemo.ConfigureProject()` remains the entry point. No new production API is required. Tests use the interfaces defined in Tasks 1–7 and actual scene components.

- [ ] **Step 1: Add the integration test before scene repair.** Configure Main twice using the existing reflection helper pattern and scene backup/restore. Assert one Session/Campaign/World/Screen presenter, exactly 3 mission buttons, non-null result button references, valid sprite/audio references and no duplicate onClick effects after binding. Preserve sentinel roots and unrelated build scene entries. Expected red until Main is repaired.
- [ ] **Step 2: Configure Main in Editor and inspect the diff.** Keep existing designer changes to input, camera, enemy/mine/projectile assets. Campaign construction must find/reuse named objects; no root-wide deletion. Save only after checking current scene ownership. Stage only task-owned hunks and generated campaign references.
- [ ] **Step 3: Add end-to-end campaign/reload test.** Disable auto-spawn for deterministic objective setup, not production configuration. Complete mission 0 via 90-second Advance. Destroy/recreate the controller with the same temporary file and Continue; assert mission 1 plus carried HP/Energy/Coins/Core state. Complete three real Z pickups using ApplyTo with ResetObject between consumed spawns, Advance(75); repeat controller reload; complete mission 2 by lethal damage to six actual initialized enemies, Advance(90).

```csharp
Assert.That(campaign.Progress.Completed, Is.True);
Assert.That(campaign.Progress.NextMission, Is.EqualTo(3));
Assert.That(campaign.Progress.Missions.All(m => m.Completed), Is.True);
Assert.That(campaign.ContinueCampaign(), Is.False);
Assert.That(campaign.SelectMission(0), Is.True); // completed campaign stays playable
Assert.That(hud.ResultText.text, Does.Not.Contain("MISSION 4"));
```

Assert final result copy *before* selecting replay, since selection legitimately hides results. Test restart-on-loss reloads the last success, not damaged current stats. Verify at least one genuine physics-trigger supply pickup in a separate PlayMode test (move player Rigidbody into Z and wait fixed updates), so direct method tests do not hide collider/wiring problems.
- [ ] **Step 4: Run full EditMode and PlayMode suites.** Use `-testFilter "CoreGuard.Tests"` with each platform and separate XML/log files. Confirm no new failures; investigate stale baseline tests against current code rather than weakening assertions to restore old README behavior. No claimed pass without fresh results.
- [ ] **Step 5: Perform a normal-speed playable acceptance run.** Start fresh, complete all three without F1; at least one controllable loss from player death and one core loss. Check Replay/Home/Progress on both results, checkpoint after restarting Play Mode, all three NPC decisions, EMP freeze, visual differences and quota readability. If missions are not realistically achievable with carried health, adjust spawn intervals/timing (and corresponding assertions) while retaining the specified distinct mechanics. Do not solve balancing by granting silent health refills contrary to checkpoint policy.
- [ ] **Step 6: Commit.** `rtk proxy git commit -m "test: verify wired campaign progression and result flows end to end"`.

### Task 9: Record acceptance evidence and hand off the improved game

**Files:** Modify `README.md`, `docs/progress.md`, `docs/acceptance.md`, `docs/asset-register.md`.

**Interfaces:** Documentation describes shipped code and evidence; no new runtime behavior.

- [ ] **Step 1: Run the presentation matrix.** At 1280x720 and 1920x1080 check Home, Settings, Progress, every mission HUD, loss, intermediate success and final win. Click every result destination; verify raycast targets, normal/hover/pressed/disabled states and no cut-off text. Listen once with SFX/BGM each enabled and disabled. Exercise existing mobile controls in the Editor and confirm they hide off-gameplay. Capture evidence to ignored `Logs/` with resolution and scenario in the filename.
- [ ] **Step 2: Replace stale player instructions.** README must describe Bullet/Rocket/Laser and Energy as current code does, mission objectives, win/loss meanings, Replay vs Continue, carryover, practice exclusion and save location. Keep Editor-demo scope; standalone export is not a new requirement. Link this plan as superseding only the new-requirements work, not erase historical plans.
- [ ] **Step 3: Add a requirement/evidence table in `docs/acceptance.md`.** Use these rows with actual test names/results and manual evidence paths filled from executed checks:

| Requirement | Automated evidence | Manual evidence |
|---|---|---|
| Game over feedback + 3 destinations | `CampaignFlowTests`, `AudioContractTests` | Player/core loss, each button, audible defeat |
| Three distinct missions | `MissionObjectiveTests`, `CampaignConfigurationTests` | Three normal-speed runs and theme screenshots |
| Saved state after each success | `CampaignSaveTests`, `CampaignLifecycleTests`, full flow | Stop/start Play Mode after each mission |
| Concrete final win + 3 destinations | Full campaign flow and result navigation tests | Final victory screen, audio and buttons |
| Three intelligent NPCs | `EnemyTacticsTests`, `EnemyTacticsPlayTests` | Intercept, kite, flank/commit demonstrations |

- [ ] **Step 4: Update progress and provenance.** Record exact current suite totals, date, XML paths, remaining manual limitations if any, and reused sprites in asset register. Never convert an unperformed manual check into a pass. Failed required acceptance means implementation is unfinished, not a documentation-only exception.
- [ ] **Step 5: Review diff and commit only these docs.** Run `rtk proxy git diff --check` and `rtk proxy git status --short`; confirm no Resources bundle, actual save files or logs are staged. `rtk proxy git commit -m "docs: document campaign controls persistence and acceptance evidence"`.

## Coverage and completion gate

| Spec item | Tasks |
|---|---|
| 1: Game over feedback | Existing feedback retained; 1, 7, 8, 9 |
| 1: At least three fully working separate destinations | 7, 8, 9 |
| 2: Three different levels/missions/world presentation | 1, 4, 5, 6, 8 |
| 2: Maintain/save player state after every completion | 2, 3, 8 |
| 3: Explicit win target | 1, 5, 7, 8 |
| 3: Win feedback and three destinations | 7, 8, 9 |
| Additional: Intelligent behavior for at least three NPCs | 4, 6, 8 |

Plan self-review: all requirement bullets have owners; the five Review Focus risks have explicit tests; new interfaces are declared in their owning tasks. Completion requires three playable distinct missions in the existing Main scene, persistence demonstrated across reload, three observable tactical roles, all six required win/loss navigation routes, passing fresh automated suites, and the manual presentation/audio acceptance record. No replacement game, asset hunt, migration, inventory subsystem or new deployment pipeline is part of this scope.

## Execution handoff

Recommend **Native execution** for speed: implement this ordered plan in-session, run each task gate, then request one independent whole-branch review. **Subagent-driven execution** instead provides a fresh implementer/reviewer per task and costs more context; interfaces here support either method. Review this plan and choose the method before implementation, as required by the explicitly invoked writing-plans skill.
