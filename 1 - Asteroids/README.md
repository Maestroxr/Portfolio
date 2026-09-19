# Portfolio - Asteroids

A sci-fi take on Asteroids: fly a starfighter around a wrapping playfield, shatter five kinds of rocks, dodge mines,
cluster bombs, comets and black holes, fight flying saucers, alien wasps and four bosses, and collect crystals, weapon
upgrades, shields and power-ups. A campaign of twelve missions in four sectors (three stars each) introduces one new
thing per mission, the hangar unlocks three more ships with the stars you earn, and an endless mode runs through every
sector until you run out of ships.

## Playing

| Control | Keyboard | Gamepad |
| --- | --- | --- |
| Turn | A / D, Left / Right | Left stick |
| Thrust / brake | W / S, Up / Down | Left stick up / down |
| Fire (hold) | Space | A or right bumper |
| Dash (short burst, invulnerable) | Left Shift | X or left bumper |
| Nova bomb (clears the screen) | B | B or Y |
| Pause | Escape or the pause button | |

- **Ship.** The hull takes damage from collisions and enemy fire; the shield absorbs hits first and recharges from
  shield cells. Losing the hull costs a ship (and one weapon level), and you respawn with a moment of invulnerability.
  The dash throws the ship forward through danger and recharges in a couple of seconds.
- **Rocks.** Plain rocks split into smaller ones, and the small ones are worth the most. Ore rocks are tougher and
  drop crystals, magma rocks explode and set off everything around them, ice shatters into a spray of fast shards, and
  void crystals release shards that home in on the ship.
- **Hazards and enemies.** Proximity mines arm when you come close (their blast breaks rocks too), cluster bombs count
  down and burst into shrapnel, comets cross the sector after a warning lane, black holes pull everything in, flying
  saucers shoot back, and alien wasps hunt the ship. Supply pods drift through and drop a guaranteed pickup.
- **Bosses.** The Rock Titan throws rocks and shrapnel, the Raider Mothership drops mines and calls in saucers, the
  Hive Queen spawns wasps, and the Dreadnought fires homing missiles. Every boss gets angrier as it weakens.
- **Pickups.** Crystals (points, and the goal of collection missions), repair kits, shield cells, spare ships, nova
  bombs, weapon crates and power-ups. A weapon crate switches to its weapon and raises the weapon level (up to four):
  Pulse Blaster, Scatter Cannon, Lancer Laser (pierces) and Seeker Missiles (home in). The timed power-ups are
  Overdrive (double fire rate), Tractor Magnet (pulls pickups in), Chrono Field (slows everything but you) and Wing
  Drones (two drones fight at your side).
- **Score.** Kills in quick succession build a combo that multiplies their points (x2 from 5 kills up to x5 from 40).
  Every ship left at the end of a mission is worth a bonus.
- **Stars.** Complete the mission, lose no ships, and reach the mission's score goal (shown on the mission select).
  Missions unlock in order, a sector's first mission also needs a number of stars, and the ships in the hangar unlock
  with the total stars. The endless mode opens after three completed missions and keeps your best wave and score.

| # | Mission | Sector | Objective | New |
| --- | --- | --- | --- | --- |
| 1 | First Light | Kepler Belt | Clear 3 waves | Rocks, supply pods |
| 2 | Ore Rush | Kepler Belt | Collect 12 crystals | Ore rocks and crystals |
| 3 | Titan | Kepler Belt | Destroy the boss | Boss: Rock Titan |
| 4 | Minefield | Crimson Expanse (3 stars) | Clear 3 waves | Proximity mines, Scatter Cannon |
| 5 | Magma Run | Crimson Expanse | Clear 3 waves | Magma rocks, comets |
| 6 | Raider Mothership | Crimson Expanse | Destroy the boss | Flying saucers, boss: Raider Mothership |
| 7 | Shatter Point | Frost Rings (8 stars) | Clear 3 waves | Ice rocks, Lancer Laser |
| 8 | Event Horizon | Frost Rings | Survive 90 seconds | Black holes, cluster bombs |
| 9 | The Hive | Frost Rings | Destroy the boss | Alien wasps, boss: Hive Queen |
| 10 | Crystal Storm | Void Core (15 stars) | Clear 3 waves | Void crystals, Seeker Missiles |
| 11 | Gauntlet | Void Core | Survive 120 seconds | Everything at once |
| 12 | Dreadnought | Void Core | Destroy the boss | Boss: Dreadnought |
| - | Deep Field | all four | Endless | Sectors change every 5 waves, a boss every 10 |

## Base game integration

The game is built on the base classes of the BaseGame package (`com.skinnerboxes.basegame`, assembly
`BaseGameAssembly`, namespace `Gamebox`):

| Base class / interface | Implementation |
| --- | --- |
| `BaseGameManager` | `AsteroidsGameManager` |
| `PlayerBase` | `AsteroidsPlayer` |
| `OfflineGameController` | `AsteroidsController` |
| `GameUI` | `AsteroidsUI` |
| `SettingsUI` | `AsteroidsSettingsUI` |
| `GameSettings` | `AsteroidSettings` |
| `Campaign` | `AsteroidsCampaign` (`Config/Campaign/AsteroidsCampaign.asset`): sectors with star gates, the endless unlock |
| `GameLevel` | `AsteroidsLevel`: objective, waves, boss, sector theme, loot tables and score goal |
| `CampaignProgress` | `AsteroidsProgress`: stars and best score per mission, the endless record, the chosen ship |
| `PrefabPool<T>` | `AsteroidPool`, `EnemyPool`, `HazardPool`, `ShotPool`, `LootablePool`, `ExplodablePool`, `RewardPool`, `EffectPool`, `PopupPool` |

The shared menu of the BaseGame package is the pause menu (restyled to match) and hosts the settings panel (ships,
hull strength, asteroid speed, spawn rate, explosion radius); the game adds its own canvas with the mission select,
the hangar, the HUD and the results screen. The `GameDefinition` asset under `Assets/Resources/Games` registers the
game with the BaseGame launcher. Campaign progress is saved automatically in the game's storage under keys prefixed
with the game type; the save and load buttons of the menu save and restore a mission in progress.

## How it works

| Code | Role |
| --- | --- |
| `Scripts/Model/*` | Plain C# rules and data: asteroid, weapon and power-up rules, the wave director, mission objectives, score and combos, flight simulation, levels, the campaign and progress |
| `Scripts/Controller/SpaceField.cs` | Every body in play: circle collisions in the playfield plane, wrap-around, blasts and chain reactions |
| `Scripts/Controller/SpawnService.cs` | Spawns rocks, hazards, enemies, bosses and pickups from the pools for the wave director |
| `Scripts/Controller/*` | The ship, the bodies (`Shootable` > `Asteroid`, `Explodable` > `Mine` / `ClusterBomb`, `Enemy` > `Saucer` / `Wasp`, `Boss`, `Comet`, `GravityWell`, `Shot`) and the pickups (`Reward` subclasses) |
| `Scripts/View/*` | Camera, backdrop (sector themes with nebula, planet and light), effects, audio, ship visuals and the interface |
| `Scripts/Controller/AsteroidsAutopilot.cs` | Flies the ship by itself; enable it on the ship in play mode to test a mission end to end |

Everything the game shows is generated by the editor code in `Assets/Editor` (menu **Asteroids**):

- **Rebuild Art**: textures (nebula, planets, ore veins, lava cracks, particle sprites, a reflection environment),
  interface sprites and icons drawn from signed distance functions, materials over the StarSparrow and asteroid model
  packs in `Art/`, low-poly models built in code (`MeshBuilder`, `SpaceModels`: saucers, drones, pickups, ice and
  crystal rocks), the sound effects and three music loops from a small synthesizer, and the hangar pictures.
- **Rebuild Prefabs**: the hangar ships, rocks, hazards, enemies, shots, pickups, effects and the four bosses.
- **Rebuild Campaign**: the four sector themes, the loot tables, the twelve missions and the endless mode, the
  post-processing profile and the launcher entry.
- **Rebuild Scene**: `Scenes/Asteroids.unity` with its lighting, backdrop, pools, effects, audio and interface.
- **Build Everything** runs all four steps. **Debug** unlocks every mission or resets the progress of this editor.

The generators write to fixed paths and update existing assets in place (GUIDs and references are kept). Values that
belong to the content (missions, waves, theme colours, ship stats) live in the builders, so hand edits to those
assets are overwritten by the next rebuild. The scene is rebuilt from scratch, so its diff after a rebuild is large.

## Project setup

- Unity 6000.6.0f1 with URP 17.6.0. The render pipeline assets are the ones that ship in the BaseGame package.
- This project and BaseGame reference each other in place. Nothing is copied either way, so the two repositories
  have to sit next to each other (`BaseGame` beside `Portfolio`):
  - `Packages/manifest.json` references BaseGame as the local package `com.skinnerboxes.basegame`
    (`file:../../../BaseGame/Assets`), which provides the base classes, the shared menu and the URP assets.
  - `Assets/package.json` makes this project's `Assets` folder the package `com.skinnerboxes.asteroids`. BaseGame's
    manifest references it (`file:../../Portfolio/1 - Asteroids/Assets`) and lists the game in its launcher.
- The same files therefore serve two projects and are mounted at `Assets/` here and at `Packages/com.skinnerboxes.asteroids/`
  in BaseGame, so nothing in them may depend on either location. The generators find their root through the package
  info of their assembly; the `GameDefinition` asset re-derives its scene path from its scene reference in the editor,
  and the launcher falls back to the scene name. The play tests find their scene through `TestScenes`
  (`Assets/Tests/Runtime/PlayerTest.cs`).
- Run the generators in one editor at a time: an editor that imports the files while the other one writes them can
  hold a lock on a file and make the save fail.
- The game uses no physics and no custom layers; collisions are circles in the playfield plane (`SpaceField`).

## Notes

Escape pauses the game and opens the shared menu; F1/F4/F5 restart, save and load a mission, and Enter launches the
selected mission. Saving needs the PlayerPrefs storage strategy.
