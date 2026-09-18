# Portfolio - Asteroids

Asteroids: fly a spaceship around a wrapping playfield, shoot asteroids and collect the loot they drop.

## Base game integration

The game is built on the base classes of the BaseGame package (`com.skinnerboxes.basegame`, assembly
`BaseGameAssembly`, namespace `Gamebox`):

| Base class / interface | Implementation |
| --- | --- |
| `BaseGameManager` | `AsteroidsGameManager` |
| `PlayerBase` | `AsteroidsPlayer` |
| `OfflineGameController` | `AsteroidsController` |
| `GameUI` | `AsteroidsUI` |
| `SettingsUI` | `AsteroidsSettingsUI` |
| `GameSettings` | `AsteroidSettings` |
| `GameLevel` | `AsteroidsLevel` |
| `PrefabPool<T>` | `ShotPool, LootablePool, ExplodablePool` |

The `GameDefinition` asset under `Assets/Resources/Games` registers the game with the BaseGame launcher, and the
editor tooling of the package (Gamebox > Sync Game Scenes To Build Settings) keeps the game scene in the build
settings. This project has no main menu scene, so the Exit button of the shared menu reloads the game scene.

## Project setup

- Unity 6000.6.0f1 with URP 17.6.0. The render pipeline assets are the ones that ship in the BaseGame package.
- `Packages/manifest.json` references BaseGame as a local package (`file:../../../BaseGame/Assets`), so the
  BaseGame repository has to sit next to this one (`BaseGame` beside `Portfolio`).
- The same game is embedded in BaseGame as the package `com.skinnerboxes.asteroids`, and the two are mirrors:
  `Runtime/` there is `Assets/` here and `Tests/` there is `Assets/Tests/` here. Only the scene path in the
  `GameDefinition` asset and the test scene folder in `Assets/Tests/Runtime/PlayerTest.cs` differ.

## Notes

Escape pauses the game and opens the shared menu; F1/F4/F5 trigger start, save and load. Saving needs the PlayerPrefs storage strategy.
