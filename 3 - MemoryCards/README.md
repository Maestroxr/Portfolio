# Portfolio - Memory Cards

A memory game with a cast of thirty cute critters. Flip the cards, find the pairs, and work through a campaign of
three worlds and eighteen levels where every level brings in a new twist: memorize the board, race the clock, play wild
cards, dodge bombs, crack ice, follow the parade and match in threes. When the campaign is done, the Critter Carnival
offers an endless run and free play with your own rules.

## Playing

- Click or tap a card to flip it; flip its twin to make a match. Matches in a row build a combo worth up to x5 points.
- After a mistake the cards flip back on their own; click or tap a card to flip them back sooner.
- Escape, the back button on a phone or the pause button opens the pause menu (resume, restart, save or load the
  game, settings, level select).
- Every campaign level has three stars: clear the board, stay under the level's mistake count, and reach its third
  goal (time, moves, combo or score). Worlds open with stars; progress, best scores and records are saved automatically.
- **Versus.** Two to four players share a board and take turns. A set scores for whoever found it (sets in a row
  build the combo) and lets them go again; a mistake shows for a moment and passes the turn, a bomb costs points and
  the turn, and so does running out of time: every turn has a clock. The most points win when the board is cleared.
  The **players** button of the level select steps from one player to a versus game for up to four at this device,
  on any level but the endless run; **online** opens the lobby to play against people elsewhere.

| Twist | What it does |
| --- | --- |
| Memorize | Every card is shown face up for a few seconds before the round starts. |
| Time attack | A countdown; completed sets add seconds. Running out of time loses the level. |
| Clock card | Flip it for extra seconds (or seconds off the clock on untimed levels). |
| Survival | A heart for every mistake; losing them all loses the level. |
| Wild card | Flip it with any animal: every card of that animal is matched at once. |
| Shuffle | After a number of mistakes the hidden cards swap places. |
| Bomb | A trap card: costs a heart, time and points. |
| Parade | The animals must be matched in the order the parade at the top shows. |
| Triplets | Sets of three identical cards instead of pairs. |
| Ice | A frozen card needs one tap to crack the ice before it flips. |
| Move limit | Clear the board in a limited number of attempts. |
| Peek card | Shows every hidden card for a moment. |

| # | Level | World | Mode | New |
| --- | --- | --- | --- | --- |
| 1 | Hello, Farm! | Sunny Farm | Classic, 6 pairs | Flipping and matching |
| 2 | Barnyard Buddies | Sunny Farm | Classic, 8 pairs | Combos |
| 3 | Peek-a-Boo | Sunny Farm | Classic, 10 pairs | Memorize |
| 4 | Race the Rooster | Sunny Farm | Time attack | Time attack |
| 5 | Tick-Tock Barn | Sunny Farm | Time attack | Clock cards |
| 6 | Hen Party | Sunny Farm | Survival | Hearts |
| 7 | Welcome to the Jungle | Jungle Jam | Classic, 5 x 5 | Wild card |
| 8 | Monkey Business | Jungle Jam | Classic | Shuffle |
| 9 | Snake Pit | Jungle Jam | Survival | Bombs |
| 10 | Parrot Parade | Jungle Jam | Parade | Parade |
| 11 | Triple Trouble | Jungle Jam | Classic | Triplets |
| 12 | Jungle Rumble | Jungle Jam | Time attack | Everything so far |
| 13 | Thin Ice | Frosty Shores | Classic | Ice |
| 14 | Penguin Waddle | Frosty Shores | Move limit | Moves and peek cards |
| 15 | Whale Songs | Frosty Shores | Time attack | Triplets against the clock |
| 16 | Blizzard | Frosty Shores | Survival | Ice and fast shuffles |
| 17 | Polar Parade | Frosty Shores | Parade | Parade with bombs |
| 18 | Aurora Finale | Frosty Shores | Survival, 7 x 4 | Everything |
| - | Endless Carnival | Critter Carnival | Endless | Boards grow and add twists; one clock for the whole run |
| - | Free Play | Critter Carnival | Free play | The rules of the settings panel |

Jungle Jam opens with 10 stars and Frosty Shores with 26 (of 54); the endless run opens after six levels, free play is
always open.

## Base game integration

The game is built on the base classes of the BaseGame package (`com.skinnerboxes.basegame`, assembly
`BaseGameAssembly`, namespace `Gamebox`):

| Base class / interface | Implementation |
| --- | --- |
| `BaseGameManager` | `MemoryCardsGameManager` |
| `PlayerBase` | `MemoryCardsPlayer` |
| `OfflineGameController` | `MemoryCardsController` |
| `GameUI` | `MemoryCardsUI` |
| `SettingsUI` | `MemoryCardsSettingsUI` |
| `GameSettings` | `MemoryCardsSettings` |
| `GameLevel` | `MemoryCardsLevel` |
| `Campaign` | `MemoryCardsCampaign` (worlds, star gates, the endless run and free play) |
| `CampaignProgress` | `MemoryCardsProgress` (endless records, lifetime counters, the last world shown) |
| `PrefabPool<T>` | `FlippableCache` (the cards) |
| `OnlineGameController` (assembly `Gamebox.Online`) | `MemoryCardsOnlineController` |

`MemoryCardsCampaign` overrides the unlock rule, the star count and the maximum stars of the base campaign: a level
opens once the campaign level before it is completed and the campaign holds the stars its world asks for, only
campaign levels earn stars, the endless run opens after six levels and free play is always open.

The game uses its own menu panel on the base `GameUI` and `SettingsUI` fields: the pause menu (resume, restart,
save, load, settings, level select, quit) and the settings panel of free play. The settings panel edits the custom
rules; "Use Custom Rules" switches free play between the default and the custom rules, as the base class does. A game
in progress can be saved from the pause menu and continued from the level select. The `GameDefinition` asset under
`Assets/Resources/Games` registers the game with the BaseGame launcher (with its icon). This project has no main menu
scene, so the Quit button closes a build of the game on its own; in the editor it reloads the game scene.

## How it works

| Code | Role |
| --- | --- |
| `Scripts/Model/MemoryRound.cs` | The rules engine of a board: matches, mistakes, combos, special cards, ice, parade, shuffles, hearts, moves, the clock and the finish bonus |
| `Scripts/Model/Dealer.cs`, `RoundRules.cs` | Deals a board (sets, special cards, frozen cards, parade order) and validates the rules |
| `Scripts/Model/StarGoals.cs`, `EndlessRules.cs` | The three stars of a level; the boards of the endless run |
| `Scripts/MemoryCardsGameManager.cs` | Level select, dealing, memorize, clicks through the rules engine, animations, results, progress, save and load |
| `Scripts/Model/VersusMatch.cs` | The rules of several players at one board: turns, scores per player, the clock of a turn, the standings; the server judges online flips with the same code |
| `Scripts/Model/BoardOptions.cs` | The board of an online room in the room's options: written by the host's client, read back by the server |
| `Scripts/MemoryCardsGameManager.Versus.cs`, `Scripts/UI/VersusHud.cs` | Versus games at one device and online: the seats, the turn clock, the flips the server judged, the scoreboard, the results |
| `Scripts/MemoryCardsOnlineController.cs` | The online game: rooms and levels for the lobby, the board of the room, flip events, turns and scores from the server (a turn reaches the manager after the flip that ended the last one) |
| `Scripts/Flippable.cs`, `Scripts/View/*` | The card (flip, deal, shuffle, match, mistake, ice, explosion), board layout, UI particles, backdrop, audio |
| `Scripts/UI/*` | Level select (world tabs, level cards, details), HUD, results, pause menu, settings panel |
| `Scripts/MemoryCardsAutopilot.cs` | Plays a level by itself; add it to any object in play mode to test a level end to end |
| `Scripts/MemoryCardsTour.cs` | Development builds only: `-memorycards-tour <folder>` plays through the game and saves screenshots; `-memorycards-versus <folder>` plays only the versus game for three |
| `Scripts/MemoryCardsOnlineTour.cs` | Development builds only, on BaseGame's `OnlineTour`: `-memorycards-online host <folder>` and `-memorycards-online join <folder>` (or `idle`, `quitter`) play an online game between two running players; `-memorycards-mistakes <rate>`, `-memorycards-bombs` and `-memorycards-level <n>` change what and how they play |

The rules engine knows nothing of Unity objects, so it is covered by edit mode tests (`Tests/Editor`, assembly
`Skinnerboxes.MemoryCards.Tests`) together with the dealer, the settings, the campaign unlocks and the progress. The
tests of the rules (the dealer, the round, the versus rules, the board options) also run outside Unity: `dotnet test`
in the `Tests` folder next to `Assets` compiles the model with a small stand-in for what it uses of UnityEngine
(`Scripts/Model/Shim~`) and runs them in about a second. The test project imports BaseGame's `ModelTests.props`, which
brings the test packages and `Gamebox.Lockstep`, whose shuffle the dealer uses (the server module compiles that file
in too).

Everything the game shows is built by the editor code in `Assets/Editor` (menu **Memory Cards**):

- **Rebuild Art and Sound**: configures the imported Kenney files, draws the card fronts, card backs (one pattern per
  world), special cards, ice, icons, panels, particles and backdrop shapes with a small signed distance rasteriser
  (`Canvas2D`, `MemoryCardsArt`), synthesises the two music loops and a few effects (`SoundFactory`) and bakes Kenney
  Future into a TextMesh Pro font asset.
- **Rebuild Worlds and Levels**: the four worlds, the rules and goals of every level, the campaign, the free play
  settings and the launcher entry. The level data lives in `MemoryCardsContentBuilder`, so hand edits of those assets
  are overwritten by a rebuild.
- **Rebuild Scene**: the card prefab and `Scenes/MemoryCards.unity` with its interface, pool, particles and audio.
- **Build Everything** runs the three steps and removes the assets of the original version. The same methods run in
  batch mode, for example `-executeMethod Portfolio.MemoryCards.EditorTools.MemoryCardsBuildMenu.BuildEverything`.

The generators write to fixed paths, update existing assets in place (GUIDs and references are kept) and do not
rewrite files whose content did not change. Run them in one editor at a time: an editor that imports the files while
the other one writes them can hold a lock on a file and make the save fail.

## Multiplayer

A versus game is any board of the campaign or free play without what limits a single player: the clock, the hearts and
the move limit go (the turn timer and the other players take that place), and so do the parade and the shuffle, which
need one player's run of play; clock cards are dealt as peek cards (`VersusMatch.RulesFor`). The board, the sets,
triplets, memorize, ice, wild cards, bombs and peek cards stay. `VersusMatch` keeps the seats, their scores and the
clock of the turn; it is plain C# and covered by edit mode tests. A set lets the player go on with a full clock, a
mistake passes the turn once it turned back, a bomb passes it at once and takes a half finished set back with it, and
the standings go by score, then by sets found (equal on both is a draw).

- **At one device** the round of the manager judges every flip as it does for one player, the versus game books the
  result for the player whose turn it is, and the players pass the device around. A click during a mistake only turns
  the cards back, because the next card belongs to the next player. A turn lasts 20 seconds.
- **Online** the server deals the board and judges every flip with the rules of the game itself (the model is compiled
  into the server module, see "Server"), so nobody can look at a card they have not turned and the two sides cannot
  disagree. The host picks a level and the seconds a turn lasts in the lobby (the shared lobby of BaseGame); the host's
  client writes the board of the level into the options of the room (`BoardOptions`, through
  `MemoryCardsOnlineController.ComposeOptions`), because the server does not know the game's level assets. On every
  client the round only mirrors the board: a click asks the server for a flip (`FlipCard`), and what the server answers
  comes back as an event that is played with the same animations as a local flip; the flip that ended a turn is shown
  before the next turn is announced. Turns and the turn timer are the base server's: the clock stops while a mistake
  shows, and a peek costs no time. The server keeps the scores and gives the places by the rules of the game; it refuses
  scores reported by a client, actions for the log and an early end by the host, so a game plays to the last set. A
  player who leaves is skipped and charged a game not won, and a game with one player left is over. Online games do not
  pause (the pause button opens the match menu), are not saved, and the next game of a room starts from the lobby.

## Phones and tablets

The game runs on Android with the shared mobile code of BaseGame (see its README, "Phones and tablets"). Cards and
buttons take taps like clicks, and with touch the tips say "tap" instead of "click". The canvas expands from 1920 x 1080
to any screen shape; the board, the level select, the HUD and the results keep to a safe area, while the backdrop and
the dims cover the whole screen. The back button closes the settings, pauses and resumes a level, returns from the
results to the level select and leaves the level select for the launcher (or closes the app when the game is built on
its own). `Gamebox > Android > Build APK` builds `Build/Android/MemoryCards.apk` (`com.skinnerboxes.memorycards`, with
the game's icon).

## Art and sound

The animals, interface buttons, stars, font, card and interface sounds and the jingles are from
[Kenney](https://kenney.nl) (CC0): Animal Pack Remastered, UI Pack, Kenney Fonts, Casino Audio, Interface Sounds and
Music Jingles (see `Assets/Art/Kenney/License.txt`). The animals were rendered at 512 x 512 from the pack's vector
sheet. Everything else is generated by the editor code.

## Server

The game's [SpacetimeDB](https://spacetimedb.com) module is the folder `Server` next to `Assets` (Unity does not show
it; open `Server/StdbModule.csproj` in the IDE). It is the Gamebox base server of the BaseGame repository (`BaseServer`:
users and login, connecting and disconnecting, the player profile) plus `Server/Lib.cs`, which declares the same
`public static partial class Module` to add the tables and reducers of this game and implements the base server's
partial methods to react to its events. The rules are the game's own: `Server/StdbModule.csproj` compiles the model
(`Assets/Scripts/Model`, shown under `Model`) into the module together with `Model/Shim~/UnityEngine.cs`, the little
the model uses of UnityEngine, so there is one rule set for the client and the server. It holds the online versus game:

| In `Server/Lib.cs` | What |
| --- | --- |
| `CardDeck` (private) | The faces of a room's board. A client learns a face when the card turns. |
| `BoardCard` (public) | One row per card: state, ice, and the face only while the card is face up. |
| `MemoryBoard` (public) | The shape of the board, the sets found, the combo of the player whose turn it is. |
| `FlipEvent` (public event table) | What a flip did (revealed, a set, a mistake, a bomb, a peek, cards turning back, time up, the board shown for memorizing), with the faces it is about. Sent, never stored. |
| `MismatchTimer`, `PlayTimer` (scheduled) | Turn a mistake back after a moment; begin the first turn once the clients dealt the board. |
| `MemoryStats` (public) | Games, wins, sets and the best score of a player. |
| `FlipCard(index)` | The player whose turn it is flips a card: the round is rebuilt from the tables, `MemoryRound.Flip` and `VersusMatch.Judge` say what happened, and it goes back into the tables and out as a `FlipEvent`. |
| `ConfigureRoom`, `OnRoomStarted`, `OnTurnTimedOut`, `OnMemberLeft`, `OnRoomFinished`, `OnRoomCleared`, `OnUserDeleted` | The base server's extension points: check the board of a room (`BoardOptions`, `RoundRules.IsValid`), deal it (`Dealer`), take the cards back from a player who ran out of time or left (and charge the one who left a game not won), rank the players by score and sets and keep the stats, clean up. |
| `ValidateAction`, `ValidateScore`, `ValidateEndRoom` | Refused: the game has no action log, the server keeps the score, and a game plays to the last set. |

- `Packages/manifest.json` references the SpacetimeDB SDK and the base server package
  (`com.skinnerboxes.baseserver`, `file:../../../BaseGame/BaseServer`); `Server/StdbModule.csproj` imports
  `BaseServer.props` from there, which compiles the base server into this module.
- `spacetime.json` names the database (`skinnerboxes-memorycards`, on the `local` server) and where the client bindings go:
  `Assets/Scripts/Server/Bindings`, namespace `Portfolio.MemoryCards.Server`.
- After a change to `Server/Lib.cs` or the model: `Gamebox > Server > Publish Module` and `Generate Client Bindings` in the editor,
  or `spacetime publish` and `spacetime generate` in this folder. Generating also writes this game's
  `GameServerClient` (`Assets/Scripts/Server`), the component that connects, logs the player in and keeps the profiles,
  the rooms and the turns. The scene builder puts it into the scene together with the online controller and the lobby
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
  - `Assets/package.json` makes this project's `Assets` folder the package `com.skinnerboxes.memorycards`. BaseGame's
    manifest references it (`file:../../Portfolio/3 - MemoryCards/Assets`) and lists the game in its launcher.
- The same files therefore serve two projects and are mounted at `Assets/` here and at `Packages/com.skinnerboxes.memorycards/`
  in BaseGame, so nothing in them may depend on either location. The generators find their root through the package
  info of their assembly; the `GameDefinition` asset re-derives its scene path from its scene reference in the editor,
  and the launcher falls back to the scene name.
- The campaign uses the virtual unlock and star rules of the BaseGame `Campaign` and its `CampaignProgress` class.

## Notes

The storage strategy tests that shipped with this game live in BaseGame (Assets/Tests/Editor Tests of that project).
