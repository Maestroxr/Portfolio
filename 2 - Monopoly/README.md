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

The `GameDefinition` asset under `Assets/Resources/Games` registers the game with the BaseGame launcher, and the
editor tooling of the package (Gamebox > Sync Game Scenes To Build Settings) keeps the game scene in the build
settings. This project has no main menu scene, so the Exit button of the shared menu reloads the game scene.

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
