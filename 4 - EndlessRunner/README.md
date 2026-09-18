# Portfolio - Endless Runner

Endless Runner: run over looping terrain tiles, collect the coins and do not fall off the edge.

## Base game integration

The game is built on the base classes of the BaseGame package (`com.skinnerboxes.basegame`, assembly
`BaseGameAssembly`, namespace `Gamebox`):

| Base class / interface | Implementation |
| --- | --- |
| `BaseGameManager` | `RunnerGameManager` |
| `PlayerBase` | `RunnerPlayer` |
| `OfflineGameController` | `RunnerController` |
| `GameUI` | `RunnerUI` |
| `SettingsUI` | `RunnerSettingsUI` |
| `GameSettings` | `RunnerSettings` |
| `GameLevel` | `RunnerLevel` |
| `PrefabPool<T>` | `TerrainCache, CollidableCache` |

The `GameDefinition` asset under `Assets/Resources/Games` registers the game with the BaseGame launcher, and the
editor tooling of the package (Gamebox > Sync Game Scenes To Build Settings) keeps the game scene in the build
settings. This project has no main menu scene, so the Exit button of the shared menu reloads the game scene.

## Project setup

- Unity 6000.6.0f1 with URP 17.6.0. The render pipeline assets are the ones that ship in the BaseGame package.
- `Packages/manifest.json` references BaseGame as a local package (`file:../../../BaseGame/Assets`), so the
  BaseGame repository has to sit next to this one (`BaseGame` beside `Portfolio`).
- The same game is embedded in BaseGame as the package `com.skinnerboxes.endlessrunner`, and the two are mirrors:
  `Runtime/` there is `Assets/` here. Only the scene path in the `GameDefinition` asset differs.

## Notes

Saving is not supported by this game; the save and load buttons report that through the menu.
