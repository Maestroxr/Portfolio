# Portfolio - Asteroids

A sci-fi take on Asteroids: fly a starfighter around a wrapping playfield, shatter five kinds of rocks, dodge mines,
cluster bombs, comets and black holes, fight flying saucers, alien wasps and four bosses, and collect crystals, weapon
upgrades, shields and power-ups. A campaign of twelve missions in four sectors (three stars each) introduces one new
thing per mission, the hangar unlocks three more ships with the stars you earn, and an endless mode runs through every
sector until you run out of ships.

A second mode, **Planet Strike**, is a vertical scroller in the manner of Raptor: Call of the Shadows. The ship flies
low over the surface of three worlds while the ground scrolls under it, shoots down aircraft and destroys ground
targets for money, buys and sells weapons and shields in the Supply Room between missions, and meets a boss at the
end of every mission of the nine-mission campaign **SHADOW STRIKE** (see [Planet Strike](#planet-strike)). Both
modes can be flown together online.

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

## Planet Strike

The PLANET STRIKE tab of the mission select holds the second campaign. Its rules follow Raptor (checked against the
released source of the DOS game) wherever they make sense here.

| Control | Keyboard | Gamepad | Touch |
| --- | --- | --- | --- |
| Fly (8 ways, the nose always points up) | W A S D, arrows | Left stick | The stick moves the ship directly |
| Fire everything you carry (hold) | Space | A or right bumper | Hold FIRE |
| Next special weapon | Left Shift, Right Shift, Q, K, Tab | X or left bumper | WEAPON |
| Megabomb | B, E, L | B or Y | MEGA |
| Pause | Escape | | Back button or the pause button |

- **The pilot.** You fly one ship per mission. Energy (0-100) is its hull; it is not refilled between missions, it
  regenerates one point every 4 seconds while you are not firing (never on Elite), and the Supply Room sells it.
  Phase shields (up to 5, 100 points each) take every hit first. With energy at 10 or less and no shield left, every
  hit also destroys a weapon ("WEAPON DESTROYED"; a spare copy takes its place, the machine gun is never lost).
  Energy at 0 loses the mission.
- **Money is the score.** Every kill pays its bounty, and money pickups (a credit orb, or the cargo of huts, depots and
  transports) pay their value. A mission that is won is paid into the wallet when the ship lands; a mission that is
  failed or abandoned leaves the pilot exactly as it took off, as reloading a pilot in Raptor does. Replaying a
  mission pays again. Strike missions cannot be saved half way.
- **Air and ground.** Aircraft fly their formations across the screen and can be rammed (both of you take damage);
  ground units, from turrets to tanks, boats and fuel depots, scroll with the ground, are flown over, and leave
  craters. Weapons hit air units, ground units or both. Fuel tanks and depots explode and set off their neighbours.
- **Weapons.** The machine gun, the plasma cannon and the micro missiles fire whenever you fire; one special weapon,
  chosen with the WEAPON key, fires with them: dumbfire missiles, an auto-tracking mini-gun, a laser turret, missile
  pods, air-to-air and air-to-ground missiles, bombs, a pulse cannon, the deathray and the twin laser. The megabomb
  clears every enemy shot and hits everything on screen.
- **Bosses.** Every mission ends with a boss made of parts: turrets, launchers and modules die one by one, and a part
  (or the core) behind a shield opens once the parts of the tier before it are gone. The scroll stops for the fight.
  Without the Ion Scanner the boss bar shows only the boss's name.
- **Difficulty** (on the mission details): Rookie halves the damage you take and the bosses' health, Veteran is the
  campaign as designed, Elite adds more enemies and stops the energy from regenerating.
- **Stars.** Complete the mission; destroy 70% of the hostiles that came on screen; take at most 30 damage. The second
  and third sectors need 4 and 10 strike stars.

The Supply Room (a button on the mission details and on the results) buys and sells at half price:

| Item | Price | | Item | Price |
| --- | --- | --- | --- | --- |
| Energy (25 points) | 10,000 | | Air/Ground Missiles | 110,000 |
| Ion Scanner | 10,000 | | Dumbfire Missiles | 145,200 |
| Megabomb (up to 5) | 32,250 | | Micro-Missile Launcher | 175,600 |
| Air/Air Missiles | 63,500 | | Missile Pods | 204,950 |
| Phase Shield (up to 5) | 78,500 | | Auto-Track Mini-Gun | 250,650 |
| Plasma Cannon | 78,800 | | Laser Turret | 512,850 |
| Bombs | 98,200 | | Pulse Cannon | 725,000 |
| | | | Deathray | 950,000 |
| | | | Twin Laser | 1,750,000 |

| # | Mission | World | Boss | New |
| --- | --- | --- | --- | --- |
| 1 | Dust Devil | Ares Flats (desert) | Sand Crawler | The scrolling ground, air and ground targets, money |
| 2 | Refinery Row | Ares Flats | Refinery Guardian | Exploding fuel tanks and depots, gunships |
| 3 | Offshore | Ares Flats (coast and sea) | Sea Fortress | Gunboats, bombers, kamikaze divers |
| 4 | Green Hell | Verdant Delta (jungle), 4 stars | Twin Rotor | Interceptors from behind, phase shields |
| 5 | Delta Run | Verdant Delta (rivers) | Twin Silos | Transports with cargo, a boss without a core |
| 6 | Night Raid | Verdant Delta (city at night) | Skyhammer | Sky mines |
| 7 | Moonbase | Outer Colonies (moon), 10 stars | Dome Fortress | Laser towers |
| 8 | Magma Works | Outer Colonies (volcanic) | Foundry Crawler | Heavy armour, lava lakes |
| 9 | Shadow Station | Outer Colonies (station) | The Shadow | Three rings of defence, three phases |

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

## Themes

Everything the game shows is held by a theme, an `AsteroidsTheme` asset (`Scripts/Model/AsteroidsTheme.cs`, a
`GameTheme` of the BaseGame package). Point the game at another theme and the whole look changes.

| Part of the theme | What it holds |
| --- | --- |
| `ships` | per hull: the model, its material, scale, engine colour and hangar picture (`ShipVisuals.SetModel` reads it) |
| `asteroids` | per kind of rock: the shape variants, the material and the hit flash colour (`Asteroid.Configure`) |
| `shots`, `enemies`, `pickups`, `effects` | the materials of every shot, enemy, hazard and boss, the pickups' cores, icons and HUD sprites, the particle, glow, flame and shield materials |
| `backdrop`, `sectors` | the far space quad, the planet's atmosphere, halo and rings, and the four sectors (`SectorTheme`, named like the ones the missions use) |
| `strikeThemes` | the worlds of Planet Strike (`StrikeTheme`); a theme with one world shows every strike mission on it |
| `ui`, `icons`, `strikeIcons`, `palette` | the interface sprites (panels, buttons, frames, hexagons, bars, overlays, stars, weapon icons), the icons by name and the interface colours |
| `Fonts`, `Menu` | the title and body fonts with their materials, and the look of the shared pause menu (`MenuSkin`) |
| `Slots` | the same art by key for the generic themed parts: the key of a part is the path of the asset it was built with, relative to `Art/` (`Materials/Rock`, `Models/Saucer`, `Icons/Star`) |

The builders put the base package's themed parts on everything they make: a `ThemedRenderer` on every renderer and a
`ThemedMesh` on every mesh of the prefabs and the backdrop (`Editor/ThemeKeys.cs`), a `ThemedImage` on every image and a
`ThemedText` on every text of the game's canvas, and a `ThemedMenu` plus `AsteroidsMenuLook` on the shared menu. The
game's own code reads the typed parts through `AsteroidsThemes` (`Scripts/Model/AsteroidsThemes.cs`): the hull's model,
the rocks' shapes, the mission's sector (`AsteroidsThemes.Sector`) and strike world (`AsteroidsThemes.Strike`), the
stars, weapon, item and hangar pictures.

Two themes ship with the game (`Config/Themes/Game`):

- **Classic** (`Classic.asset`), the game as it was built: the StarSparrow hulls in their paint, textured rocks,
  nebulae and planets, the six worlds of the strike mode, the chamfered cyan interface. The game starts with it.
- **Arcade 80s** (`Arcade80s.asset`), a vector arcade cabinet of 1985: every hull, rock, enemy, boss and ground unit
  drawn as neon line art (a hand-written unlit shader, `Art/Themes/Arcade80s/Shaders/NeonSurface.shader`, draws the
  edges of the models from coordinates baked into wire copies of them: creases bright, the other edges dim), faceted
  low-poly rocks, shots as bright lines, a synthwave sky over a perspective grid with a striped low sun
  (`SynthwaveBackground.shader`) in four colour schemes for the four sectors, a neon grid world for Planet Strike
  ("Grid", `NeonGround.shader`, every kind of ground), wire globes for planets, neon-outlined panels, buttons and icons,
  scan lines and a CRT vignette over the interface, and two arcade fonts: Press Start 2P for titles and Orbitron for
  everything else.

The Arcade 80s art is generated by the same builders as the Classic art, from a theme spec
(`AsteroidsArtBuilder.Arcade` in `Editor/AsteroidsArtBuilder.Theme.cs`: its palette, fonts, the colour of every kind
of material, the edge rules): the icons and panels are the Classic drawings traced as neon lines, every material of the
Classic art gets a neon counterpart of its kind, and every mesh drawn with a lit material gets a wire copy. Its files
are in `Art/Themes/Arcade80s`, `Prefabs/Themes/Arcade80s` and `Config/Themes/Arcade80s`. The two fonts come from
Google Fonts under the SIL Open Font License 1.1 (`Art/Themes/Arcade80s/Fonts`, with the licences and `Sources.txt`);
everything else is generated.

How to switch:

- **Definition**: `Resources/Games/Asteroids.asset` starts the game with its `theme` and lists all of them in
  `themes`. The theme builder sets both (Classic first); **Gamebox > Themes > Choose...** or a theme's "Start
  Asteroids with this theme" button makes another one the starting theme.
- **Launcher**: the button on the game's entry of the BaseGame launcher steps through the themes.
- **Settings**: the "Look" row of the settings panel (pause menu or mission select) steps through them. The pick is
  remembered on the device.
- **Command line**: `-gamebox-theme "Arcade 80s"` (display or asset name) for development players and tours, for example
  `-asteroids-tour <folder> -gamebox-theme "Arcade 80s"`. The tour's `theme` step switches to the next theme while
  the mission select shows and back, and its `settings` and `hangar` steps picture those screens.

The shared online lobby follows the theme as well (the base package's `LobbySkin`): Classic keeps the lobby the scene
builder made, and Arcade 80s, whose lobby skin is empty, gets a lobby in the look of its menu.

A theme change applies at once to the mission select, the hangar, the Supply Room, the HUD, the pause menu and the
backdrop (the manager's `OnThemeChanged` redraws them), and to the materials of everything in play; the ship and the
rocks in play take their new models at once too. Everything else (enemies, shots, pickups, effects) takes the new
look as it spawns, so in a mission started before the change some bodies keep the old look until they are replaced.

**Asteroids > Build Everything** (and **Rebuild Themes**) builds both themes (`Editor/AsteroidsThemeBuilder.cs`) and
fails the batch build when a theme leaves a reference empty. The edit mode `ThemeTest` checks that both themes are
complete, that the definition starts with Classic and lists both, that Classic holds the art the scene was built
with, that Arcade 80s replaces it, and that selecting it changes what the game reads.

## How it works

| Code | Role |
| --- | --- |
| `Scripts/Model/*` | Plain C# rules and data: asteroid, weapon and power-up rules, the wave director, mission objectives, score and combos, flight simulation, levels, the campaign and progress |
| `Scripts/Controller/SpaceField.cs` | Every body in play: circle collisions in the playfield plane, wrap-around, blasts and chain reactions |
| `Scripts/Controller/SpawnService.cs` | Spawns rocks, hazards, enemies, bosses and pickups from the pools for the wave director |
| `Scripts/Controller/*` | The ship, the bodies (`Shootable` > `Asteroid`, `Explodable` > `Mine` / `ClusterBomb`, `Enemy` > `Saucer` / `Wasp`, `Boss`, `Comet`, `GravityWell`, `Shot`) and the pickups (`Reward` subclasses) |
| `Scripts/View/*` | Camera, backdrop (sector themes with nebula, planet and light), effects, audio, ship visuals and the interface |
| `Scripts/Model/AsteroidsTheme.cs`, `AsteroidsThemes.cs`, `Scripts/View/AsteroidsMenuLook.cs`, `ThemedTextGradient.cs` | The game's theme (see Themes), the active one with its lookups (the hull, the rocks, a mission's sector and strike world), the pause menu's own touches on top of the theme's menu skin, and the logo's themed gradient |
| `Scripts/Controller/AsteroidsAutopilot.cs` | Flies the ship by itself; enable it on the ship in play mode to test a mission end to end |
| `Scripts/Controller/AsteroidsOnlineController.cs`, `AsteroidsGameManager.Coop.cs` | The online mission: rooms and missions for the lobby, the pilots of the room, the manager's co-op mode (own lives and score, a fixed playfield, no director on the clients that only show the world) |
| `Scripts/Model/LocalCoop.cs`, `Scripts/Controller/AsteroidsGameManager.Local.cs`, `LocalShipInput.cs`, `Scripts/View/PilotTag.cs` | Local co-op (see Local co-op): the rules of the setup and the control scheme, the pilots' own scores and lives, a ship's input from its seat's controls, and the seat colour and name tag every co-op ship wears |
| `Scripts/Controller/FieldReplication.cs`, `IFieldLink.cs`, `RemoteShip.cs` | Keeps the playfields in step: the simulator announces, updates and removes bodies, the others show them as puppets and report their hits, shots and claims; the stand-ins of the other ships |
| `Scripts/Model/BodyCodec.cs`, `BodyRegistry.cs`, `FieldMath.cs`, `ShipPose.cs`, `CoopRules.cs` | The plain parts of it: what a body is on the wire, the net ids, distances on a field that wraps, the flags of a ship's pose, the options of a room |
| `Scripts/Controller/AsteroidsOnlineTour.cs` | Development builds only: `-asteroids-online host <folder>` and `-asteroids-online join <folder>` fly a mission together between two running players (`-asteroids-level <index>`, `-asteroids-lives <n>`; for strike missions also `-asteroids-difficulty <0-2>` and `-asteroids-skip-to-boss <seconds>`) |
| `Scripts/Model/Strike/*` | Planet Strike's plain rules and data: `StrikeRules` (every number of the mode), `StrikeUnitRules` and `StrikeWeaponRules` (the unit and weapon tables), `StrikeArmory` (the Supply Room's catalogue and rules, and `StrikeLoadout`, a pilot's money, energy, shields and weapons), `ScrollDirector` (the scroll, the timeline of a level and the boss's arrival), `FlightPaths` (the aircraft's routes), `StrikeLevel` and `StrikeTheme` |
| `Scripts/Controller/Strike/*` | The strike bodies (`StrikeAircraft`, `GroundUnit`, `StrikeBoss` with its `BossPart`s, `StrikeReward`, `EnemyBeam`), the pilot's weapons (`StrikeGunnery`, `PlayerBeam`) and `StrikeAutopilot` |
| `Scripts/Controller/AsteroidsGameManager.Strike.cs`, `SpawnService.Strike.cs` | The strike mission's flow (the working copy of the pilot, money, stars, the fly-off, the Supply Room) and its spawning |
| `Scripts/View/Strike/*` | The scrolling ground (`StrikeTerrain`, `TerrainTile`; the ground sits a little behind the air layer and `DepthLayer` / `DepthAnchor` place it so it lines up with its logical position under the perspective camera), the aircraft's `DropShadow`s, and `StrikeUI` (the strike tab, the Supply Room, the HUD and the results) |
| `Scripts/Controller/AsteroidsTour.cs` | Development builds only: `-asteroids-tour <folder>` flies a plan (`-asteroids-tour-plan menu,shop,strike:1,boss:1,field:0`, or `strike:all`) with the autopilot and takes screenshots (`-asteroids-tour-god`, `-asteroids-tour-speed`, `-asteroids-tour-money`); it puts the saved progress back afterwards |

A strike mission keeps the playfield in screen space: the camera and the playfield stay where they are (a fixed 16:9
field that does not wrap), the ground moves down under it, and ground units carry the scroll in their motion while
aircraft fly their routes in screen space. Strike missions are appended to the one campaign after the endless mission
(their own sectors and stars, sector by sector), so progress keys and online room levels stay as they were.

Everything the game shows is generated by the editor code in `Assets/Editor` (menu **Asteroids**):

- **Rebuild Art**: textures (nebula, planets, ore veins, lava cracks, particle sprites, a reflection environment),
  interface sprites and icons drawn from signed distance functions, materials over the StarSparrow and asteroid model
  packs in `Art/`, low-poly models built in code (`MeshBuilder`, `SpaceModels`: saucers, drones, pickups, ice and
  crystal rocks), the sound effects and three music loops from a small synthesizer, and the hangar pictures.
- **Rebuild Prefabs**: the hangar ships, rocks, hazards, enemies, shots, pickups, effects and the four bosses.
- **Rebuild Campaign**: the four sector themes, the loot tables, the twelve missions and the endless mode, the
  post-processing profile and the launcher entry.
- **Rebuild Themes**: the Classic and Arcade 80s theme assets and the game definition's list of themes (see Themes).
- **Rebuild Scene**: `Scenes/Asteroids.unity` with its lighting, backdrop, pools, effects, audio and interface.
- **Build Everything** runs all the steps. **Debug** unlocks every mission or resets the progress of this editor.
- Planet Strike has its own parts of each step (the `*.Strike.cs` and `*.Ground.cs` files next to the builders):
  - art: the six worlds' ground (a hand-written ground shader, `Art/Shaders/Ground.shader`, with per-tile maps and
    detail layers), their props and the ground units' models, the recoloured StarSparrow aircraft, the boss hulls and
    parts, icons for every item, the strike sounds and two music loops (`Art/Ground`, `Art/Strike`);
  - prefabs: `Prefabs/Strike/{Air, Ground, Bosses, Shots, Pickups, Effects, Terrain, Props, Decals}` and the ship's
    shadow, beams and autopilot;
  - campaign: the six `Config/Strike/Themes`, the nine `Config/Strike/Level{n}` (each laid out tile by tile, with
    ground units only where their tile allows them) and the three strike sectors;
  - scene: the strike pools, audio, the terrain object and the strike interface.
- `AsteroidsBuildMenu.BuildEverythingBatch` runs Build Everything from the command line
  (`-executeMethod Portfolio.Asteroids.EditorTools.AsteroidsBuildMenu.BuildEverythingBatch`) and exits with 1 when a
  level is not valid or a prefab is missing. **Debug** also has "Strike: Add 2,000,000" and "Strike: Reset Pilot".

The generators write to fixed paths and update existing assets in place (GUIDs and references are kept). Values that
belong to the content (missions, waves, theme colours, ship stats) live in the builders, so hand edits to those
assets are overwritten by the next rebuild. The scene is rebuilt from scratch, so its diff after a rebuild is large.

## Local co-op

**Local Play** next to Multiplayer on the title (not on touch screens, which have no keys for several pilots) flies the
selected mission of the selected mode with two to four pilots on this screen, through BaseGame's shared local play
setup: a name for each pilot, the ships each pilot gets (asteroid field) or the difficulty (Planet Strike), and on the
**Controls** page each pilot's keys, mouse buttons or pad controls for Turn Left, Turn Right, Thrust, Brake, Fire,
Dash, Bomb and Next Special (pilot 1 starts on WASD, pilot 2 on the arrows, pilots 3 and 4 on pads 3 and 4 or on IJKL
and the number pad; see BaseGame's README). In Planet Strike the ship moves in eight directions by Left/Right and
Thrust/Brake.

- **Ships.** Pilot 1 flies the scene's ship, the others wingmen made from the ship prefab and simulated here, each with
  the seat's colour and name (`PilotTag`, which the online stand-ins wear too), starting as online co-op does. Every
  local ship takes part in the world: collisions, enemy shots and beams, blasts, comets and rams, gravity wells,
  pickups, and the enemies' aim (`SpaceField.Wingmen`). A kill is credited to the ship that fired (`HitSeat`).
- **Pilots.** Each has their own score, combo, ships and respawn (`LocalSquad`); a pilot without ships is out, the
  mission fails when every pilot is out and is won as always. The pilots panel shows each one's score, ships and hull
  (energy in strike) instead of the single ship's HUD; the results rank the pilots in their colours, and Retry flies
  again with them. Local missions keep no stars, records or progress, and cannot be saved.
- **Planet Strike.** Every pilot flies a copy of the saved pilot's kit at the setup's difficulty, the boss is tougher
  with more pilots, and a won mission banks the squad's money once into the saved pilot; the kit itself stays as it was.

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
- **Planet Strike together.** The lobby lists the strike missions as "Strike n." next to the asteroid missions (one
  list of room levels), with a "Strike difficulty" option. The host simulates the scroll, the aircraft, the ground
  units and the boss as above; it also sends the scroll distance four times a second, and the other clients ease their
  ground toward it. Ground units move with each client's own ground, so they stay on it however the scroll changes.
  A boss's parts travel as bodies of their own, tied to their boss. Every pilot flies with their own weapons, energy
  and shields (one ship each; a pilot whose ship is destroyed watches), is paid for their own kills and the pickups
  they reach first, and banks the money of a won mission into their own pilot when their ship lands (no stars are
  given online). The Supply Room is not open inside a room. Boss health grows by half for every pilot after the
  first.

## Phones and tablets

The game runs on Android with the shared mobile code of BaseGame (see its README, "Phones and tablets"):

- **Touch controls.** `ShipTouchControls` (built by `AsteroidsInterfaceBuilder.BuildTouchControls`) holds a floating
  `VirtualJoystick` for the left thumb and the FIRE, DASH and NOVA `TouchButton`s for the right one. The player's input
  (`PlayerShipInput`) reads them next to the keyboard and gamepad: `PlayerShipInput.Steer` turns the ship toward the
  stick, easing off as the nose gets there, and thrusts once the nose is within about 70 degrees of it (covered by
  `ShipInputTest`). The controls show only when the game is played by touch. In a strike mission the stick moves the
  ship directly, and DASH and NOVA become WEAPON (the next special) and MEGA (the megabomb).
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
mission, and Enter launches the selected mission. Saving needs the PlayerPrefs storage strategy. Strike missions are
not saved half way (F4/F5 and the menu's save and load are off there): the pilot is saved when a mission is won.
