# Progress

- Project / branch / Unity: Core Guard / `feature/core-guard-gameplay-updates` / `6000.3.23f1`
- Runtime scope: Unity Editor demo only; standalone Windows build/export is intentionally out of scope.
- Entry point: run `Core Guard → Configure Project`, then open `Assets/_Game/Scenes/Main.unity` and press Play.
- Implemented: playable arena/session flow, normalized movement and mouse aim, player/core/enemy health bars, equal-sized player/enemy tank visuals, three weapons, enemy projectiles, Shield/EMP, deterministic X/Y/Z 5-second lifecycle, six effects, audio toggles, four-beep restricted-zone alert, compact HUD, 3–2–1 start countdown, win/loss result artwork and audio, animated explosion feedback on player/enemy death, and repeatable F1 Demo Mode.
- Provided resource pass completed: selected result/countdown/explosion PNGs and click/explosion/victory/defeat clips are imported under `Assets/_Game/Art/Provided/` and `Assets/_Game/Audio/Provided/`; `DemoSceneBuilder` assigns them on every configuration.
- Gameplay follow-up completed: Mine limit now reports `MINE LIMIT n/3` in the HUD; speed is shown live; keyboard weapon selection refreshes the HUD; the restricted-zone ring blinks independently of SFX; Y triggers once per entry and can trigger again after leaving and re-entering.
- Automated verification: the last complete pre-death-feedback run passed EditMode **59/59** and PlayMode **12/12** on 2026-09-15 with Unity `6000.3.23f1`; the new tank-size/death-explosion regression is added but its final rerun is pending because the Unity Editor currently has the project open.
- Idempotence verification: `ConfigureProject` completed successfully twice on the real `Main.unity`; scene references for `FeedbackText` and `WarningRing` are serialized and no duplicate gameplay roots were introduced.
- Evidence: `Logs/coreguard-editmode-provided.xml`, `Logs/coreguard-playmode-provided.xml`; Unity logs are ignored local artifacts.
- Manual checks left for the owner in Unity: confirm the 3–2–1 images, explosion animation, result banners and supplied audio by ear, then movement/aim/fire, enemy shots, Q/EMP, X/Y/Z re-entry behavior, audio toggles, pause/retry, win/loss and readability at 1280×720 and 1920×1080.
- No known code-level task remains blocking the Editor demo. Remaining verification is the new 13-test PlayMode rerun plus physical input/audio perception and long-run performance, which require the owner's Unity desktop session.
- Historical Windows build evidence is retained in the earlier T1 report but is not a current handoff requirement.
