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
- `Packages/manifest.json` references BaseGame as a local package (`file:../../../BaseGame/Assets`), so the
  BaseGame repository has to sit next to this one (`BaseGame` beside `Portfolio`).
- The same game is embedded in BaseGame as the package `com.skinnerboxes.memorycards`, and the two are mirrors:
  `Runtime/` there is `Assets/` here. Only the scene path in the `GameDefinition` asset differs.

## Notes

The storage strategy tests that shipped with this game live in BaseGame (Assets/Tests/Editor Tests of that project).
