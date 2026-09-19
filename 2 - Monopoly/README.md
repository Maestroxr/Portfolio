# Portfolio - Monopoly

Monopoly: two players roll the dice, buy the properties they land on and fine each other until one goes bankrupt.

## Base game integration

The game is built on the base classes of the BaseGame package (`com.skinnerboxes.basegame`, assembly
`BaseGameAssembly`, namespace `Gamebox`):

| Base class / interface | Implementation |
| --- | --- |
| `BaseGameManager` | `MonopolyGameManager` |
| `PlayerBase` | `MonopolyPlayer` |
| `OfflineGameController` | `MonopolyController` |
| `GameUI` | `MonopolyUI` |
| `SettingsUI` | `MonopolySettingsUI` |
| `GameSettings` | `MonopolySettings` |
| `GameLevel` | `MonopolyLevel` |

The `GameDefinition` asset under `Assets/Resources/Games` registers the game with the BaseGame launcher (with its icon,
`Art/Icons/GameIcon.png`, the game's die on a red tile), and the editor tooling of the package (Gamebox > Sync Game
Scenes To Build Settings) keeps the game scene in the build settings. This project has no main menu scene, so the Exit
button of the shared menu closes a build of the game on its own; in the editor it reloads the game scene.

## Phones and tablets

The game runs on Android with the shared mobile code of BaseGame (see its README, "Phones and tablets"). The board is
laid out for 16:9, so the scene keeps it at that shape (`Board`, an `AspectRatioFitter` in the `MonopolyUI` canvas,
which expands from a 16:9 reference): wider phones get bars at the sides, 4:3 tablets above and below. The dialog is
half as big again as before, with a taller OK button, since one tap on it plays a turn. The shared menu brings the
pause button (shown while a game runs), finger sized buttons and settings rows. The back button pauses and resumes a
game and leaves the menu for the launcher (or closes the app when the game is built on its own).
`Gamebox > Android > Build APK` builds `Build/Android/Monopoly.apk` (`com.skinnerboxes.monopoly`, with the game's icon).

The scene has no builder: these changes were made to `Scenes/Monopoly.unity` directly.

## Project setup

- Unity 6000.6.0f1 with URP 17.6.0. The render pipeline assets are the ones that ship in the BaseGame package.
- This project and BaseGame reference each other in place. Nothing is copied either way, so the two repositories
  have to sit next to each other (`BaseGame` beside `Portfolio`):
  - `Packages/manifest.json` references BaseGame as the local package `com.skinnerboxes.basegame`
    (`file:../../../BaseGame/Assets`), which provides the base classes, the shared menu and the URP assets.
  - `Assets/package.json` makes this project's `Assets` folder the package `com.skinnerboxes.monopoly`. BaseGame's
    manifest references it (`file:../../Portfolio/2 - Monopoly/Assets`) and lists the game in its launcher.
- The same files therefore serve two projects and are mounted at `Assets/` here and at `Packages/com.skinnerboxes.monopoly/`
  in BaseGame, so nothing in them may depend on either location. The `GameDefinition` asset re-derives its scene
  path from its scene reference in the editor, and the launcher falls back to the scene name.

## Notes

The OK button of the dialog rolls the dice for the current player. iTween (Assets/Plugins) animates the tokens.
