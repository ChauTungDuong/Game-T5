# Asset Register

The demo now uses a selected, local subset of the downloaded Kenney packs. The
original downloaded folders remain outside the Unity project as a backup. Each
copied pack keeps its license file beside the imported assets.

| Local path | Original asset | Pack / author | Source | License | Role / modification |
|---|---|---|---|---|---|
| `Assets/_Game/Art/Kenney/Tanks/` | Top-down tank bodies, barrels and bullets | Kenney, Top-down Tanks Remastered | [kenney.nl/assets/top-down-tanks-remastered](https://kenney.nl/assets/top-down-tanks-remastered) | CC0 | Player uses blue tank body/barrel; enemies use red tank body; core uses dark tank body; projectile uses blue bullet. Sprite import is configured as point-filtered and uncompressed. |
| `Assets/_Game/Art/Kenney/Particles/` | `circle_04`, `scorch_01`, muzzle/fire/flame/smoke/spark/magic sprites | Kenney, Particle Pack | [kenney.nl/assets/particle-pack](https://kenney.nl/assets/particle-pack) | CC0 | Core glow and mine marker use imported transparent particle sprites; remaining selected sprites are available for future transient VFX. |
| `Assets/_Game/Art/Kenney/Icons/` | Audio/music, warning, target, power and plus icons | Kenney, Game Icons | [kenney.nl/assets/game-icons](https://kenney.nl/assets/game-icons) | CC0 | Sound toggles display audio/music icons; gates and X/Y/Z use warning/target/power/plus icons. |
| `Assets/_Game/Art/Kenney/UI/` | `panel_glass`, `button_rectangle_depth`, Kenney Future fonts | Kenney, UI Pack - Space Expansion | [kenney.nl/assets](https://kenney.nl/assets) | CC0 | HUD panels/buttons use sci-fi sprites; labels use Kenney Future where imported. |
| `Assets/_Game/Audio/Kenney/` | `laserSmall`, `laserLarge`, `explosionCrunch`, `computerNoise`, `forceField`, `impactMetal` | Kenney, Sci-fi Sounds | [kenney.nl/assets/sci-fi-sounds](https://kenney.nl/assets/sci-fi-sounds) | CC0 | Bullet/rocket/mine, warning, shield and EMP clips are assigned by `DemoSceneBuilder`; generated tones remain fallback if an asset is unavailable. |
| `Assets/_Game/Art/Provided/Results/` | `YOU WIN.png`, `YOU LOSE.png`, plus alternate congratulations/game-over/start artwork | User-provided bundle `Resources-20260906T110750Z-1-001` | Local source: `Resources/Other Assets/Images/` | Not supplied; verify permission before distribution | Win/loss overlays use `YOU WIN.png` and `YOU LOSE.png`; `start1.png` is used for the start button, while the alternate banners remain available for later UI variants. |
| `Assets/_Game/Art/Provided/Countdown/` | `zero.png`, `one.png`, `two.png`, `three.png` | User-provided bundle `Resources-20260906T110750Z-1-001` | Local source: `Resources/Other Assets/Images/` | Not supplied; verify permission before distribution | Center-screen 3–2–1 countdown shown before the match enters Playing. |
| `Assets/_Game/Art/Provided/Explosion/` | `explosion_01.png`–`explosion_09.png` | User-provided bundle `Resources-20260906T110750Z-1-001` | Local source: `Resources/Other Assets/Explosion_Effects/keyframes/` | Not supplied; verify permission before distribution | Nine-frame explosion animation spawned by projectile and mine detonations. |
| `Assets/_Game/Audio/Provided/` | `click.ogg`, `explosion.wav`, `congratulation.wav`, `gameover.wav`, `music.mp3` | User-provided bundle `Resources-20260906T110750Z-1-001` | Local source: `Resources/Audios_Musics/` | Not supplied; verify permission before distribution | UI click, detonation, victory/defeat feedback and the gameplay music loop; generated tones remain fallback if an asset is unavailable. |
| `Assets/_Game/Art/ArenaWhite.asset` | Generated 1×1 white texture and Sprite | Core Guard / `DemoSceneBuilder` | Local generated asset | Project-owned | Arena walls and fallback visuals. |

The local `License.txt`/`license.txt` files are copied from their respective
packs. Kenney attribution is not required by CC0, but the source links are kept
here for provenance. The provided resource bundle did not include license or
readme metadata; its original folder remains in the project root as an untouched
backup, and its selected assets should not be redistributed until permission is
confirmed.
