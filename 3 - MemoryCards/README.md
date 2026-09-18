# Portfolio - Memory Cards

Memory Cards: flip cards and match the pairs before the timer runs out, with custom settings and save games.

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
| `PrefabPool<T>` | `FlippableCache, ItemLineCache` |

The `GameDefinition` asset under `Assets/Resources/Games` registers the game with the BaseGame launcher, and the
editor tooling of the package (Gamebox > Sync Game Scenes To Build Settings) keeps the game scene in the build
settings. This project has no main menu scene, so the Exit button of the menu reloads the game scene.

## Project setup

- Unity 6000.6.0f1 with URP 17.6.0. The render pipeline assets are the ones that ship in the BaseGame package.
- This project and BaseGame reference each other in place. Nothing is copied either way, so the two repositories
  have to sit next to each other (`BaseGame` beside `Portfolio`):
  - `Packages/manifest.json` references BaseGame as the local package `com.skinnerboxes.basegame`
    (`file:../../../BaseGame/Assets`), which provides the base classes, the shared menu and the URP assets.
  - `Assets/package.json` makes this project's `Assets` folder the package `com.skinnerboxes.memorycards`. BaseGame's
    manifest references it (`file:../../Portfolio/3 - MemoryCards/Assets`) and lists the game in its launcher.
- The same files therefore serve two projects and are mounted at `Assets/` here and at `Packages/com.skinnerboxes.memorycards/`
  in BaseGame, so nothing in them may depend on either location. The `GameDefinition` asset re-derives its scene
  path from its scene reference in the editor, and the launcher falls back to the scene name.

## Notes

The storage strategy tests that shipped with this game live in BaseGame (Assets/Tests/Editor Tests of that project).
