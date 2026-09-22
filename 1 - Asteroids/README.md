# Portfolio - Asteroids

A sci-fi take on Asteroids: fly a starfighter around a wrapping playfield, shatter five kinds of rocks, dodge mines,
cluster bombs, comets and black holes, fight flying saucers, alien wasps and four bosses, and collect crystals, weapon
upgrades, shields and power-ups. A campaign of twelve missions in four sectors (three stars each) introduces one new
thing per mission, the hangar unlocks three more ships with the stars you earn, and an endless mode runs through every
sector until you run out of ships.

## Playing

| Control | Keyboard | Gamepad | Touch (phones and tablets) |
| --- | --- | --- | --- |
| Turn | A / D, Left / Right | Left stick | Left thumb anywhere on the lower left: a stick that turns the ship toward where it points |
| Thrust / brake | W / S, Up / Down | Left stick up / down | The stick thrusts once the nose points its way, harder the further it is pushed |
| Fire (hold) | Space | A or right bumper | Hold FIRE (shows the current weapon) |
| Dash (short burst, invulnerable) | Left Shift | X or left bumper | DASH (its ring shows the recharge) |
| Nova bomb (clears the screen) | B | B or Y | NOVA (shows the bombs left) |
| Pause | Escape or the pause button | | Back button or the pause button |

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
- **Multiplayer.** The **Multiplayer** button of the mission select opens the lobby: up to four pilots fly a mission
  of the campaign together, each with their own ships, hull, weapons and score, against the same rocks and enemies.
  Kills score for the pilot who made them, a pickup goes to whoever reaches it first, and a pilot without ships watches
  the others. The mission is won together, and lost when nobody flies any more.
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
| `OnlineGameController` (assembly `Gamebox.Online`) | `AsteroidsOnlineController` |

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
| `Scripts/Controller/AsteroidsOnlineController.cs`, `AsteroidsGameManager.Coop.cs` | The online mission: rooms and missions for the lobby, the pilots of the room, the manager's co-op mode (own lives and score, a fixed playfield, no director on the clients that only show the world) |
| `Scripts/Controller/FieldReplication.cs`, `IFieldLink.cs`, `RemoteShip.cs` | Keeps the playfields in step: the simulator announces, updates and removes bodies, the others show them as puppets and report their hits, shots and claims; the stand-ins of the other ships |
| `Scripts/Model/BodyCodec.cs`, `BodyRegistry.cs`, `FieldMath.cs`, `ShipPose.cs`, `CoopRules.cs` | The plain parts of it: what a body is on the wire, the net ids, distances on a field that wraps, the flags of a ship's pose, the options of a room |
| `Scripts/Controller/AsteroidsOnlineTour.cs` | Development builds only: `-asteroids-online host <folder>` and `-asteroids-online join <folder>` fly a mission together between two running players (`-asteroids-level <index>`, `-asteroids-lives <n>`) |

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

## Multiplayer

A shared mission has one world and many ships. The rocks, enemies and bosses of this game move with
`UnityEngine.Random` all over, so two clients cannot simulate the same world; one of them owns it. **The client of the
room's host simulates the world** exactly as in the single player game (waves, spawns, the objective, the boss), and
**every client flies its own ship** and decides what hurts it.

- **Ships** travel as the poses of the base server (`OnlineGameController.SendPose`: position, velocity, heading, flags
  for thrust, dash, shield and so on). The other pilots show as `RemoteShip` stand-ins of the ship prefab, moved with
  BaseGame's `PoseInterpolator` across the wrapping playfield, with the seat's colour and the pilot's name. On the
  simulator, enemies, mines, homing shots and spawn placement look for the nearest ship instead of "the" player.
- **The world** travels as puppets (`FieldReplication`). Every body that enters the simulator's field gets a net id and
  is announced as a `FieldBody` row; the other clients deploy the same prefab from the same pool as a puppet that flies
  on by itself (which is exact for rocks) and runs no rules of its own. Bodies that steer, were pushed or were hit are
  reported again a few times a second, and bodies that leave go with the reason and the seat that did it, all in one
  call per update (`SyncField`), so a rock and its fragments change hands together.
- **What a pilot does to the world** goes to the simulator: a shot that hits a puppet is reported (`ReportHits`) and
  the simulator applies it with that pilot as the attacker, so splits, loot and the score for the kill follow from
  there; a pickup is claimed (`ClaimBody`) and the server gives it to the first claimer; shots and nova bombs are shown
  to the others (`FireShots`, `SignalShip`). Enemy shots are puppets that hit the local ship on every client, and
  blasts go out as signals every client applies to its own ship.
- **The mission.** The playfield has one fixed size (16:9 at the usual height) that the camera fits on any screen. Every
  pilot has the ships the host set for the room; a pilot without ships reports their score and watches. The simulator
  reports waves and objective for the others' HUD and says when the mission is won; it is lost when nobody flies any
  more, and abandoned when the host leaves. The Chrono Field does not slow a shared world. Online missions do not pause
  (the pause button opens the match menu), are not saved and leave the campaign progress as it is.

## Phones and tablets

The game runs on Android with the shared mobile code of BaseGame (see its README, "Phones and tablets"):

- **Touch controls.** `ShipTouchControls` (built by `AsteroidsInterfaceBuilder.BuildTouchControls`) holds a floating
  `VirtualJoystick` for the left thumb and the FIRE, DASH and NOVA `TouchButton`s for the right one. The player's input
  (`PlayerShipInput`) reads them next to the keyboard and gamepad: `PlayerShipInput.Steer` turns the ship toward the
  stick, easing off as the nose gets there, and thrusts once the nose is within about 70 degrees of it (covered by
  `ShipInputTest`). The controls show only when the game is played by touch.
- **HUD.** With touch the hull and shield panel moves under the score and the weapon panel under the lives, the dash
  ring and the B key hint give way to the DASH button, and the tips move to the top (`TouchLayout`). The missions whose
  tips name keys have touch wording (`AsteroidsLevel.touchHints`, set by the content builder).
- **Screens.** The canvas expands from 1920 x 1080 to any screen shape and every screen keeps its texts and buttons in
  a safe area. The playfield is as wide as the camera shows, so wider phones see more space. The back button closes
  the settings and the hangar, pauses and resumes a mission, and leaves the mission select for the launcher (or closes
  the app when the game is built on its own).
- **Builds.** `Gamebox > Android > Build APK` builds `Build/Android/Asteroids.apk` (`com.skinnerboxes.asteroids`, with the
  game's icon).

## Server

The game's [SpacetimeDB](https://spacetimedb.com) module is the folder `Server` next to `Assets` (Unity does not show
it; open `Server/StdbModule.csproj` in the IDE). It is the Gamebox base server of the BaseGame repository (`BaseServer`:
users and login, connecting and disconnecting, the player profile) plus `Server/Lib.cs`, which declares the same
`public static partial class Module` to add the tables and reducers of this game and implements the base server's
partial methods to react to its events. It does not simulate; it relays, keeps the bodies for whoever needs them, checks
who may write what, arbitrates pickups and ends the mission:

| In `Server/Lib.cs` | What |
| --- | --- |
| `Mission` (public) | The mission of a room: who simulates it, the playfield, the ships per pilot, waves and objective for the HUD, the outcome. |
| `FieldBody` (public) | One row per body of the simulator's playfield: kind, position, velocity, health, who claimed it. Written only by the simulator. |
| `BodyGone`, `FieldSignals`, `ShotVolley`, `HitReport`, `ShipSignal` (public event tables) | Moments that are sent and never stored: a body left (and why, and by whom), blasts and comet warnings, the shots of a ship, the hits a pilot landed, nova bombs and the like. |
| `MissionTimer` (scheduled) | Ends the room a moment after the victory, so every pilot's final score is in. |
| `PilotStats` (public) | Missions flown and won, the best score of a pilot. |
| `SyncField(spawns, moves, exits, signals)` | The simulator's update of the playfield, in one call. |
| `ReportMission`, `CompleteMission` | The simulator reports waves and objective, and the victory. |
| `FireShots`, `ReportHits`, `SignalShip`, `ClaimBody` | What every pilot sends: shots to show, hits on puppets, signals of the ship, the claim on a pickup. |
| `ConfigureRoom`, `OnRoomStarted`, `OnMemberLeft`, `OnRoomFinished`, `OnRoomCleared`, `OnUserDeleted` | The base server's extension points: the limits of a room, the mission row, the end when the simulator leaves, the stats, cleaning up. |

- `Packages/manifest.json` references the SpacetimeDB SDK and the base server package
  (`com.skinnerboxes.baseserver`, `file:../../../BaseGame/BaseServer`); `Server/StdbModule.csproj` imports
  `BaseServer.props` from there, which compiles the base server into this module.
- `spacetime.json` names the database (`skinnerboxes-asteroids`, on the `local` server) and where the client bindings go:
  `Assets/Scripts/Server/Bindings`, namespace `Portfolio.Asteroids.Server`.
- After a change to `Server/Lib.cs`: `Gamebox > Server > Publish Module` and `Generate Client Bindings` in the editor,
  or `spacetime publish` and `spacetime generate` in this folder. Generating also writes this game's
  `GameServerClient` (`Assets/Scripts/Server`), the component that connects, logs the player in and keeps the profiles,
  the rooms and the poses. The scene builder puts it into the scene together with the online controller and the lobby
  (`OnlineInstaller`, object "Online"); it connects to `http://127.0.0.1:3000` (`spacetime start`) when the lobby opens.
- Several players on one machine: start every instance of a build with its own `-gamebox-identity <slot>`;
  `-gamebox-server <address>` and `-gamebox-database <name>` point a build at another server or database.

The BaseGame README ("Online play") describes the shared client, `BaseServer/README.md` the server side.

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

Escape (the back button on a phone) pauses the game and opens the shared menu; F1/F4/F5 restart, save and load a
mission, and Enter launches the selected mission. Saving needs the PlayerPrefs storage strategy.
