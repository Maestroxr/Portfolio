# Portfolio - Board Games

Three classic board games on one 3D table: **Checkers** (draughts, in English, Russian and International rules),
**Chess** and **Backgammon**. Each can be played against the computer at three strengths, in a campaign or a quick
game, by two people at one device, or online against players on devices of their own, in rooms and tournaments. Two
looks: Classic, warm wood on green felt with ivory and ebony pieces, and Midnight, slate glass under a deep blue night
with neon pieces. English and Hebrew.

## Playing

- **The title** shows three tabs over the menu: Checkers, Chess and Backgammon (keys 1, 2 and 3). The board behind the
  menu shows the game picked, and every button of the menu plays that game. The pick is remembered on the device.
- **Campaign**: the levels of the game picked (see Campaign). The first level of each game is open; the others open
  when the one before them in the same game was won. **Play vs Computer** starts one game at once against the computer
  at the level of the settings (checkers with the draughts rules of the settings, backgammon with or without the
  doubling cube as the settings say). **Two Players** sets up a game at this device: two people, or one against the
  computer, the game and its rules, who moves first, a clock per move and the doubling cube. **Play Online** opens the
  lobby (see Multiplayer).
- **Checkers**: click or tap a piece of yours to see where it may go, then the square to go to. A capture with several
  routes is clicked square by square along the route. Captures are mandatory, so only the capturing moves are offered
  when there is one.
- **Chess**: click a piece of yours; its moves are marked (captures in red), then click where it goes. A pawn that
  reaches the last rank opens a picker: Queen, Rook, Bishop or Knight (Q, R, B, N on the keyboard; Escape keeps the
  pawn selected). The last move is marked, and the king in check glows red with a "Check!" line.
- **Backgammon**: press Roll (Space or R); the opening roll throws a die for each player and the higher one plays both
  numbers. Click a checker that may move (its point glows) and then a lit point; a checker with one place to go moves at
  once, a second click on it moves it by the larger die. Each checker shows at once; Undo (Backspace) takes the last one
  back, and the play goes to the table when every die that can be played is used. Before rolling, Double offers the
  doubling cube (when the cube is yours or in the middle); the other player takes it (the game goes on at twice the
  stake) or drops it (and gives up the game at the stake as it was).
- The panels at the sides show each player's name, side and a line of the game (checkers: pieces taken; chess: pieces
  taken and the material ahead; backgammon: pips to go and checkers borne off); the line in the middle says whose turn
  it is, and the clock, when the game has one, counts the seconds left. When the clock runs out the computer decides for
  the player (in backgammon it finishes the turn: it rolls, plays the dice, and takes an offered cube). Escape pauses;
  the pause menu saves the game, opens the settings and leaves to the title.
- A game at this device saves itself under "Autosave" after every decision of a person and continues from the title
  (Load Game takes up the save made last). Save Game in the pause menu keeps any number of saves by name, as YAML files,
  in BaseGame's saved games window. An online game can be saved by name too, and a room can play it on later.
- **Settings**: show the legal moves, turn the board to the player to move (two people at one device), the computer's
  thinking time, the sound volume, the draughts rules and the doubling cube of a quick game, the computer level, and
  the look.

## Rules

### Checkers

Play is on the dark squares only. A man moves one step diagonally forward; a capture jumps an enemy piece onto the
empty square behind it and goes on from there while another capture is possible: the whole sequence is one move.
Captures are mandatory. A man that reaches the far rank is crowned a king. The game ends when the side to move has no
legal move (the other side wins), when a side resigns, or in a draw after fifty plies without a capture and without a
man moving.

| Rule set | Board | Men a side | Men capture backward | Kings | Which capture | Crowning in a capture | First move |
| --- | --- | --- | --- | --- | --- | --- | --- |
| English | 8x8 | 12 | no | one step, short jump | any sequence | the man stops on the far rank, crowned | Dark |
| Russian | 8x8 | 12 | yes | flying: slide any distance, land anywhere beyond the piece taken | any sequence; the pieces taken stay until the sequence ends and are never jumped twice | the man is crowned at once and goes on as a king | Light |
| International | 10x10 | 20 | yes | flying | the sequence that takes the most pieces (majority rule) | a man passing the far rank mid-sequence is not crowned; only when the sequence ends there | Light |

### Chess

The rules of FIDE: castling on both sides (king and rook unmoved, nothing between them, the king not in check and not
passing or landing on an attacked square), en passant right after a double step, promotion to a queen, rook, bishop or
knight, check, checkmate (a win) and stalemate (a draw). The other draws are automatic: the same position for the
third time, fifty moves of each side without a capture or a pawn move, and too little material to mate (king against
king, a king and a bishop or a knight against a king, bishops all on squares of one colour). White moves first. There
are no draw offers.

### Backgammon

Fifteen checkers a side, each player's starting position 2 on the 24-point, 5 on the 13, 3 on the 8 and 5 on the 6,
White moving towards its home board at the bottom right and Black the other way. A turn is a roll and a play: both
dice must be used when possible, the larger one when only one of them can be, and a double is played four times
(fewer when blocked). A lone checker hit by the other side goes to the bar and must come in on the other side's home
board before anything else moves. With all fifteen checkers home a player bears off, with the exact number or with a
larger one from the highest point. When a roll allows nothing, the turn passes at once. The game is won by bearing off
all fifteen: a single game, a gammon (twice the stake: the loser bore off none) or a backgammon (three times: the loser
bore off none and still has a checker on the bar or in the winner's home board). The doubling cube (on by default; off
in the local setup, the room options or the settings) doubles the stake; a dropped double gives the game up at the
stake as it was. Resigning costs a single game, or what a gammon or a backgammon would be worth once the other side
is bearing off and the resigner bore off nothing. No Crawford, Jacoby or beaver rules: each game stands alone.

## Campaign

Twelve games against the computer, in `Config/Campaign`; the window shows the levels of the game picked on the title.
A win earns one star; the second and third stars are for the level's targets:

| # | Level | Game | Computer | You play | Second star | Third star |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | First Moves | English draughts | Easy | Dark | keep 8 pieces | win within 60 plies |
| 2 | Crowning Glory | English draughts | Normal | Light | keep 7 pieces | win within 80 plies |
| 3 | Flying Kings | Russian draughts | Normal | Light | keep 7 pieces | win within 80 plies |
| 4 | The Wide Board | International draughts | Normal | Light | keep 12 pieces | win within 120 plies |
| 5 | Grandmaster's Table | English draughts | Hard | Dark | keep 6 pieces | win within 100 plies |
| 6 | World Championship | International draughts | Hard | Light | keep 10 pieces | win within 140 plies |
| 7 | King's Pawn | Chess | Easy | White | keep 8 pieces besides the king | win within 120 plies |
| 8 | The Black Pieces | Chess | Normal | Black | keep 6 pieces besides the king | win within 140 plies |
| 9 | Checkmate Master | Chess | Hard | White | keep 4 pieces besides the king | win within 160 plies |
| 10 | Bear Them Off | Backgammon | Easy | White | win a gammon | win within 110 decisions |
| 11 | Hit and Run | Backgammon | Normal | Black | win a gammon | win within 100 decisions |
| 12 | Double or Nothing | Backgammon | Hard | White | win a gammon | win within 100 decisions |

Backgammon levels play with the opening roll and the doubling cube; a backgammon "decision" is a roll, a play or an
action of the cube.

The computer of each game (`BoardAI` hands the decision to the game's own) counts positions, never the clock, so the
same game, level and random numbers give the same answer on every device:

- `CheckersAI`: alpha-beta over the rules engine, captures first; Easy looks two plies ahead and picks at random among
  the moves nearly as good as the best, Normal four plies, Hard six with iterative deepening, each within a budget of
  positions.
- `ChessAI`: negamax alpha-beta with iterative deepening, a quiescence search of captures and promotions, move
  ordering (the best move of the last iteration, captures by victim and attacker, killer moves), tapered middle game
  and end game piece-square tables, a table of searched positions; budgets of 2,500 (Easy, two plies, a random pick
  among the moves within 60 centipawns of the best), 40,000 and 250,000 positions (Hard answers in about a quarter of a
  second).
- `BackgammonAI`: every legal play of the dice weighed by an evaluation in whole numbers (the race, blots and the
  shots at them counted over the 36 rolls, made points, primes, anchors, the home board, checkers back; the Keith count
  once contact is over). Easy is a beginner with some chance in its choices that never doubles; Normal plays the best
  play by the evaluation and doubles and takes by the chances it reads from it; Hard looks one roll of the opponent
  ahead for its best candidates and has better cube decisions.

## Local play

**Two Players** opens BaseGame's local play window (`BoardGamesLocalRules`): two seats, each a person or the computer at
Easy, Normal or Hard (at least one person), their names, the game (English, Russian or International draughts, Chess,
Backgammon; the one picked on the title is preselected), who moves first (the rules decide: Dark in English draughts,
Light and White in the others, and in backgammon the opening roll; or Player 1 or 2, who then starts without an opening
roll), the time per move (none, 30 seconds, 1, 2 or 5 minutes) and the doubling cube. The seats take the colours of the
sides they play in the look shown. Two people see the board from Light's (White's) side, or from the side of the player
to act when the setting *Turn board each move* is on; every game is in the open, so there is no hand-over screen. The
setup is remembered on the device.

## Multiplayer

**Playing.** *Play Online* opens the lobby (BaseGame's shared lobby in the game's font and colours): pick a name, open
a room or join one. The host picks the game ("Plays": the three draughts rule sets, Chess or Backgammon, the level of
the room), the time to move, whether the second seat is a computer opponent and at which level, and for backgammon
the doubling cube. Everybody says Ready and the host starts the game; the side that moves first alternates between the
games of a room. A player who leaves or loses the connection is played on by the computer; with nobody left the game
ends. Nothing pauses an online game (Escape opens the match menu), and it earns no stars. The result leads back to the
room.

**Private rooms and players at one device.** A room can be private (*Who can join* in the lobby's form): it is not
listed, and the players the host gives its code to enter with it. BaseGame's lobby also lets other people join at the
device (*Players at this device*, signed in to an account of their own or as a guest) to play online through its
connection, but a board game has two seats: a device with two players fills the table, and two people at one device
playing each other is local play (*Two Players*). So such a room does not start, with or without a computer opponent
(the base server refuses a room that needs two players, the game's module the one with a computer opponent, where no
seat is left for the computer), a device with two players cannot enter a room somebody else is in, and tournaments are
one player a device. The game is ready for several seats at one device all the same: the seats played at this device
are a `DeviceSeats` (`Scripts/Model`), which the manager asks who acts for the people here (named, with the hand-over
and the board turning as in local play, "(here)" in the panels), and the controller sends a command for any of their
seats and reports the result of each.

**Tournaments.** The lobby's Tournaments tab (BaseGame's) opens a cup of up to 64 entrants for one game, two a match:
the bracket is drawn when the host starts it, every player's client goes to its match room by itself, and the winner
goes on. A draw in a draughts or chess match is decided for the bracket only: the side with more material goes on, and
with equal material the side that moved second.

**How it is built.** The rules engines are deterministic and every decision of a player is a `BoardCommand`, so an
online game is played in step over the action log of the base server, like Monopoly:

- Every client runs the same table (`BoardTable`, a `LockstepTable<BoardCommand>` of `Gamebox.Lockstep`, with
  `CheckersTable`, `ChessTable` and `BackgammonTable` for the three games). A command (a move, a backgammon roll or cube
  action, a resignation) is sent as an action with `SubmitAction`; the server orders the actions of the room and stamps
  each with a random number. Every client, the sender included, applies an action only when it arrives. Backgammon's
  dice are thrown from the table's random numbers reseeded by the stamp of the roll, so nobody knows them before the
  server spoke. The computer's decisions on the host's client travel through the log like anybody's; the clock's
  default decisions are worked out by every client alike from the stamp.
- The server (`Server/Lib.cs`) knows no rules of the games: it seats the two players (or one and a computer) when the
  game starts, checks who may act for which seat and which commands a player may send, keeps the clock and appends the
  timeout action when it runs out, hands the seat of a leaver to the computer, and keeps `BoardGamesStats`.
- `BoardGamesOnlineController` offers the rule sets and the options to the lobby, builds the table from the seat rows
  when the room starts, passes the actions on, and implements `IBoardCommands`, the interface the board gives its
  commands to: offline `BoardGamesController` applies them to the table at once, online the controller sends them.
  `BoardGamesManager.Online.cs` is the online half of the manager (`ILockstepHost<BoardCommand>`).

## Code

### Base game integration

The game is built on the base classes of the BaseGame package (`com.skinnerboxes.basegame`, assembly
`BaseGameAssembly`, namespace `Gamebox`):

| Base class / interface | Implementation |
| --- | --- |
| `BaseGameManager` | `BoardGamesManager` (runs a table for any of the games; partial over `Table`, `Modes`, `Local`, `Online`, `Saves` and one file per game: `Checkers`, `Chess`, `Backgammon`) |
| `PlayerBase` | `BoardGamesPlayer` (the two seats) |
| `OfflineGameController` | `BoardGamesController` (the commands of a game at this device) |
| `LockstepOnlineController<T>` | `BoardGamesOnlineController` (the commands of an online game, through the server's action log) |
| `GameUI` / `SettingsUI` | `BoardGamesUI` (the title, the HUD, the result panel) / `BoardGamesSettingsUI` |
| `OverlayWindow` | `CampaignWindow` (the levels of the game picked, their stars and locks) |
| `GameSettings` | `BoardGamesSettings` |
| `GameLevel` / `Campaign` / `CampaignProgress` | `BoardGamesLevel` / `BoardGamesCampaign` / `BoardGamesProgress` |
| `LocalPlayRules` | `BoardGamesLocalRules` (the options of Two Players and the table setup they make) |
| `GameTheme` | `BoardGamesTheme` (the two looks, with a `ChessLook` and a `BackgammonLook` inside) |
| `PrefabPool<T>` | `SquarePool`, `PiecePool`, `ChessPiecePool` |
| `DiceThrower` | the backgammon dice (BaseGame's thrower, lifted from Monopoly's dice) |
| named saves (`OffersNamedSaves`, `CaptureSave`, `RestoreSave`) | `SaveData`: the table setup, the game of its rule set and the campaign level (`BoardGamesManager.Saves.cs`) |

The `GameDefinition` asset under `Assets/Resources/Games` registers the game with the BaseGame launcher.

### The shared manager and the three games

`BoardGamesManager` runs every table the same way: the director offers each decision to a person at this device (who
clicks the board) or to the computer (`BoardAI` on a worker thread, synchronously on WebGL), waits while the board
animates, keeps the clock, hands the turn over between two people, and writes the result, the stars and the progress.
What differs per game is in its own file, through the same hooks (`XBegin`, `XEnd`, `XShowIdle`, `XFrame`, `XPick`,
`XClicked`, `XHovered`, `XTurnStarted`, `XEntry`, `XClearSelection`, `XHighlights`, `XBusy`, `XApplyLook`,
`XResultDetail`, `XSeatLine`, `XTurnWords`, with X the game): the shared code calls the hooks of the game played.

### Layout

- `Scripts/Model` (assembly `Skinnerboxes.BoardGames.Model`, no Unity): the rules engines. Shared: `GameMode` and
  `RuleSet` (the five rule sets, the level of a room), `IBoardGame`, `BoardCommand` (a command as data, with its text
  form for the action log), `TableSetup`, `BoardTable` and `BoardAI`. `Model/Checkers`: `Board`, `Move`,
  `MoveGenerator`, `VariantRules`, `CheckersGame`, `CheckersAI`, `CheckersTable`. `Model/Chess`: `ChessGame`,
  `ChessMoveGenerator` (with perft), `ChessNotation` (FEN and algebraic notation), `ChessAI`, `ChessTable` and their
  internal position, search and evaluation. `Model/Backgammon`: `BackgammonGame`, `CheckerMove` and
  `BackgammonPlay`, the play generator, `BackgammonAI` with its evaluation, `BackgammonTable`.
- `Scripts`: the manager, the controllers, the rules of local play, the settings, the campaign, the theme, `L` (the
  words of the game through BaseGame's localization) and the tours. `Scripts/View`: the square board shared by
  checkers and chess (`BoardView`, squares, draughts pieces, highlights), `View/Chess` (`ChessPiecesView` and its
  pieces), `View/Backgammon` (`BackgammonBoard`: points, checkers, bar, trays, dice and cube), the camera rig, the input
  and the sounds. `Scripts/UI`: the HUD, the seat panels, the mode tabs, the settings panel, the campaign window, the
  chess promotion picker (`ChessHud`) and the backgammon buttons (`BackgammonHud`). `Scripts/Server`: the generated
  client bindings and the stamped `GameServerClient`.
- `Tests/Editor`: edit mode tests. `Model/Checkers`, `Model/Chess` and `Model/Backgammon`: the rules of each game
  (chess perft counts of the standard test positions, every backgammon play checked against a plain brute-force
  reference, endings, scoring), the commands and their text, two tables fed the same log staying in step, the
  computer finishing games at every level and the stronger beating the weaker; `Model/DeviceSeatsTests.cs`: the seats
  an online table is played from at one device. The other files: the themes, the
  localization table, the campaign and its stars, games through their YAML saves and the local play rules. The model
  tests also run outside Unity: `dotnet test` in the `Tests` folder next to `Assets` (`BoardGames.Model.Tests.csproj`,
  on BaseGame's `ModelTests.props`).
- `Editor`: the builders that make the game's content from code, under the **Board Games** menu (see Building).

## Building

`Board Games > Build Everything` generates the art (wood and slate textures, the highlight disc, the shadow, the
interface sprites, the icon), the meshes (draughts piece and crown, square, slab, table; the six Staunton chess pieces
from lathed profiles and an extruded knight; the backgammon frame, felt, point, checker, die and cube), the materials of
both looks, the synthesized sounds (click, slide, capture, crowning, check, dice, checker, bearing off, cube, win,
lose), the settings asset, the twelve levels and the campaign, the two themes (`Config/Themes/Classic.asset`,
`Midnight.asset`), the `GameDefinition`, and the scene `Scenes/BoardGames.unity` with its HUD, the shared menu and the
online lobby. Rebuild Art and Sound, Rebuild Content, Rebuild Scene and Rebuild Themes redo a part; Chess Preview and
Backgammon Preview render pictures of the pieces and the board. The generated assets live in the project, so the game
runs without rebuilding. The shared helpers of the builders (`GameAssets`, `MeshFactory`, `DiceArt`) are BaseGame's,
in `Gamebox.Editor`.

## Checking it

The tests are in the Unity Test Runner (EditMode, `Skinnerboxes.BoardGames.Tests`); the model tests also run with
`dotnet test` in `Tests/`. A development player (for example `-executeMethod
Gamebox.Editor.PlayerBuild.BuildFromCommandLine -gamebox-target StandaloneWindows64 -gamebox-development
-gamebox-output Build/BoardGames.exe`) can play itself and save a screenshot of every step, then quit; without these
arguments, and in a release player, nothing of it runs:

- `-boardgames-tour <folder>` (`BoardGamesTour.cs`) tours the three games: checkers (the title, the campaign window, a
  quick game until a capture and a crowning, the pause menu and the settings, the result, two people with a clock),
  chess (a move by clicks against the computer, a campaign level, a game of two people played by clicks with en
  passant, check, a promotion through the picker and castling, saved and loaded back, the scholar's mate), backgammon
  (a whole game against the computer played by clicks with a move taken back and the cube offered both ways, a campaign
  level, two people without the opening roll or the cube with the board turning, saved and loaded back), then every
  title and a board in the next look. Every step that does not do what it should writes a FAIL line to `tour.log`.
  `-boardgames-tour-games chess,backgammon` picks the games; `-gamebox-theme Midnight` tours that look first and
  `-gamebox-language he` tours it in Hebrew.
- `-boardgames-online host|join <folder>` (`BoardGamesOnlineTour.cs`, on BaseGame's `OnlineTour`) plays an online game
  against another player: the host opens a room (`-boardgames-level 0|1|2|3|4` picks the rule set: the three draughts,
  chess, backgammon) and starts it once the guest is ready; the guest leaves its first decision to the clock. Both play
  with the computer's Normal choice. `joinquits` leaves the game after a while, and the host plays on against the
  computer. `hostprivate` and `joinprivate` play a private room, entered with the code the host leaves in the folder.
  `hostparty` brings a second player to the host's device first (`-gamebox-companions 1`, or a guest with
  `-gamebox-guests 1 -gamebox-companions 0`): the two fill the table, so the start is refused, in the room as it is and
  again with a computer opponent, and the tour photographs both refusals in the lobby and leaves; `joinparty` against
  a `host` is refused as well (the room is full), and the tour writes the refusal down and quits. `hosttournament` and `jointournament` play a cup (`-boardgames-entrants`, four by default;
  `-boardgames-ready`, the seconds to show up for a match). Every action of the log is written down with the checksum
  of the table after it, and the logs of the players carry the same numbers line for line. Give each player its own
  `-gamebox-identity <slot>`, and try it on a server of your own (`spacetime start --listen-addr 127.0.0.1:3917`,
  publish the module there with `spacetime publish --server http://127.0.0.1:3917 --module-path Server <database>`,
  start the players with `-gamebox-server http://127.0.0.1:3917 -gamebox-database <database>`).
- `-gamebox-screenshots <folder>` (BaseGame) takes the launcher's picture of each look; `Gamebox > Themes > Import
  Screenshots` puts them on the themes.

## Themes

The whole look of the game is a `BoardGamesTheme` asset (`Scripts/BoardGamesTheme.cs`, a `GameTheme` of BaseGame): the
two sides (piece and crown materials, colour), the squares, the slab, the table, the highlight and contact shadow
materials, the meshes, the highlight colours, the background, the lights, the launcher icon, the fonts and the menu
skin, plus `chess` (`ChessLook`: the meshes of the six pieces, body and trim materials per side) and `backgammon`
(`BackgammonLook`: the board's meshes and materials, checkers, dice and cube). The `GameDefinition`
(`Resources/Games/BoardGames.asset`) starts with Classic and lists both. A player picks the look with the *Look* row of
the settings, the launcher cycles it per game, and `-gamebox-theme Midnight` picks one for a run of a development
player. `BoardGamesThemeSpec` describes each look as data; the art builders run the same generators for every spec,
and the content builder writes `Config/Themes/<Name>.asset`. Midnight's art goes under `Art/Themes/Midnight/`.

## Localization

Every word the player sees goes through `L.T("English words")` / `L.F("Level {0}", n)` (`Scripts/L.cs`, BaseGame's
`Loc` with the game's table), and `Assets/Localization/BoardGames.csv` (`key,en,he`, the English is the key) holds the
Hebrew of every word of the three games. Words the shared table (`BaseGame/Assets/Localization/Gamebox.csv`) already
has, such as Easy, Normal, Hard or Autosave, are not repeated: the game falls back to it. After editing the CSV,
`Gamebox > Localization > Set Up` (or `-executeMethod Gamebox.Editor.LocalizationTools.SetUpFromCommandLine`)
rebuilds the tables; a player only shows the words the tables held when it was built. `BoardGamesLocalizationTest`
checks that every row has its Hebrew, that every key the code asks for is in a table, and that no row repeats a shared
word.

## Server

The game's [SpacetimeDB](https://spacetimedb.com) module is the folder `Server` next to `Assets` (`Server/Lib.cs` and
`StdbModule.csproj`): the Gamebox base server of the BaseGame repository (`BaseServer`) plus this game's partial
`Module`, which seats the table of two (`ConfigureRoom` checks the rule set and the clock, `OnRoomStarted`, which
refuses the start when both seats are people at one device), checks the
commands (`ValidateAction`: moves, backgammon rolls and cube actions and resignations, from the player of the seat or
the host for a computer seat; the computer taking a seat is the server's own action), appends the timeout action
(`OnTurnTimedOut`), hands the seat of a leaver to the computer (`OnMemberLeft`, `KeepPlaying`) and ranks the table
and counts the games and wins of `BoardGamesStats` (`OnRoomFinished`, `OnUserDeleted`). `spacetime.json` names the
database (`skinnerboxes-boardgames`) and where the client bindings go (`Assets/Scripts/Server/Bindings`, namespace
`Portfolio.BoardGames.Server`). After a change to `Server/Lib.cs`: `Gamebox > Server > Publish Module` and `Generate
Client Bindings` in the editor, or `spacetime publish` and `spacetime generate` in this folder. The numbers of
`CommandKind` (`Scripts/Model/BoardCommand.cs`) and of `RuleSet` (`Scripts/Model/GameMode.cs`) are part of the
protocol and of the saved games.

## Project setup

- Unity 6000.6.0f1 with URP 17.6.0. The render pipeline assets are the ones that ship in the BaseGame package.
- This project and BaseGame reference each other in place: `Packages/manifest.json` references BaseGame as the local
  package `com.skinnerboxes.basegame` (`file:../../../BaseGame/Assets`), and `Assets/package.json` makes this project's
  `Assets` folder the package `com.skinnerboxes.boardgames`, which BaseGame's manifest references
  (`file:../../Portfolio/BoardGames/Assets`) and lists in its launcher. Nothing is copied either way, so the two
  repositories sit next to each other (`BaseGame` beside `Portfolio`), and nothing in the files may depend on either
  location: the builders find their folder through the package (`BoardGamesAssets`, a `GameAssets` of BaseGame).
- The project was called Checkers before chess and backgammon joined it (2026-10-12).
