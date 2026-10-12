# Portfolio - Checkers

Checkers (draughts) for two on a 3D board, in three rule sets: English, Russian and International. Play the campaign
against the computer at rising strength, a quick game at the level you like, two people at one device, or online
against players on devices of their own, in rooms and tournaments. Two looks: Classic, a warm wooden board on green
felt with ivory and ebony pieces, and Midnight, a slate glass board under a deep blue night with neon pieces.

## Playing

- **Campaign**: six levels against the computer, each a rule set, a computer level and the side you play (see
  Campaign). A level opens when the one before it was won. **Play vs Computer** starts one game at once with the rules
  and the computer level of the settings. **Two Players** sets up a game at this device: two people, or one against
  the computer, the rule set, who moves first and a clock per move. **Play Online** opens the lobby (see
  Multiplayer).
- **A move**: click or tap a piece of yours to see where it may go (the settings can hide the hints), then the square
  to go to. A capture that can take several routes is clicked square by square along the route. Captures are
  mandatory in every rule set, so only the capturing moves are offered when there is one. Escape pauses; the pause
  menu saves the game, opens the settings and leaves to the title.
- The panels at the sides show each player's name, the side they play and the pieces they have taken; the line in the
  middle says whose move it is, and the clock, when the game has one, counts the seconds left for that move. When the
  clock runs out the computer makes a move for the player.
- A game against the computer or between two people saves itself under "Autosave" after every move of a person and
  continues from the title (Load Game takes up the save made last). Save Game in the pause menu keeps any number of
  saves by name, as YAML files, in BaseGame's saved games window (Load, Overwrite, Export, Delete, Import, Open Folder).
  Online games are not saved.
- **Settings**: show the legal moves, turn the board to the player to move (two people at one device), the computer's
  thinking time, the sound volume, the rules and the computer level of a quick game, and the look of the game.

## Rules

Play is on the dark squares only. A man moves one step diagonally forward; a capture jumps an enemy piece onto the
empty square behind it and goes on from there while another capture is possible: the whole sequence is one move.
Captures are mandatory. A man that reaches the far rank is crowned a king. The game ends when the side to move has no
legal move (the other side wins), when a side resigns, or in a draw after fifty plies without a capture and without a
man moving. The rule sets differ in the rest:

| Rule set | Board | Men a side | Men capture backward | Kings | Which capture | Crowning in a capture | First move |
| --- | --- | --- | --- | --- | --- | --- | --- |
| English | 8x8 | 12 | no | one step, short jump | any sequence | the man stops on the far rank, crowned | Dark |
| Russian | 8x8 | 12 | yes | flying: slide any distance, land anywhere beyond the piece taken | any sequence; the pieces taken stay until the sequence ends and are never jumped twice | the man is crowned at once and goes on as a king | Light |
| International | 10x10 | 20 | yes | flying | the sequence that takes the most pieces (majority rule) | a man passing the far rank mid-sequence is not crowned; only when the sequence ends there | Light |

## Campaign

Six games against the computer, in `Config/Campaign`. A win earns one star, keeping at least the level's pieces a
second and winning within the level's plies a third:

| # | Level | Rules | Computer | You play | Pieces to keep | Plies to win within |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | First Moves | English | Easy | Dark | 8 | 60 |
| 2 | Crowning Glory | English | Normal | Light | 7 | 80 |
| 3 | Flying Kings | Russian | Normal | Light | 7 | 80 |
| 4 | The Wide Board | International | Normal | Light | 12 | 120 |
| 5 | Grandmaster's Table | English | Hard | Dark | 6 | 100 |
| 6 | World Championship | International | Hard | Light | 10 | 140 |

The computer (`CheckersAI`) searches with alpha-beta over the rules engine, captures first: Easy looks two plies ahead
and picks at random among the moves that are nearly as good as the best, Normal four plies, Hard six with iterative
deepening; each level has a budget of positions so Hard answers in well under a second on a slow device. It counts
material (a man 100, a king 300, 250 in English where kings are short), the advance of the men, men guarding the
back rank, pieces in the centre and the moves available.

## Local play

**Two Players** opens BaseGame's local play window (`CheckersLocalRules`): two seats, each a person or the computer at
Easy, Normal or Hard (at least one person), their names, the rules, who moves first (the rules decide: Dark in
English, Light in the others; or Player 1 or 2) and the time per move (none, 30 seconds, 1, 2 or 5 minutes). The seats
take the colours of the sides they play in the look shown. Two people see the board from Light's side, or from the
side of the player to move when the setting *Turn board each move* is on; there is no hand-over
screen, the board is in the open. The setup is remembered on the device.

## Multiplayer

**Playing.** *Play Online* opens the lobby (BaseGame's shared lobby in the game's font and colours): pick a name,
open a room or join one. The host picks the rules (the level of the room), the time to move (no clock, 30 seconds, 1,
2 or 5 minutes), and whether the second seat is a computer opponent and at which level; otherwise two people play.
Everybody says Ready and the host starts the game; the seat that moves first alternates between the games of a room.
Both see the same board; the clock at the top says who the table waits for, and when it runs out the computer makes a
move for that player. A player who leaves or loses the connection is played on by the computer; with nobody left the
game ends. Nothing pauses an online game (Escape opens the match menu: back to the game, or leave), it is not saved
and earns no stars. The result leads back to the room.

**Tournaments.** The lobby's Tournaments tab (BaseGame's) opens a cup of up to 64 entrants, two a match: the bracket is
drawn when the host starts it, every player's client goes to its match room by itself, the match starts when both are
ready (or the clock to show up runs out), and the winner goes on. A draw in a tournament match is decided for the
bracket only: the side with more material goes on, and with equal material the side that moved second.

**How it is built.** The rules engine is deterministic and every command of a player is a `CheckersCommand` already,
so an online game is played in step over the action log of the base server, like Monopoly:

- Every client runs the same table (`CheckersTable`, a `LockstepTable<CheckersCommand>` of `Gamebox.Lockstep`). A
  command (a move as the squares of its path, a resignation, a seat handed to the computer) is sent as an action
  with `SubmitAction`; the server orders the actions of the room and stamps each with a random number. Every client,
  the sender included, applies an action only when it arrives. The computer's moves on the host's client travel
  through the log like anybody's; the clock's default move is chosen by every client alike from the stamp.
- The server (`Server/Lib.cs`) knows no rules of checkers: it seats the two players (or one and a computer) when the
  game starts, checks who may act for which seat, keeps the clock and appends the timeout action when it runs out,
  hands the seat of a leaver to the computer, and keeps `CheckersStats`.
- `CheckersOnlineController` (installed by `CheckersSceneBuilder` with BaseGame's `OnlineInstaller`) offers the rule
  sets and the options to the lobby, builds the table from the seat rows when the room starts, passes the actions
  on, and implements `ICheckersCommands`, the interface the board gives its commands to: offline `CheckersController`
  applies them to the table at once, online the controller sends them. `CheckersGameManager.Online.cs` is the online
  half of the manager (`ILockstepHost<CheckersCommand>`). Every client reports its place and the material it had left
  when the game is over.
- `CheckersOnlineTour` plays online games by itself in a development player (see Checking it).

## Code

### Base game integration

The game is built on the base classes of the BaseGame package (`com.skinnerboxes.basegame`, assembly
`BaseGameAssembly`, namespace `Gamebox`):

| Base class / interface | Implementation |
| --- | --- |
| `BaseGameManager` | `CheckersGameManager` (runs a table: the director waits for the board, the person or the computer; partial over `Table`, `Local`, `Online` and `Saves`) |
| `PlayerBase` | `CheckersPlayer` (the two seats) |
| `OfflineGameController` | `CheckersController` (the commands of a game at this device) |
| `LockstepOnlineController<T>` | `CheckersOnlineController` (the commands of an online game, through the server's action log) |
| `GameUI` / `SettingsUI` | `CheckersUI` (the title, the HUD, the result panel) / `CheckersSettingsUI` |
| `OverlayWindow` | `CampaignWindow` (the levels, their stars and locks) |
| `GameSettings` | `CheckersSettings` |
| `GameLevel` / `Campaign` / `CampaignProgress` | `CheckersLevel` / `CheckersCampaign` / `CheckersProgress` |
| `LocalPlayRules` | `CheckersLocalRules` (the options of Two Players and the table setup they make) |
| `GameTheme` | `CheckersTheme` (the two looks) |
| `PrefabPool<T>` | `SquarePool`, `PiecePool` |
| named saves (`OffersNamedSaves`, `CaptureSave`, `RestoreSave`) | `SaveData`: the table setup, the game and the campaign level (`CheckersGameManager.Saves.cs`) |

The `GameDefinition` asset under `Assets/Resources/Games` registers the game with the BaseGame launcher.

### Layout

- `Scripts/Model` (assembly `Skinnerboxes.Checkers.Model`, no Unity): the rules engine. `Board` (the cells of a board of
  any size), `Move` (a path of squares and the pieces taken), `MoveGenerator` (the legal moves of a side under a
  `VariantRules`), `CheckersGame` (a game: position, side to move, plies, the result and how it ended), `CheckersAI`
  (the computer), `CheckersCommand` (a command as data, with its text form for the action log), `TableSetup` (the two
  seats, who moves first, the clock) and `CheckersTable` (the lockstep table of a game, offline and online alike).
- `Scripts`: the manager, the controllers, the rules of local play, the settings, the campaign, the theme, `L` (the
  words of the game through BaseGame's localization) and the tours. `Scripts/View`: the board (`BoardView` with its
  squares and pieces, highlights and animations), the camera rig, the input and the sounds. `Scripts/UI`: the HUD, the
  seat panels, the settings panel and the campaign window. `Scripts/Server`: the generated client bindings and the
  stamped `GameServerClient`.
- `Tests/Editor`: edit mode tests. `Model/`: the rules (positions per rule set, moves, captures, promotion, the
  endings), the commands and their text, two tables fed the same log staying in step, the computer finishing games at
  every level; the other files: the themes, the localization table, the campaign and its stars, a game through its
  YAML save and the local play rules. The model tests also run outside Unity: `dotnet test` in the `Tests` folder next
  to `Assets` (`Checkers.Model.Tests.csproj`, on BaseGame's `ModelTests.props`).
- `Editor`: the builders that make the game's content from code, under the **Checkers** menu (see Building).

## Building

`Checkers > Build Everything` generates the art (wood and slate textures, the highlight disc, the shadow, the interface
sprites, the icon), the meshes (piece, crown, square, slab, table), the materials of both looks, the synthesized
sounds (click, slide, capture, crowning, win, lose), the settings asset, the six levels and the campaign, the two
themes (`Config/Themes/Classic.asset`, `Midnight.asset`), the `GameDefinition`, and the scene `Scenes/Checkers.unity`
with its HUD, the shared menu and the online lobby. Rebuild Art and Sound, Rebuild Content, Rebuild Scene and Rebuild
Themes redo a part. The generated assets live in the project, so the game runs without rebuilding. The shared helpers
of the builders (`GameAssets`, `MeshFactory`) are BaseGame's, in `Gamebox.Editor`.

## Checking it

The tests are in the Unity Test Runner (EditMode, `Skinnerboxes.Checkers.Tests`); the model tests also run with
`dotnet test` in `Tests/`. A development player (for example `-executeMethod
Gamebox.Editor.PlayerBuild.BuildFromCommandLine -gamebox-target StandaloneWindows64 -gamebox-development
-gamebox-output Build/Checkers.exe`) can play itself and save a screenshot of every step, then quit; without these
arguments, and in a release player, nothing of it runs:

- `-checkers-tour <folder>` (`CheckersTour.cs`) shoots the title and the campaign window, plays a quick game against
  the computer (the tour plays the person's seat through the path a click takes, until a capture and a crowning
  happened), the pause menu and the settings, the result, a game of two people at the device, and the title and a
  board in the game's next look, and writes `tour.log`. `-gamebox-theme Midnight` tours that look first.
- `-checkers-online host|join <folder>` (`CheckersOnlineTour.cs`, on BaseGame's `OnlineTour`) plays an online game
  against another player: the host opens a room (`-checkers-level 0|1|2` picks the rules, English by default, with a
  20 second clock) and starts it once the guest is ready; the guest leaves its first move to the clock. Both play with
  the computer's Normal choice. `joinquits` leaves the game after a while, and the host plays on against the computer.
  `hosttournament` and `jointournament` play a cup (`-checkers-entrants`, four by default; `-checkers-ready`, the
  seconds to show up for a match, 30 by default): three joiners and a host give a two-round bracket, photographed
  after every match. Every action of the log is written down with the checksum of the table after it, and the logs of
  the players carry the same numbers line for line. Give each player its own `-gamebox-identity <slot>`, and try it on
  a server of your own: `spacetime start --listen-addr 127.0.0.1:3917 --data-dir <folder>`, publish the module there
  (`spacetime build --module-path Server`, then `spacetime publish <database> --server http://127.0.0.1:3917
  --no-config --bin-path Server/bin/Release/net8.0/wasi-wasm/AppBundle/StdbModule.wasm` from a folder without a
  `spacetime.json`), and start the players with `-gamebox-server http://127.0.0.1:3917 -gamebox-database <database>`.
- `-gamebox-screenshots <folder>` (BaseGame) takes the launcher's picture of each look; `Gamebox > Themes > Import
  Screenshots` puts them on the themes.

## Themes

The whole look of the game is a `CheckersTheme` asset (`Scripts/CheckersTheme.cs`, a `GameTheme` of BaseGame): the
two sides (piece and crown materials, colour), the light and dark squares, the slab, the table, the highlight and the
contact shadow materials, the meshes, the colours of the highlights (selected, legal, capture, last move, hover), the
background, the ambient light and the sun, the launcher icon, the fonts (Poppins, BaseGame's) and the menu skin. The
`GameDefinition` (`Resources/Games/Checkers.asset`) starts with Classic and lists both. A player picks the look with
the *Look* row of the settings (remembered on the device), the launcher cycles it per game, and `-gamebox-theme
Midnight` picks one for a run of a development player or a tour. The board, the pieces and the panels redraw at once
(`ThemedRenderer` and `ThemedImage` parts on the scene objects, `BoardView.ApplyTheme` for what the code draws).
`CheckersThemeSpec` describes each look as data; the art builder runs the same generators for every spec, and the
content builder writes `Config/Themes/<Name>.asset`. Midnight's art goes under `Art/Themes/Midnight/`.

## Localization

Every word the player sees goes through `L.T("English words")` / `L.F("Level {0}", n)` (`Scripts/L.cs`, BaseGame's
`Loc` with the game's table), and `Assets/Localization/Checkers.csv` (`key,en,he`, the English is the key) holds the
Hebrew of every word of the game: the launcher's name and description, the looks, the sides and the rule sets, the
menu and the settings, local play, the campaign, the table and the results. Words the shared table
(`BaseGame/Assets/Localization/Gamebox.csv`) already has, such as Easy, Normal, Hard, Time to move or Autosave, are not
repeated: the game falls back to it. After editing the CSV, `Gamebox > Localization > Set Up` (or `-executeMethod
Gamebox.Editor.LocalizationTools.SetUpFromCommandLine`) rebuilds the tables. `CheckersLocalizationTest` checks that
every row has its Hebrew, that every key the code asks for is in a table, and that no row repeats a shared word.

## Server

The game's [SpacetimeDB](https://spacetimedb.com) module is the folder `Server` next to `Assets` (`Server/Lib.cs`
and `StdbModule.csproj`): the Gamebox base server of the BaseGame repository (`BaseServer`) plus this game's partial
`Module`, which seats the table of two (`ConfigureRoom`, `OnRoomStarted`), checks the commands (`ValidateAction`:
moves and resignations, from the player of the seat or the host for a computer seat), appends the timeout action
(`OnTurnTimedOut`), hands the seat of a leaver to the computer (`OnMemberLeft`, `KeepPlaying`) and ranks the table
and counts the games and wins of `CheckersStats` (`OnRoomFinished`, `OnUserDeleted`). `spacetime.json` names the
database (`skinnerboxes-checkers`) and where the client bindings go (`Assets/Scripts/Server/Bindings`, namespace
`Portfolio.Checkers.Server`). After a change to `Server/Lib.cs`: `Gamebox > Server > Publish Module` and `Generate
Client Bindings` in the editor, or `spacetime publish` and `spacetime generate` in this folder. The numbers of
`CommandKind` (`Scripts/Model/CheckersCommand.cs`) are part of the protocol.

## Project setup

- Unity 6000.6.0f1 with URP 17.6.0. The render pipeline assets are the ones that ship in the BaseGame package.
- This project and BaseGame reference each other in place: `Packages/manifest.json` references BaseGame as the local
  package `com.skinnerboxes.basegame` (`file:../../../BaseGame/Assets`), and `Assets/package.json` makes this project's
  `Assets` folder the package `com.skinnerboxes.checkers`, which BaseGame's manifest references
  (`file:../../Portfolio/Checkers/Assets`) and lists in its launcher. Nothing is copied either way, so the two
  repositories sit next to each other (`BaseGame` beside `Portfolio`), and nothing in the files may depend on either
  location: the builders find their folder through the package (`CheckersAssets.Assets`, a `GameAssets` of
  BaseGame).
