# Portfolio - Monopoly

Monopoly for up to four players, any mix of people sharing the device and computer players, on a 3D board with a
modern Monopoly look: the World Tour edition, with streets from Lisbon to New York, travel themed Chance and Community
Chest cards, die cast style tokens and a wooden table. It plays the full official rules, and the modes add the
favourite variants: the Mega Edition speed die, the official short game, the Free Parking jackpot and more.

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
- `Scripts/View`: the board, the dice, the buildings and the camera. `Scripts/UI`: the panels and popups.
- `Tests/Editor`: edit mode tests of the rules engine and of computer players finishing games in every mode.
- `Editor`: the builders that make the game's content from code, under the **Monopoly** menu. Build Everything
  generates the textures (board, cards, dice, token badges, interface shapes), the meshes (tokens, houses, hotels,
  dice, board, table), the materials, the font assets, the synthesized music and jingles, the modes and the scene with
  its interface. Rebuild after changing a builder; the generated assets live in the project, so the game runs without
  rebuilding.

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
