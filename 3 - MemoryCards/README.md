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
| `Scripts/Flippable.cs`, `Scripts/View/*` | The card (flip, deal, shuffle, match, mistake, ice, explosion), board layout, UI particles, backdrop, audio |
| `Scripts/UI/*` | Level select (world tabs, level cards, details), HUD, results, pause menu, settings panel |
| `Scripts/MemoryCardsAutopilot.cs` | Plays a level by itself; add it to any object in play mode to test a level end to end |
| `Scripts/MemoryCardsTour.cs` | Development builds only: `-memorycards-tour <folder>` plays through the game and saves screenshots |

The rules engine knows nothing of Unity objects, so it is covered by edit mode tests (`Tests/Editor`, assembly
`Skinnerboxes.MemoryCards.Tests`) together with the dealer, the settings, the campaign unlocks and the progress.

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
partial methods (`OnUserCreated`, `OnUserConnected`, ...) to react to its events. For now it adds nothing.

- `Packages/manifest.json` references the SpacetimeDB SDK and the base server package
  (`com.skinnerboxes.baseserver`, `file:../../../BaseGame/BaseServer`); `Server/StdbModule.csproj` imports
  `BaseServer.props` from there, which compiles the base server into this module.
- `spacetime.json` names the database (`skinnerboxes-memorycards`, on the `local` server) and where the client bindings go:
  `Assets/Scripts/Server/Bindings`, namespace `Portfolio.MemoryCards.Server`.
- After a change to `Server/Lib.cs`: `Gamebox > Server > Publish Module` and `Generate Client Bindings` in the editor,
  or `spacetime publish` and `spacetime generate` in this folder. Generating also writes this game's
  `GameServerClient`, the component that connects, logs the player in and keeps the profiles; until the game has
  tables of its own, BaseGame's `Gamebox.Server.GameServerClient` does the same against this game's database.

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
