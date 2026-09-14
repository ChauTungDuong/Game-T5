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
| `Assets/_Game/Audio/Kenney/` | `laserSmall`, `laserLarge`, `explosionCrunch`, `computerNoise`, `forceField`, `impactMetal`, `spaceEngineLow` | Kenney, Sci-fi Sounds | [kenney.nl/assets/sci-fi-sounds](https://kenney.nl/assets/sci-fi-sounds) | CC0 | Bullet/rocket/mine, warning, shield, EMP and ambient loop clips are assigned by `DemoSceneBuilder`; generated tones remain fallback if an asset is unavailable. |
| `Assets/_Game/Art/ArenaWhite.asset` | Generated 1×1 white texture and Sprite | Core Guard / `DemoSceneBuilder` | Local generated asset | Project-owned | Arena walls and fallback visuals. |

The local `License.txt`/`license.txt` files are copied from their respective
packs. Kenney attribution is not required by CC0, but the source links are kept
here for provenance.
