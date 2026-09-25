# Portfolio - Endless Runner

A three-lane runner through five low-poly worlds: dodge crates, jump hurdles, slide under barriers, climb ramps onto
wagons for the coins stacked on top, leap across rivers of water and lava, and grab power-ups. A campaign of eight
levels (three stars each) teaches one new thing per level, and an endless run rotates through all the worlds while the
speed keeps climbing. Two to four players can race each other online on any of these tracks and compete for its coins.

## Playing

| Control | Keyboard | Touch / mouse |
| --- | --- | --- |
| Switch lanes | Left / Right, A / D | Swipe sideways |
| Jump | Up, W, Space | Swipe up |
| Slide (dive when in the air) | Down, S | Swipe down |
| Pause | Escape or the pause button | Pause button, or the back button on a phone |

- **Hearts.** Running into the front of an obstacle costs a heart and knocks the obstacle off the track; switching
  lanes into the side of one only bounces you back. Falling into a chasm costs a heart and puts you on the far bank.
- **Obstacles.** Hurdles (jump), overhead barriers (slide), crate and barrel stacks (dodge), freight wagons (take the
  ramp up, or dodge), runaway carts that roll toward you, bounce pads that throw you into a high arc of coins, and
  chasms, sometimes with a plank bridge.
- **Pickups.** Coins, gems (worth five coins) and four power-ups: coin magnet, shield (absorbs one crash), double coins
  and super jump (high enough to land on wagons).
- **Stars.** Finish the level, collect the level's coin goal (shown in the level select), and finish without losing a
  heart. Levels unlock in order; the endless run is always open and keeps your best distance.

| # | Level | World | Length | New |
| --- | --- | --- | --- | --- |
| 1 | Sunny Start | Sunny Meadows | 420 m | Hurdles, crates |
| 2 | Ramp It Up | Sunny Meadows | 520 m | Ramps and wagons, coin magnet, gems |
| 3 | Duck and Dash | Sunset Canyon | 600 m | Barriers, bounce pads, shield |
| 4 | Canyon Leap | Sunset Canyon | 700 m | Chasms and bridges, double coins |
| 5 | Frosty Peaks | Frosty Peaks | 760 m | Runaway carts, super jump |
| 6 | Avalanche Alley | Frosty Peaks | 840 m | Wagon chains |
| 7 | Moonlight Run | Moonlit Woods | 900 m | Everything |
| 8 | Lava Rush | Lava Land | 1000 m | Everything, fastest |
| - | Endless Run | all five | endless | Worlds change every 600 m |

## Base game integration

The game is built on the base classes of the BaseGame package (`com.skinnerboxes.basegame`, assembly
`BaseGameAssembly`, namespace `Gamebox`):

| Base class / interface | Implementation |
| --- | --- |
| `BaseGameManager` | `RunnerGameManager` |
| `PlayerBase` | `RunnerPlayer` |
| `OfflineGameController` | `RunnerController` |
| `OnlineGameController` (assembly `Gamebox.Online`) | `RunnerOnlineController` |
| `PlayerBase` (a player on another device) | `RunnerGhost` |
| `GameUI` | `RunnerUI` |
| `SettingsUI` | `RunnerSettingsUI` |
| `GameSettings` | `RunnerSettings` |
| `GameLevel` | `RunnerLevel` (in the `Campaign` asset `Config/Campaign/RunnerCampaign.asset`) |
| `PrefabPool<T>` | `TerrainCache` (road tiles), `TrackPiecePool` (every other track piece) |

The shared menu of the BaseGame package is used as the pause menu and hosts the settings panel (start speed, top
speed, acceleration, lane switch speed, jump height, hearts); the game adds its own canvas with the level select, the
HUD and the results screen. The `GameDefinition` asset under `Assets/Resources/Games` registers the game with the
BaseGame launcher. Campaign progress (stars, best scores, the endless record) is kept in the game's storage under keys
prefixed with the game type; the save and load buttons are hidden because progress is saved automatically.

## How it works

| Code | Role |
| --- | --- |
| `Scripts/RunnerGameManager.cs` | Level select, countdown, the run (pickups, power-ups, hearts, finish), results and progress |
| `Scripts/RunnerPlayer.cs` | Movement on a `CharacterController`: lanes, jump buffering and coyote time, slides, crash and bump detection |
| `Scripts/Track/TrackLayout.cs` | Builds a level's track from 18 obstacle patterns, seeded per level and spaced by the speed there |
| `Scripts/Track/TrackGenerator.cs` | Spawns the layout from pools as the runner approaches and recycles what is behind |
| `Scripts/Visual/*` | Procedural character animation, camera, theme blending (sky, fog, light), particles, audio |
| `Scripts/UI/*` | Level select cards, HUD, results screen, settings panel |
| `Scripts/RunnerAutopilot.cs` | Plays the runner by itself; add it to the runner in play mode to test a level end to end |
| `Scripts/RunnerGameManager.Online.cs`, `Scripts/RunnerOnlineController.cs` | A race against the runners of an online room (see Multiplayer) |
| `Scripts/Race/*` | The ghost of a runner on another device, the state that travels with a pose, the books of the shared coins, the standings |
| `Scripts/RunnerOnlineTour.cs` | Runs an online race by itself and takes screenshots (`-runner-online host\|join\|crash <folder>`) |

Everything the game shows is generated by the editor code in `Assets/Editor` (menu **Endless Runner**):

- **Rebuild Art**: low-poly models built from primitives (`MeshBuilder`, `RunnerModels`, `SceneryModels`) and coloured
  through one palette texture, so all models share one material; ground, road, water and lava textures; interface
  icons drawn from signed distance functions; sound effects and the two music loops from a small synthesizer; the
  materials and prefabs.
- **Rebuild Worlds and Levels**: the five themes, the run settings of every level, the campaign, the piece catalog,
  the post-processing profile and the launcher entry.
- **Rebuild Scene**: `Scenes/EndlessRunner.unity` with its pools, effects, audio and interface, and the object
  "Online" with the server client, the online controller and the lobby (`OnlineInstaller` of BaseGame).
- **Build Everything** runs the three steps and removes the assets of the original terrain version.

The generators write to fixed paths and update existing assets in place (GUIDs and references are kept); files whose
content did not change are not rewritten. Values that belong to the content (level lengths, seeds, theme colours)
live in `RunnerContentBuilder`, so hand edits to those assets are overwritten by **Rebuild Worlds and Levels**.

## Multiplayer

**Multiplayer** on the level select opens the lobby of the game's server (the shared lobby of BaseGame): pick a name,
open a room for a level of the campaign or the endless run, or join one. When everybody is ready the host starts the
race. Two to four runners stand side by side at the start, everybody gets the same countdown, and then each runs the
track as in a game alone, with the others beside them as ghosts in their colours, their names over their heads: ghosts
do not block the way and cannot be bumped into. The coins and gems of the track are there once for everybody, so the
runner who gets to a coin first has it, and it disappears from the track of the others as the ghost picks it up.
Power-ups are everybody's own. The scoreboard under the score shows the places, which go by the score as always (a
point a meter, ten a coin): running far counts, and so does getting to the coins first. A runner who crosses the finish
line or runs out of hearts watches the runners who are still out there, and when the last one is done the results show
the standings. **Room** on the results leads back to the room, where the host starts the next race. A race does not
pause (the pause button and Escape open the match menu, which has the way out), and a runner who leaves does not end it
for the others. Stars and level records are not touched by races; the record of the endless run is.

How it is built:

- **The same track everywhere.** A track is laid out by `LayoutBuilder` from the level, the run settings and a seed with
  `System.Random`, so every device builds the same one, piece for piece, and the ids of the layout
  (`PiecePlacement.Id`) name a coin on all of them. In a session the manager runs the level's own settings, not the
  player's custom ones (`RunnerGameManager.RunnerSettings`), and the endless run takes the seed of the room instead of
  the dice (`TrackSeed`). Edit mode tests guard this: the same input gives the same layout, another seed or other
  settings another one, also chunk by chunk for the endless run and for every level of the campaign.
- **Ghosts.** Every client sends the pose of its runner through the base server (`RunnerOnlineController.SendRunnerPose`,
  15 times a second and at once when the state changes): the position (lane, height, distance), the velocity, the hearts,
  and the state packed into flags (`RunnerPoseState`: running, grounded, sliding, invulnerable, the lane change, launched
  by a bounce pad, the power-up auras, dead, finished). A `RunnerGhost` is a copy of the runner prefab without its
  `RunnerPlayer` and `CharacterController`. It moves between the poses with BaseGame's `PoseInterpolator`, plays the
  state as late as the position, and turns the changes of the state into the events of the `RunnerAnimator` (jump, flip,
  landing, slide, stumble when a heart is gone, the cheer, the fall). The animator and the camera read a runner through
  `IRunnerMotion`, which `RunnerPlayer` and `RunnerGhost` both are.
- **Shared coins.** Touching a coin hides it at once, with its sparkle and sound, and asks the server for it with the id
  of the piece (`ClaimPiece`). The server gives a piece to the first who asks and says so with a `CoinClaim` row, which
  every client of the room receives: the owner counts the coin then (with double coins as it was at the touch), the
  others take the piece off their track (`TrackGenerator.Take`: a piece that is not spawned yet never will be). A coin
  that is in sight waits for the ghost of its owner to reach it, out of everybody's reach. `CoinClaims` keeps these
  books; it is plain C# and covered by edit mode tests.
- **The race.** The score is reported to the room every second (`ReportScore`), and the scoreboard (`RaceHud`) ranks
  the runners by what is known of them: how far the ghost is, the coins the server gave them, or the reported score if
  that is more (`RaceStandings`, on BaseGame's `Standings`: the ranking rule of the base server). The end of a run goes to the server with
  `FinishRun(distance, score)`; when the last runner is done the base server ranks the members, and the results show its
  places. Meanwhile the camera chases the leading ghost (`RunnerCamera.Watch`) and the track is laid out around that
  runner, again from further back if need be (`TrackGenerator.Rewind`).
- **Not shared:** power-ups, the magnet's pull, obstacles knocked away by a crash and the runaway carts, which start
  rolling when the local runner comes near. A ghost can therefore run through a cart or a crate that is only there on
  this device.
- **Trying it:** `RunnerBuildMenu.BuildPlayer` (batch mode, `-runnerPlayer <folder>`) builds a Windows player. Start it
  twice with `-runner-online host <folder>` and `-runner-online join <folder>` (or `crash`, a guest who runs without the
  autopilot and soon watches), each with its own `-gamebox-identity <slot>`: the two play a race by themselves
  (`RunnerOnlineTour`), save screenshots and log who got which coins.

## Phones and tablets

The game runs on Android with the shared mobile code of BaseGame (see its README, "Phones and tablets"). Swipes are
the touch controls (`RunnerInput`); with touch the level select and the track's tips word the controls as swipes
(`TrackLayout.HintFor`). The canvas expands from 1920 x 1080 to any screen shape, every screen keeps its texts and
buttons in a safe area, and the camera shows more of the world on wider phones. The back button pauses and resumes a
run, returns from the results to the level select and leaves the level select for the launcher (or closes the app when
the game is built on its own). `Gamebox > Android > Build APK` builds `Build/Android/EndlessRunner.apk`
(`com.skinnerboxes.endlessrunner`, with the game's icon).

## Server

The game's [SpacetimeDB](https://spacetimedb.com) module is the folder `Server` next to `Assets` (Unity does not show
it; open `Server/StdbModule.csproj` in the IDE). It is the Gamebox base server of the BaseGame repository (`BaseServer`:
users and login, connecting and disconnecting, the player profile) plus `Server/Lib.cs`, which declares the same
`public static partial class Module` to add the tables and reducers of this game and implements the base server's
partial methods to react to its events. Rooms, ready and start, the poses of the runners, the scores and the places are
the base server's; `Lib.cs` holds the race:

| In `Server/Lib.cs` | What |
| --- | --- |
| `CoinClaim` (public) | A coin or gem a runner got: room, piece id of the track layout, owner, seat, value. The key is the room and the piece in one number, so a piece is claimed once. |
| `RunnerRace` (private) | The track of a room that races, as the host described it: level, length, the coins on it and the coins claimed so far. |
| `RunnerStats` (public) | Races, wins, coins, the longest run and the best score of a player. |
| `ClaimPiece(piece, value)` | A runner touched a coin. The first claim of a piece gets it; a late one, or one from a runner who is out, is ignored. Refuses values no piece has and more coins than the track holds. |
| `FinishRun(distance, score)` | The run is over. Keeps the distance for the stats, holds the distance to the length of the track and the score to what the distance and the runner's coins can be worth, and ends the run with the base server's `FinishPlaying`. |
| `ConfigureRoom`, `OnRoomStarted`, `OnRoomFinished`, `OnRoomCleared`, `OnUserDeleted` | The base server's extension points: one to four runners and no turn timer, check the track in the options of the room (`level`, `length`, `coins`, written by `RunnerOnlineController.ComposeOptions` because the server does not know the levels), keep the race row and the stats, clean up. |

- `Packages/manifest.json` references the SpacetimeDB SDK and the base server package
  (`com.skinnerboxes.baseserver`, `file:../../../BaseGame/BaseServer`); `Server/StdbModule.csproj` imports
  `BaseServer.props` from there, which compiles the base server into this module.
- `spacetime.json` names the database (`skinnerboxes-endlessrunner`, on the `local` server) and where the client bindings go:
  `Assets/Scripts/Server/Bindings`, namespace `Portfolio.EndlessRunner.Server`.
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
  - `Assets/package.json` makes this project's `Assets` folder the package `com.skinnerboxes.endlessrunner`. BaseGame's
    manifest references it (`file:../../Portfolio/4 - EndlessRunner/Assets`) and lists the game in its launcher.
- The same files therefore serve two projects and are mounted at `Assets/` here and at `Packages/com.skinnerboxes.endlessrunner/`
  in BaseGame, so nothing in them may depend on either location. The generators find their root through the package
  info of their assembly; the `GameDefinition` asset re-derives its scene path from its scene reference in the editor,
  and the launcher falls back to the scene name.
- Run the generators in one editor at a time: an editor that imports the files while the other one writes them can
  hold a lock on a file and make the save fail.
- The runner lives on the built-in Ignore Raycast layer, so the game needs no custom layers in either project.
