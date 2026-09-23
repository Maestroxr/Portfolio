# Portfolio - Monopoly

Monopoly for up to four players, any mix of people sharing the device and computer players, or people on devices of
their own playing online, on a 3D board with a modern Monopoly look: the World Tour edition, with streets from Lisbon
to New York, travel themed Chance and Community Chest cards, die cast style tokens and a wooden table. It plays the
full official rules, and the modes add the favourite variants: the Mega Edition speed die, the official short game,
the Free Parking jackpot and more.

## Playing

- **New game**: pick a mode, then set up to four seats. Each seat is a person, a computer player (Easy, Normal or Hard)
  or empty, with a name and one of eight tokens (race car, top hat, Scottie dog, battleship, cat, rubber duck, penguin,
  T-Rex). The default is you against three computer players.
- **A turn**: roll the dice, then buy the property you land on or send it to auction. The action panel in the middle of
  the board offers the choices of the moment (roll, pay the jail fine, use a card, ride the bus, end the turn), the
  **Manage** window builds and sells houses and hotels and handles mortgages, and **Trade** puts properties, cash and
  Get Out of Jail Free cards on the table (computer players answer at once and make offers of their own).
- Click or tap any space to see its title deed with the rent that applies now. Keys: Space rolls and ends turns, M
  manages, T trades, P pays the jail fine, U uses a jail card, 1 to 3 pick a bus ride, Escape pauses.
- The corner panels show each player's cash (counting up and down as money moves), net worth, a chip per color set and
  tags for computer players, jail and jail cards. News of the turn scrolls on the left, the mode, the round, the Free
  Parking jackpot and the houses and hotels left in the bank on the right.
- A match saves at the start of every human turn and continues from the title screen. Winning earns stars per mode:
  one for beating the computer players, two for beating three of them, three for beating three Hard ones.
- **Play online** opens the lobby of the game's server: two to four people on devices of their own at one table,
  against a clock (see Multiplayer).

## Rules

The rules engine plays the official game: $1,500 to start and $200 for passing GO; buying or auctioning every
property; rent doubled on unimproved full sets, by the number of stations owned, and 4 or 10 times the dice for
utilities; three doubles in a row, the Go to Jail space and cards send players to jail, where they roll for doubles,
pay $50 or use a card (the third failed roll pays the fine); even building from a bank of 32 houses and 12 hotels, with
building shortages; mortgages at half price and 10% interest to lift them (also due when a mortgaged property changes
hands); debts to raise by selling buildings and mortgaging, and bankruptcy that hands everything to the creditor (or
back to the bank, whose properties go to auction).

The modes, in `Config/Campaign`:

| Mode | Rules |
| --- | --- |
| Classic | The official rules. |
| Speed Die | The Mega Edition's speed die once a player has passed GO: 1 to 3 add to the move, Mr. Monopoly moves on to the next property for sale (or the next rent owed), the bus moves by either die or both, triples move anywhere. |
| Quick Deal | The official short game: two title deeds dealt to everyone (and paid for), hotels at three houses, the game ends at the second bankruptcy and the richest player wins. |
| House Rules | Taxes and fines feed a Free Parking jackpot, landing on GO pays double, party cards (Robin Hood, free houses, a charity gala) join the decks, and there are no auctions. |
| Tycoon Rush | Twenty rounds with the speed die and $2,000 each; the highest net worth wins. |
| Custom Rules | The house rules set on the main menu: starting cash, a round limit, dealt deeds, auctions, the speed die, the jackpot, double GO, no rent collected in jail and the party cards. |

## Code

### Base game integration

The game is built on the base classes of the BaseGame package (`com.skinnerboxes.basegame`, assembly
`BaseGameAssembly`, namespace `Gamebox`):

| Base class / interface | Implementation |
| --- | --- |
| `BaseGameManager` | `MonopolyGameManager` (directs a match: plays its events as animations, then waits for a person or a computer player) |
| `PlayerBase` | `MonopolyPlayer` (a token on the board) |
| `OfflineGameController` | `MonopolyController` (checks every human command against the rules engine) |
| `OnlineGameController` | `MonopolyOnlineController` (sends the commands of an online match through the server's action log) |
| `GameUI` | `MonopolyUI` |
| `SettingsUI` | `MonopolySettingsUI` (the house rules and the animation speed) |
| `GameSettings` | `MonopolySettings` |
| `GameLevel` | `MonopolyLevel` (a mode) |
| `Campaign` | `MonopolyCampaign` (every mode open, stars per mode) |
| `CampaignProgress` | `MonopolyProgress` (games, wins and the best net worth per mode) |
| `PrefabPool<T>` | `BuildingPool` (houses and hotels) |
| `IStorageStrategy` | the saved match (`MonopolyGameManager.Events.cs`) |

The `GameDefinition` asset under `Assets/Resources/Games` registers the game with the BaseGame launcher, and the
editor tooling of the package (Gamebox > Sync Game Scenes To Build Settings) keeps the game scene in the build
settings. This project has no main menu scene, so the Exit button of the shared menu closes a build of the game on its
own; in the editor it reloads the game scene.

### Layout

- `Scripts/Model`: the rules engine, plain C# with no scene objects. `MonopolyMatch` holds the whole state of a match
  (it saves as JSON), takes commands (roll, buy, bid, build, mortgage, trade, pay, go bankrupt) and reports what
  happened as a list of `MatchEvent`s. `BoardLayout` and `WorldTourBoard` describe the board and the cards, `RuleSet`
  the rules of a mode, and `BotBrain` plays the computer players, including the trades they propose and accept.
  `MatchCommand` is a command as data (what a computer player decides on and what an online match sends),
  `LockstepMatch` the match of an online table and `SeededRandom` its dice.
- `Scripts/View`: the board, the dice, the buildings and the camera. `Scripts/UI`: the panels and popups.
- `Tests/Editor`: edit mode tests of the rules engine, of computer players finishing games in every mode, and of what
  an online match stands on (`LockstepTest`: commands as text, the seeded dice, two tables fed the same log staying
  in step through whole games, the default moves of the clock).
- `Editor`: the builders that make the game's content from code, under the **Monopoly** menu. Build Everything
  generates the textures (board, cards, dice, token badges, interface shapes), the meshes (tokens, houses, hotels,
  dice, board, table), the materials, the font assets, the synthesized music and jingles, the modes and the scene with
  its interface. Rebuild after changing a builder; the generated assets live in the project, so the game runs without
  rebuilding.

## Multiplayer

**Playing.** *Play Online* on the title screen opens the lobby (the shared lobby of BaseGame in the game's font and
colours): pick a name, open a room or join one. The host picks the mode (every mode but Custom Rules, whose rules
live on one device), the time to move (20 to 90 seconds, or no clock) and up to two computer players with their level;
they take the seats the people leave free, up to four at the table. Two to four people play. Everybody says Ready and
the host starts the game; another player opens every game of a room.

Everybody sees the same board: dice, cards, auctions and money play out on every screen. The action panel offers
choices only to the player whose move it is and says who is thinking otherwise; a trade is put together in the trade
window and answered on the device of the other player (the host's device answers for computer players), and nothing
else moves until it is. The clock at the top of the screen says who the table waits for and counts the seconds. When
it runs out the default move is made for that player: roll, leave the property to the auction, drop out of the
bidding, take the first bus, move to GO, end the turn; a debt is raised by mortgaging and selling, or the player goes
bankrupt when that is not enough; an offer is declined. Building, mortgaging and proposing trades do not restart the
clock. A player who leaves or loses the connection is played on by the computer; with fewer than two people left the
match ends and the richest player wins, as when the host ends the game. Nothing pauses an online match (Escape opens
the match menu: back to the game, or leave), it is not saved and earns no stars. The results lead back to the room.

**How it is built.** The rules engine is deterministic given its random numbers, and every command of a player was a
call of `MonopolyController` already, so an online match is played in step over the action log of the base server:

- Every client runs the same match (`LockstepMatch`). A command is a `MatchCommand` (kind, seat, a number or a trade
  offer) sent as an action with `SubmitAction`; the server orders the actions of the room and stamps each with a
  random number. Every client, the sender included, applies an action only when it arrives, after seeding the match's
  `SeededRandom` with the stamp, so the dice are the server's and nobody knows them before. The match is started from
  the seed of the room (deck shuffles, dealt deeds). Nothing else changes the match; a trade is an offer, an answer
  and its execution in the log, with the offer held by the `LockstepMatch`.
- The server (`Server/Lib.cs`) knows no rules of Monopoly. It seats the table when the game starts (`MonopolySeat`:
  the members in seat order, then the computer players), checks who may act for which seat (a member for their own,
  the host for the computer players), keeps the clock (decisive actions restart it; when it runs out it appends the
  timeout action, on which every client makes `MatchCommand.DefaultMove` for whoever the match waits for), turns the
  seat of a leaver into a computer player with an action of its own, and keeps `MonopolyStats`.
- `MonopolyOnlineController` (installed by `MonopolySceneBuilder` with BaseGame's `OnlineInstaller`) offers the modes
  and options to the lobby, builds the table from the seat rows when the room starts, passes the actions on, and
  implements `IMonopolyCommands`, the interface the UI gives its commands to: `MonopolyController` applies them to
  the engine, the online controller sends them. `MonopolyGameManager.Online.cs` is the online half of the manager:
  it applies what arrives at once (the engine may run ahead of the board, which plays the events as ever and a little
  faster while it is behind), offers a decision only to the player at this device once the board caught up, and on
  the host's client asks `BotBrain.Decide` for the move of a computer player and sends it like any other. Every
  client reports the net worth of its player as the score of the room, and `FinishPlaying` when the match is over.
- `MonopolyOnlineTour` plays a match by itself in a development player (`-monopoly-online host|join <folder>`, with
  `-gamebox-server`, `-gamebox-database` and `-gamebox-identity`): screenshots, and a log of every action with the
  checksum of the match after it, to compare between the players.

## Phones and tablets

The game runs on Android with the shared mobile code of BaseGame (see its README, "Phones and tablets"). The camera
fits the board between the side columns of the interface at any aspect ratio, the interface keeps to the safe area,
and taps work like clicks. `Gamebox > Android > Build APK` builds `Build/Android/Monopoly.apk`
(`com.skinnerboxes.monopoly`, with the game's icon).

## Assets and credits

- [Poppins](https://fonts.google.com/specimen/Poppins) by Indian Type Foundry, SIL Open Font License 1.1
  (`Art/Fonts/Poppins-OFL.txt`).
- [Font Awesome Free](https://fontawesome.com) solid icons: icons CC BY 4.0, font SIL OFL 1.1
  (`Art/Fonts/FontAwesome-LICENSE.txt`).
- Sound effects from [Kenney](https://kenney.nl)'s Casino Audio, Interface Sounds and Music Jingles packs, CC0
  (`Audio/Kenney/License.txt`).
- Everything else (board and card art, 3D models, the music) is generated by the editor code of this project.
- Monopoly is a trademark of Hasbro. This is a portfolio piece with an original board, original card texts and
  original art.

## Server

The game's [SpacetimeDB](https://spacetimedb.com) module is the folder `Server` next to `Assets` (Unity does not show
it; open `Server/StdbModule.csproj` in the IDE). It is the Gamebox base server of the BaseGame repository (`BaseServer`:
users and login, connecting and disconnecting, the player profile) plus `Server/Lib.cs`, which declares the same
`public static partial class Module` to add the tables and reducers of this game and implements the base server's
partial methods to react to its events. For Monopoly it adds the table of an online match and the statistics, and no
reducers: the game is played through the action log of the base server (see Multiplayer).

| Part of `Server/Lib.cs` | What it does |
| --- | --- |
| `MonopolySeat` (public table) | The seats of a room that plays: order of play, human or computer, the member, name, token, computer level. |
| `MonopolyStats` (public table) | Games, wins and the best net worth of a player in online matches. |
| `ConfigureRoom` | Two to four players; the clock from the option `turn` (0 for none, else 10 to 600 seconds, 45 by default); the mode is the level of the room. Options `bots` (0 to 2) and `botlevel` (0 to 2) are read when the game starts. |
| `OnRoomStarted` | Seats the members in seat order, fills free seats with computer players, starts the clock. |
| `ValidateAction` | The command is one a player may send, and the seat is the sender's own, or a computer's when the sender is the host. Building, mortgaging and trading do not restart the clock. |
| `OnTurnTimedOut` | Appends the timeout action. |
| `OnMemberLeft` | The seat of the leaver becomes a computer player: the row changes and an action tells every client where in the log. |
| `OnRoomFinished`, `OnRoomCleared`, `OnUserDeleted` | The members take the places of their seats at the table as their clients reported them (`RankTable` of the base server: a computer player that won comes first), and the statistics count a win for the member who won; the seats of the room go; the statistics of a deleted user go. |

- `Packages/manifest.json` references the SpacetimeDB SDK and the base server package
  (`com.skinnerboxes.baseserver`, `file:../../../BaseGame/BaseServer`); `Server/StdbModule.csproj` imports
  `BaseServer.props` from there, which compiles the base server into this module.
- `spacetime.json` names the database (`skinnerboxes-monopoly`, on the `local` server) and where the client bindings go:
  `Assets/Scripts/Server/Bindings`, namespace `Portfolio.Monopoly.Server`.
- After a change to `Server/Lib.cs`: `Gamebox > Server > Publish Module` and `Generate Client Bindings` in the editor,
  or `spacetime publish` and `spacetime generate` in this folder. Generating also writes this game's
  `GameServerClient` (`Assets/Scripts/Server`), the component that connects, logs the player in and keeps the
  profiles, the rooms and the action log. The numbers of `CommandKind` (`Scripts/Model/MatchCommand.cs`) are part of
  the protocol: the server tells the commands that answer the match from the ones that manage property by them.

The BaseGame README ("Online play") describes the shared client, `BaseServer/README.md` the server side.

## Project setup

- Unity 6000.6.0f1 with URP 17.6.0. The render pipeline assets are the ones that ship in the BaseGame package.
- This project and BaseGame reference each other in place. Nothing is copied either way, so the two repositories
  have to sit next to each other (`BaseGame` beside `Portfolio`):
  - `Packages/manifest.json` references BaseGame as the local package `com.skinnerboxes.basegame`
    (`file:../../../BaseGame/Assets`), which provides the base classes, the shared menu and the URP assets.
  - `Assets/package.json` makes this project's `Assets` folder the package `com.skinnerboxes.monopoly`. BaseGame's
    manifest references it (`file:../../Portfolio/2 - Monopoly/Assets`) and lists the game in its launcher.
- The same files therefore serve two projects and are mounted at `Assets/` here and at `Packages/com.skinnerboxes.monopoly/`
  in BaseGame, so nothing in them may depend on either location: the builders find their folder through the package
  (`MonopolyAssets.Root`), and the `GameDefinition` asset re-derives its scene path from its scene reference in the
  editor.
