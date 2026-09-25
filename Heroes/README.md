# Portfolio - Heroes

A turn based strategy of heroes, towns and armies in the spirit of Heroes of Might and Magic III, on a hexagonal map
seen from above. Battles are fought in one of two ways, chosen when a game starts: on a battlefield of their own,
fifteen hexes by eleven, in a scene of its own, as in the original; or where the armies meet, on the map itself, with
the hexagonal grid drawn on the ground around them for the length of the battle and the castles, mines and treasures of
that stretch of land still in place.

Up to four realms play: people sharing one device, computer players, or people on devices of their own in a room of
the game's server.

## Opening the project

The hexagons of the map are drawn by **Terrain Grid System 2** (Kronnect), a paid Asset Store package, so it is not in
this repository. Import it into `Assets/TerrainGridSystem` before opening the project; everything else, including the
art, comes with it. The game only uses the package's `Kronnect.TerrainGridSystem` assembly (the battlefield of its own
draws its hexagons itself).

The project references BaseGame's `Assets` (`com.skinnerboxes.basegame`) and its base server
(`com.skinnerboxes.baseserver`) as local packages (`file:../../../BaseGame/...` in `Packages/manifest.json`), so the
BaseGame repository has to sit beside Portfolio. The two scenes, `Scenes/Heroes.unity` and `Scenes/HeroesBattle.unity`,
are built by code (see "Building").

## Playing

- **The title screen** stands in front of a small valley with a town, a rider, soldiers and a dragon, seen by a slowly
  drifting camera. Its buttons: Continue (the scenario left last, where it was left), Campaign, Skirmish, Play Online,
  Settings, Credits and Exit.
- **The campaign screen** lists the eight chapters of *The Shattered Crown* as cards, each with the stars it was won
  with and locked until the one before it is won, or, on its other page, the four skirmish maps, which can be played
  straight away. The story of the one picked is on parchment beside them, with the realms that play it. A skirmish is
  shown as the settings shape it: its size, how much treasure and how many wandering armies it holds, and how well each
  computer player plays. Along the bottom are Back, the choice of where battles are fought, and Begin.
- **The map**: click a hero to pick him up, then a cell to send him there. The trail of arrows shows the road, white for
  as far as today's movement carries him and red for the days after. Clicking a hero again opens his book; a town of
  yours opens its screen. The bar along the top holds the treasury, the turn and the date. The column down the right
  holds the little map, your heroes and towns, the commands (next hero, sleep, the hero's book, the town, the menu) and
  **End Turn**, which glows once no hero can do anything more. The chronicle at the bottom left shows its last two lines
  and opens on a click; it tells what happened to your realm, what you could see happen, and every battle fought. The
  pointer turns into what a click would do (travel, visit, fight, take), and its tooltip tells what a cell holds and how
  hard a wandering army looks for the hero in hand.
- **A town** builds one thing a day, recruits from the dwellings it has built, trades in its marketplace, hires heroes
  in its tavern and teaches spells in its mage guild. Its screen shows the income, the growth and whether it can build
  today, every building with its cost (or what it still needs), the creatures to recruit, the garrison and the visiting
  hero. Creatures move between the garrison and the hero by clicking one slot and then another.
- **A hero** carries his primary skills, eight secondary skills, artifacts in eight slots and a pack, a spellbook, and
  an army of up to seven stacks. Movement, mana and morale all come from what he has learned and what he carries.
- **A battle** starts when a hero walks into an enemy army, a wandering army or a town that is defended (see
  "Battles").
- **Settings** (the title screen or the pause menu): where battles are fought, hero speed, battle speed, map size,
  treasure and wandering armies of a skirmish, how hard the computer players are, music and sound, scrolling at the
  screen's edges, whether the computer's moves are shown, and saving every day. Difficulty moves every computer player
  one level down or up from the level the map gives it (and with it what it starts with and earns).
- **The pause menu** has Return, Save, Load, Settings, Leave to the Title and Exit. Leaving for the title in the middle
  of a battle asks first, since the battle is lost with it.
- A scenario saves at the start of every day and continues from the title screen; a game is not saved in the middle
  of a battle. Winning earns stars: three for winning inside the chapter's quick day count, two inside the slower one,
  one for winning at all.

## Battles

Where battles are fought is part of a game's rules (`ScenarioRules.battleStyle`, a `BattleStyle`): it is fixed when
the game starts, so a saved game goes on the way it began, and online it is the same on every device. A game at one
device takes it from the settings (the Battles row of the settings panel, or the Battles choice on the campaign screen,
which is saved with the player's own settings); an online game from the room's option **Battles**. A new game is fought
on a battlefield of its own unless the setting or the room says otherwise.

- **On a battlefield of its own** (`BattleStyle.Battlefield`) the battle has a field of fifteen by eleven hexes. The
  attacker lines up in the two columns on the left and the defender in the two on the right; between them lie clumps of
  one to three obstacles of the land they met on (trees, rocks, logs, bones, crystals, pools and so on), laid out from a
  seed made of what everybody knows about the fight, so the same fight always gets the same field. A layout that would
  cut any cell off from the attacker is thrown away and another tried. The screen fades to black, the `HeroesBattle`
  scene is loaded next to the game's scene, and the field is built from the battle: the ground of that land rising into
  hills and woods behind it, its sky and light, the obstacles, and the town behind the defenders in a siege. The
  creatures are drawn larger than on the map. When the battle is over the scene is unloaded and the map comes back.
- **On the map** (`BattleStyle.OnTheMap`) the field is the block of map cells around the place the armies met, and what
  stands there is in the way. The grid is drawn on the terrain around the two armies, the camera frames the field from a
  little steeper and holds still, the heroes and the guard standing on it and the trees at the edges of its open ground
  make room for the stacks, and the shroud lifts off the field while the battle lasts (in a hot seat game, the map
  around it shows the explored land of the person fighting). The counts of the stacks are drawn over the field, so a
  crag never hides them. Troops cut off from the fight by woods trample a way through them, or are moved to where they
  can reach it.
- **Sieges.** On a battlefield of its own a town with a Fort, Citadel or Castle puts its wall across the field in column
  10 (of 0 to 14): broken at two rows, which anybody may cross, with a gate in the middle row that only the defenders
  pass, and its arrow towers standing in the wall (one for a Citadel, three for a Castle). The defenders line up behind
  the wall; obstacles keep clear of the column before it. The wall is drawn as one line with broken ends and rubble at
  the breaches and a gatehouse with the owner's banners at the gate. A tower that is shot down falls in a cloud of dust
  and leaves a heap of stones that closes the wall where it stood, in the rules as on the screen. On the map, the arrow
  towers stand on the town's own cells nearest the besiegers.

The battle screen is the same for both styles (`UI/BattleBar.cs`). Along the bottom are the two heroes at the ends, the
commands (options, retreat and auto combat on the left; the spellbook, wait and defend on the right) and the last lines
of the combat log; above them the card of the stack whose turn it is, the order the others act in this round and the
next, and the round. The cells the stack in hand can reach and the enemies it can strike or shoot are painted, and the
pointer turns into what a click would do: walk, fly, shoot, cast, or a sword that points the way the blow is struck,
from the side of the target the pointer is on. Pointing at an enemy tells the damage the blow, the shot or the spell
would do and how many creatures it would kill, as a range (half damage for a shot from far away); the estimate draws no
random numbers. Clicking a creature's body picks that creature, not the cell behind it. Holding the right button on a
stack shows its card: attack, defense, damage, health, speed and the spells on it with the rounds they have left.

- **Keys**: W wait, D or Space defend, S the spellbook, R retreat, A auto combat. Escape takes back what is open
  first (the retreat question, the book, a spell being aimed) and then opens the menu.
- **Retreat** asks first: the hero flees and his army is lost, but he keeps his artifacts and may be hired again. R
  again or Enter confirms. The defender of a town cannot retreat.
- **Auto combat** leads the player's troops with the computer's battle sense for the rest of the battle, as commands
  like the player's own, so it works online too. It is off again at the start of every battle, and a retreat the
  computer would choose is left to the player.
- **The spellbook** lists every spell the hero knows, what it costs and whether he can cast it now; a spell picked is
  aimed at a cell and cast with a click.
- **The results** say how the battle went for the player at this device (victory, defeat or retreat), what each side
  lost and the experience the winner gained. In a hot seat battle between two people they are told from the winner's
  side. Online they close by themselves after twenty seconds, or after three on the host while the others wait for the
  computer's moves.

A battle nobody at this device fights (computer players among themselves, or online the battles of others) is told in
the chronicle and not shown.

## Rules

The rules are in `Scripts/Model`, which is an assembly of its own that knows nothing of Unity: the same code decides a
game on every device.

- **The map** is a hexagonal grid of nine terrains, with obstacles (forest, rocks, mountains, lakes, dead trees) that
  block the way and roads that speed it up. The generator lays out a map from a seed: zones per player, borders with
  guarded gates between them, a town per player, neutral towns, mines, dwellings, treasures, artifacts, the buildings
  a hero visits, and wandering armies that guard what is worth guarding. A skirmish comes in three sizes (40 x 46,
  54 x 62 and 68 x 78 cells), with as much treasure and as many wandering armies as asked for (`MapSpec.Skirmish`).
- **Resources**: gold, wood, ore, mercury, sulfur, crystal and gems. Towns give gold a day, mines give their resource,
  and the marketplace trades at a rate that improves with every marketplace you hold.
- **Three factions** (Castle, Necropolis, Stronghold), seven creature tiers each, plus the creatures of the wilds.
  Every creature has attack, defense, damage, health, speed, weekly growth and a price, and some have abilities:
  flying, shooting, double attack, no retaliation, life drain, undead, breath, charge, regeneration.
- **Heroes** have six classes, four primary skills, thirteen secondary skills, seventeen spells and twenty four
  artifacts. Experience comes from battles and from the map; a level raises one primary skill and offers a choice of
  two secondary ones.
- **A battle** is fought in rounds, in the order of each stack's speed, the same way in both styles (the field is
  `HeroesGame.BattleGrid`: the map's grid, or the battlefield's own). Damage counts attack against defense, morale gives
  a stack another turn and bad morale takes one away, luck doubles a blow, a shooter at close quarters strikes at half
  strength, and a stack that is struck strikes back once. A hero who retreats keeps his artifacts; one who is beaten
  loses them to the winner. A battle that cannot be decided (past its sixtieth round, or when the armies cannot get at
  each other) ends with the attacker breaking off: he keeps what is left of his army and stays where he stood.
- **Events.** The rules record what happened as events (`Model/Commands.cs`, `EventKind`), which the view plays out
  afterwards. The numbers each battle event carries are documented on it; cells are field cells on a battlefield of its
  own and map cells on the map. BattleStarted and BattleEnded carry a copy of the battle as it was deployed and as it
  ended, and the view builds the battle from those copies and the events alone, never from the live rules, which online
  may be several moves ahead.
- **Victory** is set per scenario: defeat everyone, take a town, slay a monster, gather gold or find an artifact. When
  every person at the table has lost, a computer player has won.

## Multiplayer

**Play Online** on the title screen opens the lobby shared by all Gamebox games (`BaseGameManager.OpenOnline`). A room
plays a random map or one of the skirmish maps, with the options Map size, Battles (on the map or on a battlefield),
Turn clock (none, 1, 2, 4 or 8 minutes; 2 by default), Computer players (up to three), Computer skill, Treasure and
Wandering armies.

An online game is played in step, which is what the deterministic rules are for. Everybody in the room lays out the
same map from the room's seed, and every client runs the same rules over the same commands: what a player does is sent
to the action log of the base server, the server puts the actions of a room in one order and stamps each with a random
number, and every client applies them in that order and seeds the chances of that action with that number. The shared
pieces come from BaseGame: `LockstepGame` is a `LockstepTable` of `Gamebox.Lockstep` (with its `SeededRandom` and
`StateChecksum`), `HeroesOnlineController` is a `LockstepOnlineController<GameCommand>` of `Gamebox.Online`, and the
manager is its `ILockstepHost` (`HeroesGameManager.Online.cs`).

The server knows no rules of Heroes (`Server/Lib.cs`). When a game starts it seats the table in the base server's
`RoomSeat` table: the members in seat order, each leading the next faction along, then the computer players the room
asks for. It checks who may act for which seat: a member for their own, the host for the computer players and for the
wandering armies of the wilds (seat 254), whose moves the host's client works out and sends like any other action. The
wilds may only send battle commands, and a kind that is no command of the game is refused.

- **The clock is a clock of turns.** Ending the turn, answering a choice, every move in a battle and everything the host
  sends for the computer and the wilds start it again; walking, building and recruiting within a turn do not. When it
  runs out, every client makes the default move at the same place in the log: a choice takes its first option, a stack
  in battle defends, a turn on the map ends. A battle opened by a move on the map would have what was left of that
  turn, so the first time the clock runs out on it no move is made, and the first decision of every battle has a whole
  turn of the clock. The time left is shown on the bar along the top of the map and, in a battle, beside the round,
  red for the last ten seconds.
- **A player who leaves** hands their seat to the computer: the server appends an action of its own, and every client
  hands that realm over at the same place in the log (`KeepPlaying` keeps the room going while one member is still at
  the table). A seat the computer plays for somebody who left counts as no person's any more.
- **The end.** Every client reports how the game ended as its rules saw it: the seat that won, the place of its own
  seat at the table (the computer players count) and what its realm is worth. The base server's `RankTable` gives the
  members those places once their reports agree on the winner, a computer player that won coming before them all, and
  `HeroesStats` counts games, wins and the largest realm of every player. The results say "Back to the Room" and give
  no stars; nothing of the campaign at the device changes.
- **The protocol.** An action is the seat and the kind of a `GameCommand`, and a payload of its five numbers
  (`a,b,c,d,e`); a command is always read with the seat and the kind of the action, never with anything the payload
  claims (`GameCommand.Parse`). Command kinds are part of the protocol: only ever append to `CommandKind`, and keep the
  server's list of kinds and of the ones that restart the clock in step with `LockstepGame`. Clients and module go
  together: publish the module again when either changes.

A battle is shown only on the screens of the players who fight it. The others see a line in the chronicle, so a
client that only watches never holds the table up, and a client whose rules have run ahead plays the map a little
faster until it catches up. There is no saving and no pausing online: the menu is the match menu of the room.

## Code

| Folder | What is in it |
| --- | --- |
| `Scripts/Model` | The rules: the grid, the content tables (with the battlefields' obstacles and walls, `Content/Battlefields.cs`), the state, the commands and events, the rules engine (`Rules/HeroesGame.*.cs`, the battlefield layout and sieges in `HeroesGame.Battlefield.cs`), the map generator, the computer players and the lockstep table. No Unity; it references only `Gamebox.Lockstep`. |
| `Scripts/View` | What is seen: the terrain built from the cells, the hexagonal grid (Terrain Grid System 2), the fog of war, the map, the paths, the puppets that animate the models, the effects. The battle view (`BattleView.cs`) shows both styles through `IBattlefield` (`Battlefield.cs`): `MapBattlefield` on the map, and `BattlefieldScene` with `BattlefieldGround` and `BattlefieldGrid` in the HeroesBattle scene. |
| `Scripts/UI` | The interface, built from code when it is first needed: the title screen and its valley (`TitleScreen.cs`), the adventure screen (`AdventureHud.cs`, `Sidebar.cs`, `MapTips.cs`), the campaign, the town with its tavern and marketplace, the hero's book, the dialogs, and the battle screen (`BattleBar.cs`, `BattleCards.cs`, `BattleOverlay.cs`, `BattleSpellbook.cs`, `BattleResults.cs`), all put together from `UIKit.cs`. |
| `Scripts` | The manager (`HeroesGameManager.cs`, which directs a game; `.Events.cs` plays the events out, `.Battle.cs` opens and closes battles, `.Online.cs` is the online half), the controllers, the settings, the art and sound assets, and the development tours (`HeroesTour.cs`, `HeroesUITour.cs`, `HeroesOnlineTour.cs`). |
| `Scripts/Server` | The generated bindings of the game's server and the client stamped for them. |
| `Scenes` | `Heroes.unity`, and `HeroesBattle.unity` with the prefabs of a town's gatehouse, the ruin of a tower, loose stones and the banners of the five colours, and its colour grading (`Scenes/HeroesBattle/`). |
| `Editor` | The builders: the art (`HeroesArtBuilder.cs`, with `HeroesObjectArt`, `HeroesBattleArt`, `HeroesPortraits` and `HeroesUIArt` for the interface, its fonts and its pointers), the scenarios (`HeroesContentBuilder.cs`), the two scenes (`HeroesSceneBuilder.cs`), contact sheets, and the menu that runs them all. |
| `Tests/Editor` | The rules under test: the grid, the generator and skirmish settings, paths, turns, battles in both styles, sieges, the event contract, the damage estimate, the protocol of the log, and that the same commands always give the same game. |
| `Server` | The server module: the seats of a room, the checks around the action log and the statistics, on top of the Gamebox base server. |

## Building

The builders under the **Heroes** menu make everything the game shows from what was downloaded. Every builder writes
in place, so a rebuild keeps the references the scenes already have and writes the same bytes again.

- **Heroes > Build Everything**: the art, the scenarios and the scene, in that order. In batch mode
  `-executeMethod Portfolio.Heroes.EditorTools.HeroesBuildMenu.BuildEverything`.
- **Heroes > Build Art**: import settings, materials, terrain layers, the prefabs of the creatures, heroes, map objects
  and towns, the battlefield art, the portraits, the interface and the sounds, tied together in `Art/HeroesArt.asset`.
  It works in a scene of its own that it throws away, so the open scene is not marked as changed.
- **Heroes > Art > Portraits**, **Interface**, **Battlefield**: one part of Build Art each. **Interface Preview**
  draws the kit as the game puts it together into `Logs/shots/uikit.png`, and **Battlefield Pictures** renders every
  obstacle and backdrop into `Logs/shots/battlefield`.
- **Heroes > Rebuild Content**: the scenarios, the campaign and the default settings.
- **Heroes > Rebuild Scene**: `Scenes/Heroes.unity` (the light, the camera on its rig, the manager, the restyled shared
  menu with the settings panel, and online play), and with it the battle scene.
- **Heroes > Rebuild Battle Scene**: `Scenes/HeroesBattle.unity` alone, next to the open scene, listed in the game's
  definition as an extra scene and in the build right after the game's own scene. The game loads it by name
  (`GameLauncher.LoadSceneAdditive`); a build without it builds the same field in a scene made in code.
- **Heroes > Contact Sheets** renders every creature, map object and town into `Logs/shots`.

## Checking it

The tests are in the Unity Test Runner (EditMode, `Skinnerboxes.Heroes.Tests`). They know nothing of Unity, so they
also run outside it: `dotnet test` in the `Tests` folder next to `Assets` compiles them with the model and
`Gamebox.Lockstep` (BaseGame's `ModelTests.props`) and runs them in a few seconds. A development player (for example
`-executeMethod Gamebox.Editor.PlayerBuild.BuildFromCommandLine -gamebox-target StandaloneWindows64
-gamebox-development -gamebox-output Build/Heroes.exe`) can play itself and save a screenshot of every step, then quit;
without these arguments, and in a release player, nothing of it runs:

- `-heroes-tour <folder>` fights the battles of the game as a player would (it points at the field, holds the right
  button on a stack, aims and casts a spell, clicks a move, a shot and a blow, then leaves the rest to auto combat),
  and writes `tour.log`. `-heroes-tour-plan <steps>` picks the steps, `field,map,siege,walls,resume,teardown` by
  default: a battle on a battlefield of its own, the same on the map, a siege, a close look at a town's wall with a
  tower shot down, a battle taken up again from a copy of the game made in the middle of it, and a scenario left for
  the title while its battle loads, is up and shows its results. `skirmish`, only when asked for, starts a skirmish on
  a large, rich and hard setting. The tour changes a copy of the settings, never the defaults or the player's own.
- `-heroes-ui-tour <folder>` walks through every screen outside the battles: the title, the credits, the settings,
  the campaign and the skirmish maps, the adventure screen and its tooltips, the hero's book, the town with its market
  and tavern, the questions, the next day, the pause menu and the results of a game. It looks at the lobby only when
  `-gamebox-server` is given; `-heroes-ui-tour-views 1` also shoots the title from its other camera views.
- `-heroes-online host <folder>` and `-heroes-online join <folder>` play an online game against each other: the host
  opens a room (a small random map, battles on a battlefield, no clock and no computer players; `-heroes-online-turn
  <seconds>` puts a clock on it), the other joins, and each plays four days of its own, walking to what is worth having
  and fighting its battles with the computer's battle sense. Every action of the log is written with a checksum of the
  whole game after it, and the two logs carry the same numbers line for line. Give each player its own
  `-gamebox-identity <slot>`, and try it on a server of your own: `spacetime start --listen-addr 127.0.0.1:3917
  --data-dir <folder>`, then from a folder without a `spacetime.json`, `spacetime publish <database> --server
  http://127.0.0.1:3917 --no-config --bin-path Server/bin/Release/net8.0/wasi-wasm/AppBundle/StdbModule.wasm` (after
  `spacetime build --module-path Server` in this folder), and start both players with
  `-gamebox-server http://127.0.0.1:3917 -gamebox-database <database>`.

## Art

Everything the game shows or plays is drawn by its builders or downloaded under a licence that allows it. Every source
has a file in `Art/Licenses` with what was taken from it, and `Art/Licenses/CREDITS.txt` is what the Credits button of
the title screen shows.

- **Downloaded**: the models of KayKit's Adventurers, Skeletons, Dungeon, Halloween and Medieval Hexagon packs and
  Quaternius's characters, monsters, animals and items (CC0); the terrain textures and the grain of the interface's
  leather, paper and wood from ambientCG (CC0); five pure skies from Poly Haven for the battlefield (CC0); the icons
  from game-icons.net (CC BY 3.0, with the author of every icon in `GameIcons-License.txt`); Kenney's Fantasy UI Borders
  and Cursor Pack (CC0); the fonts Cinzel, Cinzel Decorative, Alegreya and Noto Sans Symbols (OFL); music by Kevin
  MacLeod (CC BY 4.0); and sounds by Kenney (CC0) and from OpenGameArt. Where a sound came from was not always recorded;
  `Kenney-License.txt` and `OpenGameArt-Sounds.txt` say which.
- **Portraits** of every creature, of every class of hero (head and shoulders, and on his mount) and of every town in
  every colour are rendered from the models by `HeroesPortraits`, in BaseGame's `ModelStudio`, posed in their idle
  clips; so are the three item icons the downloaded set lacks.
- **The interface kit** (the framed windows, buttons, cards, slots, bars, banners, pennants and stars) is drawn from
  code by `HeroesUIArt`, with the grain of the ambientCG textures and Kenney's key pattern in the corners of the frames.
  Its sprites tile rather than stretch, so a window as large as the screen keeps the fine grain of a card. The fonts
  become signed distance field assets with their looks (gold titles, text shadowed over the map), and the icons a sprite
  asset, so a line of text can carry `<sprite name="gold">`. The pointers are Kenney's cursors painted in the gold of
  the interface.
- **The battlefield**: a sky for every ground with the light measured from it, every obstacle in the looks of six
  grounds (the packs' palettes repainted for snow, sand, ash and bog), and the hills, mountains and woods that ring a
  field (`HeroesBattleArt`); the scene builder adds the gatehouse, the ruin, the stones and the banners of a siege.
